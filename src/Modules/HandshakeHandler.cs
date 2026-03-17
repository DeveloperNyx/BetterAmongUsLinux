using BepInEx.Unity.IL2CPP.Utils;
using BetterAmongUs.Data;
using BetterAmongUs.Enums;
using BetterAmongUs.Helpers;
using BetterAmongUs.Mono;
using BetterAmongUs.Network;
using Hazel;
using Il2CppInterop.Runtime.Attributes;
using System.Collections;
using UnityEngine;

namespace BetterAmongUs.Modules;

internal sealed class HandshakeHandler
{
    [HideFromIl2Cpp]
    internal HandshakeHandler(ExtendedPlayerInfo extendedData)
    {
        this.extendedData = extendedData;
    }

    [HideFromIl2Cpp]
    private ExtendedPlayerInfo extendedData { get; }

    internal void WaitSendSecretToPlayer()
    {
        extendedData.StartCoroutine(CoWaitSendSecretToPlayer());
    }

    private IEnumerator CoWaitSendSecretToPlayer()
    {
        if (!BAUPlugin.SendBetterRpc.Value) yield break;

        while (extendedData._Data?.Object == null || PlayerControl.LocalPlayer == null)
        {
            if (GameState.IsFreePlay) yield break;
            yield return null;
        }

        yield return new WaitForSeconds(1f);
        SendSecretToPlayer();
    }

    internal void ResendSecretToPlayer()
    {
        if (!BAUPlugin.SendBetterRpc.Value) return;
        if (HasSendSharedSecret && extendedData.IsVerifiedBetterUser) return;

        HasSendSharedSecret = false;
        SendSecretToPlayer();
    }

    private void SendSecretToPlayer()
    {
        if (extendedData._Data.Object.IsLocalPlayer()) return;
        if (HasSendSharedSecret) return;

        HasSendSharedSecret = true;

        RPC.SendCustomRpcPacked(CustomRPC.SendSecretToPlayer, writer =>
        {
            writer.Write(SharedSecret.CryptoAvailable);
            writer.WriteBytes(SharedSecret.GetPublicKey());
            writer.Write(SharedSecret.GetTempKey());
        }, extendedData._Data.ClientId);
    }

    internal void HandleSecretFromSender(MessageReader reader)
    {
        if (extendedData._Data?.Object?.IsLocalPlayer() == true) return;

        bool senderSupportsCrypto = reader.ReadBoolean();
        byte[] sendersPublicKey = reader.ReadBytes();
        int tempKey = reader.ReadInt32();

        SharedSecret.UseFallback = !senderSupportsCrypto;
        SharedSecret.SetRemoteTempKey(tempKey);

        byte[] secret = SharedSecret.GenerateSharedSecret(sendersPublicKey);
        if (secret.Length == 0) return;

        extendedData.IsBetterUser = true;

        TryHandlePendingVerificationData();
        SendSecretHashToSender(tempKey, extendedData._Data.ClientId);
        ResendSecretToPlayer();
    }

    private void SendSecretHashToSender(int tempKey, int senderClientId)
    {
        if (!BAUPlugin.SendBetterRpc.Value) return;

        int hash = SharedSecret.GetSharedSecretHash();

        RPC.SendCustomRpcPacked(CustomRPC.CheckSecretHashFromPlayer, writer =>
        {
            writer.Write(tempKey);
            writer.Write(hash);
        }, senderClientId);
    }

    internal void HandleSecretHashFromPlayer(MessageReader reader)
    {
        int tempKey = reader.ReadInt32();
        int receivedHash = reader.ReadInt32();

        _pendingVerificationData = (tempKey, receivedHash);
        TryHandlePendingVerificationData();
    }

    internal void TryHandlePendingVerificationData()
    {
        if (!_pendingVerificationData.HasValue) return;
        if (SharedSecret.GetSharedSecret().Length == 0) return;

        var data = _pendingVerificationData.Value;

        if (data.tempKey != SharedSecret.GetTempKey())
            return;

        extendedData.IsBetterUser = true;

        if (data.receivedHash == SharedSecret.GetSharedSecretHash())
        {
            extendedData.IsVerifiedBetterUser = true;
        }

        _pendingVerificationData = null;
    }

    private (int tempKey, int receivedHash)? _pendingVerificationData = null;
    private bool HasSendSharedSecret { get; set; }

    internal SharedSecretExchange SharedSecret { get; set; } = new();
}