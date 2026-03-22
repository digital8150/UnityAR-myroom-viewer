using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ARGuidePresenter
{
    private readonly ARGuideView _view;
    private readonly List<ARGuideModel> _guides;
    private Coroutine _transitionCoroutine;

    public ARGuidePresenter(ARGuideView view, List<ARGuideModel> models)
    {
        _view = view;
        _guides = models;
    }

    public void StartGuideTransitions()
    {
        if (_guides == null || _guides.Count == 0) return;

        if (_transitionCoroutine != null)
        {
            _view.StopCoroutine(_transitionCoroutine);
        }

        _transitionCoroutine = _view.StartCoroutine(CoGuideSequence());
    }

    public void ShowFirstStartupGuide()
    {
        if (PlayerPrefs.GetInt("IsFirstAR", -1) == 1) return;
        StartGuideTransitions();
        PlayerPrefs.SetInt("IsFirstAR", 1);

    }

    private IEnumerator CoGuideSequence()
    {
        foreach (var guide in _guides)
        {
            yield return new WaitForSeconds(_view.IntervalDuration);
            _view.SetIconImage(guide._guideIcon);
            _view.SetGuideText(guide._guideDescription);

            // View의 인스펙터 설정을 그대로 사용
            _view.DoCanvasAlphaTransition(1f, _view.FadeInDuration);
            yield return new WaitForSeconds(_view.DisplayDuration);

            _view.DoCanvasAlphaTransition(0f, _view.FadeOutDuration);
            yield return new WaitForSeconds(_view.FadeOutDuration);

        }

        _transitionCoroutine = null;
    }
}