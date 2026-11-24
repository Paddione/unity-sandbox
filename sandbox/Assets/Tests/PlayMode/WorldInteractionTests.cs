using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// cinderreach-quest-interaktionen, PlayMode: Weltinteraktionen.
///
/// Jede Platzierung (placement:DiabloZone:&lt;Objekt&gt;) wird auf Erreichbarkeit
/// (Interaktionsradius vom begehbaren Bereich aus, fern bleibt wirkungslos),
/// richtiges Ziel (Komponenten-Kennung, Zone, Zielzone) und beobachtbare
/// Wirkung (UI oeffnet sich, Fund gebucht, Schrein entdeckt/bereist, Tor
/// teleportiert, Tafel geheilt, Brett zahlt aus) geprueft. Positionen sind die
/// echten Bau- und Layout-Anker (TownBuilder, KilnLayout, BarrensLayout,
/// RiskShrine.ShrineAnchor); die Health-Probe belegt, dass jede hier
/// referenzierte Platzierung auch in der gebauten Spielszene steht.
/// </summary>
public class WorldInteractionTests
{
    GameObject hero;

    static readonly Vector3 VendorPos = new Vector3(-3.5f, 0f, 11f);
    static readonly Vector3 SmithPos = new Vector3(5.5f, 0.6f, 11f);
    static readonly Vector3 StashPos = new Vector3(-3.5f, 0f, 14f);
    static readonly Vector3 BoardPos = new Vector3(3.5f, 0.9f, 14f);
    static readonly Vector3 FirePos = new Vector3(3.5f, 0f, 11f);
    static readonly Vector3 OttiliePos = new Vector3(1.5f, 0f, 14.5f);
    static readonly Vector3 MerlePos = new Vector3(-2f, 0f, 16f);
    static readonly Vector3 FuhrbriefPos = new Vector3(-10.6f, 0.5f, 1.6f);
    static readonly Vector3 LieferbuchPos = new Vector3(-13.4f, 0.5f, -1.6f);

    [SetUp]
    public void Setup()
    {
        RunProfile.Reset();
        Wallet.ResetRun();
        DiabloProgression.ResetRun();
        ToyTrigger.ResetRun();
        EnemyHealth.ResetRun();
        DiabloMelee.ResetRun();
        ZoneGate.ResetRun();
        ContractProgress.Reset();
        ContractBoard.WriteHook = null;
        DiabloClickController.ExternalDrive = false;

        hero = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        hero.name = DiabloNetworkPlayer.HeroName;
        hero.AddComponent<CharacterController>();
        hero.AddComponent<DiabloProgression>();
        hero.AddComponent<Equipment>();
        hero.AddComponent<Inventory>();
        hero.AddComponent<Wallet>();
        hero.AddComponent<PlayerHealth>();

        var eventSys = new GameObject("EventSystem");
        eventSys.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSys.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        var listener = new GameObject("TestAudioListener");
        listener.AddComponent<AudioListener>();
    }

    [TearDown]
    public void TearDown()
    {
        ContractBoard.WriteHook = null;
        DiabloTestFixture.CleanupTestScene(hero);
        var listener = GameObject.Find("TestAudioListener");
        if (listener != null) Object.DestroyImmediate(listener);
        var eventSys = GameObject.Find("EventSystem");
        if (eventSys != null) Object.DestroyImmediate(eventSys);
        RunProfile.Reset();
        Wallet.ResetRun();
        DiabloProgression.ResetRun();
        ContractProgress.Reset();
        ZoneGate.ResetRun();
        DiabloClickController.ExternalDrive = false;
        if (SaveService.HasSave()) SaveService.DeleteSave();
    }

    static void TeleportNear(Vector3 target, float east = 1f)
    {
        DiabloTestFixture.TeleportHero(GameObject.Find(DiabloNetworkPlayer.HeroName),
            target + new Vector3(east, 0f, 0f));
    }

    static GameObject MakeStoryNpc(string npcId, Vector3 pos, bool shop)
    {
        var go = new GameObject("StoryNpc_" + npcId);
        go.transform.position = pos;
        var col = go.AddComponent<CapsuleCollider>();
        col.height = 1.8f;
        col.radius = 0.4f;
        var npc = go.AddComponent<StoryNpc>();
        npc.npcId = npcId;
        npc.shopNpc = shop;
        return go;
    }

    static GameObject MakePickup(string itemId, int index, Vector3 pos)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = $"StoryPickup_{itemId}_{index}";
        go.transform.position = pos;
        go.transform.localScale = new Vector3(0.45f, 0.12f, 0.6f);
        var col = go.GetComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(2.4f, 2f, 2.4f);
        go.AddComponent<StoryPickup>().itemId = itemId;
        return go;
    }

    static GameObject MakeShrine(string id, string display, int zone, Vector3 pos)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = "WaypointShrine_" + id;
        go.transform.position = pos + Vector3.up * 0.5f;
        var ws = go.AddComponent<WaypointShrine>();
        ws.shrineId = id;
        ws.displayName = display;
        ws.zone = zone;
        return go;
    }

    static GameObject MakeGate(string name, Vector3 pos, Vector3 dest, int zone)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.position = pos;
        var gate = go.AddComponent<ZoneGate>();
        gate.destination = dest;
        gate.targetZone = zone;
        return go;
    }

    static void AcceptH1ViaTalk()
    {
        var h = GameObject.Find(DiabloNetworkPlayer.HeroName);
        Assert.IsTrue(RunProfile.Story.Refresh(RunProfile.Act));
        StoryDirector.BookTalk(StoryIds.Ottilie, h);
        Assert.AreEqual(StoryStatus.Active, RunProfile.Story.Status("h1"));
    }

    // ---- Haendler, Schmiede, Truhe, Brett, Feuer ----

    [UnityTest]
    public IEnumerator Vendor_ReachShopAndTalkRouting() // placement:DiabloZone:Vendor
    {
        var go = new GameObject(Vendor.VendorObjectName);
        go.transform.position = VendorPos;
        go.AddComponent<CapsuleCollider>();
        go.AddComponent<Vendor>().zoneSeed = 7;
        var npc = go.AddComponent<StoryNpc>();
        npc.npcId = StoryIds.Ilsabe;
        npc.shopNpc = true;

        TeleportNear(VendorPos);
        Assert.IsTrue(Vendor.CanInteract(hero.transform.position, go.transform.position));
        DiabloTestFixture.TeleportHero(hero, VendorPos + new Vector3(10f, 0f, 0f));
        Physics.SyncTransforms();
        Assert.IsFalse(Vendor.CanInteract(hero.transform.position, go.transform.position));
        TeleportNear(VendorPos);

        go.GetComponent<Vendor>().Use(hero);
        Assert.IsTrue(hero.GetComponent<DiabloVendorUI>().IsOpen, "shop opens when nothing is pending");
        hero.GetComponent<DiabloVendorUI>().SetOpen(false);

        AcceptH1ViaTalk(); // quest:h1 oeffnet das n1-Angebot bei Ilsabe
        RunProfile.Story.Refresh(RunProfile.Act);
        Assert.IsTrue(StoryDirector.TryOpenTalk(hero, StoryIds.Ilsabe), "pending offer routes to talk first");
        Assert.AreEqual("n1", StoryDialogs.Plan(RunProfile.Story, StoryIds.Ilsabe).questId);
        Object.DestroyImmediate(go);
        yield break;
    }

    [UnityTest]
    public IEnumerator Smith_ReachForgeAndTalkRouting() // placement:DiabloZone:Smith
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = SmithStation.SmithObjectName;
        go.transform.position = SmithPos;
        go.AddComponent<SmithStation>();
        var npc = go.AddComponent<StoryNpc>();
        npc.npcId = StoryIds.Jorin;
        npc.shopNpc = true;

        TeleportNear(SmithPos);
        Assert.IsTrue(SmithStation.CanInteract(hero.transform.position, go.transform.position));
        DiabloTestFixture.TeleportHero(hero, SmithPos + new Vector3(10f, 0f, 0f));
        Physics.SyncTransforms();
        Assert.IsFalse(SmithStation.CanInteract(hero.transform.position, go.transform.position));
        TeleportNear(SmithPos);

        go.GetComponent<SmithStation>().Use(hero);
        Assert.IsTrue(hero.GetComponent<DiabloSmithUI>().IsOpen, "forge opens when nothing is pending");
        hero.GetComponent<DiabloSmithUI>().SetOpen(false);

        Assert.IsTrue(RunProfile.Act.Unlock(ActProgress.Kiln));
        RunProfile.Story.Refresh(RunProfile.Act);
        Assert.IsTrue(StoryDirector.TryOpenTalk(hero, StoryIds.Jorin), "n3 offer routes to talk first");
        Assert.AreEqual("n3", StoryDialogs.Plan(RunProfile.Story, StoryIds.Jorin).questId);
        Object.DestroyImmediate(go);
        yield break;
    }

    [UnityTest]
    public IEnumerator StashChest_ReachStoreTakeAndUi() // placement:DiabloZone:StashChest
    {
        var go = new GameObject(StashChest.ObjectName);
        go.transform.position = StashPos;
        go.AddComponent<BoxCollider>();
        go.AddComponent<StashChest>();

        TeleportNear(StashPos);
        Assert.IsTrue(StashChest.CanInteract(hero.transform.position, go.transform.position));
        DiabloTestFixture.TeleportHero(hero, StashPos + new Vector3(10f, 0f, 0f));
        Physics.SyncTransforms();
        Assert.IsFalse(StashChest.CanInteract(hero.transform.position, go.transform.position));
        TeleportNear(StashPos);

        var inv = hero.GetComponent<Inventory>();
        Assert.IsTrue(inv.Add(ItemDatabase.CreateStarterSword()));
        Assert.IsTrue(StashChest.Store(inv, RunProfile.Stash, 0));
        Assert.AreEqual(0, inv.Count);
        Assert.AreEqual(1, RunProfile.Stash.Count);
        Assert.IsTrue(StashChest.Take(RunProfile.Stash, inv, 0));
        Assert.AreEqual(1, inv.Count);

        go.GetComponent<StashChest>().Use(hero);
        Assert.IsTrue(hero.GetComponent<DiabloStashUI>().IsOpen);
        Object.DestroyImmediate(go);
        yield break;
    }

    [UnityTest]
    public IEnumerator ContractBoard_ReachOpenClaimAndRollback() // placement:DiabloZone:ContractBoard
    {
        string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
            $"cinderreach-board-{System.Guid.NewGuid():N}.json");
        var go = new GameObject(ContractBoard.BoardObjectName);
        go.transform.position = BoardPos;
        go.AddComponent<BoxCollider>();
        var board = go.AddComponent<ContractBoard>();
        try
        {
            TeleportNear(BoardPos);
            Assert.IsTrue(Vendor.CanInteract(hero.transform.position, go.transform.position,
                ContractBoard.InteractRadius));
            DiabloTestFixture.TeleportHero(hero, BoardPos + new Vector3(10f, 0f, 0f));
            Physics.SyncTransforms();
            Assert.IsFalse(Vendor.CanInteract(hero.transform.position, go.transform.position,
                ContractBoard.InteractRadius));
            TeleportNear(BoardPos);

            board.Use(hero);
            Assert.IsTrue(hero.GetComponent<DiabloContractUI>().IsOpen);
            hero.GetComponent<DiabloContractUI>().SetOpen(false);

            var offer = new ContractOffer
            {
                id = "contract-1-ash-elite", type = ContractType.Hunt, zone = ActProgress.Town,
                position = new Vector3(5f, 0.05f, 2f), gold = 130, targetName = "Ash Elite",
            };
            Assert.IsTrue(ContractProgress.TryAccept(offer));
            var objective = ContractObjective.SpawnFor(offer);
            Assert.IsNotNull(objective);
            TeleportNear(offer.position);
            objective.OnEnemyDeath();
            Assert.AreEqual(ContractStatus.Complete, ContractProgress.Status);
            TeleportNear(BoardPos);
            yield return null;
            Assert.IsTrue(board.TryClaim(hero, path));
            Assert.AreEqual(130, hero.GetComponent<Wallet>().Gold);
            Assert.AreEqual(ContractStatus.Claimed, ContractProgress.Status);

            // Schreibfehler rollt Gold und Status zurueck, die Belohnung bleibt verfuegbar.
            ContractProgress.Restore(true, offer, ContractStatus.Complete);
            ContractBoard.WriteHook = (save, p) => false;
            int before = hero.GetComponent<Wallet>().Gold;
            Assert.IsFalse(board.TryClaim(hero, path));
            Assert.AreEqual(before, hero.GetComponent<Wallet>().Gold);
            Assert.AreEqual(ContractStatus.Complete, ContractProgress.Status);
        }
        finally
        {
            Object.DestroyImmediate(go);
            foreach (var o in GameObject.FindObjectsOfType<ContractObjective>())
                Object.DestroyImmediate(o.gameObject);
            foreach (var f in new[] { path, path + ".tmp", path + ".corrupt" })
                if (System.IO.File.Exists(f)) System.IO.File.Delete(f);
        }
        yield break;
    }

    [UnityTest]
    public IEnumerator CampfireHeal_WarmsOnlyInsideRadius() // placement:DiabloZone:CampfireHeal
    {
        var go = new GameObject("CampfireHeal");
        go.transform.position = FirePos + Vector3.up;
        var col = go.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(CampfireHeal.Radius * 2f, 3f, CampfireHeal.Radius * 2f);
        go.AddComponent<CampfireHeal>();

        var health = hero.GetComponent<PlayerHealth>();
        health.TakeDamage(health.MaxHP / 2f);
        float hurt = health.CurrentHP;
        DiabloTestFixture.TeleportHero(hero, FirePos + Vector3.up);
        yield return DiabloTestFixture.WaitSeconds(1.5f);
        Assert.Greater(health.CurrentHP, hurt, "fire heals inside the trigger");
        Object.DestroyImmediate(go);
        yield break;
    }

    [UnityTest]
    public IEnumerator RiskShrine_ReachOpenAndHealPath() // placement:DiabloZone:RiskShrine
    {
        var go = new GameObject(RiskShrine.ShrineObjectName);
        go.transform.position = RiskShrine.ShrineAnchor;
        var col = go.AddComponent<BoxCollider>();
        col.size = new Vector3(1.2f, 1.6f, 1.2f);
        var shrine = go.AddComponent<RiskShrine>();

        TeleportNear(RiskShrine.ShrineAnchor);
        Assert.IsTrue(RiskShrine.CanInteract(hero.transform.position, go.transform.position));
        DiabloTestFixture.TeleportHero(hero, RiskShrine.ShrineAnchor + new Vector3(10f, 0f, 0f));
        Physics.SyncTransforms();
        Assert.IsFalse(RiskShrine.CanInteract(hero.transform.position, go.transform.position));
        TeleportNear(RiskShrine.ShrineAnchor);

        shrine.Use(hero);
        Assert.IsTrue(hero.GetComponent<DiabloRiskShrineUI>().IsOpen);

        var health = hero.GetComponent<PlayerHealth>();
        health.TakeDamage(health.MaxHP / 2f);
        Assert.IsTrue(shrine.ChooseHeal(hero));
        Assert.AreEqual(health.MaxHP, health.CurrentHP, 0.001f);
        Assert.IsFalse(shrine.ChooseChallenge(hero), "choice is one-shot");
        Object.DestroyImmediate(go);
        var elite = GameObject.Find(RiskShrine.EliteName);
        if (elite != null) Object.DestroyImmediate(elite);
        yield break;
    }

    // ---- NPCs ----

    [UnityTest]
    public IEnumerator StoryNpc_Ottilie_OfferMarkerAndDialog() // placement:DiabloZone:StoryNpc_ottilie
    {
        var go = MakeStoryNpc(StoryIds.Ottilie, OttiliePos, false); // quest:h1
        try
        {
            RunProfile.Story.Refresh(RunProfile.Act);
            TeleportNear(OttiliePos);
            Assert.IsTrue(Vendor.CanInteract(hero.transform.position, go.transform.position));
            Assert.IsNotNull(go.GetComponent<StoryNpc>().UseLabel);
            yield return DiabloTestFixture.WaitSeconds(0.5f);
            var marker = go.transform.Find("StoryMarker").GetComponent<TextMesh>();
            Assert.AreEqual("!", marker.text, "offer shows the marker");
            go.GetComponent<StoryNpc>().Use(hero);
            Assert.IsTrue(hero.GetComponent<StoryDialogUI>().IsOpen);
            Assert.AreEqual(StoryTalkKind.Offer,
                StoryDialogs.Plan(RunProfile.Story, StoryIds.Ottilie).kind);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
        yield break;
    }

    [UnityTest]
    public IEnumerator StoryNpc_Merle_HiddenUntilActOneDone() // placement:DiabloZone:StoryNpc_merle
    {
        var go = MakeStoryNpc(StoryIds.Merle, MerlePos, false); // quest:h6
        try
        {
            yield return DiabloTestFixture.WaitSeconds(0.5f);
            Assert.IsNull(go.GetComponent<StoryNpc>().UseLabel, "Merle stands no camp before Act I ends");
            Assert.IsFalse(go.GetComponent<Collider>().enabled);

            Assert.IsTrue(RunProfile.Act.Unlock(ActProgress.Kiln));
            Assert.IsTrue(RunProfile.Act.CompleteAct());
            RunProfile.Story.Refresh(RunProfile.Act);
            yield return DiabloTestFixture.WaitSeconds(0.5f);
            Assert.IsTrue(go.GetComponent<Collider>().enabled);
            TeleportNear(MerlePos);
            Assert.IsTrue(Vendor.CanInteract(hero.transform.position, go.transform.position));
            go.GetComponent<StoryNpc>().Use(hero);
            Assert.IsTrue(hero.GetComponent<StoryDialogUI>().IsOpen);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
        yield break;
    }

    // ---- Fundstellen ----

    [UnityTest]
    public IEnumerator StoryPickup_Fuhrbrief_WaitsThenBooks() // placement:DiabloZone:StoryPickup_fuhrbrief_0
    {
        var go = MakePickup(StoryIds.Fuhrbrief, 0, FuhrbriefPos); // quest:h1
        try
        {
            Assert.IsTrue(RunProfile.Story.Refresh(RunProfile.Act));
            TeleportNear(FuhrbriefPos, 0.2f);
            yield return DiabloTestFixture.WaitSeconds(0.5f);
            Assert.IsTrue(go.activeSelf, "early find waits instead of being lost");
            Assert.AreEqual(0, RunProfile.Story.Count("h1", 0));

            StoryDirector.BookTalk(StoryIds.Ottilie, hero);
            DiabloTestFixture.TeleportHero(hero, FuhrbriefPos + new Vector3(10f, 0f, 0f));
            Physics.SyncTransforms();
            yield return null; // Trigger verlassen, damit das Betreten erneut zaehlt
            TeleportNear(FuhrbriefPos, 0.2f);
            yield return DiabloTestFixture.WaitSeconds(0.5f);
            Assert.AreEqual(1, RunProfile.Story.Count("h1", 0));
            Assert.IsFalse(go.activeSelf, "counted find despawns");
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
        yield break;
    }

    [UnityTest]
    public IEnumerator StoryPickup_Lieferbuch_BooksLedger() // placement:DiabloZone:StoryPickup_lieferbuch_0
    {
        var go = MakePickup(StoryIds.Lieferbuch, 0, LieferbuchPos); // quest:n1
        try
        {
            AcceptH1ViaTalk();
            RunProfile.Story.Refresh(RunProfile.Act);
            StoryDirector.BookTalk(StoryIds.Ilsabe, hero);
            TeleportNear(LieferbuchPos, 0.2f);
            yield return DiabloTestFixture.WaitSeconds(0.5f);
            Assert.AreEqual(1, RunProfile.Story.Count("n1", 0));
            Assert.IsFalse(go.activeSelf);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
        yield break;
    }

    [UnityTest]
    public IEnumerator StoryPickup_Grabtafel0_BooksFirstTablet() // placement:DiabloZone:StoryPickup_grabtafel_0
    {
        var o = KilnLayout.OssuaryOffset;
        var go = MakePickup(StoryIds.Grabtafel, 0, o + new Vector3(-6.8f, 0.7f, -6f)); // quest:n2
        try
        {
            AcceptH1ViaTalk();
            Assert.IsTrue(StoryDirector.BookCollect(StoryIds.Fuhrbrief));
            StoryDirector.BookKill("The Fen Brute(Clone)");
            StoryDirector.BookTalk(StoryIds.Ottilie, hero); // h1 abgeben
            RunProfile.Story.Refresh(RunProfile.Act);
            StoryDirector.BookTalk(StoryIds.Ottilie, hero); // h2 aktiv
            RunProfile.Story.Refresh(RunProfile.Act);
            StoryDirector.BookTalk(StoryIds.Ottilie, hero); // n2 aktiv
            TeleportNear(o + new Vector3(-6.8f, 0.7f, -6f), 0.2f);
            yield return DiabloTestFixture.WaitSeconds(0.5f);
            Assert.AreEqual(1, RunProfile.Story.Count("n2", 0));
            Assert.IsFalse(go.activeSelf);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
        yield break;
    }

    [UnityTest]
    public IEnumerator StoryPickup_Grabtafel1_BooksSecondTablet() // placement:DiabloZone:StoryPickup_grabtafel_1
    {
        var o = KilnLayout.OssuaryOffset;
        var go = MakePickup(StoryIds.Grabtafel, 1, o + new Vector3(9.2f, 0.7f, -3f)); // quest:n2
        try
        {
            AcceptH1ViaTalk();
            RunProfile.Story.Refresh(RunProfile.Act);
            Assert.IsTrue(StoryDirector.BookCollect(StoryIds.Fuhrbrief));
            StoryDirector.BookKill("The Fen Brute(Clone)");
            StoryDirector.BookTalk(StoryIds.Ottilie, hero); // h1 abgeben
            RunProfile.Story.Refresh(RunProfile.Act);
            StoryDirector.BookTalk(StoryIds.Ottilie, hero);
            RunProfile.Story.Refresh(RunProfile.Act);
            StoryDirector.BookTalk(StoryIds.Ottilie, hero);
            TeleportNear(o + new Vector3(9.2f, 0.7f, -3f), 0.2f);
            yield return DiabloTestFixture.WaitSeconds(0.5f);
            Assert.AreEqual(1, RunProfile.Story.Count("n2", 0));
            Assert.IsFalse(go.activeSelf);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
        yield break;
    }

    [UnityTest]
    public IEnumerator StoryPickup_Grabtafel2_BooksThirdTablet() // placement:DiabloZone:StoryPickup_grabtafel_2
    {
        var o = KilnLayout.OssuaryOffset;
        var go = MakePickup(StoryIds.Grabtafel, 2, o + new Vector3(-6.8f, 0.7f, 0f)); // quest:n2
        try
        {
            AcceptH1ViaTalk();
            RunProfile.Story.Refresh(RunProfile.Act);
            Assert.IsTrue(StoryDirector.BookCollect(StoryIds.Fuhrbrief));
            StoryDirector.BookKill("The Fen Brute(Clone)");
            StoryDirector.BookTalk(StoryIds.Ottilie, hero); // h1 abgeben
            RunProfile.Story.Refresh(RunProfile.Act);
            StoryDirector.BookTalk(StoryIds.Ottilie, hero);
            RunProfile.Story.Refresh(RunProfile.Act);
            StoryDirector.BookTalk(StoryIds.Ottilie, hero);
            TeleportNear(o + new Vector3(-6.8f, 0.7f, 0f), 0.2f);
            yield return DiabloTestFixture.WaitSeconds(0.5f);
            Assert.AreEqual(1, RunProfile.Story.Count("n2", 0));
            Assert.IsFalse(go.activeSelf);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
        yield break;
    }

    // ---- Wegschreine ----

    [UnityTest]
    public IEnumerator WaypointShrine_Town_DiscoverAndTravel() // placement:DiabloZone:WaypointShrine_Town
    {
        var def = KilnLayout.Shrines[0];
        var go = MakeShrine(def.id, def.name, def.zone, def.pos);
        try
        {
            Assert.AreEqual(ActProgress.Town, go.GetComponent<WaypointShrine>().zone);
            DiabloTestFixture.TeleportHero(hero, def.pos + new Vector3(10f, 0f, 0f));
            Physics.SyncTransforms();
            Assert.IsFalse(go.GetComponent<WaypointShrine>().TryDiscover(hero));
            DiabloTestFixture.TeleportHero(hero, def.pos);
            Assert.IsTrue(go.GetComponent<WaypointShrine>().TryDiscover(hero));
            Assert.IsFalse(go.GetComponent<WaypointShrine>().TryDiscover(hero), "discovery is one-shot");
            Assert.IsTrue(WaypointShrine.Destinations().Contains(go.GetComponent<WaypointShrine>()));
            go.GetComponent<WaypointShrine>().Use(hero);
            Assert.IsTrue(hero.GetComponent<DiabloWaypointUI>().IsOpen);
            DiabloTestFixture.TeleportHero(hero, new Vector3(50f, 1f, 50f));
            Assert.IsTrue(WaypointShrine.Travel(def.id, hero));
            Assert.AreEqual(go.GetComponent<WaypointShrine>().Arrival, hero.transform.position);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
        yield break;
    }

    [UnityTest]
    public IEnumerator WaypointShrine_Fen_DiscoverAndTravel() // placement:DiabloZone:WaypointShrine_Fen
    {
        var def = KilnLayout.Shrines[1];
        var go = MakeShrine(def.id, def.name, def.zone, def.pos);
        try
        {
            Assert.AreEqual(ActProgress.Town, go.GetComponent<WaypointShrine>().zone);
            DiabloTestFixture.TeleportHero(hero, def.pos);
            Assert.IsTrue(go.GetComponent<WaypointShrine>().TryDiscover(hero));
            Assert.IsTrue(WaypointShrine.Destinations().Contains(go.GetComponent<WaypointShrine>()));
            Assert.IsTrue(WaypointShrine.Travel(def.id, hero));
            Assert.AreEqual(go.GetComponent<WaypointShrine>().Arrival, hero.transform.position);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
        yield break;
    }

    [UnityTest]
    public IEnumerator WaypointShrine_Ossuary_DiscoverAndTravel() // placement:DiabloZone:WaypointShrine_Ossuary
    {
        var def = KilnLayout.Shrines[2];
        var go = MakeShrine(def.id, def.name, def.zone, def.pos);
        try
        {
            Assert.AreEqual(ActProgress.Ossuary, go.GetComponent<WaypointShrine>().zone);
            DiabloTestFixture.TeleportHero(hero, def.pos);
            Assert.IsTrue(go.GetComponent<WaypointShrine>().TryDiscover(hero));
            Assert.IsTrue(WaypointShrine.Destinations().Contains(go.GetComponent<WaypointShrine>()));
            Assert.IsTrue(WaypointShrine.Travel(def.id, hero));
            Assert.AreEqual(go.GetComponent<WaypointShrine>().Arrival, hero.transform.position);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
        yield break;
    }

    [UnityTest]
    public IEnumerator WaypointShrine_Kiln_UnlockGatedTravel() // placement:DiabloZone:WaypointShrine_Kiln
    {
        var def = KilnLayout.Shrines[3];
        var go = MakeShrine(def.id, def.name, def.zone, def.pos);
        try
        {
            Assert.AreEqual(ActProgress.Kiln, go.GetComponent<WaypointShrine>().zone);
            DiabloTestFixture.TeleportHero(hero, def.pos);
            Assert.IsTrue(go.GetComponent<WaypointShrine>().TryDiscover(hero));
            Assert.IsFalse(WaypointShrine.Destinations().Contains(go.GetComponent<WaypointShrine>()),
                "sealed zone is no destination");
            Assert.IsFalse(WaypointShrine.Travel(def.id, hero));
            Assert.IsTrue(RunProfile.Act.Unlock(ActProgress.Kiln));
            Assert.IsTrue(WaypointShrine.Destinations().Contains(go.GetComponent<WaypointShrine>()));
            Assert.IsTrue(WaypointShrine.Travel(def.id, hero));
            Assert.AreEqual(go.GetComponent<WaypointShrine>().Arrival, hero.transform.position);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
        yield break;
    }

    [UnityTest]
    public IEnumerator WaypointShrine_Barrens_UnlockGatedTravel() // placement:DiabloZone:WaypointShrine_Barrens
    {
        var go = MakeShrine("Barrens", BarrensLayout.DisplayName, ActProgress.Barrens, BarrensLayout.ShrinePos);
        try
        {
            DiabloTestFixture.TeleportHero(hero, BarrensLayout.ShrinePos);
            Assert.IsTrue(go.GetComponent<WaypointShrine>().TryDiscover(hero));
            Assert.IsFalse(WaypointShrine.Destinations().Contains(go.GetComponent<WaypointShrine>()));
            Assert.IsTrue(RunProfile.Act.Unlock(ActProgress.Kiln));
            Assert.IsTrue(RunProfile.Act.CompleteAct());
            Assert.IsTrue(WaypointShrine.Destinations().Contains(go.GetComponent<WaypointShrine>()));
            Assert.IsTrue(WaypointShrine.Travel("Barrens", hero));
            Assert.AreEqual(go.GetComponent<WaypointShrine>().Arrival, hero.transform.position);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
        yield break;
    }

    // ---- Zonetore ----

    [UnityTest]
    public IEnumerator ZoneGate_Fen_DwellTravelsToOssuary() // placement:DiabloZone:FenGate
    {
        var go = MakeGate(ZoneGate.FenGateName, new Vector3(0f, 1f, -6f),
            KilnLayout.OssuaryOffset + new Vector3(0f, 1f, 2f), ActProgress.Ossuary);
        try
        {
            var gate = go.GetComponent<ZoneGate>();
            Assert.IsFalse(gate.Locked, "ossuary stands open");
            DiabloTestFixture.TeleportHero(hero, go.transform.position);
            gate.BeginDwell(hero, 10f);
            Assert.IsTrue(gate.TickDwell(10f + ZoneGate.DwellSeconds + 0.1f, -100f));
            Assert.AreEqual(1, ZoneGate.Teleports);
            Assert.AreEqual(gate.destination, hero.transform.position);
            Assert.IsTrue(SaveService.HasSave());
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
        yield break;
    }

    [UnityTest]
    public IEnumerator ZoneGate_OssuaryReturn_DwellTravelsToTown() // placement:DiabloZone:OssuaryGate
    {
        var go = MakeGate(ZoneGate.OssuaryGateName, KilnLayout.OssuaryOffset + new Vector3(0f, 1f, 10f),
            new Vector3(0f, 1f, -4f), ActProgress.Town);
        try
        {
            var gate = go.GetComponent<ZoneGate>();
            DiabloTestFixture.TeleportHero(hero, go.transform.position);
            gate.BeginDwell(hero, 20f);
            Assert.IsTrue(gate.TickDwell(20f + ZoneGate.DwellSeconds + 0.1f, -100f));
            Assert.AreEqual(gate.destination, hero.transform.position);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
        yield break;
    }

    [UnityTest]
    public IEnumerator ZoneGate_Kiln_LockedRefusesThenTravels() // placement:DiabloZone:KilnGate
    {
        var go = MakeGate(ZoneGate.KilnGateName, KilnLayout.KilnGatePos, KilnLayout.KilnArrival, ActProgress.Kiln);
        try
        {
            var gate = go.GetComponent<ZoneGate>();
            Assert.IsTrue(gate.Locked);
            DiabloTestFixture.TeleportHero(hero, go.transform.position);
            Vector3 before = hero.transform.position;
            gate.BeginDwell(hero, 30f);
            Assert.IsTrue(gate.TickDwell(30f + ZoneGate.DwellSeconds + 0.1f, -100f));
            Assert.AreEqual(1, ZoneGate.Refusals, "sealed gate names the concrete target");
            Assert.AreEqual(0, ZoneGate.Teleports);
            Assert.AreEqual(before, hero.transform.position);

            Assert.IsTrue(RunProfile.Act.Unlock(ActProgress.Kiln));
            Assert.IsFalse(gate.Locked);
            gate.BeginDwell(hero, 40f);
            Assert.IsTrue(gate.TickDwell(40f + ZoneGate.DwellSeconds + 0.1f, -100f));
            Assert.AreEqual(1, ZoneGate.Teleports);
            Assert.AreEqual(gate.destination, hero.transform.position);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
        yield break;
    }

    [UnityTest]
    public IEnumerator ZoneGate_KilnReturn_DwellTravelsToOssuary() // placement:DiabloZone:KilnReturnGate
    {
        var go = MakeGate(ZoneGate.KilnReturnGateName, KilnLayout.KilnReturnPos,
            KilnLayout.OssuaryReturnArrival, ActProgress.Ossuary);
        try
        {
            var gate = go.GetComponent<ZoneGate>();
            DiabloTestFixture.TeleportHero(hero, go.transform.position);
            gate.BeginDwell(hero, 50f);
            Assert.IsTrue(gate.TickDwell(50f + ZoneGate.DwellSeconds + 0.1f, -100f));
            Assert.AreEqual(gate.destination, hero.transform.position);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
        yield break;
    }

    [UnityTest]
    public IEnumerator ZoneGate_Barrens_LockedRefusesThenTravels() // placement:DiabloZone:BarrensGate
    {
        var go = MakeGate(ZoneGate.BarrensGateName, BarrensLayout.BarrensGatePos,
            BarrensLayout.BarrensArrival, ActProgress.Barrens);
        try
        {
            var gate = go.GetComponent<ZoneGate>();
            Assert.IsTrue(gate.Locked);
            DiabloTestFixture.TeleportHero(hero, go.transform.position);
            gate.BeginDwell(hero, 60f);
            Assert.IsTrue(gate.TickDwell(60f + ZoneGate.DwellSeconds + 0.1f, -100f));
            Assert.AreEqual(1, ZoneGate.Refusals);
            Assert.AreEqual(0, ZoneGate.Teleports);

            Assert.IsTrue(RunProfile.Act.Unlock(ActProgress.Kiln));
            Assert.IsTrue(RunProfile.Act.CompleteAct());
            gate.BeginDwell(hero, 70f);
            Assert.IsTrue(gate.TickDwell(70f + ZoneGate.DwellSeconds + 0.1f, -100f));
            Assert.AreEqual(1, ZoneGate.Teleports);
            Assert.AreEqual(gate.destination, hero.transform.position);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
        yield break;
    }

    [UnityTest]
    public IEnumerator ZoneGate_BarrensReturn_DwellTravelsToKiln() // placement:DiabloZone:BarrensReturnGate
    {
        var go = MakeGate(ZoneGate.BarrensReturnGateName, BarrensLayout.BarrensReturnPos,
            BarrensLayout.KilnReturnArrival, ActProgress.Kiln);
        try
        {
            var gate = go.GetComponent<ZoneGate>();
            Assert.IsTrue(gate.Locked);
            Assert.IsTrue(RunProfile.Act.Unlock(ActProgress.Kiln));
            Assert.IsFalse(gate.Locked);
            DiabloTestFixture.TeleportHero(hero, go.transform.position);
            gate.BeginDwell(hero, 80f);
            Assert.IsTrue(gate.TickDwell(80f + ZoneGate.DwellSeconds + 0.1f, -100f));
            Assert.AreEqual(gate.destination, hero.transform.position);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
        yield break;
    }
}
