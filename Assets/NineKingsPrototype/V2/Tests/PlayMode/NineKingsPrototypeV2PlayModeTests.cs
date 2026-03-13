#nullable enable
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace NineKingsPrototype.V2.Tests.PlayMode
{
    public sealed class NineKingsPrototypeV2PlayModeTests
    {
        [UnityTest]
        public IEnumerator Controller_CanEnterCardPhase_ThenAutoBattle_WhenTwoCardsRemain()
        {
            var database = NineKingsV2SampleContentFactory.CreateInMemoryDatabase();
            var gameObject = new GameObject("NineKingsV2TestController");
            var controller = gameObject.AddComponent<NineKingsV2GameController>();
            controller.SetDatabase(database);
            controller.StartNewRun("king_greed");
            controller.EnterCardPhase();

            Assert.That(controller.RunState!.phase, Is.EqualTo(RunPhase.CardPhase));

            var targetA = new BoardCoord(1, 1);
            var targetB = new BoardCoord(2, 1);
            Assert.That(controller.TryPlayCard(controller.HandState.cardIds[0], targetA), Is.True);
            Assert.That(controller.TryPlayCard(controller.HandState.cardIds[0], targetB), Is.True);

            yield return null;

            Assert.That(controller.RunState!.phase == RunPhase.BattleDeploy || controller.RunState.phase == RunPhase.BattleRun, Is.True);
            Assert.That(controller.BattleSceneState.entities.Count, Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator Battle_CanResolve_ToLootChoice()
        {
            var database = NineKingsV2SampleContentFactory.CreateInMemoryDatabase();
            var gameObject = new GameObject("NineKingsV2BattleController");
            var controller = gameObject.AddComponent<NineKingsV2GameController>();
            controller.SetDatabase(database);
            controller.StartNewRun("king_nothing");
            controller.EnterCardPhase();
            controller.SetBattleSpeedMultiplier(4f);

            controller.RunState!.handCardIds.Clear();
            controller.RunState.handCardIds.AddRange(new[] { "nothing_soldier", "nothing_archer", "nothing_castle", "nothing_farm" });
            controller.HandState.cardIds.Clear();
            controller.HandState.cardIds.AddRange(controller.RunState.handCardIds);

            Assert.That(controller.TryPlayCard("nothing_soldier", new BoardCoord(1, 1)), Is.True);
            Assert.That(controller.TryPlayCard("nothing_archer", new BoardCoord(2, 1)), Is.True);

            for (var i = 0; i < 2200 && (controller.RunState!.phase == RunPhase.BattleDeploy || controller.RunState.phase == RunPhase.BattleRun); i++)
            {
                controller.TickBattle(0.1f);
                yield return null;
            }

            Assert.That(
                controller.RunState!.phase == RunPhase.LootChoice || controller.RunState.phase == RunPhase.YearStart || controller.RunState.phase == RunPhase.RunOver,
                Is.True,
                $"Battle did not resolve. Current phase: {controller.RunState!.phase}, entities: {controller.BattleSceneState.entities.Count}");
        }

        [UnityTest]
        public IEnumerator ScenePresenter_AutoStarts_Run_AndCreatesVisibleRoots()
        {
            var database = NineKingsV2SampleContentFactory.CreateInMemoryDatabase();
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.transform.position = new Vector3(0f, 0f, -10f);

            var root = new GameObject("NineKingsV2SceneRoot");
            var controller = root.AddComponent<NineKingsV2GameController>();
            controller.SetDatabase(database);
            root.AddComponent<NineKingsV2ScenePresenter>().SetController(controller);

            yield return null;

            Assert.That(controller.RunState, Is.Not.Null);
            Assert.That(controller.RunState!.phase, Is.EqualTo(RunPhase.CardPhase));
            Assert.That(root.transform.Find("WorldBackground"), Is.Not.Null);
            Assert.That(root.transform.Find("BoardCells"), Is.Not.Null);
            Assert.That(root.transform.Find("PlacedStructures"), Is.Not.Null);
        }
    }
}
