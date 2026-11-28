using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Cursor item (cinderreach-inventory-cursor-item): with the inventory open, a
/// left click lifts a bag or worn item onto the mouse cursor; the next click
/// places it. The item stays in its source slot until a placement succeeds,
/// so cancelling (click outside, Esc, closing the panel) can never lose it.
/// Drag &amp; drop and right click keep working alongside.
/// </summary>
public sealed partial class DiabloInventoryUI
{
    /// <summary>Where the held item lives while it rides the cursor.</summary>
    public struct HeldRef
    {
        public InventoryCell.Kind kind; // Bag or Equip
        public int index;               // bag index or (int)ItemSlot
    }

    HeldRef? held;
    Image cursorBackdrop;

    /// <summary>True while an item rides the cursor.</summary>
    public bool IsHolding => held.HasValue && HeldItem != null;

    /// <summary>The item on the cursor (null when nothing is held).</summary>
    public ItemInstance HeldItem
    {
        get
        {
            if (!held.HasValue) return null;
            var h = held.Value;
            if (h.kind == InventoryCell.Kind.Bag) return inventory != null ? inventory.Get(h.index) : null;
            if (h.kind == InventoryCell.Kind.Equip) return equipment != null ? equipment.GetEquipped((ItemSlot)h.index) : null;
            return null;
        }
    }

    /// <summary>Source of the held item (test seam).</summary>
    public HeldRef? Held => held;

    void BuildCursorBackdrop()
    {
        if (canvasRect == null || cursorBackdrop != null) return;
        var go = new GameObject("CursorBackdrop", typeof(RectTransform));
        go.transform.SetParent(canvasRect, false);
        go.transform.SetAsFirstSibling();
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        cursorBackdrop = go.AddComponent<Image>();
        cursorBackdrop.color = new Color(0f, 0f, 0f, 0f);
        cursorBackdrop.raycastTarget = true;
        go.AddComponent<InventoryOutsideClick>().owner = this;
        // Clicks on panel areas without their own handler bubble up to the canvas root.
        canvasRect.gameObject.AddComponent<InventoryOutsideClick>().owner = this;
        go.SetActive(false);
    }

    void Hold(InventoryCell.Kind kind, int index)
    {
        held = new HeldRef { kind = kind, index = index };
        var item = HeldItem;
        if (item == null) { held = null; return; }
        BuildCursorBackdrop();
        if (cursorBackdrop != null) cursorBackdrop.gameObject.SetActive(true);
        ItemTooltip.HideFor(gameObject);
        if (dragGhost != null)
        {
            dragGhost.sprite = DiabloMenuArt.ItemIcon(item.baseType, item.rarity);
            dragGhost.gameObject.SetActive(true);
            dragGhost.transform.SetAsLastSibling();
        }
        FollowCursor();
    }

    /// <summary>Put the held item back (it never left its slot).</summary>
    public void ReleaseHold()
    {
        held = null;
        if (cursorBackdrop != null) cursorBackdrop.gameObject.SetActive(false);
        if (dragGhost != null && dragSource == null) dragGhost.gameObject.SetActive(false);
    }

    void FollowCursor()
    {
        if (!IsHolding || dragGhost == null || canvasRect == null) return;
        var mouse = Mouse.current;
        if (mouse == null) return;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, mouse.position.ReadValue(), null, out var local))
            dragGhost.rectTransform.anchoredPosition = local;
    }

    void TickCursorItem()
    {
        if (!held.HasValue) return;
        if (!IsHolding) { ReleaseHold(); return; } // item vanished (sold, host update)
        FollowCursor();
    }

    internal void CellLeftClick(InventoryCell cell)
    {
        if (cell == null || IsDragging) return;
        if (cell.kind == InventoryCell.Kind.Bag) ClickBag(cell.index);
        else if (cell.kind == InventoryCell.Kind.Equip) ClickSlot(cell.Slot);
        else if (cell.kind == InventoryCell.Kind.Doll) ClickDoll();
    }

    /// <summary>
    /// Left click on bag cell <paramref name="index"/> (-1 = empty cell / grid):
    /// lifts the item, or places the held one (bag: move/swap, worn: unequip or swap in).
    /// </summary>
    public void ClickBag(int index)
    {
        EnsureBuilt();
        if (inventory == null) return;
        bool hasItem = index >= 0 && index < inventory.Count;
        if (!IsHolding)
        {
            if (hasItem) Hold(InventoryCell.Kind.Bag, index);
            return;
        }
        var h = held.Value;
        if (h.kind == InventoryCell.Kind.Bag)
        {
            if (!hasItem) { inventory.MoveToEnd(h.index); ReleaseHold(); Refresh(); return; }
            if (index == h.index) { ReleaseHold(); return; }
            inventory.Swap(h.index, index);
            // The displaced item now sits in the source cell and rides the cursor.
            DiabloAudio.PlayUIClick();
            Refresh();
            Hold(InventoryCell.Kind.Bag, h.index);
            return;
        }
        // Worn item onto the bag: swap with a fitting bag item, else unequip.
        var slot = (ItemSlot)h.index;
        var target = hasItem ? inventory.Get(index) : null;
        ReleaseHold();
        if (target != null && target.slot == slot && Equipment.CanEquip(target, RunProfile.ClassId)) EquipIndex(index);
        else UnequipSlot(h.index);
    }

    /// <summary>Left click on equipment slot <paramref name="slot"/>: lift the worn item or equip the held bag item.</summary>
    public void ClickSlot(ItemSlot slot)
    {
        EnsureBuilt();
        if (!IsHolding)
        {
            if (equipment != null && equipment.GetEquipped(slot) != null) Hold(InventoryCell.Kind.Equip, (int)slot);
            return;
        }
        var h = held.Value;
        if (h.kind == InventoryCell.Kind.Equip) { ReleaseHold(); return; }
        PlaceBagOnSlot(h.index, slot);
    }

    /// <summary>Left click on the 3D hero: equip the held bag item into its own slot.</summary>
    public void ClickDoll()
    {
        if (!IsHolding) return;
        var h = held.Value;
        if (h.kind != InventoryCell.Kind.Bag) { ReleaseHold(); return; }
        var item = HeldItem;
        if (item == null || !Equipment.IsGearSlot(item.slot)) { ReleaseHold(); return; }
        PlaceBagOnSlot(h.index, item.slot);
    }

    void PlaceBagOnSlot(int bagIndex, ItemSlot slot)
    {
        var relay = GetComponent<DiabloNetworkPlayer>();
        bool networked = relay != null && relay.IsSpawned;
        var worn = equipment != null ? equipment.GetEquipped(slot) : null;
        if (!DropBagOnSlot(bagIndex, slot)) return; // wrong slot or class: flashes red, item stays on the cursor
        ReleaseHold();
        // Solo/host swap: the previously worn item landed at the bag's end; it rides the cursor next.
        if (!networked && worn != null && inventory != null && inventory.Count > 0
            && inventory.Get(inventory.Count - 1) == worn)
            Hold(InventoryCell.Kind.Bag, inventory.Count - 1);
    }

    /// <summary>Click outside any cell: the held item goes back where it came from.</summary>
    public void ClickOutside()
    {
        if (held.HasValue) ReleaseHold();
    }
}

/// <summary>Outside-click catcher for the cursor item (backdrop and panel background).</summary>
public sealed class InventoryOutsideClick : MonoBehaviour, IPointerClickHandler
{
    public DiabloInventoryUI owner;

    public void OnPointerClick(PointerEventData e)
    {
        if (owner != null && e.button == PointerEventData.InputButton.Left) owner.ClickOutside();
    }
}
