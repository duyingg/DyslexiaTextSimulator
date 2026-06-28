using UnityEngine;
using Global;

public class PreviewBox : MonoBehaviour
{
    public RectTransform top;
    public RectTransform bottom;
    public RectTransform left;
    public RectTransform right;
    public float thickness = 2f;
    public float size = 200;
    
    public void SetBox()
    {
        float helf = size / 2;
        top.sizeDelta = new Vector2(size, thickness);
        top.anchoredPosition = new Vector2(GlobalConfig.gazePoint.x, GlobalConfig.gazePoint.y + helf);
        bottom.sizeDelta = new Vector2(size, thickness);
        bottom.anchoredPosition = new Vector2(GlobalConfig.gazePoint.x, GlobalConfig.gazePoint.y - helf);
        left.sizeDelta = new Vector2(thickness, size);
        left.anchoredPosition = new Vector2(GlobalConfig.gazePoint.x - helf, GlobalConfig.gazePoint.y);
        right.sizeDelta = new Vector2(thickness, size);
        right.anchoredPosition = new Vector2(GlobalConfig.gazePoint.x + helf, GlobalConfig.gazePoint.y);
    }
    private void Update()
    {
        SetBox();
    }
}