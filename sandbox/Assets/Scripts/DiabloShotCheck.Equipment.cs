using System.Collections;
using System.IO;
using UnityEngine;

/// <summary>Shotcheck part: Six-slot role sets per class, front/back and paperdoll (-equipment-shots).</summary>
public sealed partial class DiabloShotCheck
{
    IEnumerator EquipmentShots(DiabloClickController hero, Vector3 towardCamera)
    {
        var equipment = hero.GetComponent<Equipment>();
        var appearance = hero.GetComponent<ClassAppearance>();
        var inventory = hero.GetComponent<Inventory>();
        float distance = hero.distance;
        float pitch = hero.pitch;
        Face(hero, towardCamera);
        var heroLook = appearance.AppliedProfile;
        foreach (var cls in ClassCatalog.All)
            foreach (HeroGender gender in System.Enum.GetValues(typeof(HeroGender)))
            {
                var look = heroLook;
                look.gender = (int)gender;
                appearance.Apply(cls, look);
                yield return Wait(.35f);
                yield return Shot($"base_{cls.id}_{gender.ToString().ToLowerInvariant()}");
            }
        appearance.Apply(ClassCatalog.Fenwalker, heroLook);

        hero.distance = 5f;
        hero.pitch = 33f;
        foreach (var id in ItemDatabase.UniqueIds)
        {
            var item = ItemDatabase.CreateUnique(id);
            appearance.Apply(ClassCatalog.Get(ItemDatabase.ClassRestriction(item.baseType)));
            var facing = item.slot == ItemSlot.Weapon
                ? Quaternion.Euler(0, HeroGearModels.IsBowUnique(id) ? 45f : -45f, 0) * towardCamera
                : id == ItemDatabase.ShardbackAegisId ? -towardCamera : towardCamera;
            Face(hero, facing);
            equipment.Equip(item);
            yield return Wait(.35f);
            yield return Shot(id.Replace(' ', '-').Replace("'", ""));
            equipment.SetSlot(item.slot, null);
        }

        // gear-slots S7: every class in a full six-slot role set, front and back, then the paperdoll.
        foreach (var cls in ClassCatalog.All)
        {
            appearance.Apply(cls, heroLook);
            foreach (var slot in Equipment.GearSlots)
            {
                string uid = slot == ItemSlot.Weapon ? ItemDatabase.ZoneWeaponFor(cls.id)
                    : HeroGearModels.IsLimbSlot(slot) ? ItemDatabase.RoleUniquesFor(slot)[(int)cls.role]
                    : ItemDatabase.UniqueForMatrix(cls.role, slot);
                equipment.Equip(ItemDatabase.CreateUnique(uid));
            }
            Face(hero, towardCamera);
            yield return Wait(.4f);
            yield return Shot($"gearset_{cls.id}_front");
            Face(hero, -towardCamera);
            yield return Wait(.3f);
            yield return Shot($"gearset_{cls.id}_back");
            Face(hero, towardCamera);
            var ui = hero.GetComponent<DiabloInventoryUI>() ?? hero.gameObject.AddComponent<DiabloInventoryUI>();
            ui.SetOpen(true);
            ui.Refresh();
            yield return Wait(.5f);
            yield return Shot($"gearset_{cls.id}_paperdoll");
            ui.SetOpen(false);
            foreach (var slot in Equipment.GearSlots) equipment.SetSlot(slot, null);
        }
        appearance.Apply(ClassCatalog.Fenwalker, heroLook);
        hero.distance = distance;
        hero.pitch = pitch;
    }
}
