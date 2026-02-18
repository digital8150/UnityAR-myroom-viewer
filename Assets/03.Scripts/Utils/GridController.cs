using UnityEngine;
using UnityEngine.UI;

public class GridController : MonoBehaviour
{
    [SerializeField] private int _columnCount = 3; // 형이 원하는 3열 고정
    [SerializeField] private float _heightRatio = 1.25f; // 가로 대비 세로 비율 (예: 1:1.25)

    private GridLayoutGroup _gridLayout;
    private RectTransform _rectTransform;

    private void Awake()
    {
        _gridLayout = GetComponent<GridLayoutGroup>();
        _rectTransform = GetComponent<RectTransform>();
    }

    // 데이터가 추가되거나 해상도가 바뀔 때 호출
    private void Update()
    {
        UpdateCellSize();
    }

    public void UpdateCellSize()
    {
        if (_gridLayout == null || _rectTransform == null) return;

        // 1. 전체 가로 폭에서 Padding 좌우 값을 뺌
        float totalWidth = _rectTransform.rect.width;
        float widthWithoutPadding = totalWidth - _gridLayout.padding.left - _gridLayout.padding.right;

        // 2. 아이템 사이의 간격(Spacing) 합계를 뺌
        float totalSpacing = _gridLayout.spacing.x * (_columnCount - 1);
        float availableWidth = widthWithoutPadding - totalSpacing;

        // 3. 최종 Cell Width 계산 (0보다 작아지지 않게 방지)
        float cellWidth = Mathf.Max(0, availableWidth / _columnCount);

        // 4. 비율에 맞게 Height 설정
        float cellHeight = cellWidth * _heightRatio;

        // 5. Grid Layout에 적용
        _gridLayout.cellSize = new Vector2(cellWidth, cellHeight);
    }
}
