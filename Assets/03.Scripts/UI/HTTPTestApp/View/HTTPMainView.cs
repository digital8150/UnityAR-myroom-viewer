using NUnit.Framework;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HTTPMainView : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI _logText;
    [SerializeField]
    private Button _getHealthCheckButton;

    private HTTPPresenter _presenter;

    private void Awake()
    {
        var model = new HealthCheckResponse();
        _presenter = new HTTPPresenter(model, this);

        _getHealthCheckButton.onClick.AddListener(() => _presenter.OnClickGetHealthCheck());
    }

    public void UpdateLog(string message)
    {
        _logText.text += $"{DateTime.Now} : {message}\n";
    }
}
