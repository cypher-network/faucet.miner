// CypherNetwork by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Miner.Cryptography;
using Miner.Helper;
using Miner.Ledger;
using Miner.Models;
using Miner.Services;
using Microsoft.AspNetCore.SignalR.Client;
using NBitcoin.DataEncoders;
using ReactiveUI;
using Splat;

namespace Miner.ViewModels;

public class MinerViewModel: ViewModelBase
{
    private readonly IConnectionService _connectionService;
    private readonly ISessionService _sessionService;
    private readonly IBlockchain _blockchain;
    
    public string Greeting => "Welcome to $Miner ♥";
    public ICommand StartCommand { get; }
        
    /// <summary>
    /// 
    /// </summary>
    private string _address;
    public string Address
    {
        get => _address;
        set => this.RaiseAndSetIfChanged(ref _address, value);
    }
        
    /// <summary>
    /// 
    /// </summary>
    private bool _connected;
    public bool Connected
    {
        get => _connected;
        set => this.RaiseAndSetIfChanged(ref _connected, value);
    }

    /// <summary>
    /// 
    /// </summary>
    private int _reward;
    public int Reward
    {
        get => _reward;
        set => this.RaiseAndSetIfChanged(ref _reward, value);
    }

    /// <summary>
    /// 
    /// </summary>
    private string _countdown;
    public string CountDown
    {
        get => _countdown;
        set => this.RaiseAndSetIfChanged(ref _countdown, value);
    }

    /// <summary>
    /// 
    /// </summary>
    private int _sentProofCount;
    public int SentProofCount
    {
        get => _sentProofCount;
        set => this.RaiseAndSetIfChanged(ref _sentProofCount, value);
    }

    /// <summary>
    /// 
    /// </summary>
    private string _startCommandContent;
    public string StartCommandContent
    {
        get => _startCommandContent;
        set => this.RaiseAndSetIfChanged(ref _startCommandContent, value);
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="router"></param>
    public MinerViewModel(RoutingState router) : base(router)
    {
        _sessionService = Locator.Current.GetService<ISessionService>();
        _blockchain = Locator.Current.GetService<IBlockchain>();
        _connectionService = new ConnectionService(_sessionService.HttpHub);
        var canSendMessage = this.WhenAnyValue(x => x.Address).Select(x => !string.IsNullOrEmpty(x));
        StartCommand = ReactiveCommand.CreateFromTask(Start, canSendMessage);
        _connectionService.CountDownReceived.Subscribe(countdown => { CountDown = $"{countdown}s"; });
        _connectionService.RewardReceived.Subscribe(reward =>
        {
            var msg = Crypto.BoxSealOpen(reward, _sessionService.KeyPair.PrivateKey,
                _sessionService.KeyPair.PublicKey[1..33]);
            if (msg.Length == 0) return;
            var result = MessagePack.MessagePackSerializer.Deserialize<Reward>(msg);
            Reward += result.Amount;
        });

        StartCommandContent = "Start Miner";
        CountDown = "Wait..";
    }
    
    /// <summary>
    /// 
    /// </summary>
    private async Task Start()
    {
        if (StartCommandContent == "Stop Miner")
        {
            await _connectionService.DisconnectAsync();
            return;
        }
        
        if (!IsBase58(Address))
        {
            this.Log().Error("Recipient address does not phrase to a base58 format.");
            Address = string.Empty;
            return;
        }
        
        await Connect();
    }

    /// <summary>
    /// 
    /// </summary>
    private async Task Connect()
    {
        try
        {
            await _connectionService.ConnectAsync();
            _connectionService.ConnectionState.Subscribe(x =>
            {
                Connected = x == HubConnectionState.Connected;
                if (!Connected)
                {
                    StartCommandContent = "Start Miner";
                    return;
                }
                
                StartCommandContent = "Stop Miner";
                _sessionService.Address = Address.ToBytes();
                _sessionService.RemotePublicKey = AsyncHelper.RunSync(_connectionService.GetRemotePublicKey);
                _connectionService.BlockReceived.Subscribe(block =>
                {
                    if (!_blockchain.Busy)
                    {
                        Task.Run(async () =>
                        {
                            var blockProof = await _blockchain.NewProof(block);
                            if (blockProof is null) return;
                            var proof = Crypto.BoxSeal(MessagePack.MessagePackSerializer.Serialize(blockProof),
                                _sessionService.RemotePublicKey);
                            await _connectionService.SendBlockProofAsync(proof);
                            CountDown = "Proof sent!";
                            SentProofCount++;
                        });
                    }
                    else
                    {
                        // Message is to fast to see..
                        CountDown = "Miner busy...";
                    }
                });
            });
        }
        catch (Exception ex)
        {
            this.Log().Error("Something bad happened: {0}", ex.Message);
        }
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="address"></param>
    /// <returns></returns>
    private static bool IsBase58(string address)
    {
        var base58CheckEncoder = new Base58CheckEncoder();
        var isBase58 = base58CheckEncoder.IsMaybeEncoded(address);
        try
        {
            base58CheckEncoder.DecodeData(address);
        }
        catch (Exception)
        {
            isBase58 = false;
        }

        return isBase58;
    }
}