using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using URG;

public class URGSerialAsync : URGSerial
{
    CancellationTokenSource tokenSource;
    
    protected override void OnEnable()
    {
        if (Open())
        {
            StartScanAsync();
        }
    }

    protected override void Update()
    {
        // synchronous Serial Communication (slow)
        //base.Update();
    }

    protected override void OnDisable()
    {
        StopScanAsync();
    }

    #region Asynchronous
    /// <summary>
    /// Start asynchronous scanning process
    /// </summary>
    public async void StartScanAsync()
    {
        StartScan();

        tokenSource = new CancellationTokenSource();
        await Task.Run(async () =>
        {
            while (!tokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    //points = await processReadDistanceAsync();
                    points = UtilitySCIP.ProcessReadDistance(serialPort.ReadLine(), true, isShortRange);
                }
                catch (IOException ex)
                {
                    Debug.LogErrorFormat("[IO Error]: (Source){0}, (Message){1}", ex.Source, ex.Message);
                }
            }
        })
        .ContinueWith((t) =>
        {
            StopScan();
            Close();
        }); //.ConfigureAwait(false)
    }

    /// <summary>
    /// Stop asynchronous scanning process
    /// </summary>
    public async void StopScanAsync()
    {
        tokenSource.Cancel();
        await Task.Delay(100).ConfigureAwait(false);
    }

    /// <summary>
    /// Show the acquired data from asynchronous process
    /// </summary>
    public async void ShowDistanceLogAsync()
    {
        //var message = await serialPort.ReadLineAsync("ascii");
        //Debug.Log(message);
        var collection = await processReadDistanceAsync();
        foreach (var item in collection)
        {
            Debug.LogFormat("{0}: {1}", item.Key, item.Value);
        }
    }

    /// <summary>
    /// Read data via Serial communication asynchronously
    /// </summary>
    /// <returns>Result of the asyncronous process</returns>
    async Task<Dictionary<float, long>> processReadDistanceAsync()
    {
        var packets = await serialPort.ReadLineAsync();
        return UtilitySCIP.ProcessReadDistance(packets, true, isShortRange);
    }
    #endregion
}
