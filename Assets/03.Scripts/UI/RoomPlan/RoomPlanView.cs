using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RoomPlanView : MonoBehaviour
{
    [Serializable]
    internal class CategoryButton
    {
        public string CategoryName;
        public Button Button;
    }

    [Header("Page1 : ProjectList")]
    [SerializeField] private GameObject _projectListPage;
    [SerializeField] private ProjectCardView _projectCardPrefab;
    [SerializeField] private Transform _projectCardContainer;
    [SerializeField] private Button _newProjectButton;
    [Space(10)]
    [SerializeField] private GameObject _modalPanel;
    [SerializeField] private Button _modalCancleButton;
    [SerializeField] private Button _modalSelectDefaultRoomButton;
    [SerializeField] private Button _modalLoadFloorButton;

    [Header("PlayGround : Edit")]
    [SerializeField] private GameObject _editPage;
    [SerializeField] private RawImage _viewportImage;
    [SerializeField] private TouchView _viewportTouch;
    [SerializeField] private Camera _viewportCamera;
    [SerializeField] private Transform _viewportCameraTarget;
    [SerializeField] private List<CategoryButton> _categoryButtons;
    [SerializeField] private Button _backButton2;
    [SerializeField] private Material _defaultWallMaterial;


    [Header("Common")]
    [SerializeField] private Button _backButton;

    private RoomPlanPresenter _presenter;


    #region Unity Life Cycle
    private void Awake()
    {
        _presenter = new RoomPlanPresenter(this, _viewportTouch, _defaultWallMaterial);
    }
    #endregion


    #region Public Methods
    public void SetActiveProjectListPage(bool isActive)
    {
        if (_projectListPage == null)
        {
            Debug.LogWarning("RoomPlanView: _projectListPage is null.");
            return;
        }
        _projectListPage.SetActive(isActive);
    }

    public void SetActiveEditPage(bool isActive)
    {
        if (_editPage == null)
        {
            Debug.LogWarning("RoomPlanView: _editPage is null.");
            return;
        }
        _editPage.SetActive(isActive);
    }

    public ProjectCardView CreateProjectCard()
    {
        if (_projectCardPrefab == null || _projectCardContainer == null)
        {
            Debug.LogWarning("RoomPlanView: Prefab or Container is missing.");
            return null;
        }
        return Instantiate(_projectCardPrefab, _projectCardContainer);
    }

    public void ClearProjectCards()
    {
        if (_projectCardContainer == null) return;

        foreach (Transform child in _projectCardContainer)
        {
            Destroy(child.gameObject);
        }
    }

    public void SetActiveModal(bool isActive)
    {
        if (_modalPanel == null)
        {
            Debug.LogWarning("RoomPlanView: _modalPanel is null.");
            return;
        }
        _modalPanel.SetActive(isActive);
    }

    public void SetButtonActions(UnityAction onNewProject, UnityAction onBack)
    {
        SetButton(_newProjectButton, onNewProject);
        SetButton(_backButton, onBack);
        SetButton(_backButton2, onBack);
    }

    public void SetModalActions(UnityAction onCancel, UnityAction onSelectDefault, UnityAction onLoadFloor)
    {
        SetButton(_modalCancleButton, onCancel);
        SetButton(_modalSelectDefaultRoomButton, onSelectDefault);
        SetButton(_modalLoadFloorButton, onLoadFloor);
    }

    public void SetRenderTextureWithBind(RenderTexture renderTexture)
    {
        if (_viewportImage == null)
        {
            Debug.LogWarning("RoomPlanView: _viewportImage is null.");
            return;
        }
        _viewportImage.texture = renderTexture;

        if(_viewportCamera == null)
        {
            Debug.LogWarning("RoomPlanView: _viewportCamera is null.");
            return;
        }
        _viewportCamera.targetTexture = renderTexture;
    }

    public void SetCategoryButtonAction(string category, UnityAction onClick)
    {
        Button button = _categoryButtons.Find(c => c.CategoryName == category)?.Button;
        if (button == null)
        {
            Debug.LogWarning($"RoomPlanView: No button found for category '{category}'.");
            return;
        }
        SetButton(button, onClick);
    }

    public void SetViewPortCameraPosition(Vector3 position)
    {
        if(!_viewportCameraTarget)
        {
            Debug.LogError("Error : RoomPlanView: _viewportCameraTarget reference is missing.");
            return;
        }
        _viewportCameraTarget.position = position;
    }

    public void SetViewPortCameraRotation(Vector3 rotation)
    {
        if (!_viewportCameraTarget)
        {
            Debug.LogError("Error : RoomPlanView: _viewportCameraTarget reference is missing.");
            return;
        }
        _viewportCameraTarget.rotation = Quaternion.Euler(rotation);
    }

    public (Vector3 position, Vector3 rotation) GetViewPortCameraTransform()
    {
        if (!_viewportCameraTarget)
        {
            Debug.LogError("Error : RoomPlanView: _viewportCameraTarget reference is missing.");
            return (Vector3.zero, Vector3.zero);
        }
        return (_viewportCameraTarget.position, _viewportCameraTarget.rotation.eulerAngles);
    }

    #endregion

    #region Private Methods

    private void SetButton(Button button, UnityAction action)
    {
        if (button == null)
        {
            Debug.LogWarning($"RoomPlanView: {button} reference is missing.");
            return;
        }

        button.onClick.RemoveAllListeners();
        if (action != null)
        {
            button.onClick.AddListener(action);
        }
    }

    #endregion
}
