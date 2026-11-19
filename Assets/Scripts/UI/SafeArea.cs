using UnityEngine;

public class SafeArea : MonoBehaviour
{
    private RectTransform panel;
    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;

    void Awake()
    {
        panel = GetComponent<RectTransform>();
        ApplySafeArea();
    }

    void Update()
    {
        if (lastSafeArea != Screen.safeArea ||
            lastScreenSize.x != Screen.width ||
            lastScreenSize.y != Screen.height)
        {
            ApplySafeArea();
        }
    }

    void ApplySafeArea()
    {
        Rect safeArea = Screen.safeArea;

        lastSafeArea = safeArea;
        lastScreenSize = new Vector2Int(
            Screen.width,
            Screen.height
        );

        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax =
            safeArea.position + safeArea.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;

        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        panel.anchorMin = anchorMin;
        panel.anchorMax = anchorMax;

        panel.offsetMin = Vector2.zero;
        panel.offsetMax = Vector2.zero;
    }
}