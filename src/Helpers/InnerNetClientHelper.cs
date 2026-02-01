using AmongUs.InnerNet.GameDataMessages;
using Hazel;
using InnerNet;
using System.Globalization;
using System.Text;

namespace BetterAmongUs.Helpers;

/// <summary>
/// Provides helper methods for working with InnerNet messaging, RPC handling, and message serialization.
/// </summary>
internal static class InnerNetClientHelper
{
    /// <summary>
    /// Broadcasts an RPC message to all clients with optional reliability.
    /// </summary>
    /// <param name="rpcMessage">The RPC message to broadcast.</param>
    /// <param name="reliable">Whether to use reliable or unreliable transmission.</param>
    internal static void BroadcastRpc(this BaseRpcMessage rpcMessage, bool reliable = true)
    {
        if (rpcMessage.TryCast<IGameDataMessage>(out var data))
        {
            if (reliable)
                AmongUsClient.Instance.reliableMessageQueue.Enqueue(data);
            else
                AmongUsClient.Instance.unreliableMessageQueue.Enqueue(data);
        }
    }

    /// <summary>
    /// Broadcasts a game data message to all clients using reliable transmission.
    /// </summary>
    /// <param name="rpcMessage">The game data message to broadcast.</param>
    internal static void BroadcastData(this BaseGameDataMessage rpcMessage)
    {
        if (rpcMessage.TryCast<IGameDataMessage>(out var data))
        {
            AmongUsClient.Instance.reliableMessageQueue.Enqueue(data);
        }
    }

    /// <summary>
    /// Determines whether the next string value in the specified reader matches the provided string without advancing
    /// the reader's position.
    /// </summary>
    /// <param name="reader">The message reader to inspect for the presence of the string. Cannot be null.</param>
    /// <param name="string">The string to compare against the next value in the reader. If null or empty, the method returns <see
    /// langword="true"/>.</param>
    /// <returns>true if the next string value in the reader matches the specified string; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="reader"/> is null.</exception>
    internal static bool HasString(this MessageReader reader, string @string)
    {
        if (reader == null) throw new ArgumentNullException(nameof(reader));
        if (string.IsNullOrEmpty(@string)) return true;

        // Save current position
        int originalPosition = reader.Position;
        int originalReadHead = reader.readHead;

        try
        {
            // Check if we have enough bytes for at least the packed int length
            if (reader.BytesRemaining < 1)
                return false;

            // Try to read the packed int length without modifying internal state
            int lengthOffset = reader.Position;
            int lengthReadHead = reader.readHead;

            // Read packed int manually without affecting the reader's state
            bool readMore = true;
            int shift = 0;
            uint stringLength = 0;

            while (readMore)
            {
                if (lengthOffset >= reader.Length)
                    return false; // Not enough data

                byte b = reader.Buffer[reader.Offset + lengthOffset];
                lengthOffset++;

                if (b >= 0x80)
                {
                    readMore = true;
                    b ^= 0x80;
                }
                else
                {
                    readMore = false;
                }

                stringLength |= (uint)(b << shift);
                shift += 7;

                // Sanity check: string length shouldn't be ridiculously large
                if (stringLength > reader.Buffer.Length || stringLength > 1024 * 1024) // 1MB max
                    return false;
            }

            // Check if we have enough bytes for the string content
            if (reader.Length - lengthOffset < (int)stringLength)
                return false;

            // Compare the string
            if (stringLength != @string.Length)
                return false;

            byte[] stringBytes = Encoding.UTF8.GetBytes(@string);

            // Compare byte by byte
            for (int i = 0; i < stringLength; i++)
            {
                if (reader.Buffer[reader.Offset + lengthOffset + i] != stringBytes[i])
                    return false;
            }

            return true;
        }
        catch
        {
            // If anything goes wrong, assume the string is not present
            return false;
        }
        finally
        {
            // Restore original position
            reader._position = originalPosition;
            reader.readHead = originalReadHead;
        }
    }

    /// <summary>
    /// Writes an array of boolean values to a MessageWriter in a packed format to save space.
    /// Each byte stores up to 8 boolean values as bits.
    /// </summary>
    /// <param name="writer">The MessageWriter to write to.</param>
    /// <param name="boolsEnumerable">The boolean values to write.</param>
    internal static void WriteBooleans(this MessageWriter writer, IEnumerable<bool> boolsEnumerable)
    {
        bool[] bools = [.. boolsEnumerable];

        writer.Write(bools.Length);

        byte currentByte = 0;
        int bitIndex = 0;

        foreach (bool b in bools)
        {
            if (b) currentByte |= (byte)(1 << bitIndex);

            bitIndex++;

            if (bitIndex == 8)
            {
                writer.Write(currentByte);
                currentByte = 0;
                bitIndex = 0;
            }
        }

        if (bitIndex > 0) writer.Write(currentByte);
    }

    /// <summary>
    /// Reads an array of boolean values from a MessageReader that were previously packed using WriteBooleans.
    /// </summary>
    /// <param name="reader">The MessageReader to read from.</param>
    /// <returns>An array of boolean values.</returns>
    internal static bool[] ReadBooleans(this MessageReader reader)
    {
        int length = reader.ReadInt32();
        bool[] bools = new bool[length];

        int bitIndex = 0;
        byte currentByte = 0;

        for (int i = 0; i < length; i++)
        {

            if (bitIndex == 0) currentByte = reader.ReadByte();

            bools[i] = (currentByte & 1 << bitIndex) != 0;

            bitIndex++;

            if (bitIndex == 8) bitIndex = 0;
        }

        return bools;
    }

    /// <summary>
    /// Writes an array of bytes to a MessageWriter in a packed format, combining two bytes into one to save space.
    /// </summary>
    /// <param name="writer">The MessageWriter to write to.</param>
    /// <param name="bytesEnumerable">The byte values to write.</param>
    internal static void WriteBytes(this MessageWriter writer, IEnumerable<byte> bytesEnumerable)
    {
        byte[] bytes = bytesEnumerable.ToArray();

        writer.Write(bytes.Length);
        writer.Write(bytes);
    }

    /// <summary>
    /// Reads an array of bytes from a MessageReader that were previously packed using WritePackedBytes.
    /// </summary>
    /// <param name="reader">The MessageReader to read from.</param>
    /// <returns>An array of bytes.</returns>
    internal static byte[] ReadBytes(this MessageReader reader)
    {
        int count = reader.ReadInt32();
        var bytes = reader.ReadBytes(count);
        return [.. bytes];
    }

    /// <summary>
    /// Writes an array of bytes to a MessageWriter in a packed format, combining two bytes into one to save space.
    /// </summary>
    /// <param name="writer">The MessageWriter to write to.</param>
    /// <param name="bytesEnumerable">The byte values to write.</param>
    internal static void WritePackedBytes(this MessageWriter writer, IEnumerable<byte> bytesEnumerable)
    {
        byte[] bytes = bytesEnumerable.ToArray();

        writer.Write((byte)bytes.Length);

        for (int i = 0; i < bytes.Length; i += 2)
        {
            byte packedBytes;
            if (i + 1 < bytes.Length) packedBytes = (byte)((bytes[i] & 0x0F) << 4 | bytes[i + 1] & 0x0F);
            else packedBytes = (byte)((bytes[i] & 0x0F) << 4);
            writer.Write(packedBytes);
        }
    }

    /// <summary>
    /// Reads an array of bytes from a MessageReader that were previously packed using WritePackedBytes.
    /// </summary>
    /// <param name="reader">The MessageReader to read from.</param>
    /// <returns>An array of bytes.</returns>
    internal static byte[] ReadPackedBytes(this MessageReader reader)
    {
        int count = reader.ReadByte();
        List<byte> bytes = [];

        for (int i = 0; i < (count + 1) / 2; i++)
        {
            byte packedBytes = reader.ReadByte();
            bytes.Add((byte)(packedBytes >> 4 & 0x0F));

            if (bytes.Count < count) bytes.Add((byte)(packedBytes & 0x0F));
        }

        return bytes.ToArray();
    }

    /// <summary>
    /// Writes a floating-point value to a MessageWriter in a packed format to save space.
    /// Handles negative values, integers, and decimal numbers efficiently.
    /// </summary>
    /// <param name="writer">The MessageWriter to write to.</param>
    /// <param name="value">The float value to write.</param>
    internal static void WritePacked(this MessageWriter writer, float value)
    {
        bool isNegative = value < 0;
        float absValue = Math.Abs(value);

        // Check if it's a solid number (integer)
        bool isSolidNumber = absValue == (uint)absValue && absValue <= uint.MaxValue;

        // Track the number of decimal places dynamically
        int decimalPlaces = 0;
        int scaleFactor = 1;
        bool needsFallback = false; // Flag to determine if we need to send the value as a regular float

        // Check if the value is not a solid number
        if (!isSolidNumber)
        {
            // Convert to string to count decimals (avoiding floating point precision issues)
            string absValueStr = absValue.ToString("G", CultureInfo.InvariantCulture);
            if (absValueStr.Contains('.'))
            {
                decimalPlaces = absValueStr.Split('.')[1].Length; // Count decimals after the period
                scaleFactor = (int)Math.Pow(10, decimalPlaces); // Set scale factor dynamically
            }
        }

        // If the value can't be packed efficiently, we need a fallback
        if (!isSolidNumber || absValue * scaleFactor > uint.MaxValue)
        {
            needsFallback = true; // Flag for fallback as a regular float
        }

        // Pack the flags into a list of booleans
        var flags = new List<bool>
        {
            isNegative,
            isSolidNumber,
            decimalPlaces > 0,
            needsFallback
        };

        // Write the flags using WriteBooleans
        writer.WriteBooleans(flags);

        // Write the decimal places as a byte (0-3 for simplicity)
        writer.Write((byte)decimalPlaces);

        // If it's a solid number, pack as uint
        if (isSolidNumber && !needsFallback)
        {
            writer.Write((uint)absValue);
        }

        // If it's a decimal number, scale and pack it as uint (but check if it can fit)
        else if (decimalPlaces > 0 && !needsFallback)
        {
            // Check if the scaled value fits into a uint
            if (absValue * scaleFactor <= uint.MaxValue)
            {
                uint scaledValue = (uint)(absValue * scaleFactor); // Dynamically scale the value
                writer.Write(scaledValue);
            }
            else
            {
                // If it can't fit into a uint, send as regular float
                writer.Write(value);
            }
        }
        else
        {
            // Send as regular float if it requires fallback
            writer.Write(value);
        }
    }

    /// <summary>
    /// Reads a floating-point value from a MessageReader that was previously packed using WritePacked.
    /// Handles negative values, integers, and decimal numbers efficiently.
    /// </summary>
    /// <param name="reader">The MessageReader to read from.</param>
    /// <returns>The unpacked float value.</returns>
    internal static float ReadPackedSingle(this MessageReader reader)
    {
        var flags = reader.ReadBooleans();
        bool isNegative = flags[0];
        bool isSolidNumber = flags[1];
        bool hasDecimalPart = flags[2];
        bool needsFallback = flags[3];

        byte decimalPlaces = reader.ReadByte(); // Read the decimal places count

        // If the value was packed as a regular float, just read it (don't reapply the sign)
        if (needsFallback)
        {
            return reader.ReadSingle(); // The sign is already handled in the packed value
        }

        // Handle solid number (uint) case
        if (isSolidNumber)
        {
            uint packedValue = reader.ReadUInt32();
            return isNegative ? -(float)packedValue : packedValue;
        }
        // Handle decimal number (scaled uint)
        else if (hasDecimalPart)
        {
            uint scaledValue = reader.ReadUInt32();
            float scaleFactor = (float)Math.Pow(10, decimalPlaces); // Use the stored decimal places count
            return isNegative ? -(scaledValue / scaleFactor) : scaledValue / scaleFactor;
        }
        else
        {
            // For other cases where it's a normal float
            float value = reader.ReadSingle();
            return isNegative ? -value : value;
        }
    }

    /// <summary>
    /// Writes a player's ID to a MessageWriter, using 255 for null players.
    /// </summary>
    /// <param name="writer">The MessageWriter to write to.</param>
    /// <param name="player">The player whose ID to write.</param>
    internal static void WritePlayerId(this MessageWriter writer, PlayerControl player) => writer.Write(player?.PlayerId ?? 255);

    /// <summary>
    /// Reads a player ID from a MessageReader and returns the corresponding PlayerControl.
    /// </summary>
    /// <param name="reader">The MessageReader to read from.</param>
    /// <returns>The PlayerControl or null if not found.</returns>
    internal static PlayerControl? ReadPlayerId(this MessageReader reader) => Utils.PlayerFromPlayerId(reader.ReadByte());

    /// <summary>
    /// Writes a NetworkedPlayerInfo's player ID to a MessageWriter, using 255 for null data.
    /// </summary>
    /// <param name="writer">The MessageWriter to write to.</param>
    /// <param name="data">The NetworkedPlayerInfo whose ID to write.</param>
    internal static void WritePlayerDataId(this MessageWriter writer, NetworkedPlayerInfo data) => writer.Write(data?.PlayerId ?? 255);

    /// <summary>
    /// Reads a player ID from a MessageReader and returns the corresponding NetworkedPlayerInfo.
    /// </summary>
    /// <param name="reader">The MessageReader to read from.</param>
    /// <returns>The NetworkedPlayerInfo or null if not found.</returns>
    internal static NetworkedPlayerInfo? ReadPlayerDataId(this MessageReader reader) => Utils.PlayerDataFromPlayerId(reader.ReadByte());

    /// <summary>
    /// Writes a DeadBody's parent ID to a MessageWriter, using 255 for null bodies.
    /// </summary>
    /// <param name="writer">The MessageWriter to write to.</param>
    /// <param name="body">The DeadBody whose parent ID to write.</param>
    internal static void WriteDeadBodyId(this MessageWriter writer, DeadBody body) => writer.Write(body?.ParentId ?? 255);

    /// <summary>
    /// Reads a DeadBody ID from a MessageReader and returns the corresponding DeadBody.
    /// </summary>
    /// <param name="reader">The MessageReader to read from.</param>
    /// <returns>The DeadBody or null if not found.</returns>
    internal static DeadBody? ReadDeadBodyId(this MessageReader reader) => BAUPlugin.AllDeadBodys.FirstOrDefault(deadbody => deadbody.ParentId == reader.ReadByte());

    /// <summary>
    /// Writes a Vent's ID to a MessageWriter, using -1 for null vents.
    /// </summary>
    /// <param name="writer">The MessageWriter to write to.</param>
    /// <param name="vent">The Vent whose ID to write.</param>
    internal static void WriteVentId(this MessageWriter writer, Vent vent) => writer.Write(vent?.Id ?? -1);

    /// <summary>
    /// Reads a Vent ID from a MessageReader and returns the corresponding Vent.
    /// </summary>
    /// <param name="reader">The MessageReader to read from.</param>
    /// <returns>The Vent or null if not found.</returns>
    internal static Vent? ReadVentId(this MessageReader reader) => BAUPlugin.AllVents.FirstOrDefault(vent => vent.Id == reader.ReadInt32());

    /// <summary>
    /// Converts a MessageWriter to a MessageReader.
    /// </summary>
    /// <param name="writer">The MessageWriter to convert.</param>
    /// <returns>A MessageReader containing the writer's data.</returns>
    internal static MessageReader ToReader(this MessageWriter writer) => MessageReader.Get(writer.ToByteArray(false));

    /// <summary>
    /// Converts a MessageWriter into multiple MessageReaders for each contained message.
    /// </summary>
    /// <param name="writer">The MessageWriter to convert.</param>
    /// <returns>An array of MessageReaders.</returns>
    internal static MessageReader[] ToReaders(this MessageWriter writer)
    {
        var reader = writer.ToReader();
        List<MessageReader> readers = [];

        while (reader.Position < reader.Length)
        {
            readers.Add(reader.ReadMessage());
        }

        return [.. readers];
    }

    /// <summary>
    /// Converts a MessageReader into multiple MessageReaders for each contained message.
    /// </summary>
    /// <param name="reader">The MessageReader to convert.</param>
    /// <returns>An array of MessageReaders.</returns>
    internal static MessageReader[] ToReaders(this MessageReader reader)
    {
        List<MessageReader> readers = [];

        while (reader.Position < reader.Length)
        {
            readers.Add(reader.ReadMessage());
        }

        return [.. readers];
    }

    /// <summary>
    /// Converts a MessageReader into multiple MessageReaders with new buffers for each message.
    /// </summary>
    /// <param name="reader">The MessageReader to convert.</param>
    /// <returns>An array of MessageReaders with new buffers.</returns>
    internal static MessageReader[] ToReadersNewBuffer(this MessageReader reader)
    {
        List<MessageReader> readers = [];

        while (reader.Position < reader.Length)
        {
            readers.Add(reader.ReadMessageAsNewBuffer());
        }

        return [.. readers];
    }

    /// <summary>
    /// Writes multiple MessageReaders to a MessageWriter.
    /// </summary>
    /// <param name="writer">The MessageWriter to write to.</param>
    /// <param name="readers">The MessageReaders to write.</param>
    /// <param name="clear">Whether to clear the writer before writing.</param>
    /// <returns>The MessageWriter for method chaining.</returns>
    internal static MessageWriter WriteReaders(this MessageWriter writer, IEnumerable<MessageReader> readers, bool clear = false)
    {
        if (clear) writer.Clear(writer.SendOption);

        foreach (MessageReader reader in readers)
        {
            writer.StartMessage(reader.Tag);
            writer.Write(reader.ReadBytes(reader.Length));
            writer.EndMessage();
        }

        return writer;
    }

    /// <summary>
    /// Creates a copy of a MessageWriter.
    /// </summary>
    /// <param name="writer">The MessageWriter to copy.</param>
    /// <returns>A new MessageWriter with the same content.</returns>
    internal static MessageWriter Copy(this MessageWriter writer)
    {
        var newWriter = MessageWriter.Get(writer.SendOption);
        newWriter.Write(writer.ToByteArray(false));
        return newWriter;
    }

    /// <summary>
    /// Starts the RPC desynchronization process for the given player, call ID, and send option.
    /// </summary>
    /// <param name="client">The InnerNetClient instance.</param>
    /// <param name="playerNetId">The network ID of the player.</param>
    /// <param name="callId">The RPC call ID.</param>
    /// <param name="option">The send option for the RPC.</param>
    /// <param name="ignoreClientId">The client ID to ignore. Default is -1, which means no client is ignored.</param>
    /// <param name="clientCheck">Optional function to filter which clients receive the RPC.</param>
    /// <returns>A list of MessageWriter instances for the RPC calls.</returns>
    /// <example>
    /// <code>
    /// List&lt;MessageWriter&gt; messageWriter = AmongUsClient.Instance.StartRpcDesync(PlayerNetId, (byte)RpcCalls, SendOption, ClientId);
    /// messageWriter.ForEach(mW => mW.Write("RPC TEST"));
    /// AmongUsClient.Instance.FinishRpcDesync(messageWriter);
    /// </code>
    /// </example>
    internal static List<MessageWriter> StartRpcDesync(this InnerNetClient client, uint playerNetId, byte callId, SendOption option, int ignoreClientId = -1, Func<ClientData, bool>? clientCheck = null)
    {
        List<MessageWriter> messageWriters = [];

        if (ignoreClientId < 0)
        {
            messageWriters.Add(client.StartRpcImmediately(playerNetId, callId, option, -1));
        }
        else
        {
            foreach (var allClients in AmongUsClient.Instance.allClients.WhereIl2Cpp(c => c.Id != ignoreClientId))
            {
                if (clientCheck == null || clientCheck.Invoke(allClients))
                {
                    messageWriters.Add(client.StartRpcImmediately(playerNetId, callId, option, allClients.Id));
                }
            }
        }

        return messageWriters;
    }

    /// <summary>
    /// Completes and sends the RPC desynchronization messages.
    /// </summary>
    /// <param name="client">The InnerNetClient instance.</param>
    /// <param name="messageWriters">The list of MessageWriters to finish and send.</param>
    internal static void FinishRpcDesync(this InnerNetClient client, List<MessageWriter> messageWriters)
    {
        foreach (var msg in messageWriters)
        {
            msg.EndMessage();
            msg.EndMessage();
            client.SendOrDisconnect(msg);
            msg.Recycle();
        }
    }
}