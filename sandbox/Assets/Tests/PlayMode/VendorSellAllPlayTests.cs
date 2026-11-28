using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

/// <summary>cinderreach-vendor-sell-all in the trade window: button → confirm with count and gold → sold; cancel keeps all.</summary>
public sealed class VendorSellAllPlayTests
{
    GameObject player;
    DiabloVendorUI ui;
    Inventory inventory;
    Wallet wallet;

    [SetUp]
    public void SetUp()
    {
        player = new GameObject("Player");
        player.AddComponent<Equipment>();
        inventory = player.AddComponent<Inventory>();
        wallet = player.AddComponent<Wallet>();
        player.AddComponent<PlayerHealth>();
        ui = player.AddComponent<DiabloVendorUI>();
        if (EventSystem.current == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
    }

    [TearDown]
    public void TearDown()
    {
        if (player != null) Object.DestroyImmediate(player);
        var es = GameObject.Find("EventSystem");
        if (es != null) Object.DestroyImmediate(es);
        if (SaveService.HasSave()) SaveService.DeleteSave();
    }

    static ItemInstance Rare(ItemSlot slot) => new ItemInstance
    {
        slot = slot, rarity = ItemRarity.Rare, baseType = ItemDatabase.GetBaseTypes(slot)[0],
        affixes = new List<Affix> { new Affix("Damage", 3f) }, itemLevel = 4,
    };

    static void Click(string name)
    {
        var go = GameObject.Find("Player/VendorCanvas/Panel/SellAllRow/" + name);
        Assert.IsNotNull(go, name + " exists");
        Assert.IsTrue(go.activeInHierarchy, name + " visible");
        ExecuteEvents.Execute(go, new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left },
            ExecuteEvents.pointerClickHandler);
    }

    [UnityTest]
    public IEnumerator Button_ConfirmShowsSum_ThenSellsAllRares()
    {
        var a = Rare(ItemSlot.Weapon);
        var b = Rare(ItemSlot.Gloves);
        var unique = ItemDatabase.CreateUnique(ItemDatabase.ShardbackAegisId);
        inventory.Add(a); inventory.Add(unique); inventory.Add(b);
        int sum = VendorStock.SellPrice(a) + VendorStock.SellPrice(b);
        ui.SetOpen(true);
        yield return null;

        Click("SellAllRaresButton");
        Assert.IsTrue(ui.SellAllConfirmVisible);
        var text = GameObject.Find("Player/VendorCanvas/Panel/SellAllRow/SellAllConfirmText").GetComponent<UnityEngine.UI.Text>().text;
        StringAssert.Contains("2", text);
        StringAssert.Contains(sum.ToString(), text, "confirm names the gold sum");
        Assert.AreEqual(3, inventory.Count, "nothing sold before confirming");

        Click("SellAllYesButton");
        Assert.IsFalse(ui.SellAllConfirmVisible);
        Assert.AreEqual(1, inventory.Count);
        Assert.AreSame(unique, inventory.Get(0));
        Assert.AreEqual(sum, wallet.Gold);
    }

    [UnityTest]
    public IEnumerator Cancel_KeepsEverything_NoRares_ShowsNotice()
    {
        inventory.Add(Rare(ItemSlot.Helmet));
        ui.SetOpen(true);
        yield return null;
        Click("SellAllRaresButton");
        Click("SellAllNoButton");
        Assert.IsFalse(ui.SellAllConfirmVisible);
        Assert.AreEqual(1, inventory.Count);
        Assert.AreEqual(0, wallet.Gold);

        inventory.RemoveAt(0);
        inventory.Add(ItemDatabase.CreateUnique(ItemDatabase.ShardbackAegisId));
        Assert.AreEqual(0, ui.RequestSellAllRares());
        Assert.IsFalse(ui.SellAllConfirmVisible, "no confirm when there is nothing to sell");
        Assert.AreEqual(Loc.T("vendor.sellall.none"), ui.Notice);
    }
}
