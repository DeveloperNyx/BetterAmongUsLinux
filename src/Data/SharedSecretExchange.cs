using BetterAmongUs.Helpers;
using System.Security.Cryptography;

namespace BetterAmongUs.Data;

internal sealed class SharedSecretExchange
{
    private readonly ECDiffieHellman dh;
    private byte[] publicKey;
    private int tempKey;
    private byte[] sharedSecret = [];
    private bool cryptoDisabled;

    internal bool CryptoAvailable => !cryptoDisabled;
    internal bool UseFallback { get; set; }

    private int? remoteTempKey;

    internal SharedSecretExchange()
    {
        try
        {
            dh = ECDiffieHellman.Create();
            dh.GenerateKey(ECCurve.NamedCurves.nistP256);
            publicKey = dh.ExportSubjectPublicKeyInfo();
        }
        catch (Exception ex)
        {
            Logger_.Error("ECDH unavailable, using fallback handshake: " + ex.Message);
            cryptoDisabled = true;
            dh = null;
            publicKey = [];
        }

        tempKey = GenerateSecureTempKey();
    }

    private static int GenerateSecureTempKey()
    {
        byte[] buffer = new byte[4];
        RandomNumberGenerator.Fill(buffer);
        return Math.Abs(BitConverter.ToInt32(buffer, 0));
    }

    internal void SetRemoteTempKey(int key)
    {
        remoteTempKey = key;
    }

    internal byte[] GetPublicKey()
    {
        if (cryptoDisabled) return [];
        return publicKey;
    }

    internal int GetTempKey()
    {
        return tempKey;
    }

    internal byte[] GetSharedSecret()
    {
        return sharedSecret;
    }

    internal byte[] GenerateSharedSecret(byte[] otherPartyPublicKey)
    {
        if (sharedSecret.Length > 0) return sharedSecret;

        if (cryptoDisabled || UseFallback)
        {
            if (!remoteTempKey.HasValue)
                return [];

            sharedSecret = DeriveFallbackSecret(tempKey, remoteTempKey.Value);
            return sharedSecret;
        }

        try
        {
            using var otherPartyDH = ECDiffieHellman.Create();

            if (otherPartyPublicKey == null || otherPartyPublicKey.Length == 0)
                return [];

            otherPartyDH.ImportSubjectPublicKeyInfo(otherPartyPublicKey, out _);
            sharedSecret = dh.DeriveKeyMaterial(otherPartyDH.PublicKey);

            dh.Dispose();
            return sharedSecret;
        }
        catch (Exception ex)
        {
            Logger_.Error("Error generating shared secret: " + ex.Message);
            return [];
        }
    }

    internal int GetSharedSecretHash()
    {
        if (sharedSecret.Length == 0) return 0;

        using SHA256 sha256 = SHA256.Create();
        byte[] hashBytes = sha256.ComputeHash(sharedSecret);

        int numericHash = (hashBytes[0] << 24) |
                          (hashBytes[1] << 16) |
                          (hashBytes[2] << 8) |
                          hashBytes[3];

        return Math.Abs(numericHash);
    }

    private static byte[] DeriveFallbackSecret(int local, int remote)
    {
        using SHA256 sha256 = SHA256.Create();

        int a = local < remote ? local : remote;
        int b = local < remote ? remote : local;

        byte[] buffer = new byte[8];

        Buffer.BlockCopy(BitConverter.GetBytes(a), 0, buffer, 0, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(b), 0, buffer, 4, 4);

        return sha256.ComputeHash(buffer);
    }

    internal bool HasBeenCleared { get; private set; }

    internal void ClearData()
    {
        if (HasBeenCleared) return;
        HasBeenCleared = true;
        publicKey = [];
        tempKey = 0;
        sharedSecret = [];
    }
}