using UnityEngine;

public class BurnParticle : MonoBehaviour
{
    private ParticleSystem particleSystemComponent;

    void Awake()
    {
        particleSystemComponent =
            GetComponent<ParticleSystem>();
    }

    void OnEnable()
    {
        if (particleSystemComponent == null)
            return;

        particleSystemComponent.Clear();
        particleSystemComponent.Play();
    }

    void OnDisable()
    {
        if (particleSystemComponent == null)
            return;

        particleSystemComponent.Stop();
        particleSystemComponent.Clear();
    }
}