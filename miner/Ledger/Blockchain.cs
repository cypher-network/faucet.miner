// CypherNetwork by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Blake3;
using Miner.Cryptography;
using Miner.Helper;
using Miner.Models;
using Miner.Services;
using libsignal.ecc;
using NBitcoin;
using BigInteger = NBitcoin.BouncyCastle.Math.BigInteger;
using Utils = Miner.Helper.Utils;

namespace Miner.Ledger;

/// <summary>
/// 
/// </summary>
public interface IBlockchain
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="block"></param>
    /// <returns></returns>
    Task<BlockMinerProof?> NewProof(BlockMiner block);

    bool Busy { get; }
}

public class SlowHashValue
{
    public byte[] Nonce { get; init; }
    public double Elapsed { get; init; }
}

/// <summary>
/// 
/// </summary>
public class Blockchain : IBlockchain, IDisposable
{
    private const int BitTime = 4600;
    private const int LockTimeInMilliseconds = 10000;
    private const int StopSlothInMilliseconds = 10000;
    
    private readonly CancellationTokenSource _cancellation = new();
    private readonly ISessionService _sessionService;

    public bool Busy { get; private set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sessionService"></param>
    public Blockchain(ISessionService sessionService)
    {
        _sessionService = sessionService;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public async Task<BlockMinerProof?> NewProof(BlockMiner block)
    {
        if (block == null) return null;
        BlockMinerProof? blockMinerProof;
        Busy = true;

        try
        {
            var lockTime = Utils.GetAdjustedTimeAsUnixTimestamp(LockTimeInMilliseconds);
            var kernel = Kernel(block.PrevHash, block.Hash, lockTime);
            var calculatedVrfSignature = Crypto.GetCalculateVrfSignature(
                Curve.decodePrivatePoint(_sessionService.KeyPair.PrivateKey), kernel);
            var verifyVrfSignature = Crypto.GetVerifyVrfSignature(
                Curve.decodePoint(_sessionService.KeyPair.PublicKey, 0), kernel, calculatedVrfSignature);
            var slow = await GetNonceAsync(verifyVrfSignature, BitTime);
            if (slow is { Nonce.Length: 0 }) return null;
            blockMinerProof = new BlockMinerProof
            {
                Hash = block.Hash,
                Address = _sessionService.Address,
                Locktime = lockTime,
                Nonce = slow.Nonce,
                LocktimeScript = new Script(Op.GetPushOp(lockTime), OpcodeType.OP_CHECKLOCKTIMEVERIFY).ToString()
                    .ToBytes(),
                PublicKey = _sessionService.KeyPair.PublicKey,
                VrfProof = calculatedVrfSignature,
                VrfSig = verifyVrfSignature,
                SlowHashElapsedTime = slow.Elapsed
            };
        }
        finally
        {
            Busy = false;
        }

        return blockMinerProof;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="solution"></param>
    /// <returns></returns>
    private static int Bits(ulong solution)
    {
        var diff = Math.Truncate(solution * 25.0 / 8192);
        diff = diff == 0 ? 1 : diff;
        return (int)diff;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="prevHash"></param>
    /// <param name="hash"></param>
    /// <param name="lockTime"></param>
    /// <returns></returns>
    private static byte[] Kernel(byte[] prevHash, byte[] hash, long lockTime)
    {
        var txHashBig = new BigInteger(1, hash).Multiply(
            new BigInteger(Hasher.Hash(prevHash).HexToByte()).Multiply(
                new BigInteger(Hasher.Hash(lockTime.ToBytes()).HexToByte())));
        var kernel = Hasher.Hash(txHashBig.ToByteArray()).HexToByte();
        return kernel;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="vrfSignature"></param>
    /// <param name="bits"></param>
    /// <returns></returns>
    private async Task<SlowHashValue?> GetNonceAsync(byte[] vrfSignature, int bits)
    {
        var x = System.Numerics.BigInteger.Parse(vrfSignature.ByteToHex(), NumberStyles.AllowHexSpecifier);
        if (x.Sign <= 0) x = -x;
        var nonceHash = Array.Empty<byte>();
        var sloth = new Sloth(StopSlothInMilliseconds, _cancellation.Token);
        SlowHashValue? slow = null;
        
        try
        {
            var nonce = await sloth.EvalAsync(bits, x);
            if (!string.IsNullOrEmpty(nonce)) nonceHash = nonce.ToBytes();
            slow = new SlowHashValue { Elapsed = sloth.Elapsed, Nonce = nonceHash };
        }
        catch (Exception)
        {
            // Ignore
        }

        return slow;
    }

    /// <summary>
    /// 
    /// </summary>
    public void Dispose()
    {
        _cancellation.Dispose();
    }
}