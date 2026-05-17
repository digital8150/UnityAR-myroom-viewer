using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class WebsocketController : MonoBehaviour
{
    [Header("웹소켓 설정")]
    [SerializeField]
    private string _serverUri;

    public static WebsocketController Instance { get; private set; }

    public event Action<string> OnModel3DGenerated;
    public event Action<string> OnModel3DGenerateFailed;
    public event Action<string> OnAIRecommendReceived;
    public event Action<string> OnRoom3DGenerationSuccess;
    public event Action<string> OnRoom3DGenerationFailed;
    public event Action<string> OnRoom3DGenerationProgress;
    public event Action<string> OnModel3DDimensionsByImageReceived;

    private ClientWebSocket _webSocket = null;
    private CancellationTokenSource _cts;
    private bool _isDisposed = false;
    private string _userId = null;
    
    // 재연결 중복 방지용 플래그
    private bool _isReconnecting = false; 

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    private void Start()
    {
        if(_serverUri == String.Empty)
        {
            _serverUri = $"ws://{Utils.Settings.Hostname}/ws/websocket";
        }

        Application.targetFrameRate = 120;
    }

    private async void OnDestroy() { await CleanupAsync(); }
    private async void OnApplicationQuit() { await CleanupAsync(); }

    // [중요] 모바일 백그라운드 복귀 감지
    private void OnApplicationPause(bool pauseStatus)
    {
        // pauseStatus가 false면 앱이 다시 켜진 것 (Resume)
        if (!pauseStatus)
        {
            Debug.Log($"Recovered from sleep, is websocket open? : {_webSocket?.State == WebSocketState.Open}");
            if (_webSocket == null || _webSocket.State != WebSocketState.Open)
            {
                _ = ReconnectAsync();
            }
        }
    }

    public async Task ConnectToServer()
    {
        // 이미 연결 중이거나 연결된 상태면 패스
        if (_webSocket != null && _webSocket.State == WebSocketState.Open) return;

        try
        {
            _isReconnecting = true; // 연결 시도 시작
            var token = JWTToken.Token; // 토큰 로직은 형 상황에 맞게 유지
            _userId = Utils.JWTUtils.GetUserId();
            
            if (string.IsNullOrEmpty(token)) 
            {
                Debug.LogWarning("No JWT Token Found. Aborting");
                _isReconnecting = false;
                return;
            }

            await CleanupAsync(); // 기존 연결 확실히 정리

            _webSocket = new ClientWebSocket();
            _cts = new CancellationTokenSource();
            _isDisposed = false;

            long t = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var fullUri = new Uri($"{_serverUri}?token={Uri.EscapeDataString(token)}&t={t}");

            // 타임아웃 설정 (너무 오래 걸리면 끊고 재시도 하게)
            var connectTask = _webSocket.ConnectAsync(fullUri, _cts.Token);
            var timeoutTask = Task.Delay(5000); // 5초 타임아웃
            
            if (await Task.WhenAny(connectTask, timeoutTask) == timeoutTask)
            {
                throw new TimeoutException("Websocket Timed out");
            }
            await connectTask; // 예외가 있으면 여기서 던져짐

            Debug.Log("WebSocket Layer Connected!");

            await SendStompConnect();

            if (!string.IsNullOrEmpty(_userId))
            {
                await SubscribeAsync($"/topic/model3d/{_userId}");
                await SubscribeAsync($"/topic/recommand/{_userId}");
                await SubscribeAsync($"/topic/room3d/{_userId}");
                await SubscribeAsync($"/topic/model-dimensions/{_userId}");
            }
            await SubscribeAsync("/topic/model3d/all");
            await SubscribeAsync("/topic/test");
            await SubscribeAsync("/topic/pong");


            _isReconnecting = false; // 연결 성공!
            _ = ReceiveLoop();
        }
        catch (Exception e)
        {
            Debug.LogError($"Websocket Connection error: {e.Message}");
            _isReconnecting = false;
            // 연결 실패 시 재연결 시도
            _ = ReconnectAsync();
        }
    }

    // [중요] 재연결 전용 로직
    private async Task ReconnectAsync()
    {
        if (_isReconnecting) return; // 이미 재연결 시도 중이면 무시
        _isReconnecting = true;

        Debug.Log("3초 후 재연결을 시도합니다...");
        await Task.Delay(3000); // 딜레이를 줘서 서버 부하 방지

        _isReconnecting = false; // ConnectToServer에서 다시 true로 잡겠지만 여기서 풀어줌
        await ConnectToServer();
    }

    public async Task SubscribeAsync(string destination)
    {
        if (_webSocket?.State != WebSocketState.Open) return;
        string subId = "sub-" + destination.GetHashCode();
        string stompFrame = $"SUBSCRIBE\nid:{subId}\ndestination:{destination}\nack:auto\n\n\0";
        await SendRawMessage(stompFrame);
        Debug.Log($"[STOMP Subscribe]: {destination}");
    }

    public async Task SendMessageToServer(string message)
    {
        if (_webSocket?.State != WebSocketState.Open) 
        {
            Debug.LogWarning("Message Send Failed : Websocket server closed");
            return;
        }
        string stompFrame = $"SEND\ndestination:/app/message\ncontent-type:text/plain\n\n{message}\0";
        await SendRawMessage(stompFrame);
    }

    private async Task SendStompConnect()
    {
        string connectFrame = "CONNECT\naccept-version:1.1,1.0\nheart-beat:10000,10000\n\n\0"; // 하트비트 설정 추가 추천
        await SendRawMessage(connectFrame);
    }

    private async Task SendRawMessage(string rawData)
    {
        try
        {
            byte[] buffer = Encoding.UTF8.GetBytes(rawData);
            await _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, _cts.Token);
        }
        catch (Exception e)
        {
            Debug.LogError($"Send Error: {e.Message}");
            _ = ReconnectAsync();
        }
    }

    private async Task ReceiveLoop()
    {
        byte[] buffer = new byte[1024 * 8];
        try
        {
            while (_webSocket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result;
                using var ms = new MemoryStream();
                do
                {
                    // 여기서 연결이 끊기면 예외 발생
                    result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
                    ms.Write(buffer, 0, result.Count);
                } while (!result.EndOfMessage);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await CleanupAsync();
                    _ = ReconnectAsync(); // 서버에서 끊었을 때 재연결
                    break;
                }

                string rawMessage = Encoding.UTF8.GetString(ms.ToArray());
                ProcessStompMessage(rawMessage);
            }
        }
        catch (Exception e) 
        { 
            if (!_isDisposed) 
            {
                Debug.LogError($"Receive Roop Error, lost connection detected: {e.Message}");
                // 수신 중 에러 발생 시 재연결 시도
                _ = ReconnectAsync();
            }
        }
    }

    private void ProcessStompMessage(string raw)
    {
        // ... (기존 로직 동일)
        // 로깅이 너무 많으면 성능 저하되니 필요할 때만 켜는 게 좋음
        // Debug.Log($"[STOMP Received]: {raw}"); 

        string[] parts = raw.Split(new string[] { "\n\n" }, 2, StringSplitOptions.None);
        if (parts.Length < 2) return;

        string headers = parts[0];
        string body = parts[1].TrimEnd('\0');

        Debug.Log($"[WebSocket Receieved Body] {body}");

        if (headers.Contains("destination:/topic/model-dimensions/"))
        {
            UnityMainThreadDispatcher.Enqueue(() => OnModel3DDimensionsByImageReceived?.Invoke(body));
            return;
        }

        if (body.Contains("MODEL_GENERATION_SUCCESS"))
        {
            UnityMainThreadDispatcher.Enqueue(() => {
                OnModel3DGenerated?.Invoke(body);
                Generate3DPresenter.GenerateProcessingModelID = -1; // 처리 완료 후 ID 초기화
            });
        }
        
        if (body.Contains("MODEL_GENERATION_FAILED"))
        {
            UnityMainThreadDispatcher.Enqueue(() => {
                OnModel3DGenerateFailed?.Invoke(body);
                Generate3DPresenter.GenerateProcessingModelID = -1; // 처리 완료 후 ID 초기화
            });
        }

        if (body.Contains("roomAnalysis"))
        {
            UnityMainThreadDispatcher.Enqueue(() => {
                OnAIRecommendReceived?.Invoke(body);
            });
        }

        if (body.Contains("ROOM3D_GENERATION_SUCCESS"))
        {
            UnityMainThreadDispatcher.Enqueue(() => OnRoom3DGenerationSuccess?.Invoke(body));
        }

        if (body.Contains("ROOM3D_GENERATION_FAILED"))
        {
            UnityMainThreadDispatcher.Enqueue(() => OnRoom3DGenerationFailed?.Invoke(body));
        }

        if (body.Contains("ROOM3D_GENERATION_PROGRESS"))
        {
            UnityMainThreadDispatcher.Enqueue(() => OnRoom3DGenerationProgress?.Invoke(body));
        }
    }

    private async Task CleanupAsync()
    {
        if (_webSocket == null) return;
        _isDisposed = true;
        try
        {
            if (_webSocket.State == WebSocketState.Open || _webSocket.State == WebSocketState.CloseReceived)
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
        }
        catch { /* 닫을 때 나는 에러는 무시 */ }
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