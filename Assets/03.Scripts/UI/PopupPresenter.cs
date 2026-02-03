using UnityEngine;

public class PopupPresenter
{
    IPopupView _view;

    public event System.Action OnPopupYesClicked;
    public event System.Action OnPopupNoClicked;

    public PopupPresenter(IPopupView view)
    {
        _view = view;
    }

    // PopupPresenter.cs 수정
    public void ShowYesNo(string msg, System.Action onYes, System.Action onNo = null)
    {
        // 기존에 등록된 이벤트가 있다면 초기화 (안전장치)
        OnPopupYesClicked = null;
        OnPopupNoClicked = null;

        if (onYes != null) OnPopupYesClicked += onYes;
        if (onNo != null) OnPopupNoClicked += onNo;

        _view.ShowYesNoMessage(msg); // View에게 보여달라고 요청
    }

    public void OnCloseMessageClicked()
    {
        _view.CloseMessage();
    }

    public void OnYesClicked()
    {
        _view.CloseMessage();
        OnPopupYesClicked?.Invoke();
    }

    public void OnNoClicked()
    {
        _view.CloseMessage();
        OnPopupNoClicked?.Invoke();
    }
}
