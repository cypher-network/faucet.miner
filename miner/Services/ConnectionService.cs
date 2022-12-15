// CypherNetwork by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Miner.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace Miner.Services;

/// <summary>
/// 
/// </summary>
public interface IConnectionService
{
    IObservable<HubConnectionState> ConnectionState { get; }
    IObservable<BlockMiner> BlockReceived { get; }
    IObservable<int> CountDownReceived { get; }
    IObservable<byte[]> RewardReceived { get; }
    ObservableCollection<BlockMiner> Blocks { get; }

    /// <summary>
    /// 
    /// </summary>
    Task ConnectAsync();

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    Task DisconnectAsync();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="proof"></param>
    /// <returns></returns>
    Task SendBlockProofAsync(byte[] proof);

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    Task<byte[]> GetRemotePublicKey();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="pubKey"></param>
    /// <returns></returns>
    Task<byte[]> GetRewardUpdateRequest(byte[] pubKey);
}

/// <summary>
/// 
/// </summary>
public class ConnectionService : IConnectionService
{
    public IObservable<HubConnectionState> ConnectionState { get; }
    public IObservable<BlockMiner> BlockReceived { get; }
    public IObservable<int> CountDownReceived { get; }
    public IObservable<byte[]> RewardReceived { get; }

    public ObservableCollection<BlockMiner> Blocks { get; } = new();
    private readonly HubConnection _hubConnection;
    private Subject<BlockMiner> _newBlockReceivedSubject = new();
    private Subject<int> _newCountDownReceivedSubject = new();
    private Subject<byte[]> _newRewardReceivedSubject = new();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="hubUrl"></param>
    public ConnectionService(string hubUrl)
    {
        _hubConnection = new HubConnectionBuilder().WithAutomaticReconnect().WithUrl($"{hubUrl}").Build();
        _hubConnection.On<BlockMiner>("NewBlock", ProcessNewBlock);
        _hubConnection.On<int>("CountDown", ProcessCountDown);
        _hubConnection.On<byte[]>("Reward", ProcessReward);
        BlockReceived = _newBlockReceivedSubject.AsObservable();
        CountDownReceived = _newCountDownReceivedSubject.AsObservable();
        RewardReceived = _newRewardReceivedSubject.AsObservable();
        ConnectionState = Observable.Interval(TimeSpan.FromMilliseconds(500))
            .Select(x => _hubConnection.State)
            .DistinctUntilChanged();
    }

    /// <summary>
    /// 
    /// </summary>
    public async Task ConnectAsync()
    {
        await _hubConnection.StartAsync();
    }

    /// <summary>
    /// 
    /// </summary>
    public async Task DisconnectAsync()
    {
        await _hubConnection.StopAsync();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public async Task<byte[]> GetRemotePublicKey()
    {
        return await _hubConnection.InvokeAsync<byte[]>("PublicKey");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="proof"></param>
    public async Task SendBlockProofAsync(byte[] proof)
    {
        try
        {
            await _hubConnection.SendAsync("BlockProof", proof);
        }
        catch (Exception)
        {
            // Ignore
        }
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="pubKey"></param>
    /// <returns></returns>
    public async Task<byte[]> GetRewardUpdateRequest(byte[] pubKey)
    {
        return await _hubConnection.InvokeAsync<byte[]>("RewardUpdateRequest", pubKey);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="block"></param>
    private void ProcessNewBlock(BlockMiner block)
    {
        Blocks.Add(block);
        _newBlockReceivedSubject.OnNext(block);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="countdown"></param>
    private void ProcessCountDown(int countdown)
    {
        _newCountDownReceivedSubject.OnNext(countdown);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="reward"></param>
    private void ProcessReward(byte[] reward)
    {
        _newRewardReceivedSubject.OnNext(reward);
    }
}