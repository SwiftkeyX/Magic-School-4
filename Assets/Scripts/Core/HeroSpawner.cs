using UnityEngine;
using MagicSchool.Engine;
using MagicSchool.Contracts;
using MagicSchool.Combat.Heroes;
using MagicSchool.Combat.Placements;
using MagicSchool.CombatRecording;
using MagicSchool.Skills;

namespace MagicSchool.Core
{
    public class HeroSpawner
    {
        private readonly HeroMover _heroMover;
        private readonly TemplateActionRegistrySO _templateActions;
        private readonly CombatRecorder _recorder;

        // a hero now exists and stands on its placement
        // e.g.     GameManager checks a bought hero for a copy to combine with
        public event System.Action<Hero> OnSpawned;

        public HeroSpawner(HeroMover heroMover, TemplateActionRegistrySO templateActions, CombatRecorder recorder,
                           Bench bench = null, HeroSeed seed = null, StageEditor stageEditor = null)
        {
            _heroMover = heroMover;
            _templateActions = templateActions;
            _recorder = recorder;

            if (bench != null) bench.OnSpawnRequested += SpawnHero;
            if (seed != null) seed.OnSpawnRequested += SpawnHero;
            if (stageEditor != null) stageEditor.OnSpawnRequested += SpawnHero;
        }

        // basically spawn hero
        private void SpawnHero(HeroDataSO data, TeamEnum team, IPlacement placement, BattleBoard board)
            => Place(Build(data, board, team), placement);

        // a unit a hero asked for mid-fight is a summon
        // e.g.     Troll does not grow from a Small Slime's death
        private void SpawnSummon(HeroDataSO data, TeamEnum team, IPlacement placement, BattleBoard board)
        {
            Hero summon = Build(data, board, team);
            summon.MarkAsSummon();
            Place(summon, placement);
        }

        private void Place(Hero hero, IPlacement placement)
        {
            // move them to "placement"
            _heroMover.MoveThisHeroTo(hero, placement);

            OnSpawned?.Invoke(hero);
        }

        // A player hero that stands nowhere yet: the one held out of the shop before it is bought.
        // Not on a placement, so not tracked on the board, and nobody is told it exists - once it is bought
        // and put down, its buyer moves it like any other hero.
        public Hero SpawnUnboughtHero(HeroDataSO data, BattleBoard board) => Build(data, board, TeamEnum.Blue);

        private Hero Build(HeroDataSO data, BattleBoard board, TeamEnum team)
        {
            // spawn hero
            GameObject heroPrefab = SceneHelper.Instantiate(data.Prefab);
            Hero hero = heroPrefab.GetComponent<Hero>();
            hero.Init(data, board, team, SkillLibrary.Resolve(data.SkillId, _templateActions));

            // initial event between combat recorder and hero
            if (_recorder != null)
            {
                hero.OnDamaged += _recorder.Record;
                hero.OnHealed += _recorder.Record;
            }

            // a hero may ask for units of its own in the middle of a fight 
            // e.g.     Big Slime splits into smaller slime
            hero.OnSpawnRequested += SpawnSummon;

            return hero;
        }
    }
}