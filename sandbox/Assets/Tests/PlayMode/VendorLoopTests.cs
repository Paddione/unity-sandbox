using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

/// <summary>
/// S3 vendor loop: the open trade window eats clicks (ExternalDrive seam,
/// same effect as the inventory click guard) and the UI buy/sell path moves
/// gold and bag items; closing autosaves.
/// </summary>
public class VendorLoopTests
{
    GameObject player;
    DiabloVendorUI ui;

    [SetUp]
    public void Setup()
    {
        DiabloJuice.ForceCrit = false;
        ToyTrigger.ResetRun();
        EnemyHealth.ResetRun();
        DiabloMelee.ResetRun();
        GoldDrop.ResetRun();
        Wallet.ResetRun();
        DiabloProgression.ResetRun();
        DiabloClickController.ExternalDrive = false;

        player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.position = new Vector3(0f, 1.05f, 0f);
        player.AddComponent<CharacterController>();
        player.AddComponent<DiabloProgression>();
        player.AddComponent<Equipment>();
        player.AddComponent<Inventory>();
        player.AddComponent<Wallet>();
        player.AddComponent<PlayerHealth>();
        player.AddComponent<DiabloClickController>();
        ui = player.AddComponent<DiabloVendorUI>();

        var eventSys = new GameObject("EventSystem");
        eventSys.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSys.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        var listener = new GameObject("TestAudioListener");
        listener.AddComponent<AudioListener>();
    }

    [TearDown]
    public void TearDown()
    {
        DiabloTestFixture.CleanupTestScene(player);
        var listener = GameObject.Find("TestAudioListener");
        if (listener != null) Object.DestroyImmediate(listener);
        DiabloClickController.ExternalDrive = false;
        GoldDrop.ResetRun();
        Wallet.ResetRun();
        DiabloProgression.ResetRun();
        if (SaveService.HasSave()) SaveService.DeleteSave();
    }

    [UnityTest]
    public IEnumerator OpenUi_EatsClicks_CloseRestoresAndSaves()
    {
        var canvas = player.transform.Find("VendorCanvas").gameObject;
        Assert.IsFalse(ui.IsOpen);
        Assert.IsFalse(canvas.activeInHierarchy, "A closed vendor must neither render nor intercept UI clicks.");
        Assert.IsFalse(DiabloClickController.ExternalDrive);
        ui.SetOpen(true);
        Assert.IsTrue(ui.IsOpen);
        Assert.IsTrue(DiabloClickController.ExternalDrive,
            "Open vendor window must lock click-movement like the inventory guard.");
        yield return null;
        Assert.IsTrue(canvas.activeInHierarchy);
        var closeButton = canvas.transform.Find("Panel/SmallButton").gameObject;
        var click = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
        Assert.IsTrue(ExecuteEvents.Execute(closeButton, click, ExecuteEvents.pointerClickHandler));
        Assert.IsFalse(ui.IsOpen);
        Assert.IsFalse(canvas.activeInHierarchy);
        Assert.IsFalse(DiabloClickController.ExternalDrive);
        Assert.IsTrue(SaveService.HasSave(), "Vendor close must autosave (S2 seam).");
    }

    [UnityTest]
    public IEnumerator BuyAndSell_ViaUi_MovesGoldAndBag()
    {
        var wallet = player.GetComponent<Wallet>();
        var inv = player.GetComponent<Inventory>();
        wallet.Add(200);
        ui.SetOpen(true);
        yield return null;
        int price = 0;
        var stockField = typeof(DiabloVendorUI).GetField("stock",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var stock = (VendorStock)stockField.GetValue(ui);
        price = stock.offers[0].price;
        ui.BuyOffer(0);
        Assert.AreEqual(200 - price, wallet.Gold);
        Assert.AreEqual(1, inv.Count);
        int expectBack = VendorStock.SellPrice(inv.Get(0));
        ui.SellBagIndex(0);
        Assert.AreEqual(200 - price + expectBack, wallet.Gold);
        Assert.AreEqual(0, inv.Count);
        ui.BuyPotion();
        Assert.AreEqual(1, inv.Count);
        Assert.AreEqual(ItemSlot.Consumable, inv.Get(0).slot);
        ui.SetOpen(false);
    }

    [UnityTest]
    public IEnumerator VendorClick_OpensOnlyInsideRadius()
    {
        var vendorGo = new GameObject(Vendor.VendorObjectName);
        vendorGo.transform.position = new Vector3(-3.5f, 0f, 11f);
        vendorGo.AddComponent<Vendor>();
        // Hero at the campfire: out of reach, so no trade window may open.
        DiabloTestFixture.TeleportHero(player, new Vector3(3.5f, 1f, 11f));
        Assert.IsFalse(Vendor.CanInteract(player.transform.position, vendorGo.transform.position));
        // Hero at the stall: inside reach.
        DiabloTestFixture.TeleportHero(player, new Vector3(-3.5f, 1f, 11f));
        Assert.IsTrue(Vendor.CanInteract(player.transform.position, vendorGo.transform.position));
        Object.DestroyImmediate(vendorGo);
        yield return null;
    }

    [UnityTest]
    public IEnumerator S5_SellById_MovesGoldAndShowsNotice()
    {
        var wallet = player.GetComponent<Wallet>();
        var inv = player.GetComponent<Inventory>();
        wallet.Add(50);
        inv.Add(ItemDatabase.CreateStarterSword());
        string id = inv.Get(0).instanceId;
        ui.SetOpen(true);
        yield return null;
        int before = wallet.Gold;
        ui.SellById(id);
        Assert.AreEqual(0, inv.Count);
        Assert.Greater(wallet.Gold, before);
        Assert.IsNotEmpty(ui.Notice);
        ui.SetOpen(false);
    }

    [UnityTest]
    public IEnumerator S5_BuyRejectedWhenBroke_ShowsNotice()
    {
        ui.SetOpen(true);
        yield return null;
        ui.BuyOffer(0);
        Assert.IsNotEmpty(ui.Notice, "broke buy must explain itself");
        ui.SetOpen(false);
    }
}
