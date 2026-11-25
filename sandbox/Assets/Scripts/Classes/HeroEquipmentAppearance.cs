using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Worn-gear art on top of the class body (<see cref="ClassAppearance"/>):
/// visible Unique models per slot and the Fenwalker's starter chest guard.
/// The body itself stays the class+gender model, so classes keep distinct
/// silhouettes.
/// </summary>
public sealed class HeroEquipmentAppearance : MonoBehaviour
{
    /// <summary>Bone mounts this component adds (clones strip them, see CompanionFactory).</summary>
    public static readonly string[] MountNames = { "Chest art", "Head art", "Right hand art", "Left hand art" };

    Equipment equipment;
    ClassAppearance appearance;
    DiabloNetworkPlayer networkPlayer;
    DiabloSkills skills;
    Transform chest;
    Transform head;
    Transform right;
    Transform left;
    Transform sword;
    SkinnedMeshRenderer[] baseSkin;
    GameObject starterGuard;
    GameObject armor;
    GameObject helmet;
    GameObject weapon;
    string armorId;
    string helmetId;
    string weaponId;
    string classId;

    /// <param name="source">Equipment to mirror; defaults to this object's own
    /// (the inventory paperdoll passes the live hero's).</param>
    public bool Initialize(ClassAppearance owner, Equipment source = null)
    {
        appearance = owner;
        equipment = source != null ? source : GetComponent<Equipment>();
        networkPlayer = GetComponent<DiabloNetworkPlayer>();
        skills = GetComponent<DiabloSkills>();
        var bones = new Dictionary<string, Transform>();
        foreach (var child in GetComponentsInChildren<Transform>(true))
            bones.TryAdd(child.name, child);
        if (equipment == null || !bones.TryGetValue("chest", out var chestBone) ||
            !bones.TryGetValue("head", out var headBone) || !bones.TryGetValue("handslot.r", out var rightBone) ||
            !bones.TryGetValue("handslot.l", out var leftBone))
            return false;

        baseSkin = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        chest = Mount(MountNames[0], chestBone);
        head = Mount(MountNames[1], headBone);
        right = Mount(MountNames[2], rightBone);
        left = Mount(MountNames[3], leftBone);
        bones.TryGetValue(DiabloSkills.SwordName, out sword);
        return true;
    }

    Transform Mount(string name, Transform bone)
    {
        var anchor = new GameObject(name).transform;
        anchor.SetParent(bone, false);
        // Author gear facing forward in character space, then convert to the rig's bind axes.
        foreach (var skin in baseSkin)
        {
            int index = System.Array.IndexOf(skin.bones, bone);
            if (index < 0 || index >= skin.sharedMesh.bindposes.Length) continue;
            var bind = skin.sharedMesh.bindposes[index] * skin.transform.worldToLocalMatrix * transform.localToWorldMatrix;
            anchor.localRotation = bind.rotation;
            anchor.localScale = bind.lossyScale;
            return anchor;
        }
        anchor.rotation = transform.rotation;
        anchor.localScale = Vector3.one / bone.lossyScale.x;
        return anchor;
    }

    static string Id(ItemInstance item) => item?.rarity == ItemRarity.Unique ? item.uniqueId : null;

    void LateUpdate()
    {
        if (equipment == null || chest == null) return;
        if (classId != appearance.AppliedClassId)
        {
            classId = appearance.AppliedClassId;
            if (starterGuard != null)
            {
                starterGuard.SetActive(false);
                Destroy(starterGuard);
            }
            starterGuard = new GameObject("Yellow starter armor");
            starterGuard.transform.SetParent(chest, false);
            HeroGearModels.StarterArmor(starterGuard.transform, classId);
        }

        bool mirror = networkPlayer != null && networkPlayer.IsSpawned && !networkPlayer.IsOwner;
        string nextArmor = mirror ? networkPlayer.visualArmor.Value.ToString() : Id(equipment.armor);
        string nextHelmet = mirror ? networkPlayer.visualHelmet.Value.ToString() : Id(equipment.helmet);
        string nextWeapon = mirror ? networkPlayer.visualWeapon.Value.ToString() : Id(equipment.weapon);
        Replace(ref armor, ref armorId, nextArmor, ItemSlot.Armor, chest);
        Replace(ref helmet, ref helmetId, nextHelmet, ItemSlot.Helmet, head);
        Replace(ref weapon, ref weaponId, nextWeapon, ItemSlot.Weapon,
            nextWeapon == ItemDatabase.NightfangId ? left : right);

        bool lanternbearer = classId == ClassCatalog.LanternbearerId;
        bool bowDrawn = skills != null && skills.BowDrawn;
        // Brass plate is the tank's starter look; robe and leathers stay bare.
        starterGuard.SetActive(armor == null && classId == ClassCatalog.FenwalkerId);
        if (sword != null) sword.gameObject.SetActive(weapon == null && !lanternbearer && !bowDrawn);
        appearance.SetStarterLanternVisible(lanternbearer && weapon == null);
        if (skills != null) skills.SetStarterBowModelVisible(weaponId != ItemDatabase.NightfangId);
        if (weapon != null)
        {
            bool uniqueBow = weaponId == ItemDatabase.NightfangId;
            bool hangingLantern = weaponId == "Lantern of the Hold" || weaponId == ItemDatabase.WickOfHaloId;
            if (hangingLantern || (uniqueBow && !bowDrawn))
                weapon.transform.rotation = transform.rotation;
            else
                weapon.transform.localRotation = Quaternion.identity;
            weapon.SetActive(!bowDrawn || uniqueBow || lanternbearer);
        }
    }

    static void Replace(ref GameObject model, ref string previous, string id, ItemSlot slot, Transform mount)
    {
        if (previous == id) return;
        if (model != null)
        {
            model.SetActive(false);
            Destroy(model);
        }
        previous = id;
        model = HeroGearModels.Create(id, slot, mount);
    }
}
