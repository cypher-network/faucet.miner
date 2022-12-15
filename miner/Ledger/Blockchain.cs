// CypherNetwork by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.Diagnostics;
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

/// <summary>
/// 
/// </summary>
public class Blockchain : IBlockchain, IDisposable
{
    private const int LockTimeInMilliseconds = 10000;
    private const int StopSolutionInMilliseconds = 10000;
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
            var solution = await SolutionAsync(calculatedVrfSignature, kernel);
            if (solution.Sol == 0) return null;
            var nonce = await GetNonceAsync(verifyVrfSignature, Bits(solution.Sol));
            if (nonce.Length == 0) return null;
            blockMinerProof = new BlockMinerProof
            {
                Hash = block.Hash,
                Address = _sessionService.Address,
                Height = block.Height,
                Locktime = lockTime,
                Nonce = nonce,
                Solution = solution.Sol,
                LocktimeScript = new Script(Op.GetPushOp(lockTime), OpcodeType.OP_CHECKLOCKTIMEVERIFY).ToString()
                    .ToBytes(),
                PublicKey = _sessionService.KeyPair.PublicKey,
                VrfProof = calculatedVrfSignature,
                VrfSig = verifyVrfSignature,
                HashRate = solution.HashRatePerSecond
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
    /// <param name="vrfBytes"></param>
    /// <param name="kernel"></param>
    /// <returns></returns>
    private static async Task<Solution> SolutionAsync(byte[] vrfBytes, byte[] kernel)
    {
        return await Task.Factory.StartNew(() =>
        {
            var sw = new Stopwatch();
            var sol = new Solution();
            try
            {
                long itr = 0;
                var target = new BigInteger(1, Hasher.Hash(vrfBytes).HexToByte());
                var hashTarget = new BigInteger(1, kernel);
                var hashTargetValue = new BigInteger((target.IntValue / hashTarget.BitCount).ToString()).Abs();
                var hashWeightedTarget = new BigInteger(1, kernel).Multiply(hashTargetValue);
                sw.Start();
                while (true)
                {
                    if (sw.ElapsedMilliseconds > StopSolutionInMilliseconds)
                    {
                        itr = 0;
                        break;
                    }

                    var weightedTarget = target.Multiply(BigInteger.ValueOf(itr));
                    if (hashWeightedTarget.CompareTo(weightedTarget) <= 0)
                    {
                        sw.Stop();
                        break;
                    }

                    if (itr == long.MaxValue)
                    {
                        itr = 0;
                        break;
                    }

                    itr++;
                }

                if (sw.IsRunning) sw.Stop();
                sol.Sol = (ulong)itr;
                if (sol.Sol == 0) return sol;
                var seconds = sw.ElapsedMilliseconds * 10 ^ -3;
                sol.HashRatePerSecond = seconds < 0 ? (long)sol.Sol : (long)(sol.Sol / (ulong)seconds);
                return sol;
            }
            catch (Exception)
            {
                sw.Stop();
            }

            return sol;
        }, TaskCreationOptions.LongRunning).ConfigureAwait(false);
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
    private async Task<byte[]> GetNonceAsync(byte[] vrfSignature, int bits)
    {
        var x = System.Numerics.BigInteger.Parse(vrfSignature.ByteToHex(), NumberStyles.AllowHexSpecifier);
        if (x.Sign <= 0) x = -x;
        var nonceHash = Array.Empty<byte>();
        try
        {
            var sloth = new Sloth(StopSlothInMilliseconds, _cancellation.Token);
            var nonce = await sloth.EvalAsync(bits, x);
            if (!string.IsNullOrEmpty(nonce)) nonceHash = nonce.ToBytes();
        }
        catch (Exception)
        {
            // Ignore
        }

        return nonceHash;
    }

    /// <summary>
    /// 
    /// </summary>
    public void Dispose()
    {
        _cancellation.Dispose();
    }
}