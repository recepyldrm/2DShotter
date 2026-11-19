using UnityEngine;

public class EnemyType : MonoBehaviour
{
    public enum Type
    {
        Normal,
        Runner,
        Tank,
        Bomber,
        FireResistant,
        FreezeResistant
    }

    // ==================================
    // TYPE
    // ==================================

    [Header("Enemy Type")]
    public Type enemyType = Type.Normal;

    // ==================================
    // STATS
    // ==================================

    [Header("Stat Multipliers")]
    public float healthMultiplier = 1f;
    public float speedMultiplier = 1f;
    public float damageMultiplier = 1f;

    // ==================================
    // REWARDS
    // ==================================

    [Header("Rewards")]
    public float xpMultiplier = 1f;
    public float goldMultiplier = 1f;

    // ==================================
    // RESISTANCES
    // ==================================

    [Header("Resistances")]

    [Range(0f, 1f)]
    public float burnResistance = 0f;

    [Range(0f, 1f)]
    public float freezeResistance = 0f;

    // ==================================
    // BOMBER
    // ==================================

    [Header("Bomber")]

    [Tooltip("Bomber öldüğünde patlamanın menzili.")]
    public float explosionRadius = 2f;

    [Tooltip("Patlama oyuncuya ne kadar hasar verecek.")]
    public int explosionDamage = 20;

    [Tooltip("İsteğe bağlı patlama efekti.")]
    public GameObject explosionEffectPrefab;

    // ==================================
    // HELPERS
    // ==================================

    public bool IsBomber()
    {
        return enemyType == Type.Bomber;
    }

    // ==================================
    // EDITOR GIZMO
    // ==================================

    void OnDrawGizmosSelected()
    {
        if (enemyType != Type.Bomber)
            return;

        Gizmos.DrawWireSphere(
            transform.position,
            explosionRadius
        );
    }
}