using AmongUs.InnerNet.GameDataMessages;
using Hazel;
using InnerNet;

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