using System.Collections;
using System.IO;
using UnityEngine;

/// <summary>Shotcheck part: Split-body locomotion poses with bone logs (-locomotion-shots).</summary>
public sealed partial class DiabloShotCheck
{
    /// <summary>
    /// Split-body locomotion check: the hero aims to screen right (profile)
    /// and runs forward, toward the camera (strafe), backward and stops.
    /// Each pose logs Animator state and foot heights above the hero root.
    /// </summary>
    IEnumerator LocomotionShots(DiabloClickController hero, Vector3 camFwd, Vector3 camRight)
    {
        hero.distance = 6f;
        hero.pitch = 25f;
        var poses = new (string name, Vector3 move)[]
        {
            ("loco_run_forward", camRight),
            ("loco_strafe", -camFwd),
            ("loco_backpedal", -camRight),
            ("loco_stop", Vector3.zero),
        };
        foreach (var (name, move) in poses)
        {
            Face(hero, camRight);
            hero.SetMoveInput(move);
            for (int i = 0; i < 4; i++)
            {
                Face(hero, camRight);
                yield return Wait(i == 0 ? 0.6f : 0.12f);
                LogBones(hero, $"{name}_{i}");
                yield return Shot($"{name}_{i}");
            }
        }
        yield return Wait(0.8f);
        LogBones(hero, "loco_stop_settled");
        yield return Shot("loco_stop_settled");
        hero.distance = 11f;
        hero.pitch = 55f;
    }

    static void LogBones(DiabloClickController hero, string tag)
    {
        var anim = hero.GetComponent<Animator>();
        var twist = hero.GetComponent<LocomotionTwist>();
        string state = "?";
        float nt = 0f;
        if (anim != null && anim.runtimeAnimatorController != null)
        {
            var info = anim.GetCurrentAnimatorStateInfo(0);
            foreach (var s in new[] { "Idle", "Run", "Strike", "BowAim", "BowShoot" })
                if (info.IsName(s)) state = s;
            nt = info.normalizedTime;
            if (anim.IsInTransition(0)) state += "*";
        }
        float baseY = hero.transform.position.y;
        string H(string bone)
        {
            var b = FindBone(hero.transform, bone);
            return b == null ? "-" : (b.position.y - baseY).ToString("F2");
        }
        Log($"bones {tag}: state={state} t={nt:F2} speed={(anim != null ? anim.GetFloat(LocomotionTwist.MoveSpeedParam) : 0f):F1} twist={(twist != null ? twist.Twist : 0f):F0} back={(twist != null && twist.Backpedaling)} vel={(twist != null ? twist.Velocity.magnitude : 0f):F1} hips={H("hips")} footL={H("foot.l")} footR={H("foot.r")} lowL={H("lowerleg.l")} lowR={H("lowerleg.r")}");
    }

    static Transform FindBone(Transform t, string name)
    {
        if (t.name == name) return t;
        foreach (Transform c in t)
        {
            var hit = FindBone(c, name);
            if (hit != null) return hit;
        }
        return null;
    }
}
