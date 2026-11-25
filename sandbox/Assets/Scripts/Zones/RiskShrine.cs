using UnityEngine;

/// <summary>
/// Optional solo risk shrine in the Ashen Fen (cinderreach-risk-shrines S2).
/// Click within <see cref="InteractRadius"/> to open
/// <see cref="DiabloRiskShrineUI"/>: heal to max HP now, or name an empowered
/// elite (+25% HP) whose death leaves a deterministic rare (run seed) to pick
/// up at the shrine. The choice is one-shot and reload-proof; the hero's death
/// never consumes the challenge; only the active challenge spawns its elite.
/// Waypoints and quests stay untouched (no coupling to #7/#10 systems).
/// </summary>
public sealed class RiskShrine : MonoBehaviour, IUsable
{
    /// <summary>Scene object name (pinned by RiskShrineBuilder verify).</summary>
    public const string ShrineObjectName = "RiskShrine";

    /// <summary>Fen anchor of the shrine (runtime copy of the builder anchor;
    /// PlayMode tests cannot see Editor/ code).</summary>
    public static readonly Vector3 ShrineAnchor = new Vector3(6.5f, 0f, 7.5f);

    /// <summary>Click/trade reach around the shrine.</summary>
    public const float InteractRadius = 3.5f;

    /// <summary>Base elite HP before the shrine surcharge.</summary>
    public const int EliteBaseHP = 15;

    /// <summary>Surcharge on the shrine elite (start value for the playtest).</summary>
    public const float EliteHpBonus = 1.25f;

    public static int EliteHP => Mathf.CeilToInt(EliteBaseHP * EliteHpBonus);

    /// <summary>Name of the spawned challenge elite.</summary>
    public const string EliteName = "Shrine Elite";

    /// <summary>Skeleton model for the elite (assigned by the builder); scene template wins when null.</summary>
    public GameObject eliteModel;

    GameObject elite;

    public RiskShrineProgress Progress => RunProfile.RiskShrine;

    void Start()
    {
        EnsureChallengeSpawn();
    }

    /// <summary>Pure trade-reach rule (EditMode-pinnable, no scene).</summary>
    public static bool CanInteract(Vector3 heroPos, Vector3 shrinePos, float radius = InteractRadius)
    {
        var d = heroPos - shrinePos;
        d.y = 0f;
        return d.sqrMagnitude <= radius * radius;
    }

    /// <summary>Heal path: one-shot, heals the hero to max, persists. False when already chosen.</summary>
    public bool ChooseHeal(GameObject hero)
    {
        if (hero == null || !Progress.ChooseHeal()) return false;
        var health = hero.GetComponent<PlayerHealth>();
        if (health != null) health.Heal(health.MaxHP);
        Debug.Log("[RiskShrine] Chose healing: restored to max HP.");
        if (Diablo.IsLocalAuthority()) SaveService.SaveNow(hero);
        return true;
    }

    /// <summary>Challenge path: one-shot, spawns the empowered elite, persists. False when already chosen.</summary>
    public bool ChooseChallenge(GameObject hero)
    {
        if (hero == null || !Progress.ChooseChallenge(RunProfile.RunSeed)) return false;
        SpawnElite();
        Debug.Log($"[RiskShrine] Chose the challenge: {EliteName} ({EliteHP} HP) hunts the fen.");
        if (Diablo.IsLocalAuthority()) SaveService.SaveNow(hero);
        return true;
    }

    /// <summary>Claim the waiting rare into the hero's bag (atomic, see progress). False when nothing waits.</summary>
    public bool ClaimReward(GameObject hero)
    {
        bool ok = Progress.TryClaim(hero);
        if (ok) Debug.Log("[RiskShrine] Claimed the shrine reward.");
        return ok;
    }

    /// <summary>Re-spawn the challenge elite after a reload (only while the challenge is active).</summary>
    public void EnsureChallengeSpawn()
    {
        if (Progress.State != RiskShrineState.ChallengeActive) return;
        if (elite != null) return;
        SpawnElite();
        Debug.Log("[RiskShrine] Challenge restored after reload: elite hunts again.");
    }

    void SpawnElite()
    {
        if (elite != null) return;
        var found = GameObject.Find(EliteName);
        if (found != null && found.GetComponent<EnemyHealth>() != null)
        {
            elite = found;
            return;
        }
        // Skeleton body (cinderreach-companion-rendering): the bare capsule
        // rendered magenta in the URP player. Explicit builder model wins,
        // else the scene husk template, else a URP-tinted capsule.
        elite = HuskModelLibrary.SpawnHusk(EliteName,
            transform.position + new Vector3(2f, 0.05f, 0f),
            1.3f, EliteHP, LootTier.Elite, 2, eliteModel, netServer: false);
        var hook = elite.AddComponent<RiskShrineDeathRelay>();
        hook.shrine = this;
    }

    /// <summary>Elite down: roll the deterministic rare and keep the claim open at the shrine.</summary>
    public void OnEliteKilled(GameObject hero)
    {
        if (Progress.State != RiskShrineState.ChallengeActive) return;
        Progress.NotifyEliteKilled(RiskShrineProgress.RollRare(
            Progress.ChallengeSeed >= 0 ? Progress.ChallengeSeed : RunProfile.RunSeed));
        elite = null;
        Debug.Log("[RiskShrine] Elite down: a rare waits at the shrine.");
        if (hero != null && Diablo.IsLocalAuthority()) SaveService.SaveNow(hero);
    }

    // Perf: Camera.main sucht pro Aufruf per Tag durch die Szene — erst nach
    // dem Klick-Gate anfassen und cachen (Beleg: Zone-Contracts kostete
    // dieselbe Stelle ~2 fps im Autoplay).
    static Camera s_main;

    void Update()
    {
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;
        if (s_main == null) s_main = Camera.main;
        if (s_main == null) return;
        var hero = GameObject.Find("Player"); // nur auf Klick-Frames, ok
        if (hero == null) return;
        var ray = s_main.ScreenPointToRay(mouse.position.ReadValue());
        if (!Physics.Raycast(ray, out var hit, 200f) || hit.collider == null || hit.collider.gameObject != gameObject) return;
        if (!CanInteract(hero.transform.position, transform.position)) return;
        Use(hero);
    }

    public float UseRadius => InteractRadius;
    public string UseLabel => Loc.T("use.risk");
    /// <summary>Shrines in the scene (gear-slots S2: standing at one refills the flask).</summary>
    public static readonly System.Collections.Generic.List<RiskShrine> Live = new System.Collections.Generic.List<RiskShrine>();

    void OnEnable()
    {
        Usables.Register(this);
        if (!Live.Contains(this)) Live.Add(this);
    }

    void OnDisable()
    {
        Usables.Unregister(this);
        Live.Remove(this);
    }

    public void Use(GameObject hero)
    {
        if (hero == null) return;
        var ui = hero.GetComponent<DiabloRiskShrineUI>();
        if (ui == null) ui = hero.AddComponent<DiabloRiskShrineUI>();
        ui.SetOpen(true);
    }
}

/// <summary>Forwards the elite's death hook to the shrine (lives on the elite, dies with it).</summary>
public sealed class RiskShrineDeathRelay : MonoBehaviour, IEnemyDeathHook
{
    public RiskShrine shrine;

    public void OnEnemyDeath()
    {
        if (shrine != null) shrine.OnEliteKilled(GameObject.Find("Player"));
    }
}
