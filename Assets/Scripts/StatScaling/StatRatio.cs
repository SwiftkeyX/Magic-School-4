using MagicSchool.Contracts;

namespace MagicSchool.StatScaling
{
    // the thing that Scaling read from, to know:
    // 1) which stat to derive from.
    // 2) which part of that stat will be used for deriving? read ScaleFromEnum.cs
    // 3) how much the amount/percentage it scale.
    //
    // e.g. Quatre's skill = (StatEnum.ATK, 744f) damage
    // which mean [total damage] = [744%] of the [atk].
    public readonly struct StatRatio
    {
        // which stat to derived from.
        // optional because the amount added could be a flat amount that don't derived from anything.
        public readonly StatEnum? Stat;

        // if Stat is set, 200f is "200% of that [stat]".
        // if Stat isn't set, 200f is plain 200 added directly.
        public readonly float Amount;

        // scaling have 3 type: scale from [total/base/bonus] stat 
        public readonly ScaleFromEnum? ScaleFrom;

        // is this StatRatio a flat scaling or percentage scaling?
        public bool IsFlat => !Stat.HasValue;

        public StatRatio(StatEnum stat, float amount, ScaleFromEnum scaleFrom = ScaleFromEnum.Total)
        {
            Stat = stat;
            Amount = amount;
            ScaleFrom = scaleFrom;
        }

        public StatRatio(float amount)
        {
            Stat = null;
            Amount = amount;
            ScaleFrom = ScaleFromEnum.Total;
        }

        // conversion rule for StatRatio that derived from stat.
        // not specify scaleFrom => default to ScaleFromEnum.Total
        // e.g. { (StatEnum.ATK, 100f), (StatEnum.AP, 50f) } => +100% atk & +50% ap
        public static implicit operator StatRatio((StatEnum stat, float percent) pair)
        {
            return new StatRatio(pair.stat, pair.percent);
        }

        // conversion rule for StatRatio that derived from stat, and specify what it scale from
        // e.g. (StatEnum.AS, 80f, ScaleFromEnum.Bonus) => +80% of the attack speed bonus
        public static implicit operator StatRatio((StatEnum stat, float percent, ScaleFromEnum scaleFrom) triple)
        {
            return new StatRatio(triple.stat, triple.percent, triple.scaleFrom);
        }

        // conversion rule for StatRatio that use only the flat amount.
        // e.g. { (100f), (StatEnum.AP, 50f) } => +100 flat & +50% ap
        public static implicit operator StatRatio(float amount)
        {
            return new StatRatio(amount);
        }
    }
}
