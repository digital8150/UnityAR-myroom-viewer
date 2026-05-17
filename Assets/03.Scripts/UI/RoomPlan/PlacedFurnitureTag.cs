using UnityEngine;

public class PlacedFurnitureTag : MonoBehaviour
{
    public PlacedFurniture Data;

    private GameObject _indicator;

    public void SetupIndicator(Bounds worldBounds, Material indicatorMaterial)
    {
        if (indicatorMaterial == null) return;
        if (_indicator != null) Destroy(_indicator);

        _indicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Destroy(_indicator.GetComponent<Collider>());
        _indicator.name = "__SelectionIndicator";

        _indicator.transform.position = new Vector3(
            worldBounds.center.x,
            worldBounds.min.y + 0.003f,
            worldBounds.center.z);

        _indicator.transform.SetParent(transform, worldPositionStays: true);

        float parentScale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), 0.0001f);
        float worldRadius = Mathf.Max(worldBounds.extents.x, worldBounds.extents.z);
        _indicator.transform.localScale = new Vector3(
            worldRadius * 2f / parentScale,
            0.002f / parentScale,
            worldRadius * 2f / parentScale);

        _indicator.GetComponent<MeshRenderer>().material = indicatorMaterial;
        _indicator.SetActive(false);
    }

    public void SetSelected(bool selected)
    {
        if (_indicator != null) _indicator.SetActive(selected);
    }
}
