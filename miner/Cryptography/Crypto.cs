// CypherNetwork by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using libsignal.ecc;
using NitraLibSodium.Box;

namespace Miner.Cryptography;


/// <summary>
/// 
/// </summary>
public static class Crypto
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="ecPrivateKey"></param>
    /// <param name="msg"></param>
    /// <returns></returns>
    public static byte[] GetCalculateVrfSignature(ECPrivateKey ecPrivateKey, byte[] msg)
    {
        var calculateVrfSignature = Curve.calculateVrfSignature(ecPrivateKey, msg);
        return calculateVrfSignature;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="ecPublicKey"></param>
    /// <param name="msg"></param>
    /// <param name="sig"></param>
    /// <returns></returns>
    public static byte[] GetVerifyVrfSignature(ECPublicKey ecPublicKey, byte[] msg, byte[] sig)
    {
        var vrfSignature = Curve.verifyVrfSignature(ecPublicKey, msg, sig);
        return vrfSignature;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="cipher"></param>
    /// <param name="secretKey"></param>
    /// <param name="publicKey"></param>
    /// <returns></returns>
    public static byte[] BoxSealOpen(byte[] cipher, byte[] secretKey, byte[] publicKey)
    {
        var msg = new byte[cipher.Length];
        return Box.SealOpen(msg, cipher, (ulong)cipher.Length, publicKey, secretKey) != 0
            ? Array.Empty<byte>()
            : msg;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="msg"></param>
    /// <param name="publicKey"></param>
    /// <returns></returns>
    public static byte[] BoxSeal(byte[] msg, byte[] publicKey)
    {
        var cipher = new byte[msg.Length + (int)Box.Sealbytes()];
        return Box.Seal(cipher, msg, (ulong)msg.Length, publicKey) != 0
            ? Array.Empty<byte>()
            : cipher;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public static Models.KeyPair GenerateKeyPair()
    {
        var keys = Curve.generateKeyPair();
        return new Models.KeyPair(keys.getPrivateKey().serialize(), keys.getPublicKey().serialize());
    }
}