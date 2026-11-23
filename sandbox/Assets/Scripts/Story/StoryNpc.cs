using UnityEngine;

/// <summary>
/// A named story figure in the Hold (cinderreach-deutsch-story S4/S5): click
/// opens its scripted talk, a floating mark shows an offer or a hand-in.
/// Merle only stands in the Hold once Act I is done. Shops (Ilsabe, Jorin)
/// keep their own click handling and call the director first.
/// </summary>
public sealed class StoryNpc : MonoBehaviour
{
    public string npcId;
    public bool shopNpc;
    TextMesh marker;
    Renderer[] renderers;
    Collider body;
    float poll;

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        body = GetComponent<Collider>();
        var go = new GameObject("StoryMarker");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 2.35f, 0f);
        marker = go.AddComponent<TextMesh>();
        marker.anchor = TextAnchor.MiddleCenter;
        marker.alignment = TextAlignment.Center;
        marker.fontSize = 64;
        marker.characterSize = 0.09f;
        marker.color = DiabloPalette.GoldBright;
        marker.text = "";
    }

    void Update()
    {
        if (npcId == StoryIds.Merle)
        {
            bool here = RunProfile.Act.ActOneComplete;
            if (body != null) body.enabled = here;
            if (renderers != null) foreach (var r in renderers) if (r != null) r.enabled = here;
            if (marker != null) marker.gameObject.SetActive(here);
            if (!here) return;
        }
        if (marker != null && Camera.main != null)
            marker.transform.rotation = Camera.main.transform.rotation;
        poll -= Time.unscaledDeltaTime;
        if (poll > 0f) return;
        poll = 0.3f;
        if (marker == null) return;
        var plan = StoryDialogs.Plan(StoryDirector.State, npcId);
        marker.text = plan.IsPending ? "!" : "";

        if (shopNpc) return;
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame || Camera.main == null) return;
        var hero = GameObject.Find(DiabloNetworkPlayer.HeroName);
        if (hero == null || DiabloUIFactory.IsWorldInputBlocked(hero)) return;
        var ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());
        if (!Physics.Raycast(ray, out var hit, 200f)) return;
        if (hit.collider == null || hit.collider.gameObject != gameObject) return;
        if (!Vendor.CanInteract(hero.transform.position, transform.position)) return;
        StoryDirector.TryOpenTalk(hero, npcId);
    }
}
