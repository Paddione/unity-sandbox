using UnityEngine;

/// <summary>
/// Zone objective for the active contract (zone-contracts S2). The board
/// spawns exactly one per accepted contract at the frozen target position:
/// a Hunt reports the named elite's death through
/// <see cref="IEnemyDeathHook"/>, a Recovery completes on hero interaction in
/// reach. The runtime stays on the solo path (no netcode, like the stash).
/// </summary>
public sealed class ContractObjective : MonoBehaviour, IEnemyDeathHook
{
    /// <summary>Collect reach around a relic (same scale as the stash).</summary>
    public const float CollectRadius = 2.5f;

    /// <summary>Scene object name prefix (tests find the single objective).</summary>
    public const string ObjectivePrefix = "ContractObjective_";

    public string objectiveId = string.Empty;
    public ContractType kind;

    /// <summary>Spawn the single objective for an accepted offer. Null when already done.</summary>
    public static ContractObjective SpawnFor(ContractOffer offer)
    {
        if (offer == null || string.IsNullOrEmpty(offer.id)) return null;
        var go = new GameObject(ObjectivePrefix + offer.id);
        go.transform.position = offer.position;
        if (offer.type == ContractType.Recovery)
        {
            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = CollectRadius;
        }
        var obj = go.AddComponent<ContractObjective>();
        obj.objectiveId = offer.id;
        obj.kind = offer.type;
        Debug.Log($"[Contract] Objective spawned: {offer.id} ({offer.type} at {offer.position}).");
        return obj;
    }

    /// <summary>Pure collect-reach rule (EditMode-pinnable, no scene).</summary>
    public static bool CanCollect(Vector3 heroPos, Vector3 relicPos)
    {
        var d = heroPos - relicPos;
        d.y = 0f;
        return d.sqrMagnitude <= CollectRadius * CollectRadius;
    }

    /// <summary>Elite death hook: only our frozen objective id completes.</summary>
    public void OnEnemyDeath()
    {
        if (kind != ContractType.Hunt) return;
        if (ContractProgress.TryComplete(objectiveId))
            Debug.Log($"[Contract] Hunt {objectiveId} slain.");
    }

    /// <summary>Relic hand-in: hero in reach completes the frozen contract.</summary>
    public static bool Collect(ContractObjective objective, Vector3 heroPos)
    {
        if (objective == null || objective.kind != ContractType.Recovery) return false;
        if (!CanCollect(heroPos, objective.transform.position)) return false;
        return ContractProgress.TryComplete(objective.objectiveId);
    }

    Transform cachedHero;

    void Update()
    {
        if (kind != ContractType.Recovery) return;
        if (ContractProgress.Status != ContractStatus.Active) return;
        // Perf: kein GameObject.Find pro Frame (kostete ~2 fps im Autoplay).
        // Der Held lebt über die Szene; nur bei Verlust neu suchen.
        if (cachedHero == null)
        {
            var hero = GameObject.Find("Player");
            if (hero == null) return;
            cachedHero = hero.transform;
        }
        if (Collect(this, cachedHero.position))
        {
            Debug.Log($"[Contract] Relic {objectiveId} recovered.");
            cachedHero = null; // Vertraglich erledigt, Referenzen freigeben
        }
    }
}
