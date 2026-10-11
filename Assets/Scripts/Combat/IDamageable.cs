namespace Rival.Combat
{
    /// <summary>
    /// Contract for anything that can be hit. The defender decides, through its
    /// DamageReceiver, how much damage actually lands (block/parry/dodge).
    /// </summary>
    public interface IDamageable
    {
        DamageResult ApplyDamage(DamageContext context);
    }
}
