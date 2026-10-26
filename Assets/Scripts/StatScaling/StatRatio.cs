using MagicSchool.Contracts;

namespace MagicSchool.StatScaling
{
    // the thing that Scaling read from, to know:
    // 1) which stat to derive from.
    // 2) which part of that stat will be used for deriving? read ScaleFromEnum.cs
    // 3) how much the amount/percentage it scale.
    //
    // e.g. Ranger's skill = (StatEnum.ATK, 744f) damage
    // which mean [total damage] = [744%] of the [atk].
    // With no stat set, the amount is flat: 200f is plain 200, not 200%.
    public readonly struct StatRatio
    {
        public readonly StatEnum? Stat;

        public readonly float Amount;

        public readonly ScaleFromEnum? ScaleFrom;

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

        public static implicit operator StatRatio((StatEnum stat, float percent) pair)
        {
            return new StatRatio(pair.stat, pair.percent);
        }

        public static implicit operator StatRatio((StatEnum stat, float percent, ScaleFromEnum scaleFrom) triple)
        {
            return new StatRatio(triple.stat, triple.percent, triple.scaleFrom);
        }

        public static implicit operator StatRatio(float amount)
        {
            return new StatRatio(amount);
        }
    }
}
