using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Pointer glue for one inventory cell (bag cell or equipment slot): hover
/// shows the shared tooltip, right click swaps instantly, left drag carries
/// the item to another cell. All state changes go back to
/// <see cref="DiabloInventoryUI"/>, which routes them through the
/// authoritative equip/unequip path.
/// </summary>
public sealed class InventoryCell : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, ISubmitHandler
{
    /// <summary>Keyboard/gamepad submit on a bag item equips it (mouse clicks lift it instead).</summary>
    public void OnSubmit(BaseEventData e) { if (owner != null) owner.CellSubmit(this); }

    /// <summary>Doll is the 3D hero: a bag item dropped there goes to its own slot.</summary>
    public enum Kind { Bag, Equip, Doll }

    public Kind kind;
    /// <summary>Bag index for <see cref="Kind.Bag"/>, (int)ItemSlot for <see cref="Kind.Equip"/>.</summary>
    public int index;
    public DiabloInventoryUI owner;

    public ItemSlot Slot => (ItemSlot)index;

    public void OnPointerEnter(PointerEventData e) { if (owner != null) owner.CellHover(this, true); }

    public void OnPointerExit(PointerEventData e) { if (owner != null) owner.CellHover(this, false); }

    public void OnPointerClick(PointerEventData e)
    {
        if (owner == null) return;
        if (e.button == PointerEventData.InputButton.Right) owner.CellRightClick(this);
        else if (e.button == PointerEventData.InputButton.Left) owner.CellLeftClick(this);
    }

    public void OnBeginDrag(PointerEventData e)
    {
        if (owner == null || e.button != PointerEventData.InputButton.Left) return;
        owner.CellBeginDrag(this, e.position);
    }

    public void OnDrag(PointerEventData e) { if (owner != null) owner.CellDrag(e.position); }

    public void OnEndDrag(PointerEventData e) { if (owner != null) owner.CellEndDrag(); }

    public void OnDrop(PointerEventData e) { if (owner != null) owner.CellDrop(this); }
}
