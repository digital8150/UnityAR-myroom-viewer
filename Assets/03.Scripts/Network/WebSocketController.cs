using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class WebsocketController : MonoBehaviour
{
    //--- Settings ---//
    [Header("웹소켓 설정")]
    [SerializeField]
    private string _serverUri = "http://home.codingbot.kr:8080/ws/info?token=";

    public static WebsocketController Instance { get; private set; }

    private ClientWebSocket _webSocket = null;
    private CancellationTokenSource _cts;
    private bool _isDisposed = false;

    //--- Unity Methods ---//
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private async void OnDestroy()
    {
        await CleanupAsync();
    }

    private async void OnApplicationQuit()
    {
        await CleanupAsync();
    }

    //--- Public Methods ---//
    public async Task ConnectToServer()
    {
        try
        {
            var token = JWTToken.Token; // JWT 토큰 가져오기
            if(string.IsNullOrEmpty(token))
            {
                Debug.LogError("JWT 토큰이 없습니다. 연결을 시도할 수 없습니다.");
                return;
            }

            if(_webSocket != null)
            {
                _cts?.Cancel();
                if(_webSocket.State == WebSocketState.Open || _webSocket.State == WebSocketState.CloseReceived)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                }
                _webSocket.Dispose();
                _webSocket = null;
                _cts?.Dispose();
                _cts = null;
            }
            _webSocket = new ClientWebSocket();
            _cts = new CancellationTokenSource();

            var uri = new Uri(_serverUri + Uri.EscapeDataString(token));
            await _webSocket.ConnectAsync(uri, _cts.Token);
            Debug.Log("웹소켓 연결 성공!");

            // 메시지 수신 루프 시작 (별도 Task)
            _ = ReceiveLoop();
        }
        catch (Exception e)
        {
            Debug.LogError($"연결 에러: {e.Message}");
            _webSocket?.Dispose();
            _webSocket = null;
            _cts?.Dispose(); 
            _cts = null;
        }
    }

    /// <summary>
    /// 웹소켓 서버로 메시지를 전송합니다.
    /// </summary>
    /// <param name="message">메시지</param>
    public async Task SendMessageToServer(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            Debug.LogWarning("전송할 메시지가 비어있습니다.");
            return;
        }

        if (_webSocket == null || _webSocket.State != WebSocketState.Open) return;

        try
        {
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            await _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, _cts.Token);
            Debug.Log($"전송 메시지: {message}");
        }
        catch (Exception e)
        {
            Debug.LogError($"전송 에러: {e.Message}");
        }
    }

    //--- Private Methods ---//
    private async Task ReceiveLoop()
    {
        byte[] buffer = new byte[1024 * 4];

        try
        {
            while (_webSocket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result;
                using var ms = new System.IO.MemoryStream();
                do
                {
                    result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
                    if(result.MessageType == WebSocketMessageType.Close)
                    {
                        await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                        Debug.Log("서버가 연결을 닫았습니다.");
                        break;
                    }
                    ms.Write(buffer, 0, result.Count);
                }while (!result.EndOfMessage);
                if(result.MessageType == WebSocketMessageType.Close)
                {
                    continue;
                }

                string message = Encoding.UTF8.GetString(ms.ToArray());
                Debug.Log($"수신 메세지 : {message}");
                UnityMainThreadDispatcher.Enqueue(() =>
                {
                    // 여기에 메인 스레드에서 실행할 코드를 작성하세요.
                    // 예: UI 업데이트 등
                });
            }

        }
        catch (Exception e)
        {
            Debug.LogError($"수신 에러: {e.Message}");
        }
    }

    private async Task CleanupAsync()
    {
        if (_isDisposed || _webSocket == null)
        {
            return;
        }
        _isDisposed = true;

        try
        {
            _cts?.Cancel();
            if (_webSocket.State == WebSocketState.Open || _webSocket.State == WebSocketState.CloseReceived)
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"정리 에러: {e.Message}");
        }
        finally
        {
            _webSocket?.Dispose();
            _webSocket = null;
            _cts?.Dispose();
            _cts = null;
            Debug.Log("웹소켓 정리 완료");
        }
    }
}
