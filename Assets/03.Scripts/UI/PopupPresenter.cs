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
