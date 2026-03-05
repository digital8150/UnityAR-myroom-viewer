using TMPro;
using UnityEngine;
using UnityEngine.UI.ProceduralImage;
using UnityEngine.UI;

public class ProjectInspectView : MonoBehaviour
{
    [Header("General")]
    [SerializeField] private Button _returnBtn;

    [Header("Pages")]
    [SerializeField] private GameObject _page3Done;
    [SerializeField] private GameObject _page4Failed;

    [Header("Page 3 : Done Page")]
    [SerializeField] private ProceduralImage _doneImage;
    [SerializeField] private TMP_InputField _widthInputField;
    [SerializeField] private TMP_InputField _heightInputField;
    [SerializeField] private TMP_InputField _lengthInputField;
    [SerializeField] private TMP_InputField _webSiteInputField;
    [SerializeField] private TMP_InputField _nameInputField;
    [SerializeField] private TMP_InputField _descriptionInputField;
    [SerializeField] private Toggle _isPublicToggle;
    [SerializeField] private Button _saveButton;
    [SerializeField] private Button _onArPlaceButton;

    [Header("Page 4 : Failed Page")]
    [SerializeField] private Button _reTryBtn;
    [SerializeField] private TextMeshProUGUI _generateFailReasonText;

    private ProjectInspectPresenter _presenter;



    private void Awake()
    {
        _presenter = new ProjectInspectPresenter(this);
    }

    private void Start()
    {
        _presenter.InitializeView();
        if(_returnBtn) _returnBtn.onClick.AddListener(_presenter.OnReturnClicked);
        if(_reTryBtn) _reTryBtn.onClick.AddListener(_presenter.OnRetryClicked);
        if(_saveButton) _saveButton.onClick.AddListener(_presenter.OnSaveClicked);
    }

    private void OnDestroy()
    {
        if(_returnBtn) _returnBtn.onClick.RemoveAllListeners();
        if(_reTryBtn) _reTryBtn.onClick.RemoveAllListeners();
        if(_saveButton) _saveButton.onClick.RemoveAllListeners();
    }

    //--- Page Control ---//
    public void ShowDonePage()
    {
        if (_page3Done == null)
        {
            return;
        }
        HideAllPage();
        _page3Done?.SetActive(true);
    }

    public void ShowFailedPage()
    {
        if (_page4Failed == null)
        {
            return;
        }
        HideAllPage();
        _page4Failed?.SetActive(true);
    }

    public void HideAllPage()
    {
        if (_page3Done) _page3Done.SetActive(false);
        if (_page4Failed) _page4Failed.SetActive(false);
    }

    //--- Done Page ---//
    public void SetDoneImage(Sprite sprite)
    {
        if (_doneImage != null)
        {
            _doneImage.sprite = sprite;
        }
    }

    public void SetSizeInputField(ModelDimension modelDimension)
    {
        if(_widthInputField)
        {
            _widthInputField.text = modelDimension.width.ToString();
        }
        if(_heightInputField)
        {
            _heightInputField.text = modelDimension.height.ToString();
        }
        if(_lengthInputField)
        {
            _lengthInputField.text = modelDimension.length.ToString();
        }
    }

    public ModelDimension GetSizeInputField()
    {
        ModelDimension dimension = new ModelDimension();
        if(_widthInputField && float.TryParse(_widthInputField.text, out float width))
        {
            dimension.width = width;
        }
        if(_heightInputField && float.TryParse(_heightInputField.text, out float height))
        {
            dimension.height = height;
        }
        if(_lengthInputField && float.TryParse(_lengthInputField.text, out float length))
        {
            dimension.length = length;
        }
        return dimension;
    }

    public void SetWebsiteInputField(string content)
    {
        if(_webSiteInputField)
        {
            _webSiteInputField.text = content;
        }
    }

    public string GetWebsiteInputField()
    {
        if(_webSiteInputField)
        {
            return _webSiteInputField.text;
        }
        return string.Empty;
    }

    public void SetNameInputField(string content)
    {
        if(_nameInputField)
        {
            _nameInputField.text = content;
        }
    }

    public string GetNameInputField()
    {
        if(_nameInputField)
        {
            return _nameInputField.text;
        }
        return string.Empty;
    }

    public void SetDescriptionInputField(string content)
    {
        if(_descriptionInputField)
        {
            _descriptionInputField.text = content;
        }
    }

    public string GetDescriptionInputField()
    {
        if(_descriptionInputField)
        {
            return _descriptionInputField.text;
        }
        return string.Empty;
    }

    public void SetIsPublicToggle(bool isOn)
    {
        if(_isPublicToggle)
        {
            _isPublicToggle.isOn = isOn;
        }
    }

    public bool GetIsPublicToggle()
    {
        if(_isPublicToggle)
        {
            return _isPublicToggle.isOn;
        }
        return false;
    }

    //--- Failed Page ---//
    public void SetFailReason(string reason)
    {
        if (_generateFailReasonText == null) return;
        _generateFailReasonText.text = reason;
    }


}
