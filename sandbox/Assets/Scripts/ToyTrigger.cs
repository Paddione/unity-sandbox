using UnityEngine;

/// <summary>
/// Greybox toy-vs-goal wiring (Q2 trigger interaction, Q3 toy/goal loop).
/// Primitives only, no packages: a trigger zone (counter), pickup orbs,
/// and a goal pad that opens once enough orbs are collected.
/// State is static so the headless run log can report it without UI.
/// </summary>
public sealed class ToyTrigger : MonoBehaviour
{
    public enum Mode { Counter, Pickup, Goal }

    public Mode mode = Mode.Counter;

    public const int Needed = 3;

    public static int TriggerEntries;
    public static int Pickups;
    public static bool GoalOpen;

    bool collected;

    void OnTriggerEnter(Collider other)
    {
        if (other == null || other.gameObject == null) return;
        if (other.gameObject.name != "Player") return;

        switch (mode)
        {
            case Mode.Counter:
                TriggerEntries++;
                Debug.Log($"[Toy] Trigger zone entered by Player (entries={TriggerEntries}).");
                break;
            case Mode.Pickup:
                if (collected) return;
                collected = true;
                Pickups++;
                Debug.Log($"[Toy] Orb picked up ({Pickups}/{Needed}): {gameObject.name}.");
                var col = GetComponent<Collider>();
                if (col != null) col.enabled = false;
                var rend = GetComponent<Renderer>();
                if (rend != null) rend.enabled = false;
                ToyTriggerShared.RefreshGoal();
                break;
            case Mode.Goal:
                if (Pickups >= Needed)
                {
                    GoalOpen = true;
                    ToyTriggerShared.PaintGoal(gameObject, true);
                    Debug.Log($"[Toy] Goal reached with {Pickups}/{Needed} orbs. Loop complete.");
                }
                else
                {
                    Debug.Log($"[Toy] Goal locked ({Pickups}/{Needed} orbs). Collect orbs first.");
                }
                break;
        }
    }

    /// <summary>Reset run state (scene load / headless verify).</summary>
    public static void ResetRun()
    {
        TriggerEntries = 0;
        Pickups = 0;
        GoalOpen = false;
    }
}

/// <summary>Shared helpers so pickups can arm the goal pad.</summary>
public static class ToyTriggerShared
{
    public static void RefreshGoal()
    {
        var goal = GameObject.Find("GoalPad");
        if (goal == null) return;
        bool open = ToyTrigger.Pickups >= ToyTrigger.Needed;
        PaintGoal(goal, open);
        if (open) Debug.Log("[Toy] Goal pad armed (3/3 orbs).");
    }

    public static void PaintGoal(GameObject goal, bool open)
    {
        var rend = goal.GetComponent<Renderer>();
        if (rend == null) return;
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"))
        {
            color = open ? new Color(0.2f, 0.8f, 0.3f) : new Color(0.8f, 0.25f, 0.25f)
        };
        rend.sharedMaterial = mat;
    }
}
