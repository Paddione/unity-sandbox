using System.Collections;
using UnityEngine;

/// <summary>
/// Hit points for Diablo husks (Q5). Death drops a pickup orb (Q3 semantics:
/// yellow sphere + <see cref="ToyTrigger"/> pickup mode) and increments the
/// shared kill counter. KayKit shares one material per pack, so the hit
/// flash uses a material instance (<c>renderer.material</c>), never the
/// shared asset.
/// </summary>
public sealed class EnemyHealth : MonoBehaviour
{
    public int maxHP = 3;

    public static int HusksKilled;
    public static int OrbsDropped;

    int hp;
    bool dead;

    void Awake()
    {
        hp = maxHP;
    }

    /// <summary>Apply damage; kills at zero and drops a loot orb.</summary>
    public void TakeDamage(int amount)
    {
        if (dead) return;
        hp -= amount;
        Debug.Log($"[Diablo] {gameObject.name} took {amount} dmg ({Mathf.Max(hp, 0)}/{maxHP}).");
        if (hp <= 0)
        {
            Die();
            return;
        }
        StartCoroutine(Flash());
    }

    void Die()
    {
        dead = true;
        HusksKilled++;
        Debug.Log($"[Diablo] {gameObject.name} died (kills={HusksKilled}).");
        DropOrb();
        Destroy(gameObject);
    }

    void DropOrb()
    {
        var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        OrbsDropped++;
        orb.name = $"DiabloOrb_{OrbsDropped}";
        orb.transform.position = transform.position + Vector3.up * 0.8f;
        orb.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"))
        {
            color = new Color(1f, 0.85f, 0.2f)
        };
        orb.GetComponent<Renderer>().sharedMaterial = mat;
        orb.GetComponent<SphereCollider>().isTrigger = true;
        orb.AddComponent<ToyTrigger>().mode = ToyTrigger.Mode.Pickup;
    }

    IEnumerator Flash()
    {
        var rend = GetComponent<Renderer>();
        if (rend == null) yield break;
        var mat = rend.material;
        var baseColor = mat.color;
        mat.color = new Color(1f, 0.25f, 0.25f);
        yield return new WaitForSeconds(0.08f);
        if (mat != null) mat.color = baseColor;
    }

    /// <summary>Reset run state (scene load / headless tests).</summary>
    public static void ResetRun()
    {
        HusksKilled = 0;
        OrbsDropped = 0;
    }
}
