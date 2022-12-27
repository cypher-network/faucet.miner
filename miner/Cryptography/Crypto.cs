// CypherNetwork by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using libsignal.ecc;

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
    public static unsafe ReadOnlyMemory<byte> BoxSealOpen(ReadOnlySpan<byte> cipher, ReadOnlySpan<byte> secretKey, ReadOnlySpan<byte> publicKey)
    {
        var len = cipher.Length;
        var msg = stackalloc byte[len];
        int result;
        fixed (byte* cPtr = cipher, pkPtr = publicKey, skPtr = secretKey)
        {
            result = Box.SealOpen(msg, cPtr, (ulong)cipher.Length, pkPtr, skPtr);
        }
        
        var destination = new Span<byte>(msg, len);
        return result != 0 ? ReadOnlyMemory<byte>.Empty : destination[..len].ToArray();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="msg"></param>
    /// <param name="publicKey"></param>
    /// <returns></returns>
    public static ReadOnlyMemory<byte> BoxSeal(ReadOnlyMemory<byte> msg, ReadOnlyMemory<byte> publicKey)
    {
        var cipher = new byte[msg.Length + (int)Box.Sealbytes()];
        var result = 0;
        
        unsafe
        {
            fixed (byte* mPtr = msg.Span, cPtr = cipher, pkPtr = publicKey.Span)
            {
                result = Box.Seal(cPtr, mPtr, (ulong)msg.Span.Length, pkPtr);
            }
        }
        
        return result != 0 ? Memory<byte>.Empty : cipher.AsMemory();
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