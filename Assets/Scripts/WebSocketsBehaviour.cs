using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Threading;
using System.Text;

public class WebSocketsBehaviour : MonoBehaviour
{
    private static ClientWebSocket _client;
    private Uri _senseServerURI;

    /// <summary>
    /// Simple semaphore for checking state of actions of websocket connection
    /// to do next action with websocket connection
    /// </summary>
    private static bool connectionFree = true;


    private readonly string webSocketConnectionString = "wss://geo-helper.ga";

    private async void Start()
    {
        _client = new ClientWebSocket();
        _senseServerURI = new Uri(webSocketConnectionString);
        await ConnectToSenseServer();
    }

    private async void Update()
    {
        if (_client.State != WebSocketState.Open)
        {
            await TryToConnectToServer();
        }
    }

    public string GetWSConnectionState()
    {
        return _client.State.ToString();
    }

    private async Task SendSelfLocationToServer(string jsonRequestString)
    {
        if (connectionFree)
        {
            if (_client != null && _client.State == WebSocketState.Open)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(jsonRequestString);
                ArraySegment<byte> buffer = new ArraySegment<byte>(bytes);

                // lock semaphore
                connectionFree = false;

                await _client.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);

                //Debug.Log($"Sended location request: {jsonRequestString}");

                // unlock semaphore
                connectionFree = true;

                return;
            }
            else if (_client != null && (_client.State == WebSocketState.Aborted || _client.State == WebSocketState.Closed))
            {
                Debug.LogError("Connection not established to send self location");
                await TryToConnectToServer();
            }
        }
        else
        {
            //Debug.LogError("Connection not free for SendSelfLocationToServer");
        }

    }

    private async Task TryToConnectToServer()
    {
        Debug.Log("Connection state: " + _client.State);
        if (_client != null && (_client.State == WebSocketState.Aborted || _client.State == WebSocketState.Closed))
        {
            Debug.Log("TryToConnectToServer");
            await ConnectToSenseServer();
            Debug.Log("Connection state: " + _client.State);
        }
    }

    public async Task<string> ReceiveObjectsFromServer(string jsonRequestString)
    {
        if (connectionFree)
        {
            if (_client != null && _client.State == WebSocketState.Open)
            {
                await SendSelfLocationToServer(jsonRequestString);

                byte[] buffer = new byte[65536];
                var segment = new ArraySegment<byte>(buffer, 0, buffer.Length);

                // lock semaphore
                connectionFree = false;

                await _client.ReceiveAsync(segment, CancellationToken.None);

                // unlock semaphore
                connectionFree = true;

                string jsonString = Encoding.UTF8.GetString(segment.Array);
                //Debug.Log(jsonString + " Received from server");
                return jsonString;
            }
            else if (_client != null && (_client.State == WebSocketState.Aborted || _client.State == WebSocketState.Closed))
            {
                Debug.LogError("Connection not established to send self location");
                await TryToConnectToServer();
            }
        }
        else
        {
            //Debug.LogError("Connection not free for ReceiveObjectsFromServer");
        }
        return "";
    }

    private async Task ConnectToSenseServer()
    {
        await _client.ConnectAsync(_senseServerURI, CancellationToken.None);
    }
}

