using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.Text;

using NativeWebSocket;

public class WebSocketsBehaviour : MonoBehaviour
{
    private static WebSocket _websocket;
    private const string WebSocketConnectionString = "wss://geohelper.rtuitlab.dev/api/test";
    /// <summary>
    /// Simple semaphore for checking state of actions of websocket connection
    /// to do next action with websocket connection
    /// </summary>
    private static bool connectionFree = true;

    private static string _responseDataString = "";

    private async void Start()
    {
        // websocket = new WebSocket("ws://echo.websocket.org");
        _websocket = new WebSocket(WebSocketConnectionString);


        _websocket.OnOpen += () =>
        {
            Debug.Log("Connection open!");
        };

        _websocket.OnError += (e) =>
        {
            Debug.LogError("Error! " + e);
        };

        _websocket.OnClose += (e) =>
        {
            Debug.LogWarning("Connection closed!");
        };

        _websocket.OnMessage += (bytes) =>
        {
            // Reading a plain text message
            var message = System.Text.Encoding.UTF8.GetString(bytes);
            // Debug.Log("Received OnMessage! (" + bytes.Length + " bytes) " + message);
            _responseDataString = message;
        };

        await _websocket.Connect();
    }

    private void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        _websocket.DispatchMessageQueue();
#endif
    }

    // private async void Update()
    // {
    //     if (_client != null && _client.State != WebSocketState.Open)
    //     {
    //         await TryToConnectToServer();
    //     }
    // }

    public static string GetWsConnectionState()
    {
        return _websocket.State.ToString();
    }

    private static async Task SendSelfLocationToServer(string jsonRequestString)
    {
        if (connectionFree)
        {
            if (_websocket != null && _websocket.State == WebSocketState.Open)
            {

                // lock semaphore
                connectionFree = false;
    
                await _websocket.SendText(jsonRequestString);
    
                //Debug.Log($"Sended location request: {jsonRequestString}");
    
                // unlock semaphore
                connectionFree = true;
            }
        }
    }

    public async Task<string> ReceiveObjectsFromServer(string jsonRequestString)
    {
        try
        {
            if (!connectionFree 
                || _websocket == null 
                || _websocket.State != WebSocketState.Open) 
                return "";
            
            await SendSelfLocationToServer(jsonRequestString);
            
            StartCoroutine(WaitResponseFromServer());
            
            string response = _responseDataString;
            _responseDataString = "";
            
            return response;
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            return "";
        }
    }

    private static IEnumerator WaitResponseFromServer()
    {
        if (_responseDataString == "")
            yield return new WaitForSeconds(1f);
        
    }
    
    private async void OnApplicationQuit()
    {
        await _websocket.Close();
    }

}