namespace MagicSchool.Skills
{
    /// <summary>
    /// Which template action a skill wants to play.
    /// 
    /// This is enum used to reference matched prefab. 
    /// E.g. projectile prefab
    /// </summary>
    public enum TemplateActionEnum
    {
        None = 0,

        // ==================================== AOE ====================================
        BoxAOE = 1,
        CircleAOE = 2,
        CircleAOESticky = 15,
        HalfCircleAOESticky = 17,
        TriangleAOE = 4,
        ZoneAOE = 5,

        // ==================================== Projectile ====================================
        FirstHitProjectile = 7,
        HomingProjectile = 8,
        PiercingProjectile = 9,

        // ==================================== FireTimingRunner ====================================
        FireTimingRunnerHomingProjectile = 18,
        FireTimingRunnerFirstHitProjectile = 21,
        FireTimingRunnerTriangleAOE = 19,
        FireTimingRunnerCast = 20,

        // ==================================== Other ====================================
        Cast = 10,
        Move = 13,
    }
}
