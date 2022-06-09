using System;
using System.Numerics;
using System.Reflection;
using System.Text;

namespace Miner.Helper;

/// <summary>
/// 
/// </summary>
public static class Utils
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="hex"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static byte[] HexToByte<T>(this T hex)
    {
        return Convert.FromHexString(hex.ToString()!);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static string ByteToHex(this byte[] data)
    {
        return Convert.ToHexString(data);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="a"></param>
    /// <param name="n"></param>
    /// <returns></returns>
    public static BigInteger Mod(BigInteger a, BigInteger n)
    {
        var result = a % n;
        if (result < 0 && n > 0 || result > 0 && n < 0) result += n;

        return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static byte[] ToBytes(this string? value)
    {
        return Encoding.UTF8.GetBytes(value ?? string.Empty, 0, value!.Length);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static byte[] ToBytes(this long value)
    {
        return Encoding.UTF8.GetBytes(value.ToString(), 0, value!.ToString().Length);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public static DateTime GetUtcNow()
    {
        return DateTime.UtcNow;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    private static DateTime GetAdjustedTime()
    {
        return GetUtcNow().Add(TimeSpan.Zero);
    }

    /// <summary>
    /// </summary>
    /// <returns></returns>
    public static long GetAdjustedTimeAsUnixTimestamp()
    {
        return new DateTimeOffset(GetAdjustedTime()).ToUnixTimeSeconds();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="timeStampMask"></param>
    /// <returns></returns>
    public static long GetAdjustedTimeAsUnixTimestamp(uint timeStampMask)
    {
        return GetAdjustedTimeAsUnixTimestamp() & ~timeStampMask;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public static string GetAssemblyVersion()
    {
        return Assembly.GetExecutingAssembly().GetName().Version.ToString();
    }
}