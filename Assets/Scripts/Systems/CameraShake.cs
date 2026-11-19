using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;

    private Vector3 originalLocalPosition;
    private Coroutine activeShakeCoroutine;

    void Awake()
    {
        Instance = this;
        originalLocalPosition = transform.localPosition;
    }

    // magnitude: sarsıntı şiddeti (örn. 0.1f küçük, 0.3f büyük)
    // duration: ne kadar süre sarsılsın (örn. 0.15f)
    public void Shake(float magnitude, float duration)
    {
        if (activeShakeCoroutine != null)
        {
            StopCoroutine(activeShakeCoroutine);
            transform.localPosition = originalLocalPosition;
        }

        activeShakeCoroutine = StartCoroutine(ShakeRoutine(magnitude, duration));
    }

    IEnumerator ShakeRoutine(float magnitude, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float offsetX = Random.Range(-1f, 1f) * magnitude;
            float offsetY = Random.Range(-1f, 1f) * magnitude;

            transform.localPosition = originalLocalPosition + new Vector3(offsetX, offsetY, 0f);

            elapsed += Time.unscaledDeltaTime; // shop açıkken (timeScale=0) bile sorunsuz çalışsın
            yield return null;
        }

        transform.localPosition = originalLocalPosition;
        activeShakeCoroutine = null;
    }
}