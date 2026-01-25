using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class HTTPMainView : MonoBehaviour
{
    //--- Settings ---//
    [SerializeField]
    private TextMeshProUGUI _logText;
    [SerializeField]
    private Button _getHealthCheckButton;

    //--- Fields ---//
    private HTTPPresenter _presenter;
    private UnityAction _onHealthCheckClick;

    //--- Unity Methods ---//
    private void Awake()
    {
        if (IsCheckFailed())
        {
            return;
        }

        InitializeView();
    }

    private void OnDestroy()
    {
        if(_getHealthCheckButton != null && _onHealthCheckClick != null)
        {
            _getHealthCheckButton.onClick.RemoveListener(_onHealthCheckClick);
        }
    }

    //--- Public Methods ---//
    /// <summary>
    /// Log 텍스트 박스에 메세지를 추가합니다.
    /// </summary>
    /// <param name="message">추가할 내용</param>
    public void UpdateLog(string message)
    {
        _logText.text += $"{DateTime.Now} : {message}\n";
    }

    //--- Private Methods ---//
    bool IsCheckFailed()
    {
        if(_logText == null || _getHealthCheckButton == null)
        {
            Debug.LogError($"{nameof(HTTPMainView)} : UI 레퍼런스가 할당되지 않았습니다.");
            enabled = false;
            return true;
        }
        return false;
    }

    private void InitializeView()
    {
        var model = new HealthCheckResponse();
        _presenter = new HTTPPresenter(model, this);
        _onHealthCheckClick = _presenter.OnClickGetHealthCheck;
        _getHealthCheckButton.onClick.AddListener(_onHealthCheckClick);
    }
}
