using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// cinderreach-loot-click-filter: walking over items no longer collects them;
/// a click on the ground symbol or the name plate does (optional loot-key
/// gate); the loot filter hides Rare plates; kill gold has no ground pile.
/// </summary>
public class LootClickPlayTests
{
    GameObject player;
    GameObject camGo;
    DiabloClickController ctl;
    bool savedAlways, savedGate, savedFilter;

    [SetUp]
    public void Setup()
    {
        DiabloJuice.ForceCrit = false;
        DiabloClickController.ExternalDrive = false;
        ToyTrigger.ResetRun();
        EnemyHealth.ResetRun();
        savedAlways = DiabloSettings.LootLabelsAlways;
        savedGate = DiabloSettings.LootRequiresKey;
        savedFilter = DiabloSettings.LootFilterUniqueOnly;
        DiabloSettings.LootLabelsAlways = true;
        DiabloSettings.LootRequiresKey = false;
        DiabloSettings.LootFilterUniqueOnly = false;
        LootClick.ForceKeyHeld = null;

        camGo = new GameObject("TestCam");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        camGo.transform.position = new Vector3(0f, 14f, -10f);
        camGo.transform.LookAt(Vector3.zero);
        cam.enabled = true;

        player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.position = new Vector3(0f, 1.05f, 0f);
        player.AddComponent<CharacterController>();
        player.AddComponent<DiabloProgression>();
        player.AddComponent<Equipment>();
        player.AddComponent<Inventory>();
        player.AddComponent<Wallet>();
        player.AddComponent<PlayerHealth>();
        ctl = player.AddComponent<DiabloClickController>();
        DiabloClickController.ExternalDrive = true; // no real mouse in batchmode: drive via API
    }

    [TearDown]
    public void TearDown()
    {
        DiabloTestFixture.CleanupTestScene(player);
        if (camGo != null) Object.DestroyImmediate(camGo);
        DiabloSettings.LootLabelsAlways = savedAlways;
        DiabloSettings.LootRequiresKey = savedGate;
        DiabloSettings.LootFilterUniqueOnly = savedFilter;
        DiabloLootLabels.ForceShow = false;
        LootClick.ForceKeyHeld = null;
        LootClick.CapturingKey = false;
        DiabloClickController.ExternalDrive = false;
        if (SaveService.HasSave()) SaveService.DeleteSave();
    }

    static ItemInstance Item(ItemRarity rarity, int seed = 3)
    {
        var rng = new System.Random(seed);
        return LootDrop.BuildItem(new LootRoll { drops = true, rarity = rarity, slot = ItemSlot.Armor }, rng);
    }

    static LootDrop Spawn(Vector3 at, ItemRarity rarity) =>
        LootDrop.SpawnItem(at, Item(rarity), false).GetComponent<LootDrop>();

    [UnityTest]
    public IEnumerator WalkingOverItem_DoesNotPickUp()
    {
        var drop = Spawn(new Vector3(2f, 0.8f, 0f), ItemRarity.Rare);
        DiabloTestFixture.TeleportHero(player, drop.transform.position);
        yield return DiabloTestFixture.WaitSeconds(0.3f);
        Assert.AreEqual(0, player.GetComponent<Inventory>().Count, "walk-over must not collect");
        Assert.IsFalse(drop.collected);
    }

    [UnityTest]
    public IEnumerator ClickOrder_InReach_PicksUpAtOnce()
    {
        var drop = Spawn(new Vector3(1f, 0.8f, 0f), ItemRarity.Rare);
        ctl.OrderPickup(drop);
        yield return null;
        Assert.AreEqual(1, player.GetComponent<Inventory>().Count);
        Assert.IsTrue(drop.collected);
    }

    [UnityTest]
    public IEnumerator ClickOrder_OutOfReach_WalksThenPicksUp()
    {
        DiabloClickController.ExternalDrive = false; // let Update tick movement (no mouse in batchmode)
        var drop = Spawn(new Vector3(6f, 0.8f, 0f), ItemRarity.Rare);
        ctl.OrderPickup(drop);
        Assert.AreEqual(drop, ctl.PendingLoot);
        Assert.AreEqual(0, player.GetComponent<Inventory>().Count, "not yet: out of reach");
        float t = 0f;
        while (!drop.collected && t < 5f) { yield return null; t += Time.deltaTime; }
        Assert.IsTrue(drop.collected, "hero walks over and picks up on arrival");
        Assert.AreEqual(1, player.GetComponent<Inventory>().Count);
    }

    [UnityTest]
    public IEnumerator SymbolClick_FindsDrop_PlateClick_FindsDrop()
    {
        var cam = camGo.GetComponent<Camera>();
        var drop = Spawn(new Vector3(-3f, 0.8f, 2f), ItemRarity.Rare);
        Vector2 symbol = cam.WorldToScreenPoint(drop.transform.position);
        Assert.AreEqual(drop, LootClick.DropAt(cam, symbol), "click on the ground symbol");
        Assert.IsNull(LootClick.DropAt(cam, symbol + new Vector2(400f, 0f)), "far click misses");

        player.AddComponent<DiabloLootLabels>();
        yield return null; // labels lay out their plates
        yield return null;
        Assert.AreEqual(1, DiabloLootLabels.PlateCount, "one plate shown");
        var rect = DiabloLootLabels.PlateRect(0);
        Assert.Greater(rect.width, 0f);
        Assert.AreEqual(drop, DiabloLootLabels.PlateAt(rect.center), "click on the name plate");
        Assert.IsNull(DiabloLootLabels.PlateAt(rect.center + new Vector2(rect.width * 2f, 0f)), "beside the plate misses");
        // The plate sits above the drop: its bottom edge is above the ground symbol on screen.
        Assert.Greater(rect.yMin, ((Vector2)cam.WorldToScreenPoint(drop.transform.position)).y - 1f);
    }

    [Test]
    public void KeyGate_RequiresHeldKeyOnlyWhenEnabled()
    {
        Assert.IsTrue(LootClick.GateOpen(false, false));
        Assert.IsFalse(LootClick.GateOpen(true, false));
        Assert.IsTrue(LootClick.GateOpen(true, true));
        DiabloSettings.LootRequiresKey = true;
        LootClick.ForceKeyHeld = false;
        Assert.IsFalse(LootClick.GateOpen());
        LootClick.ForceKeyHeld = true;
        Assert.IsTrue(LootClick.GateOpen());
    }

    [Test]
    public void KeyBinding_PersistsAndRejectsEscape()
    {
        var saved = DiabloSettings.LootKey;
        try
        {
            DiabloSettings.LootKey = UnityEngine.InputSystem.Key.V;
            DiabloSettings.Save();
            DiabloSettings.LootKey = UnityEngine.InputSystem.Key.LeftAlt;
            DiabloSettings.Load();
            Assert.AreEqual(UnityEngine.InputSystem.Key.V, DiabloSettings.LootKey, "rebound key survives a reload");
            Assert.IsFalse(LootClick.IsBindable(UnityEngine.InputSystem.Key.Escape));
            DiabloSettings.LootKey = UnityEngine.InputSystem.Key.Escape;
            DiabloSettings.Clamp();
            Assert.AreEqual(UnityEngine.InputSystem.Key.LeftAlt, DiabloSettings.LootKey, "invalid binding falls back to Alt");
        }
        finally { DiabloSettings.LootKey = saved; DiabloSettings.Save(); }
    }

    [UnityTest]
    public IEnumerator LootFilter_UniqueOnly_HidesRarePlates_KeepsUnique()
    {
        Spawn(new Vector3(-2f, 0.8f, 1f), ItemRarity.Rare);
        Spawn(new Vector3(2f, 0.8f, 1f), ItemRarity.Unique);
        var labels = player.AddComponent<DiabloLootLabels>();
        yield return null;
        yield return null;
        Assert.AreEqual(2, labels.ShownCount, "filter 'All' shows both plates");
        DiabloSettings.LootFilterUniqueOnly = true;
        yield return null;
        Assert.AreEqual(1, labels.ShownCount, "filter 'Unique only' hides the Rare plate");
        Assert.IsTrue(DiabloLootLabels.PassesFilter(ItemRarity.Unique, true));
        Assert.IsFalse(DiabloLootLabels.PassesFilter(ItemRarity.Rare, true));
        Assert.IsFalse(DiabloLootLabels.PassesFilter(ItemRarity.Magic, true));
    }

    [UnityTest]
    public IEnumerator LabelsOff_ShowOnlyWithAlt()
    {
        Spawn(new Vector3(0f, 0.8f, 2f), ItemRarity.Rare);
        var labels = player.AddComponent<DiabloLootLabels>();
        DiabloSettings.LootLabelsAlways = false;
        yield return null;
        Assert.AreEqual(0, labels.ShownCount, "labels off and no Alt: no plates");
    }

    [UnityTest]
    public IEnumerator FilteredItem_StaysClickableBySymbol()
    {
        DiabloSettings.LootFilterUniqueOnly = true;
        var cam = camGo.GetComponent<Camera>();
        var drop = Spawn(new Vector3(1.5f, 0.8f, 3f), ItemRarity.Rare);
        yield return null;
        Assert.AreEqual(drop, LootClick.DropAt(cam, cam.WorldToScreenPoint(drop.transform.position)));
    }
}
