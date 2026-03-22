using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ARGuideView : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private CanvasGroup _bgCanvasGroup;
    [SerializeField] private Image _iconImage;
    [SerializeField] private TextMeshProUGUI _guideText;
    [SerializeField] private Button _showGuideButton;

    [Header("Transition Settings")]
    [SerializeField] private float _fadeInDuration = 0.5f;
    [SerializeField] private float _fadeOutDuration = 0.5f;
    [SerializeField] private float _displayDuration = 2.0f;
    [SerializeField] private float _intervalDuration = 0.6f;

    [Header("Guides")]
    [SerializeField] private List<ARGuideModel> _models;

    // Presenter에서 가져다 쓸 수 있게 프로퍼티 제공
    public float FadeInDuration => _fadeInDuration;
    public float FadeOutDuration => _fadeOutDuration;
    public float DisplayDuration => _displayDuration;
    public float IntervalDuration => _intervalDuration;

    private Coroutine _alphaCoroutine;
    private ARGuidePresenter _presenter;

    private void Awake()
    {
        _presenter = new ARGuidePresenter(this, _models);
    }

    private void Start()
    {
        _presenter.ShowFirstStartupGuide();
        if (_showGuideButton) _showGuideButton.onClick.AddListener(_presenter.StartGuideTransitions);
    }

    private void OnDestroy()
    {
        if(_showGuideButton) _showGuideButton.onClick?.RemoveListener(_presenter.StartGuideTransitions);
    }

    public void SetIconImage(Sprite icon)
    {
        if (_iconImage != null)
        {
            _iconImage.sprite = icon;
        }
    }

    public void SetGuideText(string text)
    {
        if (_guideText != null)
        {
            _guideText.text = text;
        }
    }

    public void DoCanvasAlphaTransition(float targetAlpha, float duration)
    {
        if (_bgCanvasGroup == null)
        {
            return;
        }

        // 실행 중인 코루틴이 있다면 중복 방지를 위해 정지
        if (_alphaCoroutine != null)
        {
            StopCoroutine(_alphaCoroutine);
        }

        _alphaCoroutine = StartCoroutine(FadeCanvasGroup(targetAlpha, duration));
    }

    private IEnumerator FadeCanvasGroup(float targetAlpha, float duration)
    {
        float startAlpha = _bgCanvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _bgCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }

        _bgCanvasGroup.alpha = targetAlpha;
        _alphaCoroutine = null;
    }
}