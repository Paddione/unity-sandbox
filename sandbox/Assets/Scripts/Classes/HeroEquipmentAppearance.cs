using System.Collections.Generic;
using UnityEngine;

public sealed class HeroEquipmentAppearance : MonoBehaviour
{
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

    public bool Initialize(ClassAppearance owner, GameObject clothing)
    {
        appearance = owner;
        equipment = GetComponent<Equipment>();
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
        chest = Mount("Chest art", chestBone);
        head = Mount("Head art", headBone);
        right = Mount("Right hand art", rightBone);
        left = Mount("Left hand art", leftBone);
        bones.TryGetValue(DiabloSkills.SwordName, out sword);
        BuildClothing(clothing, bones);
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

    void BuildClothing(GameObject clothing, Dictionary<string, Transform> bones)
    {
        var imported = Instantiate(clothing);
        imported.SetActive(false);
        var root = new GameObject("Light clothing");
        root.transform.SetParent(transform, false);
        foreach (var source in imported.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (source.name.Contains("Cape") || source.name.Contains("Quiver")) continue;
            var mapped = source.bones;
            for (int i = 0; i < mapped.Length; i++)
                if (mapped[i] != null && bones.TryGetValue(mapped[i].name, out var bone))
                    mapped[i] = bone;
            source.bones = mapped;
            if (source.rootBone != null && bones.TryGetValue(source.rootBone.name, out var rootBone))
                source.rootBone = rootBone;
            source.transform.SetParent(root.transform, false);
        }
        Destroy(imported);
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
        starterGuard.SetActive(armor == null);
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
