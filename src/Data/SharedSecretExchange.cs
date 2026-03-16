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

    internal SharedSecretExchange()
    {
        try
        {
            dh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
            publicKey = dh.ExportSubjectPublicKeyInfo();
        }
        catch (Exception ex)
        {
            Logger_.Error("ECDH unavailable, disabling secure handshake: " + ex.Message);
            cryptoDisabled = true;
            dh = null;
            publicKey = [];
        }

        var random = new Random();
        tempKey = random.Next();
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
        if (cryptoDisabled) return [];
        return sharedSecret;
    }

    internal byte[] GenerateSharedSecret(byte[] otherPartyPublicKey)
    {
        if (cryptoDisabled) return [];
        if (sharedSecret.Length > 0) return sharedSecret;

        try
        {
            using ECDiffieHellman otherPartyDH = ECDiffieHellman.Create();
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
        if (cryptoDisabled) return 0;
        if (sharedSecret.Length == 0) return 0;

        using SHA256 sha256 = SHA256.Create();
        byte[] hashBytes = sha256.ComputeHash(sharedSecret);
        int numericHash = BitConverter.ToInt32(hashBytes, 0);
        return Math.Abs(numericHash);
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