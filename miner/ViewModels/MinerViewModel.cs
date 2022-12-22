// CypherNetwork by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.Notifications;
using Avalonia.Input.Platform;
using Miner.Cryptography;
using Miner.Helper;
using Miner.Ledger;
using Miner.Models;
using Miner.Services;
using Microsoft.AspNetCore.SignalR.Client;
using Miner.Views;
using NBitcoin.DataEncoders;
using ReactiveUI;
using Splat;

namespace Miner.ViewModels;

public class MinerViewModel : ViewModelBase
{
    private readonly IConnectionService _connectionService;
    private readonly ISessionService _sessionService;
    private readonly IBlockchain _blockchain;

    public string Greeting => $"$Miner ♥ v{Utils.GetAssemblyVersion()}";

    public ICommand StartCommand { get; }
    public ICommand PkCommand { get; }

    /// <summary>
    /// 
    /// </summary>
    private string _address;
    public string Address
    {
        get => _address;
        set => this.RaiseAndSetIfChanged(ref _address, value);
    }

    private string _hashRate;
    public string HashRate
    {
        get => _hashRate;
        set => this.RaiseAndSetIfChanged(ref _hashRate, value);
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
    private decimal _reward;
    public decimal Reward
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

    private int _wonProofCount;
    public int WonProofCount
    {
        get => _wonProofCount;
        set => this.RaiseAndSetIfChanged(ref _wonProofCount, value);
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
        var canShowPkMessage = this.WhenAnyValue(x => x._sessionService.KeyPair.PublicKey).Select(x => !string.IsNullOrEmpty(x.ByteToHex()));
        PkCommand = ReactiveCommand.CreateFromTask(ShowPublicKey, canShowPkMessage);
        StartCommand = ReactiveCommand.CreateFromTask(Start, canSendMessage);
        _connectionService.CountDownReceived.Subscribe(countdown => { CountDown = $"{countdown}s"; });
        _connectionService.RewardReceived.Subscribe(reward =>
        {
            try
            {
                var msg = Crypto.BoxSealOpen(reward, _sessionService.KeyPair.PrivateKey,
                    _sessionService.KeyPair.PublicKey[1..33]);
                if (msg.Length == 0) return;
                var result = MessagePack.MessagePackSerializer.Deserialize<Reward>(msg);
                Reward += result.Amount.DivCoin();
                WonProofCount += 1;
            }
            catch (Exception ex)
            {
                this.Log().Error(ex.Message);
            }
        });

        StartCommandContent = "Start Miner";
        CountDown = "...";
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
            const string err = "Recipient address does not phrase to a base58 format.";
            MainWindow.ShowNotification("Main-net Address", err, NotificationType.Error);
            Address = string.Empty;
            this.Log().Error(err);
            return;
        }

        await Connect();
    }

    /// <summary>
    /// 
    /// </summary>
    private async Task ShowPublicKey()
    {
        var pk = _sessionService.KeyPair.PublicKey.ByteToHex();
        var clipboard = (IClipboard)AvaloniaLocator.Current.GetService(typeof(IClipboard));
        await clipboard.SetTextAsync(pk);
        MainWindow.ShowNotification("Public Key", $"{pk}\n\n Copied to clipboard!", NotificationType.Success);
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

                Reward = AsyncHelper.RunSync(async delegate
                {
                    var pubKey = _sessionService.KeyPair.PublicKey;
                    var cipher = await _connectionService.GetRewardUpdateRequest(pubKey);
                    if (cipher is null) return Reward;
                    var msg = Crypto.BoxSealOpen(cipher, _sessionService.KeyPair.PrivateKey, pubKey[1..33]);
                    if (msg.Length == 0) return Reward;
                    var result = MessagePack.MessagePackSerializer.Deserialize<Reward>(msg);
                    Reward = result.Amount.DivCoin();
                    return Reward;
                });

                _sessionService.RemotePublicKey = AsyncHelper.RunSync(_connectionService.GetRemotePublicKey);
                _connectionService.BlockReceived.Subscribe(async block =>
                {
                    CountDown = "New block..";
                    if (!_blockchain.Busy)
                    {
                        CountDown = "Hashing...";
                        var blockProof = await Task.Run(async () => await _blockchain.NewProof(block)).ConfigureAwait(false);
                        if (blockProof is null) return;

                        HashRate = $"Hash Rate: {blockProof.HashRate} PS";
                        var proof = Crypto.BoxSeal(MessagePack.MessagePackSerializer.Serialize(blockProof),
                            _sessionService.RemotePublicKey);

                        await _connectionService.SendBlockProofAsync(proof);
                        CountDown = "Proof sent!";
                        SentProofCount++;
                    }
                    else
                    {
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