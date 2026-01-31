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

    //--- Public Methods ---//
    public void ReConnect()
    {
        ConnectToServer();
    }

    public async Task ConnectToServer()
    {
        _webSocket = new ClientWebSocket();
        _cts = new CancellationTokenSource();

        try
        {
            await _webSocket.ConnectAsync(new Uri(_serverUri + JWTToken.Token), _cts.Token);
            Debug.Log("웹소켓 연결 성공!");

            // 메시지 수신 루프 시작 (별도 Task)
            _ = ReceiveLoop();
        }
        catch (Exception e)
        {
            Debug.LogError($"연결 에러: {e.Message}");
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
                    UnityMainThreadDispatcher.Enqueue(() =>
                    {
                        // 여기에 메인 스레드에서 실행할 코드를 작성
                    });
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"수신 에러: {e.Message}");
        }
    }

}
