using UnityEngine;
using System.Collections;

public class MuzzleFlash : MonoBehaviour
{
    [Header("Görünme Süresi")]
    public float showDuration = 0.05f;

    [Header("Rastgelelik")]
    public bool randomizeScale = true;
    public float minScale = 0.8f;
    public float maxScale = 1.2f;

    public bool randomizeRotation = true;

    private SpriteRenderer spriteRenderer;
    private Coroutine activeRoutine;
    private Vector3 originalScale;
    private bool initialized = false;

   void EnsureInitialized()
{
    if (initialized)
        return;

    spriteRenderer = GetComponent<SpriteRenderer>();
    originalScale = transform.localScale;
    initialized = true;

    GameLog.Log("MuzzleFlash originalScale: " + originalScale + " | spriteRenderer null mu: " + (spriteRenderer == null));
}

    public void Show()
    {
        GameLog.Log("MuzzleFlash.Show() çağrıldı!");
        EnsureInitialized();

        gameObject.SetActive(true);

        if (randomizeScale)
        {
            float scaleMultiplier = Random.Range(minScale, maxScale);
            transform.localScale = originalScale * scaleMultiplier;
        }

        if (randomizeRotation)
        {
            transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        }

        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
        }

        activeRoutine = StartCoroutine(HideAfterDelay());
    }

    IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(showDuration);

        gameObject.SetActive(false);
        activeRoutine = null;
    }
}