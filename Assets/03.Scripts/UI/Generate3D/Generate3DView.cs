using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class ImageSlotUI
{
    public Button addButton;
    public Button removeButton;
    public RawImage previewImage;
    public GameObject emptyState;
    public GameObject filledState;
}

public class Generate3DView : MonoBehaviour
{
    [Header("Pages")]
    [SerializeField] private GameObject _page1LandingPage;
    [SerializeField] private GameObject _page2ImageSelection;

    [Header("Common")]
    [SerializeField] private Button _returnBtn;

    [Header("Page 1 : Landing Page")]
    [SerializeField] private Button _loadImageBtn;
    [SerializeField] private Button _takePictureBtn;

    [Header("Page 2 : Image Selection")]
    [SerializeField] private ImageSlotUI[] _imageSlots = new ImageSlotUI[4];
    [SerializeField] private Button _generateBtn;
    [SerializeField] private Button _backToLandingBtn;

    private Generate3DPresenter _presenter;

    private void Awake()
    {
        InitializeView();
    }

    private void Start()
    {
        ShowLandingPage();
    }

    private void OnDestroy()
    {
        DisposeListeners();
    }

    public void ShowLandingPage()
    {
        HideAllPage();
        _page1LandingPage?.SetActive(true);
    }

    public void ShowImageSelectionPage()
    {
        HideAllPage();
        _page2ImageSelection?.SetActive(true);
    }

    public void SetSlotPreview(int index, Texture2D texture)
    {
        if (!IsValidSlot(index)) return;
        var slot = _imageSlots[index];
        if (slot.previewImage != null) slot.previewImage.texture = texture;
        slot.emptyState?.SetActive(false);
        slot.filledState?.SetActive(true);
    }

    public void ClearSlotPreview(int index)
    {
        if (!IsValidSlot(index)) return;
        var slot = _imageSlots[index];
        if (slot.previewImage != null && slot.previewImage.texture != null)
        {
            Destroy(slot.previewImage.texture);
            slot.previewImage.texture = null;
        }
        slot.emptyState?.SetActive(true);
        slot.filledState?.SetActive(false);
    }

    public void SetGenerateButtonInteractable(bool interactable)
    {
        if (_generateBtn) _generateBtn.interactable = interactable;
    }

    private void InitializeView()
    {
        _presenter = new Generate3DPresenter(this);
        _returnBtn?.onClick.AddListener(_presenter.OnReturnClicked);
        _loadImageBtn?.onClick.AddListener(_presenter.OnEnterImageSelectionClicked);
        _takePictureBtn?.onClick.AddListener(_presenter.OnTakePictureClicked);
        _backToLandingBtn?.onClick.AddListener(_presenter.OnBackToLandingClicked);
        _generateBtn?.onClick.AddListener(_presenter.OnGenerateClicked);

        for (int i = 0; i < _imageSlots.Length; i++)
        {
            if (_imageSlots[i] == null) continue;
            int idx = i;
            _imageSlots[i].addButton?.onClick.AddListener(() => _presenter.OnAddImageClicked(idx));
            _imageSlots[i].removeButton?.onClick.AddListener(() => _presenter.OnRemoveImageClicked(idx));
            ClearSlotPreview(i);
        }

        SetGenerateButtonInteractable(false);
    }

    private void DisposeListeners()
    {
        _returnBtn?.onClick.RemoveAllListeners();
        _loadImageBtn?.onClick.RemoveAllListeners();
        _takePictureBtn?.onClick.RemoveAllListeners();
        _backToLandingBtn?.onClick.RemoveAllListeners();
        _generateBtn?.onClick.RemoveAllListeners();

        foreach (var slot in _imageSlots)
        {
            if (slot == null) continue;
            slot.addButton?.onClick.RemoveAllListeners();
            slot.removeButton?.onClick.RemoveAllListeners();
        }
    }

    private bool IsValidSlot(int index)
    {
        return index >= 0 && index < _imageSlots.Length && _imageSlots[index] != null;
    }

    private void HideAllPage()
    {
        if (_page1LandingPage) _page1LandingPage.SetActive(false);
        if (_page2ImageSelection) _page2ImageSelection.SetActive(false);
    }

    private IEnumerator LerpRectTransformRightOffset(RectTransform rectTransform, float targetRightOffset, float duration)
    {
        float elapsed = 0f;
        float initialRightOffset = rectTransform.offsetMax.x;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float newRightOffset = Mathf.Lerp(initialRightOffset, targetRightOffset, elapsed / duration);
            rectTransform.offsetMax = new Vector2(newRightOffset, rectTransform.offsetMax.y);
            yield return null;
        }
        rectTransform.offsetMax = new Vector2(targetRightOffset, rectTransform.offsetMax.y);
    }
}
