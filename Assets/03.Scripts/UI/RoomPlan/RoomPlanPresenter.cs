using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using GLTFast;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class RoomPlanPresenter
{
    private const int MODELS_PER_PAGE = 10;
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    private readonly RoomPlanView _view;
    private readonly TouchView _touchView;
    private string _imagePath = null;
    private long _processingRoom3dId = -1;

    private Vector3 _currentPosition;
    private float _currentRotationY;

    private int _modelPageIndex = 0;
    private bool _isModelLoading = false;
    private bool _isLastModelPage = false;
    private RoomPlanModelButtonView _selectedModelButton = null;
    private string _selectedCategory = "";

    // 다운로드 완료 후 탭 대기 중인 모델 정보
    private string _pendingLocalPath = null;
    private ModelDimension _pendingDimension = null;
    private int _pendingModelId = -1;
    private string _pendingModelLink = null;

    // 씬에 배치된 가구 목록과 선택 상태
    private readonly List<PlacedFurnitureTag> _placedFurniture = new List<PlacedFurnitureTag>();
    private PlacedFurnitureTag _selectedFurniture = null;

    // 바닥 판정용 보이지 않는 Collider
    private GameObject _floorColliderGo = null;

    // Builder가 생성한 방 루트 (wall/door/window 부모)
    private GameObject _roomBuilderGo = null;

    // 현재 활성 Room3D 컨텍스트 (저장/불러오기용)
    private long _currentRoom3dId = -1;
    private string _currentRoomXml = null;
    private string _currentRoomName = null;
    private bool _isDefaultRoom = false;
    private bool _isCurrentRoomFake = false;

    // 페이크룸 세션 상태 (isFake = true일 때만 사용)
    // 앱 실행 세션 내 유지가 필요해 static — 씬 재진입에도 살아남고 앱 재시작 시 자연히 초기화된다.
    private const long FAKE_ROOM_ID = 1;
    private const int FAKE_GENERATION_DELAY_MS = 5000;
    private static bool _isFakeUsedThisSession = false;
    private static Room3DDto _sessionFakeRoom = null;
    private static string _sessionFakeRoomXml = null;
    private static List<PlacedFurniture> _sessionFakeFurniture = new List<PlacedFurniture>();

    public RoomPlanPresenter(RoomPlanView view, TouchView touchView, Material defaultWallMaterial)
    {
        _view = view;
        _touchView = touchView;
        Builder.wallMat = defaultWallMaterial;

        _touchView.OnSingleDrag += HandleSingleDrag;
        _touchView.OnDoubleDrag += HandleDoubleDrag;
        _touchView.OnTap += OnViewportTap;
        _touchView.OnPinch += OnViewportPinch;

        var (initPos, initRot) = _view.GetViewPortCameraTransform();
        _currentPosition = initPos;
        _currentRotationY = initRot.y;

        InitView();
    }

    private void InitView()
    {
        ShowProjectListPage();

        _view.SetButtonActions(
            onNewProject: OnNewProjectButtonClicked,
            onBack: OnBackButtonClicked,
            onEditBack: OnEditBackClicked
        );

        _view.SetModalActions(
            onCancel: OnModalCancelClicked,
            onSelectDefault: OnModalSelectDefaultClicked,
            onLoadFloor: OnModalLoadFloorClicked
        );

        foreach (var name in _view.GetCategoryNames())
        {
            string captured = name;
            _view.SetCategoryButtonAction(captured, () => OnCategoryButtonClicked(captured));
        }
        _view.SetSelectedCategoryVisual(_selectedCategory);

        _view.SetDeleteFurnitureButtonAction(OnDeleteFurnitureClicked);
        _view.SetDeleteFurnitureButtonActive(false);

        _view.SetInspectBackButtonAction(OnInspectBackClicked);
    }

    private void OnCategoryButtonClicked(string category)
    {
        // 같은 카테고리 재선택 시 필터 해제
        _selectedCategory = _selectedCategory == category ? "" : category;
        _view.SetSelectedCategoryVisual(_selectedCategory);
        ResetModelList();
    }

    private void OnDeleteFurnitureClicked()
    {
        if (_selectedFurniture == null) return;
        var toDelete = _selectedFurniture;
        _selectedFurniture = null;
        _placedFurniture.Remove(toDelete);
        Object.Destroy(toDelete.gameObject);
        _view.SetDeleteFurnitureButtonActive(false);
    }

    #region Page Controls
    private void HideAllPages()
    {
        _view.SetActiveModal(false);
        _view.SetActiveProjectListPage(false);
        _view.SetActiveEditPage(false);
        _view.SetActiveInspectPage(false);
    }

    private void ShowProjectListPage()
    {
        HideAllPages();
        _view.SetActiveNameInputModal(false);
        _view.SetActiveProjectListPage(true);
        ClearAllPlaced();
        DestroyFloorCollider();
        DestroyRoomBuilder();
        _view.DestroyDefaultRoom();
        _view.SetFloorPlaneActive(true);
        _currentRoom3dId = -1;
        _currentRoomXml = null;
        _currentRoomName = null;
        _isDefaultRoom = false;
        _isCurrentRoomFake = false;
        LoadProjectList();
    }

    private void ShowEditPage(bool isDefaultRoom)
    {
        HideAllPages();
        _view.SetActiveEditPage(true);
        _view.SetModelScrollListener(OnModelScrollChanged);
        CreateFloorCollider();
        _view.SetFloorPlaneActive(!isDefaultRoom);
        if (isDefaultRoom) _view.InstantiateDefaultRoom();
        else _view.DestroyDefaultRoom();
        ResetModelList();
    }

    private void ShowNameInputModal(string title, string initialName, UnityEngine.Events.UnityAction onConfirm, UnityEngine.Events.UnityAction onCancel)
    {
        _view.SetNameInputActions(onConfirm, onCancel);
        _view.SetActiveNameInputModal(true, title, initialName);
    }
    #endregion

    #region Button Actions
    private void OnBackButtonClicked()
    {
        Utils.SceneHistory.BackToPrevious();
    }

    private void OnEditBackClicked()
    {
        ShowNameInputModal(
            "방 이름",
            _currentRoomName ?? "",
            OnEditBackNameConfirmed,
            () => _view.SetActiveNameInputModal(false)
        );
    }

    private async void OnEditBackNameConfirmed()
    {
        string newName = _view.GetNameInputValue();
        if (string.IsNullOrWhiteSpace(newName))
        {
            PopupView.Instance.ShowMessage("방 이름을 입력하세요.");
            return;
        }
        _view.SetActiveNameInputModal(false);
        await SaveCurrentRoom(newName);
        ShowProjectListPage();
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
        ShowNameInputModal(
            "방 이름을 입력하세요",
            "내 방",
            OnDefaultRoomNameConfirmed,
            () => _view.SetActiveNameInputModal(false)
        );
    }

    private async void OnDefaultRoomNameConfirmed()
    {
        string roomName = _view.GetNameInputValue();
        if (string.IsNullOrWhiteSpace(roomName))
        {
            PopupView.Instance.ShowMessage("방 이름을 입력하세요.");
            return;
        }
        _view.SetActiveNameInputModal(false);

        string xmlPath;
        string xmlContent;
        try
        {
            (xmlPath, xmlContent) = WriteDefaultRoomXml();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"기본 방 XML 생성 실패: {e.Message}");
            PopupView.Instance.ShowMessage("방 생성에 실패했습니다.");
            return;
        }

        PopupView.Instance.SetLoadingPannelActive(true);
        var (code, json) = await RoomPlanService.CreateRoom3DFromXml(xmlPath, roomName);
        PopupView.Instance.SetLoadingPannelActive(false);

        if (code < 200 || code >= 300 || string.IsNullOrEmpty(json))
        {
            PopupView.Instance.ShowMessage("방 생성에 실패했습니다.");
            return;
        }

        var dto = JsonConvert.DeserializeObject<Room3DDto>(json);
        if (dto == null)
        {
            PopupView.Instance.ShowMessage("방 생성 응답 처리에 실패했습니다.");
            return;
        }

        _currentRoom3dId = dto.id;
        _currentRoomName = !string.IsNullOrEmpty(dto.roomName) ? dto.roomName : roomName;
        _currentRoomXml = xmlContent;
        _isDefaultRoom = true;
        ShowEditPage(true);
    }

    private static (string path, string content) WriteDefaultRoomXml()
    {
        var doc = new XmlDocument();
        var root = doc.CreateElement("Room");
        root.SetAttribute("defaultRoom", "true");
        doc.AppendChild(root);
        string path = Path.Combine(Application.temporaryCachePath, $"default_room_{System.DateTime.UtcNow.Ticks}.xml");
        doc.Save(path);
        return (path, doc.OuterXml);
    }

    private static bool IsDefaultRoomXml(string xmlContent)
    {
        if (string.IsNullOrEmpty(xmlContent)) return false;
        try
        {
            var doc = new XmlDocument();
            doc.LoadXml(xmlContent);
            var attr = doc.DocumentElement?.Attributes?["defaultRoom"];
            return attr != null && string.Equals(attr.Value, "true", System.StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
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

        // 세션에 보관 중인 페이크룸을 먼저 노출
        if (_sessionFakeRoom != null)
            AddProjectCard(_sessionFakeRoom, isFake: true);

        var (code, json) = await RoomPlanService.GetMyRoom3DList();

        if (code != 200 || string.IsNullOrEmpty(json))
        {
            Debug.LogWarning($"프로젝트 목록 조회 실패 ({code})");
            if (_sessionFakeRoom == null) _view.SetActiveProjectListEmpty(true);
            return;
        }

        var page = JsonConvert.DeserializeObject<PageDto<Room3DDto>>(json);
        int realCount = page?.content?.Count ?? 0;
        if (realCount == 0 && _sessionFakeRoom == null)
        {
            _view.SetActiveProjectListEmpty(true);
            return;
        }

        if (realCount > 0)
        {
            foreach (var room in page.content)
                AddProjectCard(room, isFake: false);
        }
    }

    private void AddProjectCard(Room3DDto room, bool isFake)
    {
        var card = _view.CreateProjectCard();
        if (card == null) return;

        card.SetCardText(room.roomName);
        var captured = room;
        card.SetButtonAction(() => OnProjectCardClicked(captured, isFake));
        card.SetLongPressAction(() => OnProjectCardLongPressed(captured, isFake));

        if (!string.IsNullOrEmpty(room.drawingImageUrl))
            LoadCardImage(card, room.drawingImageUrl);
        else
            card.SetCardImage(_view.DefaultProjectCardThumbnail);
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

    private void OnProjectCardLongPressed(Room3DDto room, bool isFake)
    {
        string name = !string.IsNullOrEmpty(room.roomName) ? room.roomName : "이 방";
        PopupView.Instance.Presenter.ShowYesNo(
            $"'{name}' 방을 삭제하시겠습니까?",
            () => DeleteRoom(room, isFake),
            null
        );
    }

    private async void DeleteRoom(Room3DDto room, bool isFake)
    {
        if (isFake)
        {
            // 서버에 삭제 엔드포인트가 없으므로 세션 메모리에서만 제거
            _sessionFakeRoom = null;
            _sessionFakeRoomXml = null;
            _sessionFakeFurniture.Clear();
            _isFakeUsedThisSession = false;
            LoadProjectList();
            return;
        }

        PopupView.Instance.SetLoadingPannelActive(true);
        var (code, body) = await RoomPlanService.DeleteRoom3D(room.id);
        PopupView.Instance.SetLoadingPannelActive(false);

        if (code < 200 || code >= 300)
        {
            Debug.LogError($"방 삭제 실패 ({code}): {body}");
            PopupView.Instance.ShowMessage("방 삭제에 실패했습니다.");
            return;
        }
        LoadProjectList();
    }

    private async void OnProjectCardClicked(Room3DDto room, bool isFake)
    {
        if (isFake)
        {
            await OpenFakeRoomFromSession();
            return;
        }

        if (string.IsNullOrEmpty(room.drawingXmlUrl))
        {
            PopupView.Instance.ShowMessage("저장된 도면 XML이 없습니다.");
            return;
        }

        PopupView.Instance.SetLoadingPannelActive(true);
        string xmlContent = await FetchText(room.drawingXmlUrl);

        if (string.IsNullOrEmpty(xmlContent))
        {
            PopupView.Instance.SetLoadingPannelActive(false);
            PopupView.Instance.ShowMessage("도면 데이터를 불러오는 데 실패했습니다.");
            return;
        }

        _currentRoom3dId = room.id;
        _currentRoomName = room.roomName;
        _currentRoomXml = xmlContent;
        _isDefaultRoom = IsDefaultRoomXml(xmlContent);
        _isCurrentRoomFake = false;

        if (_isDefaultRoom)
        {
            ShowEditPage(true);
        }
        else
        {
            ConstructRoom(xmlContent);
            ShowEditPage(false);
        }
        await RestoreFurnitureFromXml(xmlContent);
        PopupView.Instance.SetLoadingPannelActive(false);
    }

    private async Task OpenFakeRoomFromSession()
    {
        if (_sessionFakeRoom == null || string.IsNullOrEmpty(_sessionFakeRoomXml))
        {
            PopupView.Instance.ShowMessage("페이크룸 세션 정보가 유실되었습니다.");
            return;
        }

        PopupView.Instance.SetLoadingPannelActive(true);
        _currentRoom3dId = _sessionFakeRoom.id;
        _currentRoomName = _sessionFakeRoom.roomName;
        _currentRoomXml = _sessionFakeRoomXml;
        _isDefaultRoom = false;
        _isCurrentRoomFake = true;

        ConstructRoom(_sessionFakeRoomXml);
        ShowEditPage(false);
        await InstantiateFurnitureFromList(_sessionFakeFurniture);
        PopupView.Instance.SetLoadingPannelActive(false);
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
            var (code, json) = await GalleryService.GetSharedSearch(_modelPageIndex, MODELS_PER_PAGE, category: _selectedCategory, sort: "createdAt,desc");
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
                if (btn.InfoButton != null)
                    btn.InfoButton.onClick.AddListener(() => OnModelInfoClicked(captured));

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
        Debug.Log($"[RoomPlan] OnModelButtonClicked - ModelID: {model.id}, Name: {model.name}, Link: {model.link}");

        if (_selectedModelButton != null) _selectedModelButton.SetSelected(false);
        _selectedModelButton = btn;
        btn.SetSelected(true);

        DeselectFurniture();

        _pendingLocalPath = null;
        _pendingDimension = null;
        _pendingModelId = -1;
        _pendingModelLink = null;
        _view.SetPlacementHintActive(false);

        PopupView.Instance.SetLoadingPannelActive(true);
        var fileTask = ProjectInspectService.GetModel3DFile(model.link);
        var dimTask = ProjectInspectService.GetModel3DDimension(model.id);
        await Task.WhenAll(fileTask, dimTask);
        PopupView.Instance.SetLoadingPannelActive(false);

        var (code, localPath) = fileTask.Result;
        if (code != 200 || string.IsNullOrEmpty(localPath))
        {
            PopupView.Instance.ShowMessage("모델 파일을 불러오는 데 실패했습니다.");
            btn.SetSelected(false);
            _selectedModelButton = null;
            return;
        }

        var (dimCode, dimJson) = dimTask.Result;
        if (dimCode == 200 && !string.IsNullOrEmpty(dimJson))
            _pendingDimension = JsonConvert.DeserializeObject<ModelDimension>(dimJson);

        _pendingLocalPath = localPath;
        _pendingModelId = model.id;
        _pendingModelLink = model.link;
        _view.SetPlacementHintActive(true);
    }

    private async void OnModelInfoClicked(ModelData model)
    {
        PopupView.Instance.SetLoadingPannelActive(true);
        var fileTask = ProjectInspectService.GetModel3DFile(model.link);
        var dimTask = ProjectInspectService.GetModel3DDimension(model.id);
        await Task.WhenAll(fileTask, dimTask);

        var (code, localPath) = fileTask.Result;
        if (code != 200 || string.IsNullOrEmpty(localPath))
        {
            PopupView.Instance.SetLoadingPannelActive(false);
            PopupView.Instance.ShowMessage("모델 파일을 불러오는 데 실패했습니다.");
            return;
        }

        var (dimCode, dimJson) = dimTask.Result;
        ModelDimension dim = (dimCode == 200 && !string.IsNullOrEmpty(dimJson))
            ? JsonConvert.DeserializeObject<ModelDimension>(dimJson)
            : new ModelDimension();

        _view.InspectView.SetContent(dim, model);
        _view.InspectView.SpawnModel3D(localPath);
        _view.SetActiveEditPage(false);
        _view.SetActiveInspectPage(true);
        PopupView.Instance.SetLoadingPannelActive(false);
    }

    private void OnInspectBackClicked()
    {
        _view.SetActiveInspectPage(false);
        _view.SetActiveEditPage(true);
    }

    private void OnViewportTap(Vector2 screenPos)
    {
        if (!_view.TryGetViewportRay(screenPos, out Ray ray))
        {
            Debug.LogWarning("[OnViewportTap] TryGetViewportRay returned false.");
            return;
        }

        // 1) 새 가구 배치 대기 중이면 배치
        if (!string.IsNullOrEmpty(_pendingLocalPath))
        {
            PlacePendingAtRay(ray);
            return;
        }

        // 2) 이미 배치된 가구 선택 / 빈 곳 탭 시 해제
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, ~0, QueryTriggerInteraction.UseGlobal))
        {
            var tag = hit.collider.GetComponentInParent<PlacedFurnitureTag>();
            if (tag != null)
            {
                SelectFurniture(tag);
                return;
            }
        }

        DeselectFurniture();
    }

    private void PlacePendingAtRay(Ray ray)
    {
        Vector3 spawnPos;
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, ~0, QueryTriggerInteraction.UseGlobal))
        {
            spawnPos = hit.point;
        }
        else
        {
            if (ray.direction.y >= 0f) return;
            float t = -ray.origin.y / ray.direction.y;
            spawnPos = ray.origin + ray.direction * t;
        }

        string pathToSpawn = _pendingLocalPath;
        ModelDimension dimensionToSpawn = _pendingDimension;
        int modelId = _pendingModelId;
        string modelLink = _pendingModelLink;

        _pendingLocalPath = null;
        _pendingDimension = null;
        _pendingModelId = -1;
        _pendingModelLink = null;
        _view.SetPlacementHintActive(false);
        if (_selectedModelButton != null) _selectedModelButton.SetSelected(false);
        _selectedModelButton = null;

        SpawnAndPlace(pathToSpawn, spawnPos, dimensionToSpawn, modelId, modelLink);
    }

    private async void SpawnAndPlace(string localPath, Vector3 floorPos, ModelDimension dimension, int modelId, string modelLink)
    {
        var go = await LoadGlbAsGameObject(localPath);
        if (go == null)
        {
            PopupView.Instance.ShowMessage("모델 배치에 실패했습니다.");
            return;
        }

        ApplyScale(go, dimension);
        LiftToFloor(go, floorPos);
        var wb = AddSelectionCollider(go);

        var data = new PlacedFurniture
        {
            modelId = modelId,
            link = modelLink,
            dimWidth = dimension != null ? dimension.width : 0f,
            dimLength = dimension != null ? dimension.length : 0f,
            dimHeight = dimension != null ? dimension.height : 0f,
            posX = go.transform.position.x,
            posY = go.transform.position.y,
            posZ = go.transform.position.z,
            rotY = 0f,
            userScale = 1f
        };
        var tag = go.AddComponent<PlacedFurnitureTag>();
        tag.Data = data;
        tag.SetupIndicator(wb, _view.SelectionIndicatorMaterial);
        _placedFurniture.Add(tag);
        Debug.Log($"[RoomPlanPresenter] 배치 완료. 현재 배치 가구 수: {_placedFurniture.Count}");
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
        ApplyDefaultPBRSettings(go);
        return go;
    }

    private static void ApplyDefaultPBRSettings(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            foreach (Material mat in renderer.materials)
            {
                if (mat.HasProperty("metallicFactor"))
                    mat.SetFloat("metallicFactor", 0.0f);
                if (mat.HasProperty("roughnessFactor"))
                    mat.SetFloat("roughnessFactor", 0.5f);
            }
        }
    }

    // 모델의 가장 긴 변을 dimension의 가장 긴 변(cm→m)에 맞춰 등비 스케일
    private static void ApplyScale(GameObject root, ModelDimension dimension)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        root.transform.localScale = Vector3.one;

        Bounds bounds = renderers[0].bounds;
        foreach (var r in renderers) bounds.Encapsulate(r.bounds);

        float currentMax = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        float targetMax = (dimension != null)
            ? Mathf.Max(dimension.width, dimension.length, dimension.height) * 0.01f
            : 0f;
        float scaleFactor = (targetMax > 0f && currentMax > 0f)
            ? targetMax / currentMax
            : (currentMax > 0f ? 1.0f / currentMax : 1.0f);
        root.transform.localScale = Vector3.one * scaleFactor;
    }

    // 모델의 바닥(min.y)이 floorPoint.y에 닿도록 배치
    private static void LiftToFloor(GameObject root, Vector3 floorPoint)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            root.transform.position = floorPoint;
            return;
        }
        Bounds bounds = renderers[0].bounds;
        foreach (var r in renderers) bounds.Encapsulate(r.bounds);
        root.transform.position = new Vector3(
            floorPoint.x,
            floorPoint.y - bounds.min.y,
            floorPoint.z
        );
    }

    // 회전 적용 전(identity 상태)에 호출해야 안전함
    private static Bounds AddSelectionCollider(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(root.transform.position, Vector3.zero);

        Bounds wb = renderers[0].bounds;
        foreach (var r in renderers) wb.Encapsulate(r.bounds);

        var bc = root.AddComponent<BoxCollider>();
        Vector3 lossy = root.transform.lossyScale;
        bc.size = new Vector3(
            lossy.x != 0f ? wb.size.x / lossy.x : wb.size.x,
            lossy.y != 0f ? wb.size.y / lossy.y : wb.size.y,
            lossy.z != 0f ? wb.size.z / lossy.z : wb.size.z);
        Vector3 worldOffset = wb.center - root.transform.position;
        bc.center = new Vector3(
            lossy.x != 0f ? worldOffset.x / lossy.x : worldOffset.x,
            lossy.y != 0f ? worldOffset.y / lossy.y : worldOffset.y,
            lossy.z != 0f ? worldOffset.z / lossy.z : worldOffset.z);
        return wb;
    }

    private void SelectFurniture(PlacedFurnitureTag tag)
    {
        if (_selectedFurniture == tag) return;
        _selectedFurniture?.SetSelected(false);
        _selectedFurniture = tag;
        _selectedFurniture.SetSelected(true);
        _view.SetDeleteFurnitureButtonActive(true);
    }

    private void DeselectFurniture()
    {
        if (_selectedFurniture == null) return;
        _selectedFurniture.SetSelected(false);
        _selectedFurniture = null;
        _view.SetDeleteFurnitureButtonActive(false);
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
        _pendingDimension = null;
        _pendingModelId = -1;
        _pendingModelLink = null;
        _view.SetPlacementHintActive(false);
        if (_selectedModelButton != null)
        {
            _selectedModelButton.SetSelected(false);
            _selectedModelButton = null;
        }
        _selectedFurniture = null;
        _view.SetDeleteFurnitureButtonActive(false);
        foreach (var tag in _placedFurniture)
            if (tag != null) Object.Destroy(tag.gameObject);
        _placedFurniture.Clear();
    }

    #endregion

    #region Save / Load Furniture

    private async Task SaveCurrentRoom(string newRoomName = null)
    {
        if (_currentRoom3dId < 0 || string.IsNullOrEmpty(_currentRoomXml))
        {
            Debug.LogWarning("저장할 Room3D 컨텍스트가 없습니다.");
            return;
        }

        if (_isCurrentRoomFake)
        {
            await SaveFakeRoom(newRoomName);
            return;
        }

        string newXml;
        try
        {
            newXml = BuildXmlWithFurniture(_currentRoomXml, _placedFurniture);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"가구 XML 직렬화 실패: {e.Message}");
            return;
        }

        string tmpPath = Path.Combine(Application.temporaryCachePath, $"room3d_{_currentRoom3dId}.xml");
        File.WriteAllText(tmpPath, newXml);

        string nameToSend = (!string.IsNullOrWhiteSpace(newRoomName) && newRoomName != _currentRoomName)
            ? newRoomName
            : null;

        PopupView.Instance.SetLoadingPannelActive(true);
        var (code, body) = await RoomPlanService.UpdateRoom3D(_currentRoom3dId, roomName: nameToSend, xmlFilePath: tmpPath);
        PopupView.Instance.SetLoadingPannelActive(false);

        if (code < 200 || code >= 300)
        {
            Debug.LogError($"방 저장 실패 ({code}): {body}");
            PopupView.Instance.ShowMessage("방 저장에 실패했습니다.");
            return;
        }

        _currentRoomXml = newXml;
        if (!string.IsNullOrWhiteSpace(newRoomName)) _currentRoomName = newRoomName;
    }

    private static string BuildXmlWithFurniture(string baseXml, List<PlacedFurnitureTag> furniture)
    {
        var doc = new XmlDocument();
        doc.LoadXml(baseXml);

        // 기존 Furniture 노드 제거
        var existing = doc.SelectNodes("//Furniture");
        if (existing != null)
        {
            foreach (XmlNode n in existing)
                n.ParentNode?.RemoveChild(n);
        }

        XmlNode root = doc.DocumentElement;
        if (root == null) return doc.OuterXml;

        foreach (var f in furniture)
        {
            if (f == null || f.Data == null) continue;

            // 현재 Transform 상태를 데이터에 반영
            var pos = f.transform.position;
            var rot = f.transform.eulerAngles;
            f.Data.posX = pos.x;
            f.Data.posY = pos.y;
            f.Data.posZ = pos.z;
            f.Data.rotY = rot.y;

            var el = doc.CreateElement("Furniture");
            el.SetAttribute("modelId", f.Data.modelId.ToString(Inv));
            el.SetAttribute("link", f.Data.link ?? "");
            el.SetAttribute("dimW", f.Data.dimWidth.ToString(Inv));
            el.SetAttribute("dimL", f.Data.dimLength.ToString(Inv));
            el.SetAttribute("dimH", f.Data.dimHeight.ToString(Inv));
            el.SetAttribute("px", f.Data.posX.ToString(Inv));
            el.SetAttribute("py", f.Data.posY.ToString(Inv));
            el.SetAttribute("pz", f.Data.posZ.ToString(Inv));
            el.SetAttribute("ry", f.Data.rotY.ToString(Inv));
            el.SetAttribute("userScale", f.Data.userScale.ToString(Inv));
            root.AppendChild(el);
        }

        return doc.OuterXml;
    }

    private async Task RestoreFurnitureFromXml(string xmlContent)
    {
        XmlNodeList nodes;
        try
        {
            var doc = new XmlDocument();
            doc.LoadXml(xmlContent);
            nodes = doc.SelectNodes("//Furniture");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"가구 XML 파싱 실패: {e.Message}");
            return;
        }
        if (nodes == null || nodes.Count == 0) return;

        var parsed = new List<PlacedFurniture>(nodes.Count);
        foreach (XmlNode node in nodes)
        {
            var data = ParseFurnitureNode(node);
            if (data != null) parsed.Add(data);
        }
        await InstantiateFurnitureFromList(parsed);
    }

    private async Task InstantiateFurnitureFromList(List<PlacedFurniture> dataList)
    {
        if (dataList == null || dataList.Count == 0) return;

        foreach (var data in dataList)
        {
            if (data == null || string.IsNullOrEmpty(data.link)) continue;

            var (code, localPath) = await ProjectInspectService.GetModel3DFile(data.link);
            if (code != 200 || string.IsNullOrEmpty(localPath))
            {
                Debug.LogWarning($"가구 모델 다운로드 실패 modelId={data.modelId} code={code}");
                continue;
            }

            var go = await LoadGlbAsGameObject(localPath);
            if (go == null) continue;

            var dim = new ModelDimension(data.dimWidth, data.dimLength, data.dimHeight);
            ApplyScale(go, dim);
            go.transform.localScale *= data.userScale;
            go.transform.position = new Vector3(data.posX, 0f, data.posZ);
            LiftToFloor(go, new Vector3(data.posX, 0f, data.posZ));
            var wb = AddSelectionCollider(go);
            go.transform.rotation = Quaternion.Euler(0f, data.rotY, 0f);

            var tag = go.AddComponent<PlacedFurnitureTag>();
            tag.Data = data;
            tag.SetupIndicator(wb, _view.SelectionIndicatorMaterial);
            _placedFurniture.Add(tag);
        }
        Debug.Log($"[RoomPlanPresenter] 가구 인스턴스화 완료. 수: {_placedFurniture.Count}");
    }

    private Task SaveFakeRoom(string newRoomName)
    {
        // 데모 플로우는 클라이언트 자체 처리: 가구/이름 모두 세션 메모리에만 반영
        _sessionFakeFurniture = CapturePlacedFurnitureData(_placedFurniture);

        if (!string.IsNullOrWhiteSpace(newRoomName) && newRoomName != _currentRoomName)
        {
            _currentRoomName = newRoomName;
            if (_sessionFakeRoom != null) _sessionFakeRoom.roomName = newRoomName;
        }
        return Task.CompletedTask;
    }

    private static List<PlacedFurniture> CapturePlacedFurnitureData(List<PlacedFurnitureTag> tags)
    {
        var list = new List<PlacedFurniture>(tags.Count);
        foreach (var t in tags)
        {
            if (t == null || t.Data == null) continue;
            var pos = t.transform.position;
            var rot = t.transform.eulerAngles;
            list.Add(new PlacedFurniture
            {
                modelId = t.Data.modelId,
                link = t.Data.link,
                dimWidth = t.Data.dimWidth,
                dimLength = t.Data.dimLength,
                dimHeight = t.Data.dimHeight,
                posX = pos.x,
                posY = pos.y,
                posZ = pos.z,
                rotY = rot.y,
                userScale = t.Data.userScale,
            });
        }
        return list;
    }

    private static PlacedFurniture ParseFurnitureNode(XmlNode node)
    {
        if (node?.Attributes == null) return null;
        try
        {
            return new PlacedFurniture
            {
                modelId = ParseInt(node.Attributes["modelId"]?.Value),
                link = node.Attributes["link"]?.Value ?? "",
                dimWidth = ParseFloat(node.Attributes["dimW"]?.Value),
                dimLength = ParseFloat(node.Attributes["dimL"]?.Value),
                dimHeight = ParseFloat(node.Attributes["dimH"]?.Value),
                posX = ParseFloat(node.Attributes["px"]?.Value),
                posY = ParseFloat(node.Attributes["py"]?.Value),
                posZ = ParseFloat(node.Attributes["pz"]?.Value),
                rotY = ParseFloat(node.Attributes["ry"]?.Value),
                userScale = ParseFloat(node.Attributes["userScale"]?.Value, 1f)
            };
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Furniture 노드 파싱 실패: {e.Message}");
            return null;
        }
    }

    private static int ParseInt(string s) =>
        int.TryParse(s, NumberStyles.Integer, Inv, out var v) ? v : 0;

    private static float ParseFloat(string s, float fallback = 0f) =>
        float.TryParse(s, NumberStyles.Float, Inv, out var v) ? v : fallback;

    #endregion

    #region Touch Input Handles

    private void HandleSingleDrag(Vector2 delta)
    {
        if (_selectedFurniture != null)
        {
            MoveSelectedFurniture(delta);
            return;
        }
        HandleCameraPositionUpdate(delta);
    }

    private void HandleDoubleDrag(Vector2 centerDelta, float rotationDelta)
    {
        if (_selectedFurniture != null)
        {
            RotateSelectedFurniture(rotationDelta);
            return;
        }
        HandleCameraRotationUpdate(centerDelta, rotationDelta);
    }

    private void HandleCameraPositionUpdate(Vector2 delta)
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

    private void HandleCameraRotationUpdate(Vector2 centerDelta, float rotationDelta)
    {
        float rotSensitivity = 0.3f;
        _currentRotationY -= rotationDelta * rotSensitivity;
        SetRotation(_currentRotationY);
    }

    private void MoveSelectedFurniture(Vector2 delta)
    {
        const float sensitivity = 0.01f;
        var (_, rot) = _view.GetViewPortCameraTransform();
        Quaternion camRot = Quaternion.Euler(rot);

        Vector3 right = camRot * Vector3.right;
        Vector3 forward = camRot * Vector3.forward;
        right.y = 0f; forward.y = 0f;
        right.Normalize(); forward.Normalize();

        Vector3 worldDelta = (right * delta.x + forward * delta.y) * sensitivity;
        _selectedFurniture.transform.position += worldDelta;
    }

    private void RotateSelectedFurniture(float rotationDelta)
    {
        const float rotSensitivity = 0.3f;
        _selectedFurniture.transform.Rotate(0f, -rotationDelta * rotSensitivity, 0f, Space.World);
    }

    private void OnViewportPinch(float pinchDelta)
    {
        if (_selectedFurniture != null)
        {
            ScaleSelectedFurniture(pinchDelta);
            return;
        }
        ZoomCamera(pinchDelta);
    }

    private void ZoomCamera(float pinchDelta)
    {
        const float sensitivity = 0.05f;
        const float minFov = 20f;
        const float maxFov = 80f;
        float newFov = Mathf.Clamp(_view.GetViewPortCameraFov() - pinchDelta * sensitivity, minFov, maxFov);
        _view.SetViewPortCameraFov(newFov);
    }

    private void ScaleSelectedFurniture(float pinchDelta)
    {
        const float sensitivity = 0.005f;
        float newUserScale = Mathf.Max(0.1f, _selectedFurniture.Data.userScale + pinchDelta * sensitivity);
        float ratio = newUserScale / _selectedFurniture.Data.userScale;
        _selectedFurniture.transform.localScale *= ratio;
        _selectedFurniture.Data.userScale = newUserScale;

        // Reset Y to 0 before re-lifting so LiftToFloor computes correct bounds
        var pos = _selectedFurniture.transform.position;
        _selectedFurniture.transform.position = new Vector3(pos.x, 0f, pos.z);
        LiftToFloor(_selectedFurniture.gameObject, new Vector3(pos.x, 0f, pos.z));
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
        // 페이크룸 분기: isFake가 켜져 있고 세션 내에서 아직 페이크 생성을 사용하지 않았다면 페이크 경로 사용
        if (_view.IsFake && !_isFakeUsedThisSession)
        {
            await StartFakeRoomGeneration();
            return;
        }

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
        if (dto == null)
        {
            PopupView.Instance.ShowMessage("방 생성 응답 처리에 실패했습니다.");
            PopupView.Instance.SetLoadingPannelActive(false);
            return;
        }

        // API returns PROCESSING state — result arrives via WebSocket
        _processingRoom3dId = dto.id;

        if (WebsocketController.Instance != null)
        {
            WebsocketController.Instance.OnRoom3DGenerationSuccess += OnRoom3DSuccess;
            WebsocketController.Instance.OnRoom3DGenerationFailed += OnRoom3DFailed;
        }
        else
        {
            PopupView.Instance.ShowMessage("업로드 완료. 도면 생성 중입니다.\n잠시 후 목록에서 확인하세요.");
            PopupView.Instance.SetLoadingPannelActive(false);
            ShowProjectListPage();
        }
    }

    private async Task StartFakeRoomGeneration()
    {
        PopupView.Instance.SetLoadingPannelActive(true);

        string roomName = Path.GetFileNameWithoutExtension(_imagePath);

        // 데모 플로우는 클라이언트가 자체 처리: 5초 대기 후 단건 조회로 미리 준비된 fake-room을 가져옴
        await Task.Delay(FAKE_GENERATION_DELAY_MS);

        var (code, json) = await RoomPlanService.GetFakeRoom(FAKE_ROOM_ID);
        Debug.Log($"[FakeRoom] GET /api/fake-rooms/{FAKE_ROOM_ID} → code={code}, body={json}");

        if (code < 200 || code >= 300 || string.IsNullOrEmpty(json))
        {
            Debug.LogError($"페이크룸 조회 실패 ({code}): {json}");
            PopupView.Instance.ShowMessage($"페이크룸 조회 실패 ({code})");
            PopupView.Instance.SetLoadingPannelActive(false);
            return;
        }

        // 페이크 API는 xmlFileUrl, 실서버 Room3DDto는 drawingXmlUrl을 쓰므로 직접 파싱한다.
        string imageUrl;
        string xmlUrl;
        try
        {
            var obj = JObject.Parse(json);
            imageUrl = (string)(obj["drawingImageUrl"] ?? obj["imageUrl"]);
            xmlUrl = (string)(obj["xmlFileUrl"] ?? obj["drawingXmlUrl"]);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[FakeRoom] JSON 파싱 실패: {e.Message}\nbody={json}");
            PopupView.Instance.ShowMessage("페이크룸 응답 파싱 실패");
            PopupView.Instance.SetLoadingPannelActive(false);
            return;
        }

        if (string.IsNullOrEmpty(xmlUrl))
        {
            Debug.LogError($"[FakeRoom] xmlFileUrl 누락. body={json}");
            PopupView.Instance.ShowMessage("페이크룸 응답에 XML URL이 없습니다.");
            PopupView.Instance.SetLoadingPannelActive(false);
            return;
        }

        Debug.Log($"[FakeRoom] xml fetch from {xmlUrl}");
        string xmlContent = await FetchText(xmlUrl);
        if (string.IsNullOrEmpty(xmlContent))
        {
            Debug.LogError($"[FakeRoom] XML 패치 실패. url={xmlUrl}");
            PopupView.Instance.ShowMessage("페이크룸 도면을 불러오지 못했습니다.");
            PopupView.Instance.SetLoadingPannelActive(false);
            return;
        }
        Debug.Log($"[FakeRoom] XML 패치 성공. length={xmlContent.Length}");

        // 세션 컨텍스트 구성 (이름은 이미지 파일명으로 덮어쓴다)
        _sessionFakeRoom = new Room3DDto
        {
            id = FAKE_ROOM_ID,
            roomName = roomName,
            drawingImageUrl = imageUrl,
            drawingXmlUrl = xmlUrl,
        };
        _sessionFakeRoomXml = xmlContent;
        _sessionFakeFurniture = new List<PlacedFurniture>();
        _isFakeUsedThisSession = true;

        _currentRoom3dId = FAKE_ROOM_ID;
        _currentRoomName = roomName;
        _currentRoomXml = xmlContent;
        _isDefaultRoom = false;
        _isCurrentRoomFake = true;

        ConstructRoom(xmlContent);
        ShowEditPage(false);
        PopupView.Instance.SetLoadingPannelActive(false);
    }

    private async void OnRoom3DSuccess(string json)
    {
        var notification = JsonConvert.DeserializeObject<Room3DNotificationDto>(json);
        if (notification == null || notification.room3dId != _processingRoom3dId) return;

        UnsubscribeRoom3DEvents();
        long roomId = _processingRoom3dId;
        _processingRoom3dId = -1;

        if (string.IsNullOrEmpty(notification.xmlFileUrl))
        {
            PopupView.Instance.SetLoadingPannelActive(false);
            PopupView.Instance.ShowMessage("도면 생성 완료. XML 파일을 찾을 수 없습니다.");
            return;
        }

        string xmlContent = await FetchText(notification.xmlFileUrl);

        if (string.IsNullOrEmpty(xmlContent))
        {
            PopupView.Instance.SetLoadingPannelActive(false);
            PopupView.Instance.ShowMessage("도면 데이터를 불러오는 데 실패했습니다.");
            return;
        }

        _currentRoom3dId = roomId;
        _currentRoomName = null;
        _currentRoomXml = xmlContent;
        _isDefaultRoom = false;
        _isCurrentRoomFake = false;

        ConstructRoom(xmlContent);
        ShowEditPage(false);
        await RestoreFurnitureFromXml(xmlContent);
        PopupView.Instance.SetLoadingPannelActive(false);
    }

    private void OnRoom3DFailed(string json)
    {
        var notification = JsonConvert.DeserializeObject<Room3DNotificationDto>(json);
        if (notification == null || notification.room3dId != _processingRoom3dId) return;

        UnsubscribeRoom3DEvents();
        _processingRoom3dId = -1;

        PopupView.Instance.SetLoadingPannelActive(false);
        PopupView.Instance.ShowMessage(
            !string.IsNullOrEmpty(notification.message) ? notification.message : "방 도면 생성에 실패했습니다.");
    }

    private void UnsubscribeRoom3DEvents()
    {
        if (WebsocketController.Instance == null) return;
        WebsocketController.Instance.OnRoom3DGenerationSuccess -= OnRoom3DSuccess;
        WebsocketController.Instance.OnRoom3DGenerationFailed -= OnRoom3DFailed;
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
            CultureInfo.InvariantCulture);

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
                ["x1"] = double.Parse(obj.Attributes["x1"].Value, CultureInfo.InvariantCulture),
                ["y1"] = double.Parse(obj.Attributes["y1"].Value, CultureInfo.InvariantCulture),
                ["x2"] = double.Parse(obj.Attributes["x2"].Value, CultureInfo.InvariantCulture),
                ["y2"] = double.Parse(obj.Attributes["y2"].Value, CultureInfo.InvariantCulture)
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
        DestroyRoomBuilder();
        Analyze.data = XmlToJson(xmlContent);
        _roomBuilderGo = new GameObject("RoomBuilder");
        _roomBuilderGo.AddComponent<Builder>();
    }

    private void DestroyRoomBuilder()
    {
        if (_roomBuilderGo != null)
        {
            Object.Destroy(_roomBuilderGo);
            _roomBuilderGo = null;
        }
    }
    #endregion
}
