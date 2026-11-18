/// <summary>
/// Called by <see cref="EnemyHealth"/> on death, before the enemy is
/// destroyed (act-one S3/S4: Cinder Thrall blast, Kilnmother act completion).
/// </summary>
public interface IEnemyDeathHook
{
    void OnEnemyDeath();
}
