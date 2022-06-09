// CypherNetwork by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;

namespace Miner.Models;

/// <summary>
/// 
/// </summary>
[MessagePack.MessagePackObject]
public class KeyPair : IDisposable
{
    [MessagePack.Key(0)] public byte[] PrivateKey { get; }
    [MessagePack.Key(1)] public byte[] PublicKey { get; }

    public KeyPair(byte[] privateKey, byte[] publicKey)
    {
        if (privateKey.Length % 16 != 0)
            throw new ArgumentOutOfRangeException(
                $"{nameof(privateKey)} Private Key length must be a multiple of 16 bytes.");
        PrivateKey = privateKey;
        PublicKey = publicKey;
    }

    /// <summary>
    /// 
    /// </summary>
    public void Dispose()
    {
        Array.Clear(PrivateKey, 0, 32);
    }
}