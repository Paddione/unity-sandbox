using UnityEngine;

/// <summary>
/// One teleport path for zone gates and waypoint shrines (act-one S5):
/// CharacterController off, move, on. Co-op replication of this step lives
/// in cinderreach-coop-session S3 and only needs to hook here.
/// </summary>
public static class ZoneTravel
{
    public static int Travels;

    public static void TeleportHero(GameObject hero, Vector3 destination)
    {
        if (hero == null) return;
        var cc = hero.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        hero.transform.position = destination;
        if (cc != null) cc.enabled = true;
        Travels++;
    }

    public static void ResetRun() => Travels = 0;
}
