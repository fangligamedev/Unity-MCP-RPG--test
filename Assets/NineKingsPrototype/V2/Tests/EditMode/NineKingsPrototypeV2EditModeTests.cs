#nullable enable
using System.Linq;
using NUnit.Framework;

namespace NineKingsPrototype.V2.Tests.EditMode
{
    public sealed class NineKingsPrototypeV2EditModeTests
    {
        [Test]
        public void ContentDatabase_BuildsDefaultKingsCardsAndValidates()
        {
            var database = NineKingsV2SampleContentFactory.CreateInMemoryDatabase();
            var errors = database.Validate();

            Assert.That(errors, Is.Empty, string.Join("\n", errors));
            Assert.That(database.kings.Select(king => king.kingId), Does.Contain("king_greed"));
            Assert.That(database.kings.Select(king => king.kingId), Does.Contain("king_nothing"));
            Assert.That(database.cards.Count(card => card.ownerKingId == "king_greed"), Is.EqualTo(9));
            Assert.That(database.cards.Count(card => card.ownerKingId == "king_nothing"), Is.EqualTo(9));
        }

        [Test]
        public void RunState_CreatesThreeByThreeBoardAndFourCards()
        {
            var database = NineKingsV2SampleContentFactory.CreateInMemoryDatabase();
            var run = RunState.CreateNew(database, "king_greed");

            Assert.That(run.GetUnlockedPlots().Count, Is.EqualTo(9));
            Assert.That(run.handCardIds.Count, Is.EqualTo(4));
            Assert.That(run.lives, Is.EqualTo(3));
            Assert.That(run.year, Is.EqualTo(1));
        }

        [Test]
        public void PlacementValidator_DistinguishesPlaceUpgradeAndEnchant()
        {
            var database = NineKingsV2SampleContentFactory.CreateInMemoryDatabase();
            var run = RunState.CreateNew(database, "king_nothing");
            var target = new BoardCoord(2, 2);

            var placeResult = PlacementValidator.ValidatePlotPlacement(database, run, "nothing_soldier", target);
            Assert.That(placeResult.IsValid, Is.True);
            Assert.That(PlacementValidator.TryApply(database, run, "nothing_soldier", target), Is.True);

            run.handCardIds.Add("nothing_soldier");
            var upgradeResult = PlacementValidator.ValidatePlotPlacement(database, run, "nothing_soldier", target);
            Assert.That(upgradeResult.IsUpgrade, Is.True);

            run.handCardIds.Add("nothing_steel_coat");
            var enchantResult = PlacementValidator.ValidatePlotPlacement(database, run, "nothing_steel_coat", target);
            Assert.That(enchantResult.IsEnchantment, Is.True);
        }

        [Test]
        public void RunState_And_BattleSceneState_SupportJsonRoundTrip()
        {
            var database = NineKingsV2SampleContentFactory.CreateInMemoryDatabase();
            var run = RunState.CreateNew(database, "king_greed");
            var simulation = new CombatSimulation(database);
            var battle = simulation.CreateBattleScene(run);

            var json = JsonSnapshotUtility.ToJson(new SaveGameState
            {
                runState = run,
                battleSceneState = battle,
            });
            var restored = JsonSnapshotUtility.FromJson<SaveGameState>(json);

            Assert.That(restored.runState.playerKingId, Is.EqualTo("king_greed"));
            Assert.That(restored.battleSceneState.entities.Count, Is.GreaterThan(0));
        }
    }
}
