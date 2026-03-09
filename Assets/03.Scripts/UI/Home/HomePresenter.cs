using UnityEngine;
public class HomePresenter
{
    private readonly IHomeView _view;

    public HomePresenter(IHomeView view)
    {
        _view = view;
    }

    public void OnToGenerate3DClicked()
    {
        Debug.Log("Navigate to 3D Generation Scene");
        Utils.SceneHistory.ChangeScene("Generate3D");
    }

    public void OnToProjectsClicked()
    {
        Debug.Log("Navigate to Projects Scene");
        Utils.SceneHistory.ChangeScene("Projects");
    }

    public void OnToCommunityClicked()
    {
        Debug.Log("Navigate to Community Scene");
        Utils.SceneHistory.ChangeScene("Community");
    }
}
