using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

public class MoorbrutEncounterTests
{
    [Test]
    public void SavedFenHasAFlankingPackAndForwardStrikes()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/DiabloZone.unity");
        var pack = Object.FindObjectsByType<MoorbrutCombat>(FindObjectsSortMode.None);
        Assert.AreEqual(1, pack.Count(enemy => enemy.tank));
        Assert.AreEqual(4, pack.Where(enemy => !enemy.tank).Select(enemy => enemy.flankOffset).Distinct().Count());
        foreach (var enemy in pack)
        {
            Assert.AreEqual(enemy.tank ? 240 : 12, enemy.GetComponent<EnemyHealth>().maxHP);
            Assert.IsNotNull(enemy.GetComponent<EnemyAttack>());
            var model = enemy.model.gameObject;
            var part = model.GetComponentsInChildren<MeshRenderer>().First();
            var clip = enemy.model.GetClip("Strike");
            clip.SampleAnimation(model, 0);
            var start = part.bounds.center;
            clip.SampleAnimation(model, enemy.ImpactTime);
            Assert.Greater(Vector3.Dot(part.bounds.center - start, enemy.transform.forward), 0.4f,
                enemy.name + " strikes forward");
            Assert.IsTrue(enemy.InArc(enemy.transform.position + enemy.transform.forward));
            Assert.IsFalse(enemy.InArc(enemy.transform.position + enemy.transform.right));
            clip.SampleAnimation(model, 0);
        }
    }
}
