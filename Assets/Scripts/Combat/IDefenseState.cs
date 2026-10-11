namespace Rival.Combat
{
    /// <summary>
    /// The defender's side of a hit. Implemented by CombatController so hit
    /// resolution asks the defender "what are you doing?" without the receiver
    /// knowing anything about attack state machines or stamina.
    /// </summary>
    public interface IDefenseState
    {
        bool IsBlocking { get; }
        bool IsParryWindowActive { get; }
        bool IsDodgeInvulnerable { get; }

        /// <summary>Returns true if the guard held (stamina spent); false if it broke.</summary>
        bool OnBlocked(DamageContext context);

        /// <summary>Called when an attack lands inside the parry window.</summary>
        void OnSuccessfulParry(DamageContext context);
    }
}
