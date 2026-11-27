using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public sealed class BossIntroTimeline : MonoBehaviour
{
    public const string VoiceKey = "dlg.h2.offer.2";
    public bool Finished { get; private set; }
    CinemachineBrain brain;
    bool createdBrain;
    PlayableDirector director;

    public static BossIntroTimeline StartIntro(Camera camera, Transform boss, Vector3 hero)
    {
        if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-director-only") >= 0)
            return null;
        var timeline = Res.Get<TimelineAsset>("Cutscene_BossIntro");
        if (!timeline) return null;
        var host = new GameObject("Abbot intro");
        var intro = host.AddComponent<BossIntroTimeline>();
        var brain = camera.GetComponent<CinemachineBrain>();
        intro.createdBrain = !brain;
        intro.brain = brain ? brain : camera.gameObject.AddComponent<CinemachineBrain>();
        var director = intro.director = host.AddComponent<PlayableDirector>();
        director.playableAsset = timeline;
        director.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
        var body = boss.GetComponentInChildren<SkinnedMeshRenderer>();
        var focus = body ? body.bounds.center : boss.position + Vector3.up * 1.65f;
        var target = new GameObject("Abbot framing target").transform;
        target.SetParent(host.transform, false);
        target.position = focus;
        intro.body = body;
        intro.target = target;
        var axis = Vector3.ProjectOnPlane(hero - boss.position, Vector3.up).normalized;
        if (axis == Vector3.zero) axis = Vector3.back;
        var side = Vector3.Cross(Vector3.up, axis);
        foreach (var track in timeline.GetOutputTracks())
        {
            if (track is CinemachineTrack)
            {
                director.SetGenericBinding(track, intro.brain);
                int i = 0;
                foreach (var clip in track.GetClips())
                {
                    var shot = new GameObject(clip.displayName);
                    shot.transform.SetParent(host.transform);
                    shot.transform.position = focus + (i++ == 0 ? axis * 10 + side * 4 + Vector3.up * 6
                        : axis * 5 - side * 1.5f + Vector3.up * 5);
                    shot.transform.LookAt(focus);
                    var vcam = shot.AddComponent<CinemachineCamera>();
                    vcam.LookAt = target;
                    shot.AddComponent<CinemachineHardLookAt>();
                    vcam.Lens = LensSettings.FromCamera(camera);
                    director.SetReferenceValue(((CinemachineShot)clip.asset).VirtualCamera.exposedName, vcam);
                }
            }
            else if (track is AudioTrack)
            {
                var source = host.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.volume = StoryVoice.Active ? DiabloSettings.EffectiveVoice : 0;
                director.SetGenericBinding(track, source);
            }
            else if (track is AnimationTrack) director.SetGenericBinding(track, boss.GetComponentInChildren<Animator>());
            else if (track is SignalTrack)
            {
                var receiver = host.AddComponent<SignalReceiver>();
                foreach (var marker in track.GetMarkers())
                {
                    if (!(marker is SignalEmitter emitter)) continue;
                    var reaction = new UnityEngine.Events.UnityEvent();
                    reaction.AddListener(() => intro.Finished = true);
                    receiver.AddReaction(emitter.asset, reaction);
                }
                director.SetGenericBinding(track, receiver);
            }
        }
        director.Play();
        return intro;
    }

    SkinnedMeshRenderer body;
    Transform target;

    void Update()
    {
        if (body && target) target.position = body.bounds.center;
    }

    public void Release()
    {
        director.Stop();
        gameObject.SetActive(false);
        if (createdBrain && brain)
        {
            brain.enabled = false;
            Destroy(brain);
        }
        Destroy(gameObject);
    }
}
