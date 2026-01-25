using UnityEngine;

public class HTTPPresenter
{
    private const string HealthCheckEndpoint = "http://localhost:5000/api";
    private HealthCheckResponse _healthCheckModel;
    private HTTPMainView _mainView;

    public HTTPPresenter(HealthCheckResponse model, HTTPMainView view)
    {
        _healthCheckModel = model;
        _mainView = view;
    }

    public void OnClickGetHealthCheck()
    {
        _mainView.UpdateLog($"베이스 엔드포인트 주소 : {HealthCheckEndpoint}");
        HTTPRequest httpRequest = new HTTPRequest(HealthCheckEndpoint, _mainView);
        httpRequest.GetHealthCheck(
            onSuccess: (Response) =>
            {
                _mainView.UpdateLog($"헬스 체크 응답을 수신했습니다");
                _healthCheckModel = Response;
                _mainView.UpdateLog($"Status : {_healthCheckModel.status}");
                _mainView.UpdateLog($"Timestamp : {_healthCheckModel.timestamp}");
                _mainView.UpdateLog($"Version : {_healthCheckModel.version}");
            },
            onError : (ErrorMessage)=>
            {
                _mainView.UpdateLog($"헬스 체크 요청 중 오류가 발생했습니다: {ErrorMessage}");
            }
        );
    }
}
