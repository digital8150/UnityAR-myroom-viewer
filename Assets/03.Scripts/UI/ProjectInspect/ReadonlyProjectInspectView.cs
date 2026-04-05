using GLTFast;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utils;

public class ReadonlyProjectInspectView : MonoBehaviour
{
    [Header("Showcase Settings")]
    [SerializeField] private float _targetMaxScale = 1.4f;      // 가장 긴 축의 목표 크기
    [SerializeField] private bool _enableAutoRotation = true;  // 자동 회전 여부
    [SerializeField] private float _rotationSpeed = 30f;       // 회전 속도
    [SerializeField] private Transform _doneModelSpawnPos;
    [SerializeField] private GameObject _shadowCatchPlane;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private Image _profileImage;
    [SerializeField] private TextMeshProUGUI _userNameText;
    [SerializeField] private TextMeshProUGUI _infoText;
    [SerializeField] private TextMeshProUGUI _categoryBadgeText;
    [SerializeField] private TextMeshProUGUI _contentText;
    [SerializeField] private Sprite _defaultProfilePic;

    private GameObject _spawnedModel; // 생성된 모델 참조용

    private void Update()
    {
        UpdateModelRotation();
    }

    public async void SetContent(ModelDimension modelDimension, ModelData modelData)
    {
        if(_titleText) _titleText.text = modelData.name;
        if(_infoText) _infoText.text = TranslateInfo(modelData);
        if(_userNameText) _userNameText.text = $"{await MemberService.GetMemberUsernameByMemberId(modelData.creatorId)}";

        Sprite userProfilePic = await ImageUtils.LoadSpriteFromUrlAsync(await MemberService.GetMemberProfilePicUrlByMemberId(modelData.creatorId));
        if(userProfilePic == null) userProfilePic = _defaultProfilePic;

        if (_profileImage) _profileImage.sprite = await ImageUtils.LoadSpriteFromUrlAsync(await MemberService.GetMemberProfilePicUrlByMemberId(modelData.creatorId));
        if (_contentText)
        {
            string content = string.Empty;
            content += 
                $"크기\r\n" +
                $"<size=80%><color=#585757><font=\"Pretendard-Medium SDF\">{modelDimension} cm</font></color></size>\r\n\r\n" +
                $"상품 페이지\r\n" +
                $"<size=80%><color=#0269E2><link=\"{modelData.shopPageLink}\"><font=\"Pretendard-Medium SDF\">{modelData.shopPageLink}</font></link></color></size>\r\n\r\n" +
                $"설명\r\n" +
                $"<size=80%><color=#585757><font=\"Pretendard-Medium SDF\">{modelData.description}</font></color></size>";
            _contentText.text = content;
        }
        if (_categoryBadgeText) _categoryBadgeText.text = TranslateCategory(modelData.furniture_type);
        SpawnModel3D(modelData.link);
    }

    private string TranslateInfo(ModelData modelData)
    {
        return $"{GetRelativeTime(modelData.createdAt)}";
    }

    private string TranslateCategory(string category)
    {
        return category?.ToLower() switch
        {
            "shelf" => "선반",
            "sofa" => "소파",
            "storage" => "수납장",
            "chair" => "의자",
            "lighting" => "조명",
            "desk" => "책상",
            "bed" => "침대",
            "table" => "테이블",
            "others" => "카테고리 미지정",
            _ => category // 매칭되는 것이 없으면 원래 문자열 반환
        };
    }

    public async void SpawnModel3D(string localModelPath)
    {
        // 기존에 생성된 모델이 있다면 삭제
        if (_spawnedModel) Destroy(_spawnedModel);

        _spawnedModel = new GameObject("AR_Model_Instance");
        _spawnedModel.transform.position = _doneModelSpawnPos.position;
        _spawnedModel.transform.rotation = _doneModelSpawnPos.rotation;

        var gltf = new GltfImport();
        bool success = await gltf.Load(localModelPath);

        if (success)
        {
            bool instantSuccess = await gltf.InstantiateMainSceneAsync(_spawnedModel.transform);
            if (instantSuccess)
            {
                ScaleToUnitSize(_spawnedModel, _targetMaxScale);

                // 추가된 로직: 모델 바닥에 섀도우 플레인 위치시키기
                UpdateShadowPlanePosition(_spawnedModel);
                ApplyDefaultPBRSettings(_spawnedModel);

                Debug.Log("[ProjectInspectView] Successfully loaded and scaled model for preview");
            }
            else { Destroy(_spawnedModel); }
        }
        else { Destroy(_spawnedModel); }
    }

    #region priate methods
    private void UpdateModelRotation()
    {
        // 모델이 존재하고 자동 회전이 활성화된 경우에만 회전
        if (_spawnedModel != null && _enableAutoRotation)
        {
            _spawnedModel.transform.Rotate(Vector3.up, _rotationSpeed * Time.deltaTime);
        }
    }

    private void ScaleToUnitSize(GameObject target, float targetScale)
    {
        // 1. 모든 자식 Renderer의 Bounds를 합쳐서 전체 영역 계산
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds combinedBounds = renderers[0].bounds;
        foreach (Renderer render in renderers)
        {
            combinedBounds.Encapsulate(render.bounds);
        }

        // 2. 가장 긴 축의 길이 찾기 ($x, y, z$ 중 max)
        float maxDimension = Mathf.Max(combinedBounds.size.x, combinedBounds.size.y, combinedBounds.size.z);

        if (maxDimension <= 0) return;

        // 3. 현재 스케일을 기준으로 1이 되도록 비율 계산
        // 1 / maxDimension 을 현재 로컬 스케일에 곱해줌
        float scaleFactor = targetScale / maxDimension;
        target.transform.localScale *= scaleFactor;

        // (선택 사항) 피벗이 바닥이 아니라 중앙이라면 위치 보정이 필요할 수도 있어!
    }

    private void UpdateShadowPlanePosition(GameObject target)
    {
        if (_shadowCatchPlane == null) return;

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        // 1. 모델의 전체 Bounds 계산 (Scale된 상태 기준)
        Bounds combinedBounds = renderers[0].bounds;
        foreach (Renderer render in renderers)
        {
            combinedBounds.Encapsulate(render.bounds);
        }

        // 2. Bounds의 최하단 y값 찾기
        float bottomY = combinedBounds.min.y;

        // 3. 섀도우 플레인의 위치를 모델의 바닥 높이로 설정 (x, z는 스폰 위치 유지)
        Vector3 planePos = _shadowCatchPlane.transform.position;
        planePos.y = bottomY + 0.001f; // Z-Fighting 방지를 위해 살짝 띄움
        _shadowCatchPlane.transform.position = planePos;
    }

    private string GetRelativeTime(string isoDateTime)
    {
        if (string.IsNullOrEmpty(isoDateTime)) return "시간 정보 없음";

        // 1. ISO 8601 문자열을 DateTime 객체로 변환
        if (!DateTime.TryParse(isoDateTime, out DateTime dateTime))
        {
            return "알 수 없음";
        }

        // 2. 현재 시간과의 차이 계산
        TimeSpan timeSpan = DateTime.Now - dateTime;

        // 3. 차이에 따른 문자열 반환 (조건문 순서가 중요합니다)
        if (timeSpan.TotalSeconds < 60)
        {
            return "방금 전";
        }
        if (timeSpan.TotalMinutes < 60)
        {
            return $"{(int)timeSpan.TotalMinutes}분 전";
        }
        if (timeSpan.TotalHours < 24)
        {
            return $"{(int)timeSpan.TotalHours}시간 전";
        }
        if (timeSpan.TotalDays < 7)
        {
            return $"{(int)timeSpan.TotalDays}일 전";
        }
        if (timeSpan.TotalDays < 31)
        {
            return $"{(int)Math.Ceiling(timeSpan.TotalDays / 7)}주 전";
        }

        // 한 달이 넘어가면 날짜 그대로 표시 (예: 2026-03-01)
        return dateTime.ToString("yyyy-MM-dd");
    }

    private void ApplyDefaultPBRSettings(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            foreach (Material mat in renderer.materials)
            {
                // Metallic 설정 (0 ~ 1)
                if (mat.HasProperty("metallicFactor"))
                    mat.SetFloat("metallicFactor", 0.0f);

                // Roughness 설정 (0 ~ 1)
                if (mat.HasProperty("roughnessFactor"))
                    mat.SetFloat("roughnessFactor", 0.5f);
            }
        }
    }
    #endregion
}
