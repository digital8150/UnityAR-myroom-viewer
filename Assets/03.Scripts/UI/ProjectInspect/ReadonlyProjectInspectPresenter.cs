using System;
using UnityEngine;

public class ReadonlyProjectInspectPresenter
{
    private readonly ReadonlyProjectInspectView _view;
    private int _modelId = -1;
    private bool _isBookmarked = false;

    public ReadonlyProjectInspectPresenter(ReadonlyProjectInspectView view)
    {
        _view = view;
    }

    public async void Initialize(int modelId)
    {
        _modelId = modelId;

        var (responseCode, isBookmarked) = await ModelService.GetBookmarkStatus(_modelId);
        if (responseCode == 200)
        {
            _isBookmarked = isBookmarked;
            _view.SetBookmarkText(_isBookmarked);
            _view.SetBookmarkButtonListener(OnBookmarkButtonClicked);
        }
    }

    private async void OnBookmarkButtonClicked()
    {
        if (_modelId <= 0) return;

        if (_isBookmarked)
        {
            long responseCode = await ModelService.RemoveBookmark(_modelId);
            if (responseCode == 200)
            {
                _isBookmarked = false;
                _view.SetBookmarkText(_isBookmarked);
            }
            else
            {
                Debug.LogError($"[ReadonlyProjectInspectPresenter] Failed to remove bookmark. Code: {responseCode}");
            }
        }
        else
        {
            long responseCode = await ModelService.AddBookmark(_modelId);
            if (responseCode == 200)
            {
                _isBookmarked = true;
                _view.SetBookmarkText(_isBookmarked);
            }
            else
            {
                Debug.LogError($"[ReadonlyProjectInspectPresenter] Failed to add bookmark. Code: {responseCode}");
            }
        }
    }
}
