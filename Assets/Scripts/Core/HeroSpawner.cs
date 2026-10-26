using UnityEngine;
using MagicSchool.Engine;
using MagicSchool.Contracts;
using MagicSchool.Combat.Heroes;
using MagicSchool.Combat.Placements;
using MagicSchool.CombatRecording;
using MagicSchool.Skills;
using MagicSchool.Items;

namespace MagicSchool.Core
{
    // A unit a hero asks for mid-fight is a summon, at its summoner's level: Troll never grows from a Small Slime's
    // death, and a level 10 Big Slime splits into level 10 Small Slimes. A hero held out of the shop before it is
    // bought stands nowhere, is not tracked, and nobody is told it exists. A forged item's asset comes from the
    // forger's forge table, so neither the skill nor the hero names an item.
    public class HeroSpawner
    {
        private readonly HeroMover _heroMover;
        private readonly TemplateActionRegistrySO _templateActions;
        private readonly CombatRecorder _recorder;

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

        private void SpawnHero(HeroDataSO data, TeamEnum team, IPlacement placement, BattleBoard board)
            => Place(Build(data, board, team), placement);

        private void SpawnSummon(HeroDataSO data, TeamEnum team, IPlacement placement, BattleBoard board, int level)
        {
            Hero summon = Build(data, board, team);
            summon.MarkAsSummon();
            summon.SetLevel(level);
            Place(summon, placement);
        }

        private void Place(Hero hero, IPlacement placement)
        {
            _heroMover.MoveThisHeroTo(hero, placement);

            OnSpawned?.Invoke(hero);
        }

        public Hero SpawnUnboughtHero(HeroDataSO data, BattleBoard board) => Build(data, board, TeamEnum.Blue);

        private static void Forge(Hero wearer, HeroRoleEnum forRole, int tier, ICombatant forger)
        {
            if (!(forger is Hero smith) || !(smith.Data.ForgedItem(forRole, tier) is ItemDataSO data))
            {
                Debug.LogError($"[HeroSpawner] {(forger as Hero)?.name} has nothing to forge for a {forRole} - set it in its HeroDataSO's Forge Table.");
                return;
            }

            Item item = Item.Spawn(data, wearer.transform.position);
            if (item != null) wearer.WearForged(item);
        }

        private Hero Build(HeroDataSO data, BattleBoard board, TeamEnum team)
        {
            GameObject heroPrefab = SceneHelper.Instantiate(data.Prefab);
            Hero hero = heroPrefab.GetComponent<Hero>();
            hero.Init(data, board, team, SkillLibrary.Resolve(data.SkillId, _templateActions));

            if (_recorder != null)
            {
                hero.OnDamaged += _recorder.Record;
                hero.OnHealed += _recorder.Record;
            }

            hero.OnSpawnRequested += (unit, unitTeam, placement, onBoard) => SpawnSummon(unit, unitTeam, placement, onBoard, hero.Level);

            hero.OnForgedItemRequested += Forge;

            return hero;
        }
    }
}