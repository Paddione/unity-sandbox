using UnityEngine;

/// <summary>
/// Ash Lancer (act-one S3): keeps 8 m from the hero, shows a 0.7 s telegraph
/// line, then charges along it (14 m/s, up to 9 m). A hero within 1.2 m of
/// the lancer during the charge takes 4 damage once; afterwards the lancer is
/// exhausted for 2 s. Acts only inside its aggro range (no cross-map march).
/// </summary>
public sealed class AshLancer : MonoBehaviour
{
    public const float AggroRange = 16f;
    public const float HoldDistance = 8f;
    public const float Telegraph = 0.7f;
    public const float ChargeSpeed = 14f;
    public const float ChargeLength = 9f;
    public const float HitRadius = 1.2f;
    public const int ChargeDamage = 4;
    public const float Exhaust = 2f;
    public const float WalkSpeed = 2.5f;

    public enum State { Idle, Approach, Telegraph, Charge, Exhausted }

    public static int Charges;
    public static int HeroHits;

    public State Current { get; private set; } = State.Idle;
    float stateSince;
    Vector3 chargeDir;
    float charged;
    bool hitThisCharge;
    Transform line;

    /// <summary>Ansturmrichtung des laufenden Telegraphen (S2-Seam für Tests und Interrupts).</summary>
    public Vector3 TelegraphDir => chargeDir;

    /// <summary>Ist die Ansturmrichtung gerade sichtbar (Ziel- oder Telegraphenlinie)?</summary>
    public bool TelegraphVisible => line != null && line.gameObject.activeSelf;

    void Update() => Tick(Time.time, Time.deltaTime, Hero());

    static Transform Hero()
    {
        return HeroLocator.Solo();
    }

    /// <summary>State machine step (headless-callable with explicit time).</summary>
    public void Tick(float now, float dt, Transform hero)
    {
        if (hero != null && KilnLayout.PlazaSafe(hero.position))
        {
            // Fair-fights arrival plaza: hold while the hero orients.
            if (Current == State.Telegraph) ShowLine(false);
            Current = State.Idle;
            return;
        }
        Vector3 flat = hero != null ? hero.position - transform.position : Vector3.zero;
        flat.y = 0f;
        float dist = flat.magnitude;
        switch (Current)
        {
            case State.Idle:
            case State.Approach:
                if (hero == null || dist > AggroRange)
                {
                    Current = State.Idle;
                    ShowLine(false);
                    return;
                }
                Current = State.Approach;
                if (flat.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(flat);
                if (dist > HoldDistance + 0.5f)
                {
                    transform.position += flat.normalized * Mathf.Min(WalkSpeed * dt, dist - HoldDistance);
                    ShowAim(flat.normalized, dist); // S2: Ansturmrichtung schon beim Annähern sichtbar
                }
                else Enter(State.Telegraph, now, flat);
                break;
            case State.Telegraph:
                if (now - stateSince >= Telegraph) Enter(State.Charge, now, chargeDir);
                break;
            case State.Charge:
                float step = Mathf.Min(ChargeSpeed * dt, ChargeLength - charged);
                transform.position += chargeDir * step;
                charged += step;
                if (!hitThisCharge && hero != null && Diablo.IsLocalAuthority())
                {
                    var d = hero.position - transform.position;
                    d.y = 0f;
                    if (d.magnitude <= HitRadius)
                    {
                        hitThisCharge = true;
                        HeroHits++;
                        int dmg = DifficultyProfile.Current.ScaleDamage(ChargeDamage);
                        var hp = hero.GetComponent<PlayerHealth>();
                        if (hp != null) hp.TakeDamage(dmg);
                        DiabloJuice.OnPlayerHurt(hero.position, dmg);
                    }
                }
                if (charged >= ChargeLength - 0.001f) Enter(State.Exhausted, now, chargeDir);
                break;
            case State.Exhausted:
                if (now - stateSince >= Exhaust) Current = State.Approach;
                break;
        }
    }

    void Enter(State s, float now, Vector3 dir)
    {
        Current = s;
        stateSince = now;
        if (s == State.Telegraph)
        {
            chargeDir = dir.sqrMagnitude > 0.0001f ? dir.normalized : transform.forward;
            ShowLine(true);
        }
        else if (s == State.Charge)
        {
            charged = 0f;
            hitThisCharge = false;
            Charges++;
            ShowLine(false);
        }
    }

    void ShowLine(bool on)
    {
        ShowAim(chargeDir, ChargeLength, on);
    }

    /// <summary>Ziel- (Annäherung) oder Telegraphenlinie in Ansturmrichtung.</summary>
    void ShowAim(Vector3 dir, float length, bool on = true)
    {
        if (dir.sqrMagnitude < 0.0001f) dir = transform.forward;
        else dir = dir.normalized;
        chargeDir = dir;
        if (!Application.isPlaying) return;
        if (on && line == null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "LancerTelegraph";
            Destroy(go.GetComponent<Collider>());
            var r = go.GetComponent<Renderer>();
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (r != null && shader != null)
            {
                var m = new Material(shader) { color = new Color(1f, 0.4f, 0.1f) };
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", new Color(1f, 0.35f, 0.08f) * 1.5f);
                r.sharedMaterial = m;
            }
            line = go.transform;
        }
        if (line == null) return;
        line.gameObject.SetActive(on);
        if (on)
        {
            line.position = transform.position + dir * (length * 0.5f) + Vector3.up * 0.05f;
            line.rotation = Quaternion.LookRotation(dir);
            float width = Current == State.Telegraph ? 0.35f : 0.12f;
            line.localScale = new Vector3(width, 0.02f, length);
        }
    }

    void OnDestroy()
    {
        if (line != null) Destroy(line.gameObject);
    }

    /// <summary>
    /// Build unique (lancer-ward): a Cleave interrupt drops a charging or
    /// telegraphing lancer into the existing Exhausted recovery. No-op in any
    /// other state (Interrupt only valid mid-charge).
    /// </summary>
    public void InterruptToExhausted()
    {
        if (Current != State.Charge && Current != State.Telegraph) return;
        Current = State.Exhausted;
        stateSince = Time.time;
        ShowLine(false);
    }

    public static void ResetRun()
    {
        Charges = 0;
        HeroHits = 0;
    }
}
