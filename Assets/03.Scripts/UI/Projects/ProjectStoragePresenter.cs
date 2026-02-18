using UnityEngine;
using UnityEngine.SceneManagement;

public class ProjectStoragePresenter
{
    private IProjectStorageView _view;

    public ProjectStoragePresenter(IProjectStorageView view)
    {
        _view = view;
    }

    public void OnToHomeClicked()
    {
        SceneManager.LoadScene("Home");
    }
}
