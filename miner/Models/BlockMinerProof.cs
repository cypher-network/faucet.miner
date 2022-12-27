// CypherNetwork by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using MessagePack;

namespace Miner.Models;

/// <summary>
/// 
/// </summary>
[MessagePackObject]
public record BlockMinerProof
{
    [Key(0)] public byte[] Hash { get; init; }
    [Key(1)] public ulong Height { get; init; }
    [Key(2)] public ulong Solution { get; init; }
    [Key(3)] public byte[] Nonce { get; init; }
    [Key(4)] public byte[] VrfProof { get; init; }
    [Key(5)] public byte[] VrfSig { get; init; }
    [Key(6)] public byte[] PublicKey { get; init; }
    [Key(7)] public byte[] Address { get; init; }
    [Key(8)] public long Locktime { get; init; }
    [Key(9)] public byte[] LocktimeScript { get; init; }
    [IgnoreMember] public double SlowHashElapsedTime { get; init; }
}