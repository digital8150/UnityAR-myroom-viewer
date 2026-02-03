using UnityEngine;

public class PopupPresenter
{
    IPopupView _view;

    public PopupPresenter(IPopupView view)
    {
        _view = view;
    }

    public void OnCloseMessageClicked()
    {
        _view.CloseMessage();
    }
}
