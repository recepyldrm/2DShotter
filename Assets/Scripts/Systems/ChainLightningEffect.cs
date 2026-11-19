using UnityEngine;

public class ChainLightningEffect : MonoBehaviour
{
    [Header("Line Renderer")]
    public LineRenderer lineRenderer;

    [Header("Yıldırım Ayarları")]
    [Range(4, 30)]
    public int segmentCount = 12;

    public float zigzagAmount = 0.12f;

    [Header("Titreşim")]
    public float refreshRate = 0.025f;

    [Header("Süre")]
    public float duration = 0.15f;

    [Header("Pool")]
    public string poolKey = GameKeys.ChainLightningPool;

    private Vector3 startPosition;
    private Vector3 endPosition;

    private float lifeTimer;
    private float refreshTimer;

    private bool isPlaying = false;

    void Awake()
    {
        if (lineRenderer == null)
        {
            lineRenderer =
                GetComponent<LineRenderer>();
        }
    }

    void OnEnable()
    {
        isPlaying = false;

        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 0;
        }
    }

    void OnDisable()
    {
        isPlaying = false;

        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 0;
        }
    }

    public void Play(
        Vector3 start,
        Vector3 end
    )
    {
        if (lineRenderer == null)
        {
            Debug.LogError(
                "ChainLightningEffect: LineRenderer bulunamadı!"
            );

            ReturnToPool();
            return;
        }

        startPosition = start;
        endPosition = end;

        lifeTimer = duration;
        refreshTimer = 0f;

        isPlaying = true;

        CreateLightning();
    }

    void Update()
    {
        if (!isPlaying)
            return;

        lifeTimer -= Time.deltaTime;
        refreshTimer -= Time.deltaTime;

        if (refreshTimer <= 0f)
        {
            refreshTimer = refreshRate;

            CreateLightning();
        }

        if (lifeTimer <= 0f)
        {
            ReturnToPool();
        }
    }

    void CreateLightning()
    {
        if (lineRenderer == null)
            return;

        if (segmentCount < 2)
            segmentCount = 2;

        lineRenderer.positionCount =
            segmentCount;

        Vector3 direction =
            endPosition -
            startPosition;

        Vector3 perpendicular =
            new Vector3(
                -direction.y,
                direction.x,
                0f
            ).normalized;

        for (int i = 0; i < segmentCount; i++)
        {
            float t =
                i /
                (float)(segmentCount - 1);

            Vector3 position =
                Vector3.Lerp(
                    startPosition,
                    endPosition,
                    t
                );

            if (
                i != 0 &&
                i != segmentCount - 1
            )
            {
                float randomOffset =
                    Random.Range(
                        -zigzagAmount,
                        zigzagAmount
                    );

                position +=
                    perpendicular *
                    randomOffset;
            }

            lineRenderer.SetPosition(
                i,
                position
            );
        }
    }

    void ReturnToPool()
    {
        if (!isPlaying)
            return;

        isPlaying = false;

        if (
            ObjectPool.Instance != null &&
            !string.IsNullOrEmpty(poolKey)
        )
        {
            ObjectPool.Instance.Despawn(
                poolKey,
                gameObject
            );
        }
        else
        {
            Destroy(gameObject);
        }
    }
}