using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MoorbrutEncounter
{
    const string RootName = "Moorbrut • Fen";
    const string Art = "Assets/Art/Moorbrut/";

    public static void Place()
    {
        if (GameObject.Find(RootName)) return;
        var root = new GameObject(RootName);
        var anchor = new Vector3(-17f, 0.05f, 12f);
        Spawn(root.transform, anchor, true, 0);
        for (int i = 0; i < 4; i++)
        {
            var offset = Quaternion.Euler(0, 45f + i * 90f, 0) * Vector3.forward * 2.4f;
            Spawn(root.transform, anchor + offset, false, i);
        }
    }

    static void Spawn(Transform parent, Vector3 pos, bool tank, int index)
    {
        string asset = tank ? "Moorbastion" : "Glutlaeufer";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Art + asset + ".fbx");
        var host = new GameObject(tank ? asset : $"Glutläufer {index + 1}");
        host.tag = "Husk";
        host.transform.SetParent(parent);
        host.transform.position = pos;
        var visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab, host.transform);
        var model = visual.GetComponent<Animation>();
        if (!model || !model.GetClip("Walk") || !model.GetClip("Strike"))
            throw new System.InvalidOperationException($"Missing walk/strike clips on {asset}");
        var collider = host.AddComponent<CapsuleCollider>();
        collider.height = tank ? 2.25f : 0.7f;
        collider.radius = tank ? 0.95f : 0.35f;
        collider.center = Vector3.up * collider.height * 0.5f;
        host.AddComponent<NetworkObject>();
        var hp = host.AddComponent<EnemyHealth>();
        hp.SetTier(tank ? 240 : 12, tank ? LootTier.Elite : LootTier.Normal);
        hp.corpseModel = prefab;
        var combat = host.AddComponent<MoorbrutCombat>();
        combat.tank = tank;
        combat.model = model;
        combat.flankOffset = (index - 1.5f) * 18f;
        var attack = host.AddComponent<EnemyAttack>();
        attack.damage = tank ? 4 : 1;
        attack.turnRate = tank ? 3f : 12f;
    }

    public static void UpdateFenScene()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/DiabloZone.unity");
        Place();
        if (!EditorSceneManager.SaveScene(scene)) throw new System.IO.IOException(scene.path);
        Debug.Log("Moorbrut encounter saved");
    }
}
