using NUnit.Framework;

/// <summary>Global test mute (EditMode leg): clip-building asserts run,
/// no AudioSource ever plays, so suites stay silent on dev machines.</summary>
[SetUpFixture]
public class TestAudioMute
{
    [OneTimeSetUp]
    public void Mute()
    {
        DiabloAudio.MutePlayback = true;
    }

    [OneTimeTearDown]
    public void Unmute()
    {
        DiabloAudio.MutePlayback = false;
    }
}
