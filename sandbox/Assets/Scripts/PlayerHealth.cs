using UnityEngine;

/// <summary>
/// Hero hit points for the Blood Moor (campfire heal target). Max tracks
/// <see cref="Equipment"/> so armor still matters; no enemy damage calls this
/// yet (husks stay passive), so there is no death handling in this cut.
/// Local-only per client, like <see cref="Equipment"/>.
/// </summary>
public sealed class PlayerHealth : MonoBehaviour
{
    float currentHP = -1f;

    Equipment equipment;

    void Awake()
    {
        equipment = GetComponent<Equipment>();
        Refresh();
    }

    public float MaxHP => equipment != null ? equipment.GetMaxHP() : Equipment.BaseMaxHP;

    public float CurrentHP
    {
        get
        {
            if (currentHP < 0f) currentHP = MaxHP;
            return Mathf.Min(currentHP, MaxHP);
        }
    }

    public void Heal(float amount)
    {
        if (amount <= 0f) return;
        currentHP = Mathf.Clamp(CurrentHP + amount, 0f, MaxHP);
    }

    public void TakeDamage(float amount)
    {
        if (amount <= 0f) return;
        currentHP = Mathf.Clamp(CurrentHP - amount, 0f, MaxHP);
    }

    /// <summary>Clamp current HP after equipment swaps change the max.</summary>
    public void Refresh()
    {
        currentHP = Mathf.Clamp(CurrentHP, 0f, MaxHP);
    }
}
