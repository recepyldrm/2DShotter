using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    [Header("Kayma Hızı")]
    [Tooltip("Negatif değer sola, pozitif değer sağa kaydırır. Uzak katmanlar yavaş, yakın katmanlar hızlı olmalı.")]
    public float scrollSpeed = -0.5f;

    [Header("Loop Ayarı")]
    [Tooltip("Bu katmanın tek bir kopyasının genişliği (world units). Sprite'ın gerçek genişliğine göre ayarla.")]
    public float textureWidth = 20f;

    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        transform.position += Vector3.right * scrollSpeed * Time.deltaTime;

        // Belirli bir mesafe kayınca başa sar (loop)
        float distanceMoved = transform.position.x - startPosition.x;

        if (Mathf.Abs(distanceMoved) >= textureWidth)
        {
            float offset = distanceMoved % textureWidth;
            transform.position = new Vector3(startPosition.x + offset, transform.position.y, transform.position.z);
        }
    }
}