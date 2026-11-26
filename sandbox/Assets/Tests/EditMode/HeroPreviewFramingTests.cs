using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Character select visibility: the preview camera frames the whole figure,
/// hair sits outside the chibi head (visible colour and style), and the
/// Knight's built-in helmet no longer hides the chosen hair.
/// </summary>
public class HeroPreviewFramingTests
{
    GameObject camGo;

    [TearDown]
    public void TearDown()
    {
        if (camGo != null) Object.DestroyImmediate(camGo);
    }

    Camera MakeCamera(Vector3 pos, Vector3 target)
    {
        camGo = new GameObject("FramingCam");
        var cam = camGo.AddComponent<Camera>();
        cam.fieldOfView = HeroPreviewView.FieldOfView;
        cam.aspect = HeroPreviewView.TextureAspect;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 100f;
        cam.transform.position = pos;
        cam.transform.LookAt(target);
        return cam;
    }

    static void AssertInside(Camera cam, Bounds b, string what)
    {
        for (int i = 0; i < 8; i++)
        {
            var corner = new Vector3(
                (i & 1) == 0 ? b.min.x : b.max.x,
                (i & 2) == 0 ? b.min.y : b.max.y,
                (i & 4) == 0 ? b.min.z : b.max.z);
            var vp = cam.WorldToViewportPoint(corner);
            Assert.That(vp.z, Is.GreaterThan(0f), $"{what}: corner {corner} behind the camera");
            Assert.That(vp.x, Is.InRange(0f, 1f), $"{what}: corner {corner} cut off left/right (x={vp.x})");
            Assert.That(vp.y, Is.InRange(0f, 1f), $"{what}: corner {corner} cut off top/bottom (y={vp.y})");
        }
    }

    [Test]
    public void FrameFigure_KeepsFeetAndHeadOfATallHeroInView()
    {
        // Measured in play mode: a male Lanternbearer with hat stands 2.83 m tall, feet at the pivot.
        var pivot = new Vector3(5000f, -60f, 5000f);
        var figure = new Bounds(pivot + new Vector3(-0.02f, 1.42f, 0.02f), new Vector3(2.18f, 2.83f, 2.13f));
        HeroPreviewView.FrameFigure(figure, HeroPreviewView.FieldOfView, HeroPreviewView.TextureAspect,
            out var pos, out var target);
        var cam = MakeCamera(pos, target);
        AssertInside(cam, figure, "lanternbearer");
    }

    [Test]
    public void FrameFigure_KeepsAWideFigureInViewWhileSwaying()
    {
        // A sword and cape push the bounds off the pivot (measured: centre x -0.47, z +0.26).
        var pivot = Vector3.zero;
        var figure = new Bounds(new Vector3(-0.47f, 1.18f, 0.26f), new Vector3(2.15f, 2.72f, 2.49f));
        HeroPreviewView.FrameFigure(HeroPreviewView.SweptBounds(figure, pivot, HeroPreviewView.SwayDegrees), HeroPreviewView.FieldOfView, HeroPreviewView.TextureAspect,
            out var pos, out var target);
        var cam = MakeCamera(pos, target);
        // The sway turns the figure around the pivot by up to ±SwayDegrees.
        for (float deg = -HeroPreviewView.SwayDegrees; deg <= HeroPreviewView.SwayDegrees; deg += 10f)
        {
            var rot = Quaternion.Euler(0f, deg, 0f);
            var turned = new Bounds(rot * figure.center, Vector3.zero);
            for (int i = 0; i < 8; i++)
            {
                var c = new Vector3((i & 1) == 0 ? figure.min.x : figure.max.x,
                    (i & 2) == 0 ? figure.min.y : figure.max.y, (i & 4) == 0 ? figure.min.z : figure.max.z);
                turned.Encapsulate(rot * c);
            }
            AssertInside(cam, turned, $"turn {deg}°");
        }
    }

    [Test]
    public void FrameFigure_UsesMostOfTheHeight()
    {
        var pivot = Vector3.zero;
        var figure = new Bounds(new Vector3(0f, 1.2f, 0f), new Vector3(1.6f, 2.4f, 1.4f));
        HeroPreviewView.FrameFigure(figure, HeroPreviewView.FieldOfView, HeroPreviewView.TextureAspect,
            out var pos, out var target);
        var cam = MakeCamera(pos, target);
        float top = cam.WorldToViewportPoint(new Vector3(0f, figure.max.y, 0f)).y;
        float bottom = cam.WorldToViewportPoint(new Vector3(0f, figure.min.y, 0f)).y;
        Assert.That(top - bottom, Is.GreaterThan(0.6f), "the figure fills the portrait, not a corner of it");
    }

    [Test]
    public void HeadBounds_KnownBasesAreMeasured_UnknownFallsBack()
    {
        Assert.AreEqual(0.51f, ClassAppearance.HeadBoundsFor("Knight_Head").center.y, 0.001f);
        Assert.AreEqual(0.38f, ClassAppearance.HeadBoundsFor("Mage_Head").center.y, 0.001f);
        Assert.AreEqual(ClassAppearance.DefaultHeadBounds, ClassAppearance.HeadBoundsFor("Barbarian_Head"));
        Assert.AreEqual(ClassAppearance.DefaultHeadBounds, ClassAppearance.HeadBoundsFor(null));
    }

    [Test]
    public void Hair_SitsOutsideTheHeadSoStyleAndColourShow()
    {
        var head = new GameObject("head");
        try
        {
            var bounds = ClassAppearance.HeadBoundsFor("Knight_Head");
            for (int style = 0; style < HeroAppearance.HairStyleCount; style++)
            {
                if (style == 4) continue; // Glatze
                var hair = ClassAppearance.BuildHair(head.transform,
                    new HeroProfile { heroName = "Test", hairStyle = style, hairColor = 6 }, bounds, hooded: false);
                var cap = hair.transform.Find("HairCap");
                Assert.IsNotNull(cap, $"style {style} has a cap");
                float capTop = cap.localPosition.y + cap.localScale.y * 0.5f;
                Assert.That(capTop, Is.GreaterThan(bounds.max.y), $"style {style}: cap clears the crown");
                float capSide = cap.localScale.x * 0.5f;
                Assert.That(capSide, Is.GreaterThan(bounds.extents.x), $"style {style}: cap clears the sides");
                var lockL = hair.transform.Find("HairLockL");
                Assert.IsNotNull(lockL, $"style {style}: side locks frame the face");
                Assert.That(Mathf.Abs(lockL.localPosition.x) + lockL.localScale.x * 0.5f,
                    Is.GreaterThan(bounds.extents.x), $"style {style}: the lock pokes out beside the head");
                float capFront = cap.localPosition.z + cap.localScale.z * 0.5f;
                Assert.That(capFront, Is.LessThan(bounds.max.z), $"style {style}: the cap leaves the face free");
                Object.DestroyImmediate(hair);
            }
        }
        finally { Object.DestroyImmediate(head); }
    }

    [Test]
    public void Hair_HoodedBaseKeepsItsHoodButShowsLocks()
    {
        var head = new GameObject("head");
        try
        {
            var hair = ClassAppearance.BuildHair(head.transform,
                new HeroProfile { heroName = "Test", hairStyle = 0, hairColor = 3 },
                ClassAppearance.HeadBoundsFor("RogueHooded_Head"), hooded: true);
            Assert.IsNull(hair.transform.Find("HairCap"), "no hair-coloured cap over the hood");
            Assert.IsNotNull(hair.transform.Find("HairLockL"));
            Assert.IsNotNull(hair.transform.Find("HairLockR"));
        }
        finally { Object.DestroyImmediate(head); }
    }

    [Test]
    public void BuiltInHelmet_IsRecognisedSoTheHairShows()
    {
        Assert.IsTrue(ClassAppearance.IsBuiltInHelmet("Knight_Helmet"));
        Assert.IsTrue(ClassAppearance.IsBuiltInHelmet("Knight_HelmetVisor"));
        Assert.IsFalse(ClassAppearance.IsBuiltInHelmet("Knight_Head"));
        Assert.IsFalse(ClassAppearance.IsBuiltInHelmet(null));
    }

    [Test]
    public void CardProfiles_ShowDistinctLooksPerClass()
    {
        var fen = ClassSelectUI.CardProfile(ClassCatalog.Fenwalker);
        var lan = ClassSelectUI.CardProfile(ClassCatalog.Lanternbearer);
        var rog = ClassSelectUI.CardProfile(ClassCatalog.Rogue);
        Assert.AreNotEqual(fen.hairColor, lan.hairColor);
        Assert.AreNotEqual(lan.hairColor, rog.hairColor);
        Assert.AreNotEqual(4, fen.hairStyle, "the card hero is not bald, its hair shows");
    }
}
