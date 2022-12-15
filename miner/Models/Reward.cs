// CypherNetwork by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using MessagePack;

namespace Miner.Models;

[MessagePackObject]
public record Reward([property: Key(0)] byte[] Id, [property: Key(2)] byte[] Hash, [property: Key(3)] ulong Height, [property:Key(4)] ulong Amount);