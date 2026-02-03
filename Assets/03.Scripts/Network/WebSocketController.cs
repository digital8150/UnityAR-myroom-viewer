using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class WebsocketController : MonoBehaviour
{
    [Header("웹소켓 설정")]
    [SerializeField]
    private string _serverUri = "ws://home.codingbot.kr:8080/ws/websocket"; // SockJS를 위해 /websocket 명시

    public static WebsocketController Instance { get; private set; }

    private ClientWebSocket _webSocket = null;
    private CancellationTokenSource _cts;
    private bool _isDisposed = false;

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    private async void OnDestroy() { await CleanupAsync(); }
    private async void OnApplicationQuit() { await CleanupAsync(); }

    public async Task ConnectToServer()
    {
        try
        {
            var token = JWTToken.Token;
            if (string.IsNullOrEmpty(token)) return;

            await CleanupAsync();

            _webSocket = new ClientWebSocket();
            _cts = new CancellationTokenSource();
            _isDisposed = false;

            // 1. URL 구성: SockJS 스타일의 타임스탬프와 토큰 결합
            long t = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var fullUri = new Uri($"{_serverUri}?token={Uri.EscapeDataString(token)}&t={t}");

            await _webSocket.ConnectAsync(fullUri, _cts.Token);
            Debug.Log("WebSocket Layer Connected!");

            // 2. STOMP CONNECT 프레임 전송 (이게 없으면 서버가 응답 안 함)
            await SendStompConnect();

            _ = ReceiveLoop();
        }
        catch (Exception e)
        {
            Debug.LogError($"연결 에러: {e.Message}");
            await CleanupAsync();
        }
    }

    private async Task SendStompConnect()
    {
        // StompHelper가 있다면 StompHelper.CreateConnectFrame() 사용 가능
        // 직접 구성 시: CONNECT\naccept-version:1.1,1.2\nheart-beat:10000,10000\n\n\0
        string connectFrame = "CONNECT\naccept-version:1.1,1.0\nheart-beat:0,0\n\n\0";
        await SendRawMessage(connectFrame);
    }

    // 기존 컴포넌트 호환용 (STOMP SEND 프레임으로 래핑)
    public async Task SendMessageToServer(string message)
    {
        if (_webSocket?.State != WebSocketState.Open) return;

        // 일반 텍스트를 STOMP SEND 규격으로 변환
        // destination은 서버 설정에 따라 /app/hello 등으로 수정 필요
        string stompFrame = $"SEND\ndestination:/app/message\ncontent-type:text/plain\n\n{message}\0";
        await SendRawMessage(stompFrame);
    }

    private async Task SendRawMessage(string rawData)
    {
        byte[] buffer = Encoding.UTF8.GetBytes(rawData);
        await _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, _cts.Token);
    }

    private async Task ReceiveLoop()
    {
        byte[] buffer = new byte[1024 * 8];
        try
        {
            while (_webSocket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result;
                using var ms = new System.IO.MemoryStream();
                do
                {
                    result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
                    ms.Write(buffer, 0, result.Count);
                } while (!result.EndOfMessage);

                string rawMessage = Encoding.UTF8.GetString(ms.ToArray());

                // STOMP 프레임 분석 (CONNECTED인지, MESSAGE인지 등)
                ProcessStompMessage(rawMessage);
            }
        }
        catch (Exception e) { if (!_isDisposed) Debug.LogError($"수신 에러: {e.Message}"); }
    }

    private void ProcessStompMessage(string raw)
    {
        // 여기서 StompHelper.Parse(raw)를 사용하여 body만 추출 가능
        Debug.Log($"[STOMP Received]: {raw}");

        UnityMainThreadDispatcher.Enqueue(() => {
            // 메인 스레드 로직 (UI 업데이트 등)
        });
    }

    private async Task CleanupAsync()
    {
        if (_webSocket == null) return;
        _isDisposed = true;
        try
        {
            if (_webSocket.State == WebSocketState.Open)
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
        }
        finally
        {
            _webSocket?.Dispose();
            _webSocket = null;
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }
    }
}