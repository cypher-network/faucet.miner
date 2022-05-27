// CypherNetwork by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using MessagePack;

namespace Miner.Models;

[MessagePackObject]
public record BlockMiner
{
    [Key(0)] public byte[] Hash { get; init; }
    [Key(1)] public byte[] PrevHash { get; init; }
    [Key(2)] public ulong Height { get; init; }
}