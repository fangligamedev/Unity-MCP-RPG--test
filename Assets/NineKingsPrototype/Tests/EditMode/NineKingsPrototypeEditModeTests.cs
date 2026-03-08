#nullable enable
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace NineKingsPrototype.Tests.EditMode
{
    public sealed class NineKingsPrototypeEditModeTests
    {
        private NineKingsContentDatabase LoadDatabase()
        {
            var database = Resources.Load<NineKingsContentDatabase>("NineKingsContentDatabase");
            Assert.That(database, Is.Not.Null, "NineKingsContentDatabase 资源缺失。");
            return database!;
        }

        [Test]
        public void ContentDatabase_HasExpectedKingsAndCards()
        {
            var database = LoadDatabase();

            Assert.That(database.playerKing.kingId, Is.EqualTo("king_nothing"));
            Assert.That(database.playerKing.cardIds.Count, Is.EqualTo(9));
            Assert.That(database.opponentKings.Count, Is.EqualTo(2));
            Assert.That(database.cards.Count, Is.GreaterThanOrEqualTo(15));
        }

        [Test]
        public void YearSchedule_MatchesFullAlphaCalendar()
        {
            var database = LoadDatabase();
            var years = database.yearEvents.Select(item => item.year).OrderBy(year => year).ToArray();
            var expected = new[] { 4, 6, 8, 10, 12, 14, 16, 19, 21, 23, 25, 27, 29, 31, 33 };
            Assert.That(years, Is.EqualTo(expected));
        }

        [Test]
        public void NewRun_InitializesBoardAndHand()
        {
            var database = LoadDatabase();
            var state = NKRunState.CreateNew(database);

            Assert.That(state.Year, Is.EqualTo(1));
            Assert.That(state.Lives, Is.EqualTo(3));
            Assert.That(state.HandCardIds.Count, Is.EqualTo(4));
            Assert.That(state.GetUnlockedPlots().Count(), Is.EqualTo(9));
            Assert.That(state.CurrentEnemyKingId, Is.Not.Empty);
        }
    }
}
