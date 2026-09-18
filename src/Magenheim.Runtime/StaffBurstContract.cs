using System;
using BepInEx.Logging;

namespace Magenheim.Runtime;

/// <summary>
/// Makes a staff's authored volley actually reach the world, whatever its burst profile.
/// </summary>
/// <remarks>
/// Six of the eight Master staves depleted stamina and produced nothing in play, while Radiance and
/// Earth fired. The split is exact and it is not damage, damage type, projectile source or payload:
/// every staff that fires has <c>m_projectileBursts == 1</c> and every staff that produces nothing
/// has more than one. Valheim's own code is why. <c>Attack.ProjectileAttackTriggered</c> ends with
///
///     if (m_projectileBursts == 1) FireProjectileBurst();
///     else m_projectileAttackStarted = true;
///
/// A single burst fires synchronously, inside the animation trigger. More than one defers to
/// <c>Attack.UpdateProjectile</c>, which only runs while the attack is live — and <c>Attack.Update</c>
/// calls <c>Stop()</c> as soon as <c>InAttack()</c> goes false with <c>m_wasInAttack</c> set, which
/// sets <c>m_attackDone</c> and makes every later <c>Update</c> return immediately. Every Magenheim
/// staff clones <c>StaffIceShards</c>, a single-shot staff whose animation ends right after its
/// trigger, and no registrar sets <c>m_loopingAttack</c>. So a staff authored for three or six
/// deferred bursts has no window in which to fire them.
///
/// Stamina still drains because <c>Attack.Update</c> charges it up front whenever
/// <c>m_perBurstResourceUsage</c> is false, which every staff sets. That is why the symptom reads as
/// "costs stamina, does nothing" rather than as a refused attack.
///
/// The correction keeps each family's authored identity — its bolt count, damage, damage type,
/// spread, velocity and payload are all untouched — and only moves the volley onto the code path
/// that works, by folding the burst count into the projectile count. A staff authored as three bolts
/// across three waves now releases its nine bolts as one volley.
///
/// What this deliberately does not do is restore the stagger. Doing that needs
/// <c>m_loopingAttack</c> plus an attack animation that holds open while the sequence plays, and a
/// looping attack that never self-terminates would leave the player stuck mid-swing. That is a
/// separate change with its own risk, and it should not ride along with a repair.
/// </remarks>
internal static class StaffBurstContract
{
    internal static void Apply(Attack attack, string staffIdentity, ManualLogSource? log = null)
    {
        if (attack is null) throw new ArgumentNullException(nameof(attack));
        if (attack.m_projectileBursts <= 1) return;

        var bursts = attack.m_projectileBursts;
        var perBurst = Math.Max(1, attack.m_projectiles);
        var total = checked(perBurst * bursts);

        attack.m_projectiles = total;
        attack.m_projectileBursts = 1;
        attack.m_burstInterval = 0f;

        log?.LogInfo(
            $"Staff '{staffIdentity}' volley reshaped from {perBurst}x{bursts} deferred bursts to " +
            $"{total} bolts in one release: Valheim only fires a multi-burst projectile attack while " +
            "the attack is still live, and the donor's single-shot animation ends first.");
    }
}
