using System.Collections.Generic;
using System.Linq;
using MagicSchool.Contracts;
using MagicSchool.Combat.Align;
using MagicSchool.Combat.Heroes;
using MagicSchool.Combat.Placements;
using MagicSchool.Skills;

namespace MagicSchool.Core
{
    // How a fight starts and when it is over, the one copy the game (CombatState) and the Team Comp Benchmark both use,
    // so the benchmark measures the game that is played. A hero waiting to get back up (IRevivable) still counts as
    // standing, so a fight never ends while Reaper is down.
    public static class FightRules
    {
        // aligning null: fought without aligning bonuses
        public static void Begin(BattleBoard board, AlignBonuses aligning, TemplateActionRegistrySO templateActions)
        {
            board.SetBattleOn(true);

            aligning?.Refresh(board, TeamEnum.Blue);

            foreach (Hero hero in board.HeroesOnBoard.OfType<Hero>().ToList())
            {
                if (hero.IsSkillOutdated) hero.RebuildSkill(SkillLibrary.Resolve(hero.Data.SkillId, templateActions));
            }

            foreach (Hero hero in board.HeroesOnBoard.OfType<Hero>().ToList())
            {
                if (hero.IsAlive) hero.TriggerOnCombatStart();
            }
        }

        public static bool IsStanding(ICombatant unit)
            => unit != null && (unit.StateType != HeroStateEnum.Dead || (unit is IRevivable revivable && revivable.IsReviving));

        public static void Standing(BattleBoard board, out bool blue, out bool red)
        {
            List<ICombatant> standing = board.HeroesOnBoard.Where(IsStanding).ToList();
            blue = standing.Any(unit => unit.Team == TeamEnum.Blue);
            red = standing.Any(unit => unit.Team == TeamEnum.Red);
        }
    }
}
