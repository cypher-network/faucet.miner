// CypherNetwork by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.IO;
using Miner.Models;
using MessagePack;
using Newtonsoft.Json.Linq;

namespace Miner.Services;

/// <summary>
/// 
/// </summary>
public interface ISessionService
{
    KeyPair KeyPair { get; }
    byte[] Address { get; set; }
    byte[] RemotePublicKey { get; set; }
    string HttpHub { get; }
}

/// <summary>
/// 
/// </summary>
public class SessionService : ISessionService
{
    private readonly string _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "keypair.file");
    
    public KeyPair KeyPair { get; private set; }
    public byte[] Address { get; set; }
    public byte[] RemotePublicKey { get; set; }
    public string HttpHub { get; }

    /// <summary>
    /// 
    /// </summary>
    public SessionService()
    {
        if (!File.Exists(_filePath))
        {
            KeyPair = Cryptography.Crypto.GenerateKeyPair();
        }
        else
        {
            ReadKeyPair();
        }

        if (!File.Exists(_filePath))
        {
            SaveKeyPair();   
        }
        
        _filePath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        try
        {
            var jObject = Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(File.ReadAllText(_filePath));
            HttpHub = jObject[nameof(HttpHub)].Value<string>();
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    /// <summary>
    /// Nothing really to hide as the keypair is used to encrypt/decrypt block proofs and incoming rewards from tampering
    /// or prying eyes on the wire.
    /// We can make this more secure later..
    /// </summary>
    private void SaveKeyPair()
    {
        try
        {
            var bytes = MessagePackSerializer.Serialize(KeyPair);
            File.WriteAllBytes(_filePath, bytes);
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    private void ReadKeyPair()
    {
        try
        {
            var bytes = File.ReadAllBytes(_filePath);
            KeyPair = MessagePackSerializer.Deserialize<KeyPair>(bytes);
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }
}