using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class WebSocketController : MonoBehaviour
{
    //--- Settings ---//
    [Header("WebSocket Settings")]
    [SerializeField] private string _serverUri = "ws://localhost:8080";
    [SerializeField] private int _receiveBufferSize = 1024;
    [SerializeField] private int _sendBufferSize = 1024;

    public string ServerUri => _serverUri;

    //--- Fields ---//
    private ClientWebSocket _webSocket = null;
    private CancellationTokenSource _cts;

    //--- Unity Methods ---//
    async void Start()
    {
        await ConnectToServer();
    }

    private async void OnApplicationQuit()
    {
        if (_webSocket != null)
        {
            _cts.Cancel();
            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "App Quit", CancellationToken.None);
            _webSocket.Dispose();
            Debug.Log("웹소켓 연결 종료");
        }
    }

    //--- Private Methods ---//
    private async Task ConnectToServer()
    {
        _webSocket = new ClientWebSocket();
        _cts = new CancellationTokenSource();

        try
        {
            Debug.Log($"서버 연결 시도 중: {_serverUri}");
            await _webSocket.ConnectAsync(new Uri(_serverUri), _cts.Token);
            Debug.Log("서버 연결 성공!");

            // 메시지 수신 루프 시작 (별도 Task)
            _ = ReceiveLoop();
        }
        catch (Exception e)
        {
            Debug.LogError($"연결 에러: {e.Message}");
        }
    }

    private async Task ReceiveLoop()
    {
        byte[] buffer = new byte[1024 * 4];

        try
        {
            while (_webSocket.State == WebSocketState.Open)
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, _cts.Token);
                    Debug.Log("서버에서 연결 종료 요청");
                }
                else
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    // Unity 메인 스레드에서 실행되도록 주의 (로그는 안전함)
                    Debug.Log($"수신 메시지: {message}");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"수신 에러: {e.Message}");
        }
    }

    public async void SendMessageToServer(string message)
    {
        if (_webSocket == null || _webSocket.State != WebSocketState.Open) return;

        byte[] buffer = Encoding.UTF8.GetBytes(message);
        await _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, _cts.Token);
        Debug.Log($"메시지 전송: {message}");
    }
}
