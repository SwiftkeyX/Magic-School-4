namespace MagicSchool.Contracts
{
    // Which part of a stat a StatRatio derived of.
    //
    // e.g. a hero have base 100 AP, holding items worth another 60:
    //   Total = 160    Base = 100    Bonus = 60
    //
    // The three real cases, and why one enum is not enough without them:
    //   Total  - Deathcap, "+35% of total AP". Also what a skill's damage means: Quatre's 744% AD
    //            is a share of the attack he actually swings with, items included.
    //   Base   - GuildRun's item, "+33% of base AD". Derived from the base stat, so the
    //            buff is worth the same no matter how much item, buff it have.
    //   Bonus  - Aatrox's skill, "AD equal to 80% of BONUS attack speed". Derived only from the bonus, 
    //            so a hero with no items gains nothing from it.
    public enum ScaleFromEnum
    {
        Total = 0,
        Base = 1,
        Bonus = 2,
    }
}
