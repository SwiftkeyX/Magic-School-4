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

            // move them to "placement"
            _heroMover.MoveThisHeroTo(hero, placement);
        }
    }
}