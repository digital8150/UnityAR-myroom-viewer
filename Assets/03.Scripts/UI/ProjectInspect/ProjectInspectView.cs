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
    [SerializeField] private TMP_InputField _sizeInputField;
    [SerializeField] private TMP_InputField _webSiteInputField;
    [SerializeField] private TMP_InputField _nameInputField;
    [SerializeField] private TMP_InputField _descriptionInputField;
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
        _returnBtn.onClick.AddListener(_presenter.OnReturnClicked);
        _reTryBtn.onClick.AddListener(_presenter.OnRetryClicked);
    }

    private void OnDestroy()
    {
        _returnBtn.onClick.RemoveAllListeners();
        _reTryBtn.onClick.RemoveAllListeners();
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

    public void SetSizeInputField(string content)
    {
        if(_sizeInputField)
        {
            _sizeInputField.text = content;
        }
    }

    public string GetSizeInputField()
    {
        if(_sizeInputField)
        {
            return _sizeInputField.text;
        }
        return string.Empty;
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

    //--- Failed Page ---//
    public void SetFailReason(string reason)
    {
        if (_generateFailReasonText == null) return;
        _generateFailReasonText.text = reason;
    }


}
