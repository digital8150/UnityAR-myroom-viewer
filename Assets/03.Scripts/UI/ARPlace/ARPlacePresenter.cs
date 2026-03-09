using UnityEngine;
using UnityEngine.SceneManagement;

public class ARPlacePresenter
{
    ARPlaceView _view;

    public ARPlacePresenter(ARPlaceView view)
    {
        _view = view;
    }

    //--- Button Handler ---//
    public void OnCancleButtonClicked()
    {
        Utils.SceneHistory.BackToPrevious();
    }

    public void OnShutterButtonClicked()
    {
        Debug.Log("Shutter Button Clicked");
    }
}
