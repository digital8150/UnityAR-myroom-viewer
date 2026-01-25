using TMPro;
using UnityEngine;

public class SendButton : MonoBehaviour
{
    //--- Settings ---//
    [Header("외부 컴포넌트 종속성")]
    [SerializeField] private TMP_InputField _inputField;
    [SerializeField] private WebSocketController _webSocketController;

    /// <summary>
    /// 버튼 클릭시 호출, 서버로 메세지 전송
    /// </summary>
    public void OnSendButtonClicked()
    {
        string message = _inputField?.text;
        if (_webSocketController != null)
        {
            _webSocketController.SendMessageToServer(message);
        }
        else
        {
            Debug.LogWarning("WebSocketController를 찾을 수 없습니다.");
        }
    }
}
