using AmongUs.Data;
using BetterAmongUs.Enums;
using BetterAmongUs.Helpers;
using BetterAmongUs.Mono;
using Hazel;

namespace BetterAmongUs.Network;

/// <summary>
/// Handles custom RPC (Remote Procedure Call) messages for BetterAmongUs.
/// This class provides methods for sending and receiving custom RPC messages
/// packed within vanilla RPC calls to maintain compatibility with vanilla servers.
/// </summary>
internal static class RPC
{
    /// <summary>
    /// The flag string used to identify custom RPC messages packed within vanilla RPC calls.
    /// </summary>
    internal const string CUSTOM_RPC_FLAG = "bau:rpc";

    /// <summary>
    /// Sends a custom RPC message packed within a vanilla SetNamePlateStr RPC call.
    /// This method is used to maintain compatibility with vanilla Among Us servers
    /// while allowing custom RPC communication.
    /// </summary>
    /// <param name="customRPC">The custom RPC type to send.</param>
    /// <param name="action">A delegate that writes the custom RPC payload to the message writer.</param>
    /// <param name="targetClientId">The specific client ID to target, or -1 to broadcast to all clients.</param>
    internal static void SendCustomRpc(CustomRPC customRPC, Action<MessageWriter> action, int targetClientId = -1)
    {
        var writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)RpcCalls.SetNamePlateStr, SendOption.Reliable, targetClientId);

        writer.Write(DataManager.Player.Customization.NamePlate);
        writer.Write(PlayerControl.LocalPlayer.GetNextRpcSequenceId(RpcCalls.SetNamePlateStr));

        writer.Write(CUSTOM_RPC_FLAG); // Flag to check if its a rpc packed into SetNamePlateStr
        writer.Write((byte)customRPC);
        action(writer);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    /// <summary>
    /// Handles incoming custom RPC messages by extracting and processing them
    /// from the packed SetNamePlateStr RPC call.
    /// </summary>
    /// <param name="player">The player who sent the RPC message.</param>
    /// <param name="oldReader">The message reader containing the RPC data.</param>
    internal static void HandleCustomRPC(PlayerControl player, MessageReader oldReader)
    {
        if (player == null || player.IsLocalPlayer() || player.Data == null) return;

        MessageReader reader = MessageReader.Get(oldReader);

        _ = reader.ReadString();
        _ = reader.ReadByte();

        if (IsPackedCustomRpc(reader))
        {
            CustomRPC customRPC = (CustomRPC)reader.ReadByte();
            switch (customRPC)
            {
                case CustomRPC.SendSecretToPlayer:
                    {
                        player.BetterData().HandshakeHandler.HandleSecretFromSender(reader);
                    }
                    break;
                case CustomRPC.CheckSecretHashFromPlayer:
                    {
                        player.BetterData().HandshakeHandler.HandleSecretHashFromPlayer(reader);
                    }
                    break;
            }
        }

        reader.Recycle();
    }

    /// <summary>
    /// Determines whether a MessageReader contains a packed custom RPC message.
    /// </summary>
    /// <param name="reader">The MessageReader to check for custom RPC content.</param>
    /// <returns>
    /// <c>true</c> if the reader contains a custom RPC flag and custom RPC data;
    /// otherwise, <c>false</c>.
    /// </returns>
    internal static bool IsPackedCustomRpc(MessageReader reader)
    {
        if (reader.BytesRemaining > 0)
        {
            try
            {
                if (reader.ReadString() == CUSTOM_RPC_FLAG)
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        return false;
    }
}