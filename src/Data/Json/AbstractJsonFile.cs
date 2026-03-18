using BetterAmongUs.Helpers;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BetterAmongUs.Data.Json;

/// <summary>
/// Abstract base class for JSON file handling with serialization and deserialization capabilities.
/// </summary>
internal abstract class AbstractJsonFile
{
    /// <summary>
    /// Gets the file path where the JSON data will be stored.
    /// </summary>
    internal abstract string FilePath { get; }
    private bool _hasInit;

    /// <summary>
    /// Gets the JSON serializer options used for serialization and deserialization.
    /// </summary>
    protected virtual JsonSerializerOptions SerializerOptions { get; } = new()
    {
        WriteIndented = true,
        IncludeFields = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Initializes the JSON file, creating it if it doesn't exist or loading existing data.
    /// </summary>
    internal virtual void Init()
    {
        if (_hasInit)
            return;

        _hasInit = true;

        if (!IsFileValid())
        {
            Save();
            return;
        }

        Load();
        Save();
    }

    /// <summary>
    /// Loads data from the JSON file into the current instance.
    /// </summary>
    /// <returns>True if loading was successful, false otherwise.</returns>
    protected virtual bool Load()
    {
        try
        {
            var content = TryReadFromFile();
            if (string.IsNullOrEmpty(content.Trim()))
            {
                return false;
            }

            var data = JsonSerializer.Deserialize(content, GetType(), SerializerOptions);
            if (data == null)
            {
                Logger_.Error("Deserialization returned null");
                return false;
            }

            foreach (var property in GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.GetCustomAttribute<JsonPropertyNameAttribute>() == null) continue;
                if (property.CanWrite)
                {
                    var value = property.GetValue(data);
                    property.SetValue(this, value);
                }
            }

            return true;
        }
        catch (JsonException ex)
        {
            Logger_.Error($"JSON parsing error: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Logger_.Error($"Unexpected error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Attempts to read content from the file, returning an empty string on failure.
    /// </summary>
    /// <returns>The file content or an empty string if reading fails.</returns>
    private string TryReadFromFile()
    {
        try
        {
            return ReadFromFile();
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Saves the current instance's data to the JSON file.
    /// </summary>
    /// <returns>True if saving was successful, false otherwise.</returns>
    internal virtual bool Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(this, GetType(), SerializerOptions);
            WriteToFile(json);
        }
        catch (Exception ex)
        {
            Logger_.Error(ex);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Determines whether the file specified by the current FilePath exists and contains valid, non-empty JSON content.
    /// </summary>
    /// <returns>true if the file exists, is not empty, and contains valid, non-empty JSON content; otherwise, false.</returns>
    private bool IsFileValid()
    {
        var directory = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (!File.Exists(FilePath))
        {
            return false;
        }

        var content = TryReadFromFile();
        if (string.IsNullOrEmpty(content.Trim())) return false;

        try
        {
            var jsonElement = JsonSerializer.Deserialize<JsonElement>(content);

            var fileInfo = new FileInfo(FilePath);
            if (fileInfo.Length == 0)
            {
                return false;
            }

            if (jsonElement.ValueKind == JsonValueKind.Object && !jsonElement.EnumerateObject().Any() ||
                jsonElement.ValueKind == JsonValueKind.Array && !jsonElement.EnumerateArray().Any())
            {
                return false;
            }
        }
        catch (Exception)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Reads the content from the JSON file.
    /// </summary>
    /// <returns>The content of the JSON file.</returns>
    protected virtual string ReadFromFile()
    {
        return File.ReadAllText(FilePath);
    }

    /// <summary>
    /// Writes the JSON string to the file.
    /// </summary>
    /// <param name="json">The JSON string to write.</param>
    protected virtual void WriteToFile(string json)
    {
        File.WriteAllText(FilePath, json);
    }
}