using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Runtime story wiring (cinderreach-deutsch-story S4): polls the act state,
/// books zone entries, grants quest rewards once, shows toasts and the HUD
/// tracker, autosaves on change and lets the co-op host broadcast its story
/// snapshot. Pure quest logic lives in <see cref="StoryState"/>.
/// </summary>
public sealed class StoryDirector : MonoBehaviour
{
    public static StoryState State => RunProfile.Story;

    float poll;
    string lastZone;
    int lastRevision = -1;
    readonly Queue<string> toasts = new Queue<string>();
    float toastLeft;
    TextMesh trackerLabel;
    Canvas canvas;
    UnityEngine.UI.Text toastText;
    UnityEngine.UI.Text trackerText;

    void Awake()
    {
        if (GetComponent<StoryDialogUI>() == null) gameObject.AddComponent<StoryDialogUI>();
        if (GetComponent<StoryQuestUI>() == null) gameObject.AddComponent<StoryQuestUI>();
    }

    void Start()
    {
        BuildHud();
        lastZone = WorldBounds.ZoneAt(transform.position).name;
        State.Refresh(RunProfile.Act);
        State.TakeChanges();
        SyncDoneFlags();
        lastRevision = State.Revision;
    }

    void BuildHud()
    {
        var canvasGo = new GameObject("StoryCanvas");
        canvasGo.transform.SetParent(transform, false);
        canvas = DiabloUIFactory.ConfigureOverlayCanvas(canvasGo, sortingOrder: 25, raycaster: false);
        trackerText = DiabloUIFactory.MakeText(canvasGo.transform, "StoryTracker", "", 16,
            DiabloPalette.GoldBright, TextAnchor.UpperLeft);
        var rt = trackerText.rectTransform;
        rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(0f, 1f); rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(18f, -14f); rt.sizeDelta = new Vector2(620f, 48f);
        toastText = DiabloUIFactory.MakeText(canvasGo.transform, "StoryToast", "", 19,
            DiabloPalette.Parchment, TextAnchor.MiddleCenter);
        var tr = toastText.rectTransform;
        tr.anchorMin = new Vector2(0.5f, 1f); tr.anchorMax = new Vector2(0.5f, 1f); tr.pivot = new Vector2(0.5f, 1f);
        tr.anchoredPosition = new Vector2(0f, -64f); tr.sizeDelta = new Vector2(760f, 36f);
        toastText.enabled = false;
    }

    void Update()
    {
        if (DiabloUIFactory.IsRemoteMirror(this)) return;
        if (toastLeft > 0f)
        {
            toastLeft -= Time.unscaledDeltaTime;
            if (toastLeft <= 0f) { toastText.enabled = false; ShowNextToast(); }
        }
        poll -= Time.unscaledDeltaTime;
        if (poll > 0f) return;
        poll = 0.25f;

        State.Refresh(RunProfile.Act);
        var zone = WorldBounds.ZoneAt(transform.position).name;
        if (lastZone != null && zone != lastZone) BookEnter(zone);
        lastZone = zone;
        SyncDoneFlags();
        GrantRewards();
        DrainToasts();
        RefreshTracker();

        if (lastRevision >= 0 && State.Revision != lastRevision)
        {
            var relay = GetComponent<DiabloNetworkPlayer>();
            bool networked = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
            if (!networked || (relay != null && relay.IsServer))
                SaveService.SaveNow(gameObject);
            if (relay != null && relay.IsSpawned && relay.IsServer)
                relay.BroadcastStory(State.Snapshot());
        }
        lastRevision = State.Revision;
    }

    void SyncDoneFlags()
    {
        foreach (var q in StoryCatalog.All)
            if (State.Status(q.id) == StoryStatus.Done) State.Flags.Add(q.id);
    }

    void GrantRewards()
    {
        foreach (var q in StoryCatalog.All)
        {
            if (State.Status(q.id) != StoryStatus.Done || q.reward == StoryReward.None) continue;
            string flag = "reward:" + q.id;
            if (State.Flags.Contains(flag)) continue;
            if (q.rewardGold > 0) Wallet.EnsureWallet(gameObject)?.Add(q.rewardGold);
            bool granted = true;
            if (q.reward == StoryReward.SmithKit)
            {
                RunProfile.SmithingMaterial += 3;
                Toast(Loc.F("story.reward.item", Loc.T("story.reward.material").Replace("{0}", "3")));
            }
            else if (q.reward == StoryReward.RareHelm || q.reward == StoryReward.SeerCirclet)
            {
                var item = q.reward == StoryReward.SeerCirclet
                    ? ItemDatabase.CreateUnique("Seer's Circlet")
                    : RollRareHelm();
                var inv = GetComponent<Inventory>();
                granted = inv != null && inv.Add(item);
                if (granted) Toast(Loc.F("story.reward.item", LootDrop.PrettyName(
                    item.rarity == ItemRarity.Unique ? item.uniqueId : item.baseType)));
            }
            if (granted) State.Flags.Add(flag);
        }
    }

    static ItemInstance RollRareHelm()
    {
        var rng = new System.Random(RunProfile.RunSeed ^ 0x51ab);
        var item = new ItemInstance
        {
            slot = ItemSlot.Helmet,
            rarity = ItemRarity.Rare,
            baseType = "Helm",
            itemLevel = 5,
            affixTier = 1,
        };
        item.affixes = AffixLibrary.RollAffixes(item.slot, item.rarity, rng, item.affixTier, item.itemLevel);
        item.EnsureIdentity();
        return item;
    }

    void DrainToasts()
    {
        foreach (var c in State.TakeChanges())
        {
            var q = StoryCatalog.Get(c.questId);
            if (q == null) continue;
            if (c.objective < 0 && c.status == StoryStatus.Active) Toast(Loc.F("story.quest.new", Loc.T(q.TitleKey)));
            else if (c.objective < 0 && c.status == StoryStatus.Ready) Toast(Loc.F("story.quest.ready", Loc.T(q.TitleKey)));
            else if (c.objective < 0 && c.status == StoryStatus.Done) Toast(Loc.F("story.quest.done", Loc.T(q.TitleKey)));
        }
        ShowNextToast();
    }

    void ShowNextToast()
    {
        if (toastText == null || toastText.enabled || toasts.Count == 0) return;
        toastText.text = toasts.Dequeue();
        toastText.enabled = true;
        toastLeft = 3.5f;
    }

    /// <summary>Queue a line on the story toast (pickups, rewards).</summary>
    public void Toast(string text)
    {
        if (!string.IsNullOrEmpty(text)) toasts.Enqueue(text);
        ShowNextToast();
    }

    void RefreshTracker()
    {
        if (trackerText == null) return;
        var q = State.Tracked();
        if (q == null) { trackerText.text = ""; return; }
        if (State.Status(q.id) == StoryStatus.Ready)
        {
            trackerText.text = Loc.F("story.tracker.ready", Loc.T(q.TitleKey), Loc.T("npc." + q.giver + ".name"));
            return;
        }
        for (int i = 0; i < q.objectives.Length; i++)
        {
            if (State.ObjectiveDone(q, i)) continue;
            var o = q.objectives[i];
            trackerText.text = o.count > 1
                ? Loc.T(q.TitleKey) + " — " + Loc.T(o.textKey) + "  " + State.Count(q.id, i) + "/" + o.count
                : Loc.T(q.TitleKey) + " — " + Loc.T(o.textKey);
            return;
        }
        trackerText.text = Loc.T(q.TitleKey);
    }

    // ---- booking (static so kills, pickups, tests and the network can call) ----

    public static void BookKill(string objectName)
    {
        State.Apply(StoryObjectiveType.Kill, StoryIds.KillTarget(objectName ?? ""));
        State.Refresh(RunProfile.Act);
    }

    public static bool BookCollect(string itemId)
    {
        bool counted = State.Apply(StoryObjectiveType.Collect, itemId);
        if (counted) State.Refresh(RunProfile.Act);
        return counted;
    }

    public static void BookEnter(string zoneName)
    {
        State.Apply(StoryObjectiveType.Enter, zoneName);
        State.Refresh(RunProfile.Act);
    }

    /// <summary>Books the pending talk with <paramref name="npc"/> after its dialog ran. Co-op clients forward it to the host.</summary>
    public static StoryTalkPlan BookTalk(string npc, GameObject hero)
    {
        var plan = StoryDialogs.Plan(State, npc);
        if (!plan.IsPending) return plan;
        var relay = hero != null ? hero.GetComponent<DiabloNetworkPlayer>() : null;
        bool networked = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        if (networked && relay != null && relay.IsSpawned && !relay.IsServer)
        {
            relay.SubmitStoryTalkServerRpc(npc);
            return plan;
        }
        BookTalkLocal(npc);
        return plan;
    }

    /// <summary>Host/solo talk booking (also the ServerRpc target on the host).</summary>
    public static void BookTalkLocal(string npc)
    {
        var plan = StoryDialogs.Plan(State, npc);
        if (!plan.IsPending) return;
        switch (plan.kind)
        {
            case StoryTalkKind.Offer: State.Accept(plan.questId); break;
            case StoryTalkKind.TurnIn: if (State.TurnIn(plan.questId)) DiabloAudio.PlayQuest(); break;
            case StoryTalkKind.Talk: State.Apply(StoryObjectiveType.Talk, npc); break;
        }
        State.Refresh(RunProfile.Act);
    }

    /// <summary>Opens the dialog for <paramref name="npc"/> when one is scripted; shops only for pending talks.</summary>
    public static bool TryOpenTalk(GameObject hero, string npc)
    {
        if (hero == null) return false;
        var plan = StoryDialogs.Plan(State, npc);
        if (plan.dialog == null) return false;
        if (StoryDialogs.IsShop(npc) && !plan.IsPending) return false;
        var ui = hero.GetComponent<StoryDialogUI>();
        if (ui == null) ui = hero.AddComponent<StoryDialogUI>();
        if (ui.IsOpen) return true;
        ui.Show(plan, npc);
        return true;
    }
}
