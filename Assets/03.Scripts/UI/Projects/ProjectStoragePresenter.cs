using UnityEngine;

public class ProjectStoragePresenter
{
    private IProjectStorageView _view;

    public ProjectStoragePresenter(IProjectStorageView view)
    {
        _view = view;
    }

    public void OnToHomeClicked()
    {
        Utils.SceneHistory.BackToPrevious();
    }
}
