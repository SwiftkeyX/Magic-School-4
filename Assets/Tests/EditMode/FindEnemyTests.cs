using NUnit.Framework;
using UnityEngine;
using MagicSchool.Contracts;
using MagicSchool.Combat.Heroes;
using MagicSchool.Combat.Placements;

namespace MagicSchool.Combat.Tests
{
    // FLAGGING: I don't understand any of this. It was library gap? I can't read this.
    // BUT I should though, I'll come back later.
    /// <summary>
    /// Every question ITargeter can be asked, on a board built in code.
    ///
    /// Read the coordinates as (column, row) on the fixture's grid. Each test says what the shape
    /// is, because a hex board's distances don't read off the indices: a same-column neighbour is
    /// 1.0 away and a diagonal one 1.118.
    /// </summary>
    internal class FindEnemyTests
    {
        private BoardFixture _board;

        [SetUp]
        public void SetUp() => _board = new BoardFixture();

        [TearDown]
        public void TearDown() => _board.Dispose();

        private Hex Blue(int column, int row) => _board.HexAt(TeamEnum.Blue, column, row);
        private Hex Red(int column, int row) => _board.HexAt(TeamEnum.Red, column, row);

        // ==================================== FindNearestEnemy ====================================
        [Test]
        public void NearestEnemy_is_the_closer_of_two()
        {
            Hero me = _board.AddHero(TeamEnum.Blue, Blue(0, 3));
            Hero near = _board.AddHero(TeamEnum.Red, Blue(1, 3));      // 1.118 away
            _board.AddHero(TeamEnum.Red, Blue(3, 3));                  // 3.04 away

            Assert.That(me.FindNearestEnemy(), Is.EqualTo(near));
        }

        [Test]
        public void NearestEnemy_ignores_allies_however_close_they_stand()
        {
            Hero me = _board.AddHero(TeamEnum.Blue, Blue(0, 3));
            _board.AddHero(TeamEnum.Blue, Blue(1, 3));                 // ally, nearer
            Hero enemy = _board.AddHero(TeamEnum.Red, Blue(2, 3));     // enemy, further

            Assert.That(me.FindNearestEnemy(), Is.EqualTo(enemy));
        }

        [Test]
        public void NearestEnemy_ignores_an_enemy_that_is_not_on_the_board()
        {
            Hero me = _board.AddHero(TeamEnum.Blue, Blue(0, 3));
            _board.AddBenchedHero(TeamEnum.Red);                       // tracked, but standing nowhere
            Hero onBoard = _board.AddHero(TeamEnum.Red, Blue(3, 3));

            Assert.That(me.FindNearestEnemy(), Is.EqualTo(onBoard));
        }

        [Test]
        public void NearestEnemy_is_null_when_every_enemy_is_off_the_board()
        {
            Hero me = _board.AddHero(TeamEnum.Blue, Blue(0, 3));
            _board.AddBenchedHero(TeamEnum.Red);

            Assert.That(me.FindNearestEnemy(), Is.Null);
        }

        // =================================== FindFurthestEnemy ===================================
        [Test]
        public void FurthestEnemy_is_the_further_of_two()
        {
            Hero me = _board.AddHero(TeamEnum.Blue, Blue(0, 3));
            _board.AddHero(TeamEnum.Red, Blue(1, 3));
            Hero far = _board.AddHero(TeamEnum.Red, Blue(3, 3));

            Assert.That(me.FindFurthestEnemy(int.MaxValue), Is.EqualTo(far));
        }

        [Test]
        public void FurthestEnemy_within_reachRange_skips_one_that_is_further_but_out_of_reach()
        {
            Hero me = _board.AddHero(TeamEnum.Blue, Blue(0, 3));
            Hero near = _board.AddHero(TeamEnum.Red, Blue(1, 3));     // 1.118 away - in reach
            _board.AddHero(TeamEnum.Red, Blue(3, 3));                 // 3.04 away - out of reach

            Assert.That(me.FindFurthestEnemy(reachRange: 2), Is.EqualTo(near));
        }

        [Test]
        public void FurthestEnemy_is_null_when_nobody_is_within_reachRange()
        {
            Hero me = _board.AddHero(TeamEnum.Blue, Blue(0, 3));
            _board.AddHero(TeamEnum.Red, Blue(3, 3));                 // 3.04 away - out of reach

            Assert.That(me.FindFurthestEnemy(reachRange: 2), Is.Null);
        }

        // =================================== FindCurrentTarget ===================================
        [Test]
        public void CurrentTarget_starts_as_the_nearest_enemy()
        {
            Hero me = _board.AddHero(TeamEnum.Blue, Blue(0, 3));
            Hero near = _board.AddHero(TeamEnum.Red, Blue(1, 3));
            _board.AddHero(TeamEnum.Red, Blue(3, 3));

            Assert.That(me.FindCurrentTarget(), Is.EqualTo(near));
        }

        [Test]
        public void CurrentTarget_sticks_to_who_it_is_engaged_with_when_someone_closer_walks_up()
        {
            Hero me = _board.AddHero(TeamEnum.Blue, Blue(0, 3), range: 3);
            Hero engaged = _board.AddHero(TeamEnum.Red, Blue(2, 3));
            Assert.That(me.FindCurrentTarget(), Is.EqualTo(engaged), "should have engaged the only enemy");

            _board.AddHero(TeamEnum.Red, Blue(1, 3));                  // closer, but we are busy

            Assert.That(me.FindCurrentTarget(), Is.EqualTo(engaged));
        }

        [Test]
        public void CurrentTarget_is_dropped_once_it_walks_out_of_range()
        {
            Hero me = _board.AddHero(TeamEnum.Blue, Blue(0, 3), range: 1);
            Hero engaged = _board.AddHero(TeamEnum.Red, Blue(1, 3));   // adjacent: in range
            Hero other = _board.AddHero(TeamEnum.Red, Blue(2, 3));
            Assert.That(me.FindCurrentTarget(), Is.EqualTo(engaged), "should have engaged the adjacent enemy");

            _board.MoveHero(engaged, Red(3, 6));                       // clean off the other side

            Assert.That(me.FindCurrentTarget(), Is.EqualTo(other));
        }

        // =================================== FindClusteredLaser ===================================
        [Test]
        public void ClusteredLaser_aims_down_the_line_that_passes_through_the_most_enemies()
        {
            Hero me = _board.AddHero(TeamEnum.Blue, Blue(0, 3));

            // three enemies on one straight line out of my hex, one enemy off on its own
            Hero onTheLine = _board.AddHero(TeamEnum.Red, Blue(1, 3));
            _board.AddHero(TeamEnum.Red, Blue(2, 2));
            _board.AddHero(TeamEnum.Red, Blue(3, 2));
            _board.AddHero(TeamEnum.Red, Blue(0, 0));                  // straight up, alone

            // the three on the line tie at two others caught, so the tie-break takes the nearest
            Assert.That(me.FindClusteredLaser(reachRange: 20, beamHalfWidth: 0.3f), Is.EqualTo(onTheLine));
        }

        // ================================== FindEnemiesNear ==================================
        [Test]
        public void EnemiesNear_is_centred_on_the_target_not_the_caster()
        {
            Hero me = _board.AddHero(TeamEnum.Blue, Blue(0, 0));               // far from everything below
            Hero target = _board.AddHero(TeamEnum.Red, Blue(3, 3));
            Hero nearTarget = _board.AddHero(TeamEnum.Red, Blue(3, 2));        // 1.0 from target, out of me's reach

            var found = me.FindEnemiesNear(target, reachRange: 1);

            Assert.That(found, Is.EquivalentTo(new[] { target, nearTarget }));
        }

        [Test]
        public void EnemiesNear_excludes_allies_of_the_caster_even_next_to_the_target()
        {
            Hero me = _board.AddHero(TeamEnum.Blue, Blue(0, 3));
            Hero target = _board.AddHero(TeamEnum.Red, Blue(2, 3));
            _board.AddHero(TeamEnum.Blue, Blue(1, 3));                        // my ally, 1.118 from target

            var found = me.FindEnemiesNear(target, reachRange: 2);

            Assert.That(found, Is.EqualTo(new[] { target }));
        }

        [Test]
        public void EnemiesNear_excludes_enemies_outside_reachRange()
        {
            Hero me = _board.AddHero(TeamEnum.Blue, Blue(0, 0));
            Hero target = _board.AddHero(TeamEnum.Red, Blue(0, 3));
            _board.AddHero(TeamEnum.Red, Blue(3, 3));                         // 3 hexes from the target

            var found = me.FindEnemiesNear(target, reachRange: 1);

            Assert.That(found, Is.EqualTo(new[] { target }));
        }

        [Test]
        public void EnemiesNear_is_empty_when_the_target_is_off_the_board()
        {
            Hero me = _board.AddHero(TeamEnum.Blue, Blue(0, 3));
            Hero benchedTarget = _board.AddBenchedHero(TeamEnum.Red);

            var found = me.FindEnemiesNear(benchedTarget, reachRange: 5);

            Assert.That(found, Is.Empty);
        }

        // ================================== FindClusteredLanding ==================================
        [Test]
        public void ClusteredLanding_moves_to_the_hex_whose_blast_catches_the_pair()
        {
            Hero me = _board.AddHero(TeamEnum.Blue, Blue(1, 3));
            _board.AddHero(TeamEnum.Red, Blue(3, 2));
            _board.AddHero(TeamEnum.Red, Blue(3, 3));                  // the two stand 1.0 apart

            // Blue(2,2) sits 1.118 from both - the reachable hex that covers the pair, and it is
            // empty, which is the answer a list of enemies could never have given.
            IPlacement landing = me.FindClusteredCircle(reachRange: 2, blastRadius: 1.2f, isJump: true);

            Assert.That(landing, Is.EqualTo(Blue(2, 2)));
        }

        [Test]
        public void ClusteredLanding_is_null_when_nothing_in_reach_catches_anyone()
        {
            Hero me = _board.AddHero(TeamEnum.Blue, Blue(0, 3));
            _board.AddHero(TeamEnum.Red, Blue(3, 2));
            _board.AddHero(TeamEnum.Red, Blue(3, 3));

            // one hop only: every hex it could reach is 2.0 or more from either enemy
            IPlacement landing = me.FindClusteredCircle(reachRange: 1, blastRadius: 1.2f, isJump: true);

            Assert.That(landing, Is.Null, "holding the cast beats jumping somewhere that hits nobody");
        }

        [Test]
        public void ClusteredLanding_reaches_the_cluster_once_the_range_is_long_enough()
        {
            Hero me = _board.AddHero(TeamEnum.Blue, Blue(0, 3));
            _board.AddHero(TeamEnum.Red, Blue(3, 2));
            _board.AddHero(TeamEnum.Red, Blue(3, 3));

            // the same board as the null case above, two hops instead of one
            IPlacement landing = me.FindClusteredCircle(reachRange: 2, blastRadius: 1.2f, isJump: true);

            Assert.That(landing, Is.EqualTo(Blue(2, 2)));
        }

        [Test]
        public void ClusteredLanding_never_lands_on_a_hex_someone_is_standing_on()
        {
            Hero me = _board.AddHero(TeamEnum.Blue, Blue(1, 3));
            Hero first = _board.AddHero(TeamEnum.Red, Blue(3, 2));
            Hero second = _board.AddHero(TeamEnum.Red, Blue(3, 3));
            Hero third = _board.AddHero(TeamEnum.Red, Blue(3, 4));     // a column of three

            IPlacement landing = me.FindClusteredCircle(reachRange: 3, blastRadius: 1.2f, isJump: true);

            Assert.That(landing, Is.Not.Null);
            Assert.That(landing, Is.Not.EqualTo(first.CurrentPlacement));
            Assert.That(landing, Is.Not.EqualTo(second.CurrentPlacement));
            Assert.That(landing, Is.Not.EqualTo(third.CurrentPlacement));
        }

        // ================================== FindClusteredCharge ==================================
        [Test]
        public void ClusteredCharge_ends_on_the_hex_whose_path_sweeps_the_most_enemies()
        {
            Hero me = _board.AddHero(TeamEnum.Blue, Blue(0, 3));

            // a row of three straight across from me, and one lone enemy off the other way
            _board.AddHero(TeamEnum.Red, Blue(1, 3));
            _board.AddHero(TeamEnum.Red, Blue(2, 3));
            _board.AddHero(TeamEnum.Red, Blue(3, 3));
            _board.AddHero(TeamEnum.Red, Blue(0, 0));

            IPlacement landing = me.FindClusteredCharge(reachRange: 4, chargeHalfWidth: 1.25f);

            // whatever hex it picks, the charge has to run through the row rather than at the loner
            Assert.That(landing, Is.Not.Null);
            Assert.That(landing.transform.position.x, Is.GreaterThan(me.transform.position.x),
                        "should have charged along the row, not away from it");
        }

        [Test]
        public void ClusteredCharge_is_null_when_no_path_in_reach_sweeps_anyone()
        {
            Hero me = _board.AddHero(TeamEnum.Blue, Blue(0, 0));
            _board.AddHero(TeamEnum.Red, Red(3, 6));                   // the far corner of the board

            IPlacement landing = me.FindClusteredCharge(reachRange: 2, chargeHalfWidth: 1.25f);

            Assert.That(landing, Is.Null, "charging at nobody is worse than holding the cast");
        }

        [Test]
        public void ClusteredCharge_does_not_score_enemies_it_could_never_reach()
        {
            Hero me = _board.AddHero(TeamEnum.Blue, Blue(0, 3));

            // a pair close enough to run through, and three more massed on the far side
            _board.AddHero(TeamEnum.Red, Blue(1, 3));
            _board.AddHero(TeamEnum.Red, Blue(1, 4));
            _board.AddHero(TeamEnum.Red, Red(1, 3));
            _board.AddHero(TeamEnum.Red, Red(2, 3));
            _board.AddHero(TeamEnum.Red, Red(3, 3));

            // Two hops of reach. Aiming down an unbounded line would point at the three, because
            // that line catches the most - and then the charge would stop three hexes short of
            // them having hit nobody. Scoring the charge itself takes the pair it can run through.
            //
            // A narrow lane on purpose: at the hitbox's real 1.25 several paths clip both of the
            // pair and the tie-break picks whichever is shortest, which is right in game and
            // useless to assert on. At 0.6 exactly one path runs through both.
            IPlacement landing = me.FindClusteredCharge(reachRange: 2, chargeHalfWidth: 0.6f);

            Assert.That(landing, Is.EqualTo(Blue(2, 3)));
        }

        [Test]
        public void ClusteredLanding_breaks_a_tie_by_taking_the_shorter_jump()
        {
            Hero me = _board.AddHero(TeamEnum.Blue, Blue(0, 3));
            Hero near = _board.AddHero(TeamEnum.Red, Blue(2, 3));
            _board.AddHero(TeamEnum.Red, Red(1, 3));                   // same prize, far side of the board

            // one enemy caught either way, so the near one wins
            IPlacement landing = me.FindClusteredCircle(reachRange: 4, blastRadius: 1.2f, isJump: true);

            Assert.That(landing, Is.Not.Null);
            float toNear = Vector3.Distance(landing.transform.position, near.transform.position);
            Assert.That(toNear, Is.LessThanOrEqualTo(1.2f), "should have jumped at the enemy it could reach sooner");
        }
    }
}
