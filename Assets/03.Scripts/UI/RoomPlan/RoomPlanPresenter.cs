using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using GLTFast;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class RoomPlanPresenter
{
    private const int MODELS_PER_PAGE = 10;

    private readonly RoomPlanView _view;
    private readonly TouchView _touchView;
    private string _imagePath = null;

    private Vector3 _currentPosition;
    private float _currentRotationY;

    private int _modelPageIndex = 0;
    private bool _isModelLoading = false;
    private bool _isLastModelPage = false;
    private RoomPlanModelButtonView _selectedModelButton = null;

    // 다운로드 완료 후 탭 대기 중인 모델의 로컬 경로
    private string _pendingLocalPath = null;
    // 씬에 배치 완료된 모델 목록
    private readonly List<GameObject> _placedModels = new List<GameObject>();
    // 바닥 판정용 보이지 않는 Collider
    private GameObject _floorColliderGo = null;

    public RoomPlanPresenter(RoomPlanView view, TouchView touchView, Material defaultWallMaterial)
    {
        _view = view;
        _touchView = touchView;
        Builder.wallMat = defaultWallMaterial;

        _touchView.OnSingleDrag += HandlePositionUpdate;
        _touchView.OnDoubleDrag += HandleRotationUpdate;
        _touchView.OnTap += OnViewportTap;

        var (initPos, initRot) = _view.GetViewPortCameraTransform();
        _currentPosition = initPos;
        _currentRotationY = initRot.y;

        InitView();
    }

    private void InitView()
    {
        ShowProjectListPage();

        _view.SetButtonActions(
            onBack: OnBackButtonClicked,
            onNewProject: OnNewProjectButtonClicked
        );

        _view.SetModalActions(
            onCancel: OnModalCancelClicked,
            onSelectDefault: OnModalSelectDefaultClicked,
            onLoadFloor: OnModalLoadFloorClicked
        );
    }

    #region Page Controls
    private void HideAllPages()
    {
        _view.SetActiveModal(false);
        _view.SetActiveProjectListPage(false);
        _view.SetActiveEditPage(false);
    }

    private void ShowProjectListPage()
    {
        HideAllPages();
        _view.SetActiveProjectListPage(true);
        ClearAllPlaced();
        DestroyFloorCollider();
        LoadProjectList();
    }

    private void ShowEditPage()
    {
        HideAllPages();
        _view.SetActiveEditPage(true);
        _view.SetModelScrollListener(OnModelScrollChanged);
        CreateFloorCollider();
        ResetModelList();
    }
    #endregion

    #region Button Actions
    private void OnBackButtonClicked()
    {
        Utils.SceneHistory.BackToPrevious();
    }

    private void OnNewProjectButtonClicked()
    {
        _view.SetActiveModal(true);
    }

    private void OnModalCancelClicked()
    {
        _view.SetActiveModal(false);
    }

    private void OnModalSelectDefaultClicked()
    {
        _view.SetActiveModal(false);
        //TODO : 기본 도면으로 방 생성 플로우로 이동
    }

    private void OnModalLoadFloorClicked()
    {
        _view.SetActiveModal(false);

        if (NativeFilePicker.IsFilePickerBusy())
        {
            PopupView.Instance.ShowMessage("파일 선택기가 이미 사용 중입니다. 잠시 후 다시 시도해주세요.");
            return;
        }

        try
        {
            NativeFilePicker.PickFile((path) =>
            {
                if (path == null) return;

                if (!IsValidFileExtension(path))
                {
                    PopupView.Instance.ShowMessage("유효하지 않은 파일 형식입니다. PNG, JPG, JPEG 파일만 선택하세요.");
                    return;
                }

                _imagePath = path;
                PopupView.Instance.Presenter.ShowYesNo(
                    "선택한 이미지로 방을 생성하시겠습니까?",
                    OnUserConfirmedGeneration,
                    OnUserDeniedGeneration
                );
            });
        }
        catch (System.Exception e)
        {
            PopupView.Instance.ShowMessage($"파일 선택 중 오류가 발생했습니다: {e.Message}");
        }
    }
    #endregion

    #region Project List
    private async void LoadProjectList()
    {
        _view.ClearProjectCards();
        _view.SetActiveProjectListEmpty(false);

        var (code, json) = await RoomPlanService.GetMyRoom3DList();

        if (code != 200 || string.IsNullOrEmpty(json))
        {
            Debug.LogWarning($"프로젝트 목록 조회 실패 ({code})");
            _view.SetActiveProjectListEmpty(true);
            return;
        }

        var page = JsonConvert.DeserializeObject<PageDto<Room3DDto>>(json);
        if (page?.content == null || page.content.Count == 0)
        {
            _view.SetActiveProjectListEmpty(true);
            return;
        }

        foreach (var room in page.content)
        {
            var card = _view.CreateProjectCard();
            if (card == null) continue;

            card.SetCardText(room.roomName);
            var captured = room;
            card.SetButtonAction(() => OnProjectCardClicked(captured));

            if (!string.IsNullOrEmpty(room.drawingImageUrl))
                LoadCardImage(card, room.drawingImageUrl);
        }
    }

    private async void LoadCardImage(ProjectCardView card, string imageUrl)
    {
        using var request = UnityWebRequestTexture.GetTexture(imageUrl);
        await request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success) return;

        var texture = DownloadHandlerTexture.GetContent(request);
        var sprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f)
        );
        card.SetCardImage(sprite);
    }

    private async void OnProjectCardClicked(Room3DDto room)
    {
        if (string.IsNullOrEmpty(room.drawingXmlUrl))
        {
            PopupView.Instance.ShowMessage("저장된 도면 XML이 없습니다.");
            return;
        }

        PopupView.Instance.SetLoadingPannelActive(true);
        string xmlContent = await FetchText(room.drawingXmlUrl);
        PopupView.Instance.SetLoadingPannelActive(false);

        if (string.IsNullOrEmpty(xmlContent))
        {
            PopupView.Instance.ShowMessage("도면 데이터를 불러오는 데 실패했습니다.");
            return;
        }

        ConstructRoom(xmlContent);
        ShowEditPage();
    }
    #endregion

    #region Model List

    private void ResetModelList()
    {
        _modelPageIndex = 0;
        _isModelLoading = false;
        _isLastModelPage = false;
        _selectedModelButton = null;
        _view.ClearModelButtons();
        LoadNextModelPage();
    }

    private async void LoadNextModelPage()
    {
        if (_isModelLoading || _isLastModelPage) return;

        _isModelLoading = true;
        _view.SetModelListLoading(true);

        try
        {
            var (code, json) = await GalleryService.GetSharedSearch(_modelPageIndex, MODELS_PER_PAGE);
            if (code != 200 || string.IsNullOrEmpty(json))
            {
                Debug.LogWarning($"[RoomPlanPresenter] 모델 목록 조회 실패 ({code})");
                return;
            }

            var data = JsonConvert.DeserializeObject<ModelSearchResponse>(json);
            if (data?.content == null) return;

            foreach (var model in data.content)
            {
                var btn = _view.CreateModelButton();
                if (btn == null) continue;

                var captured = model;
                btn.MainButton.onClick.AddListener(() => OnModelButtonClicked(btn, captured));

                if (!string.IsNullOrEmpty(model.thumbnailUrl))
                    LoadModelButtonImage(btn, model.thumbnailUrl);
            }

            _isLastModelPage = data.last;
            _modelPageIndex++;
        }
        finally
        {
            _isModelLoading = false;
            _view.SetModelListLoading(false);
        }
    }

    private async void LoadModelButtonImage(RoomPlanModelButtonView btn, string url)
    {
        using var request = UnityWebRequestTexture.GetTexture(url);
        await request.SendWebRequest();
        if (request.result != UnityWebRequest.Result.Success) return;

        var tex = DownloadHandlerTexture.GetContent(request);
        var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        btn.SetImage(sprite);
    }

    private void OnModelScrollChanged(Vector2 pos)
    {
        if (pos.y <= 0.1f) LoadNextModelPage();
    }

    private async void OnModelButtonClicked(RoomPlanModelButtonView btn, ModelData model)
    {
        if (_selectedModelButton != null) _selectedModelButton.SetSelected(false);
        _selectedModelButton = btn;
        btn.SetSelected(true);

        _pendingLocalPath = null;
        _view.SetPlacementHintActive(false);

        PopupView.Instance.SetLoadingPannelActive(true);
        var (code, localPath) = await ProjectInspectService.GetModel3DFile(model.link);
        PopupView.Instance.SetLoadingPannelActive(false);

        if (code != 200 || string.IsNullOrEmpty(localPath))
        {
            PopupView.Instance.ShowMessage("모델 파일을 불러오는 데 실패했습니다.");
            btn.SetSelected(false);
            _selectedModelButton = null;
            return;
        }

        // 다운로드만 완료 — 스폰은 탭 시 수행
        _pendingLocalPath = localPath;
        _view.SetPlacementHintActive(true);
    }

    private void OnViewportTap(Vector2 screenPos)
    {
        Debug.Log($"[OnViewportTap] called. pending={(string.IsNullOrEmpty(_pendingLocalPath) ? "null" : "set")} screenPos={screenPos}");
        if (_pendingLocalPath == null) return;

        if (!_view.TryGetViewportRay(screenPos, out Ray ray))
        {
            Debug.LogWarning("[OnViewportTap] TryGetViewportRay returned false.");
            return;
        }

        Vector3 spawnPos;
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, ~0, QueryTriggerInteraction.UseGlobal))
        {
            Debug.Log($"[OnViewportTap] Raycast HIT: '{hit.collider.name}' at {hit.point}, normal={hit.normal}");
            spawnPos = hit.point;
        }
        else
        {
            Debug.LogWarning("[OnViewportTap] Raycast missed — projecting onto y=0 plane.");
            if (ray.direction.y >= 0f) return;
            float t = -ray.origin.y / ray.direction.y;
            spawnPos = ray.origin + ray.direction * t;
        }

        string pathToSpawn = _pendingLocalPath;
        _pendingLocalPath = null;
        _view.SetPlacementHintActive(false);
        if (_selectedModelButton != null) _selectedModelButton.SetSelected(false);
        _selectedModelButton = null;

        SpawnAndPlace(pathToSpawn, spawnPos);
    }

    private async void SpawnAndPlace(string localPath, Vector3 position)
    {
        var go = await LoadGlbAsGameObject(localPath);
        if (go == null)
        {
            PopupView.Instance.ShowMessage("모델 배치에 실패했습니다.");
            return;
        }
        go.transform.position = position;
        _placedModels.Add(go);
        Debug.Log($"[RoomPlanPresenter] 배치 완료. 현재 배치 모델 수: {_placedModels.Count}");
    }

    private static async Task<GameObject> LoadGlbAsGameObject(string localPath)
    {
        var go = new GameObject("PlacedModel");
        go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        var gltf = new GltfImport();
        if (!await gltf.Load(localPath) || !await gltf.InstantiateMainSceneAsync(go.transform))
        {
            Object.Destroy(go);
            return null;
        }
        return go;
    }

    private void CreateFloorCollider()
    {
        DestroyFloorCollider();
        _floorColliderGo = new GameObject("__FloorCollider");
        _floorColliderGo.transform.position = new Vector3(0f, -0.01f, 0f);
        var col = _floorColliderGo.AddComponent<BoxCollider>();
        col.size = new Vector3(500f, 0.02f, 500f);
    }

    private void DestroyFloorCollider()
    {
        if (_floorColliderGo != null)
        {
            Object.Destroy(_floorColliderGo);
            _floorColliderGo = null;
        }
    }

    private void ClearAllPlaced()
    {
        _pendingLocalPath = null;
        _view.SetPlacementHintActive(false);
        if (_selectedModelButton != null)
        {
            _selectedModelButton.SetSelected(false);
            _selectedModelButton = null;
        }
        foreach (var go in _placedModels)
            if (go != null) Object.Destroy(go);
        _placedModels.Clear();
    }

    #endregion

    #region Touch Input Handles
    private void HandlePositionUpdate(Vector2 delta)
    {
        float sensitivity = 0.02f;

        var (pos, rot) = _view.GetViewPortCameraTransform();
        Quaternion currentRot = Quaternion.Euler(rot);

        Vector3 right = currentRot * Vector3.right;
        Vector3 forward = currentRot * Vector3.forward;

        right.y = 0;
        forward.y = 0;
        right.Normalize();
        forward.Normalize();

        _currentPosition += (right * delta.x * sensitivity) + (forward * delta.y * sensitivity);
        SetPosition(_currentPosition);
    }

    private void HandleRotationUpdate(Vector2 centerDelta, float rotationDelta)
    {
        float rotSensitivity = 0.3f;
        _currentRotationY -= rotationDelta * rotSensitivity;
        SetRotation(_currentRotationY);
    }

    private void SetPosition(Vector3 pos)
    {
        _view.SetViewPortCameraPosition(pos);
    }

    private void SetRotation(float yAngle)
    {
        _view.SetViewPortCameraRotation(new Vector3(0, yAngle, 0));
    }
    #endregion

    #region Private Helpers
    private bool IsValidFileExtension(string path)
    {
        string lowerPath = path.ToLower();
        return lowerPath.EndsWith(".png") || lowerPath.EndsWith(".jpg") || lowerPath.EndsWith(".jpeg");
    }

    private async void OnUserConfirmedGeneration()
    {
        PopupView.Instance.SetLoadingPannelActive(true);

        string roomName = Path.GetFileNameWithoutExtension(_imagePath);
        var (code, json) = await RoomPlanService.CreateRoom3DFromImage(_imagePath, roomName);

        if (code != 201 || string.IsNullOrEmpty(json))
        {
            PopupView.Instance.ShowMessage("이미지 업로드 및 방 생성에 실패했습니다.");
            PopupView.Instance.SetLoadingPannelActive(false);
            return;
        }

        var dto = JsonConvert.DeserializeObject<Room3DDto>(json);
        PopupView.Instance.SetLoadingPannelActive(false);

        if (dto == null || dto.success == false)
        {
            PopupView.Instance.ShowMessage("방 도면 생성에 실패했습니다.");
            return;
        }

        if (dto.success == null || string.IsNullOrEmpty(dto.drawingXmlUrl))
        {
            PopupView.Instance.ShowMessage("업로드 완료. 도면 생성 중입니다.\n잠시 후 목록에서 확인하세요.");
            ShowProjectListPage();
            return;
        }

        string xmlContent = await FetchText(dto.drawingXmlUrl);

        if (string.IsNullOrEmpty(xmlContent))
        {
            PopupView.Instance.ShowMessage("도면 데이터를 불러오는 데 실패했습니다.");
            return;
        }

        ConstructRoom(xmlContent);
        ShowEditPage();
    }

    private void OnUserDeniedGeneration()
    {
        _imagePath = null;
    }

    private async Task<string> FetchText(string url)
    {
        using var request = UnityWebRequest.Get(url);
        await request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"FetchText 실패: {request.error} | URL: {url}");
            return null;
        }
        return request.downloadHandler.text;
    }

    private string XmlToJson(string xmlContent)
    {
        var doc = new XmlDocument();
        doc.LoadXml(xmlContent);

        var imageNode = doc.SelectSingleNode("//Image");
        float averageDoor = float.Parse(
            imageNode.Attributes["averageDoor"].Value,
            System.Globalization.CultureInfo.InvariantCulture);

        var rawNodes = doc.SelectNodes("//Object");
        var objects = new List<XmlNode>();
        foreach (XmlNode node in rawNodes)
            objects.Add(node);
        objects.Sort((a, b) =>
            int.Parse(a.Attributes["index"].Value)
                .CompareTo(int.Parse(b.Attributes["index"].Value)));

        var points = new JArray();
        var classes = new JArray();
        foreach (var obj in objects)
        {
            points.Add(new JObject
            {
                ["x1"] = double.Parse(obj.Attributes["x1"].Value, System.Globalization.CultureInfo.InvariantCulture),
                ["y1"] = double.Parse(obj.Attributes["y1"].Value, System.Globalization.CultureInfo.InvariantCulture),
                ["x2"] = double.Parse(obj.Attributes["x2"].Value, System.Globalization.CultureInfo.InvariantCulture),
                ["y2"] = double.Parse(obj.Attributes["y2"].Value, System.Globalization.CultureInfo.InvariantCulture)
            });
            classes.Add(new JObject { ["name"] = obj.Attributes["type"].Value });
        }

        return new JObject
        {
            ["points"] = points,
            ["classes"] = classes,
            ["averageDoor"] = averageDoor
        }.ToString(Newtonsoft.Json.Formatting.None);
    }

    private void ConstructRoom(string xmlContent)
    {
        Analyze.data = XmlToJson(xmlContent);
        GameObject builder = new GameObject("RoomBuilder");
        builder.AddComponent<Builder>();
    }
    #endregion
}
