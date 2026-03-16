#nullable enable
using System.Collections;
using System.Linq;
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
                controller.RunState!.phase == RunPhase.LootChoice
                || controller.RunState.phase == RunPhase.YearStart
                || controller.RunState.phase == RunPhase.CardPhase
                || controller.RunState.phase == RunPhase.RunOver,
                Is.True,
                $"Battle did not resolve. Current phase: {controller.RunState!.phase}, entities: {controller.BattleSceneState.entities.Count}");
        }

        [UnityTest]
        public IEnumerator ScenePresenter_AutoStarts_Run_UsesCardCameraPreset_AndCreatesVisibleRoots()
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
            var presenter = root.GetComponent<NineKingsV2ScenePresenter>() ?? root.AddComponent<NineKingsV2ScenePresenter>();
            presenter.SetController(controller);

            yield return null;
            yield return null;

            Assert.That(controller.RunState, Is.Not.Null);
            Assert.That(controller.RunState!.phase, Is.EqualTo(RunPhase.CardPhase));
            Assert.That(root.transform.Find("WorldBackground"), Is.Not.Null);
            Assert.That(root.transform.Find("BoardCells"), Is.Not.Null);
            Assert.That(root.transform.Find("PlacedStructures"), Is.Not.Null);

            var preset = presenter.GetTargetCameraPreset();
            var layout = presenter.CreateCurrentLayoutSnapshot(1920f, 1080f);
            Assert.That(preset.Size, Is.EqualTo(layout.CameraPreset.Size).Within(0.001f));
            Assert.That(preset.Size, Is.EqualTo(5.20f).Within(0.01f));
            Assert.That(preset.Position.y, Is.GreaterThan(1.2f));
            Assert.That(layout.BottomHand.Visible, Is.True);
            Assert.That(layout.Overlay.Visible, Is.False);
            Assert.That(layout.TimelineSubtitleRect.width, Is.GreaterThan(0f));
            Assert.That(layout.BoardCellsUseStrongOutline, Is.True);
            Assert.That(layout.VisibleBuildCellCount, Is.EqualTo(0));
            Assert.That(layout.HandCardRects.Zip(layout.HandCardRects.Skip(1), (left, right) => left.xMax <= right.xMin + 0.01f).All(value => value), Is.True);
            Assert.That(presenter.GetVisibleBoardCellSet().Count, Is.EqualTo(0));
            var gridLines = presenter.GetActiveBoardGridLineCounts();
            Assert.That(gridLines.MainLines, Is.EqualTo(0));
            Assert.That(gridLines.AccentLines, Is.EqualTo(0));
            var activeCells = root.transform.Find("BoardCells")!.Cast<Transform>().Count(cell => cell.gameObject.activeSelf);
            Assert.That(activeCells, Is.EqualTo(0));

            presenter.SetHoveredHandIndexForTests(0);
            yield return null;

            layout = presenter.CreateCurrentLayoutSnapshot(1920f, 1080f);
            Assert.That(layout.VisibleBuildCellCount, Is.EqualTo(9));
            Assert.That(presenter.GetVisibleBoardCellSet().Count, Is.EqualTo(9));
            gridLines = presenter.GetActiveBoardGridLineCounts();
            Assert.That(gridLines.MainLines, Is.EqualTo(24));
            Assert.That(gridLines.AccentLines, Is.EqualTo(24));
            activeCells = root.transform.Find("BoardCells")!.Cast<Transform>().Count(cell => cell.gameObject.activeSelf);
            Assert.That(activeCells, Is.EqualTo(9));
        }

        [UnityTest]
        public IEnumerator ScenePresenter_CardPhase_ShowsTroopSourceAsUnitSprite_OnBoard()
        {
            var database = NineKingsV2SampleContentFactory.CreateInMemoryDatabase();
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.transform.position = new Vector3(0f, 0f, -10f);

            var root = new GameObject("NineKingsV2TroopBoardRoot");
            var controller = root.AddComponent<NineKingsV2GameController>();
            controller.SetDatabase(database);
            var presenter = root.GetComponent<NineKingsV2ScenePresenter>() ?? root.AddComponent<NineKingsV2ScenePresenter>();
            presenter.SetController(controller);

            yield return null;

            controller.StartNewRun("king_nothing");
            controller.EnterCardPhase();
            controller.RunState!.handCardIds.Clear();
            controller.RunState.handCardIds.AddRange(new[] { "nothing_soldier", "nothing_archer", "nothing_castle", "nothing_farm" });
            controller.HandState.cardIds.Clear();
            controller.HandState.cardIds.AddRange(controller.RunState.handCardIds);
            Assert.That(controller.TryPlayCard("nothing_soldier", new BoardCoord(1, 1)), Is.True);

            yield return null;

            var unitRoot = root.transform.Find("PlacedStructures/Structure_1_1");
            Assert.That(unitRoot, Is.Not.Null);
            Assert.That(presenter.GetDisplayedStructureMemberCount(new BoardCoord(1, 1)), Is.EqualTo(3));
            Assert.That(presenter.GetStructureCountLabelText(new BoardCoord(1, 1)), Is.EqualTo(string.Empty));
        }

        [UnityTest]
        public IEnumerator ScenePresenter_HoverPlacedPlot_ShowsRuntimeTooltip()
        {
            var database = NineKingsV2SampleContentFactory.CreateInMemoryDatabase();
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.transform.position = new Vector3(0f, 0f, -10f);

            var root = new GameObject("NineKingsV2TooltipRoot");
            var controller = root.AddComponent<NineKingsV2GameController>();
            controller.SetDatabase(database);
            var presenter = root.GetComponent<NineKingsV2ScenePresenter>() ?? root.AddComponent<NineKingsV2ScenePresenter>();
            presenter.SetController(controller);

            yield return null;

            Assert.That(controller.TryPlayCard("nothing_soldier", new BoardCoord(1, 1)), Is.True);
            presenter.SetHoveredPlotForTests(new BoardCoord(1, 1));
            yield return null;

            var tooltip = presenter.GetHoveredPlotTooltipSnapshot();
            Assert.That(tooltip, Is.Not.Null);
            Assert.That(tooltip!.title, Is.EqualTo("士兵"));
            Assert.That(tooltip.statLines.Any(line => line.Contains("单位数", System.StringComparison.Ordinal)), Is.True);
        }

        [UnityTest]
        public IEnumerator ScenePresenter_CardPhase_PreviewHighlight_MatchesSelectedCell()
        {
            var database = NineKingsV2SampleContentFactory.CreateInMemoryDatabase();
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.transform.position = new Vector3(0f, 0f, -10f);

            var root = new GameObject("NineKingsV2PreviewRoot");
            var controller = root.AddComponent<NineKingsV2GameController>();
            controller.SetDatabase(database);
            var presenter = root.GetComponent<NineKingsV2ScenePresenter>() ?? root.AddComponent<NineKingsV2ScenePresenter>();
            presenter.SetController(controller);

            yield return null;

            var layout = presenter.CreateCurrentLayoutSnapshot(Screen.width, Screen.height);
            var targetCoord = new BoardCoord(2, 2);
            var pointer = presenter.ProjectBoardCoordPointerForTests(targetCoord);
            presenter.BeginCardDragForTests(controller.HandState.cardIds[0], layout.HandCardRects[0].center);
            presenter.UpdateCardDragForTests(pointer.Screen, pointer.Design);
            yield return null;

            Assert.That(controller.PlacementPreviewState.targetCoord, Is.EqualTo(targetCoord));
            var previewCell = root.transform.Find("BoardCells/Cell_2_2");
            Assert.That(previewCell, Is.Not.Null);
            var activeHighlightEdges = previewCell!.Cast<Transform>().Count(child => child.name.StartsWith("Edge_", System.StringComparison.Ordinal) && child.gameObject.activeSelf);
            Assert.That(activeHighlightEdges, Is.EqualTo(4));
        }

        [UnityTest]
        public IEnumerator GameController_LootChoice_SelectedRewardAppearsInNextYearHand()
        {
            var database = NineKingsV2SampleContentFactory.CreateInMemoryDatabase();
            var gameObject = new GameObject("NineKingsV2LootRewardController");
            var controller = gameObject.AddComponent<NineKingsV2GameController>();
            controller.SetDatabase(database);
            controller.StartNewRun("king_nothing");
            controller.RunState!.handCardIds.Clear();
            controller.RunState.handCardIds.AddRange(new[] { "nothing_castle", "nothing_soldier" });
            controller.RunState.deckCardIds.Clear();
            controller.RunState.deckCardIds.AddRange(new[] { "nothing_archer", "nothing_blacksmith" });
            controller.HandState.cardIds.Clear();
            controller.HandState.cardIds.AddRange(controller.RunState.handCardIds);
            controller.RunState.phase = RunPhase.LootChoice;

            controller.ResolveLootChoice("nothing_farm");
            yield return null;

            Assert.That(controller.RunState.phase, Is.EqualTo(RunPhase.CardPhase));
            Assert.That(controller.RunState.handCardIds, Does.Contain("nothing_farm"));
            Assert.That(controller.HandState.cardIds, Does.Contain("nothing_farm"));
        }

        [UnityTest]
        public IEnumerator GameController_FarmAdjacency_GrowsTroops_AndMapDisplayStaysInSync()
        {
            var database = NineKingsV2SampleContentFactory.CreateInMemoryDatabase();
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.transform.position = new Vector3(0f, 0f, -10f);

            var root = new GameObject("NineKingsV2FarmGrowthRoot");
            var controller = root.AddComponent<NineKingsV2GameController>();
            controller.SetDatabase(database);
            var presenter = root.GetComponent<NineKingsV2ScenePresenter>() ?? root.AddComponent<NineKingsV2ScenePresenter>();
            presenter.SetController(controller);

            yield return null;

            controller.RunState!.handCardIds.Clear();
            controller.RunState.handCardIds.AddRange(new[] { "nothing_farm", "nothing_paladin", "nothing_castle", "nothing_archer" });
            controller.HandState.cardIds.Clear();
            controller.HandState.cardIds.AddRange(controller.RunState.handCardIds);
            Assert.That(controller.TryPlayCard("nothing_farm", new BoardCoord(2, 2)), Is.True);
            Assert.That(controller.TryPlayCard("nothing_paladin", new BoardCoord(2, 1)), Is.True);

            yield return null;

            controller.AdvanceYear();
            yield return null;
            yield return null;

            var paladinPlot = controller.RunState.GetPlot(new BoardCoord(2, 1));
            var stats = controller.ResolvePlotRuntimeStats(paladinPlot);
            Assert.That(paladinPlot.bonusUnitCount, Is.EqualTo(1));
            Assert.That(stats.EffectiveUnitCount, Is.EqualTo(2));
            Assert.That(presenter.GetDisplayedStructureMemberCount(new BoardCoord(2, 1)), Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator ScenePresenter_CanDragCardToBoard_AndToWell()
        {
            var database = NineKingsV2SampleContentFactory.CreateInMemoryDatabase();
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.transform.position = new Vector3(0f, 0f, -10f);

            var root = new GameObject("NineKingsV2DragRoot");
            var controller = root.AddComponent<NineKingsV2GameController>();
            controller.SetDatabase(database);
            var presenter = root.GetComponent<NineKingsV2ScenePresenter>() ?? root.AddComponent<NineKingsV2ScenePresenter>();
            presenter.SetController(controller);

            yield return null;

            var layout = presenter.CreateCurrentLayoutSnapshot(Screen.width, Screen.height);
            var frame = layout.Frame;
            var firstCardId = controller.HandState.cardIds[0];
            presenter.BeginCardDragForTests(firstCardId, layout.HandCardRects[0].center);
            Assert.That(presenter.IsCardDragActive, Is.True);

            var targetCoord = new BoardCoord(2, 2);
            var pointer = presenter.ProjectBoardCoordPointerForTests(targetCoord);
            presenter.UpdateCardDragForTests(pointer.Screen, pointer.Design);
            yield return null;

            Assert.That(controller.PlacementPreviewState.targetCoord, Is.EqualTo(targetCoord));
            Assert.That(controller.PlacementPreviewState.isValid, Is.True);

            var played = presenter.ReleaseCardDragForTests(pointer.Screen, pointer.Design);
            yield return null;

            Assert.That(played, Is.True);
            Assert.That(controller.RunState.GetPlot(targetCoord).cardId, Is.EqualTo(firstCardId));
            Assert.That(presenter.IsCardDragActive, Is.False);

            var secondCardId = controller.HandState.cardIds[0];
            presenter.BeginCardDragForTests(secondCardId, layout.HandCardRects[0].center);
            var wellDesign = layout.WellDropRect.center;
            presenter.UpdateCardDragForTests(Vector2.zero, wellDesign);
            var discarded = presenter.ReleaseCardDragForTests(Vector2.zero, wellDesign);
            yield return null;

            Assert.That(discarded, Is.True);
            Assert.That(controller.RunState.discardCardIds, Does.Contain(secondCardId));
        }

        [UnityTest]
        public IEnumerator ScenePresenter_UpgradedBuilding_UsesLevelColor()
        {
            var database = NineKingsV2SampleContentFactory.CreateInMemoryDatabase();
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.transform.position = new Vector3(0f, 0f, -10f);

            var root = new GameObject("NineKingsV2StructureLevelRoot");
            var controller = root.AddComponent<NineKingsV2GameController>();
            controller.SetDatabase(database);
            var presenter = root.GetComponent<NineKingsV2ScenePresenter>() ?? root.AddComponent<NineKingsV2ScenePresenter>();
            presenter.SetController(controller);

            yield return null;

            controller.RunState!.handCardIds.Clear();
            controller.RunState.handCardIds.AddRange(new[] { "nothing_farm", "nothing_castle", "nothing_archer", "nothing_soldier" });
            controller.HandState.cardIds.Clear();
            controller.HandState.cardIds.AddRange(controller.RunState.handCardIds);
            Assert.That(controller.TryPlayCard("nothing_farm", new BoardCoord(1, 1)), Is.True);
            controller.RunState.GetPlot(new BoardCoord(1, 1)).level = 3;

            yield return null;

            Assert.That(presenter.GetStructureBodyColor(new BoardCoord(1, 1)), Is.EqualTo(NineKingsV2ScenePresenter.ResolveStructureLevelColor(3)));
        }

        [UnityTest]
        public IEnumerator ScenePresenter_SwitchesToBattleCameraPreset_WhenBattleBegins()
        {
            var database = NineKingsV2SampleContentFactory.CreateInMemoryDatabase();
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.transform.position = new Vector3(0f, 0f, -10f);

            var root = new GameObject("NineKingsV2BattleSceneRoot");
            var controller = root.AddComponent<NineKingsV2GameController>();
            controller.SetDatabase(database);
            var presenter = root.GetComponent<NineKingsV2ScenePresenter>() ?? root.AddComponent<NineKingsV2ScenePresenter>();
            presenter.SetController(controller);

            yield return null;

            controller.RunState!.handCardIds.Clear();
            controller.RunState.handCardIds.AddRange(new[] { "nothing_soldier", "nothing_archer", "nothing_castle", "nothing_farm" });
            controller.HandState.cardIds.Clear();
            controller.HandState.cardIds.AddRange(controller.RunState.handCardIds);

            var cardPreset = presenter.GetTargetCameraPreset();
            var initialCameraSize = presenter.GetCurrentCameraSizeForTests();
            Assert.That(controller.TryPlayCard(controller.HandState.cardIds[0], new BoardCoord(1, 1)), Is.True);
            Assert.That(controller.TryPlayCard(controller.HandState.cardIds[0], new BoardCoord(2, 1)), Is.True);

            yield return null;

            Assert.That(controller.RunState!.phase, Is.EqualTo(RunPhase.BattleDeploy));
            Assert.That(controller.BattleDeployTimeRemaining, Is.GreaterThan(2f));
            var battlePreset = presenter.GetTargetCameraPreset();
            var battleLayout = presenter.CreateCurrentLayoutSnapshot(1920f, 1080f);
            Assert.That(presenter.GetCurrentCameraSizeForTests(), Is.GreaterThanOrEqualTo(initialCameraSize));
            Assert.That(presenter.GetCurrentCameraSizeForTests(), Is.LessThanOrEqualTo(battlePreset.Size));
            Assert.That(battlePreset.Size, Is.GreaterThan(cardPreset.Size));
            Assert.That(battlePreset.Position.y, Is.LessThan(cardPreset.Position.y));
            Assert.That(battleLayout.BottomHand.Visible, Is.False);
            Assert.That(battleLayout.TopRightControls.Dimmed, Is.False);
            Assert.That(battleLayout.BottomBattleForces.Visible, Is.True);
            Assert.That(battleLayout.VisibleBuildCellCount, Is.EqualTo(0));
            Assert.That(presenter.GetVisibleBoardCellSet().Count, Is.EqualTo(0));
            var battleGridLines = presenter.GetActiveBoardGridLineCounts();
            Assert.That(battleGridLines.MainLines, Is.EqualTo(0));
            Assert.That(battleGridLines.AccentLines, Is.EqualTo(0));
            Assert.That(battleLayout.FriendlyForceRect.width, Is.GreaterThan(0f));
            Assert.That(presenter.GetBattleForceSummary().FriendlyTotal, Is.GreaterThan(0));
            Assert.That(presenter.GetBattleForceSummary().EnemyTotal, Is.GreaterThan(0));
            var deployDebug = presenter.GetBattleEntityDebugSnapshots();
            Assert.That(deployDebug.Any(snapshot => snapshot.UsesAnimator), Is.True);
            Assert.That(deployDebug.Any(snapshot =>
                snapshot.VisualState == NineKingsV2ScenePresenter.BattleVisualState.Idle
                || snapshot.VisualState == NineKingsV2ScenePresenter.BattleVisualState.Run), Is.True);

            for (var i = 0; i < 60 && controller.RunState.phase == RunPhase.BattleDeploy; i++)
            {
                controller.TickBattle(0.1f);
                yield return null;
            }

            Assert.That(controller.RunState.phase == RunPhase.BattleRun || controller.RunState.phase == RunPhase.BattleResolve || controller.RunState.phase == RunPhase.LootChoice || controller.RunState.phase == RunPhase.CardPhase || controller.RunState.phase == RunPhase.RunOver, Is.True);
            if (controller.RunState.phase == RunPhase.BattleRun)
            {
                for (var i = 0; i < 30; i++)
                {
                    controller.TickBattle(0.1f);
                    yield return null;
                }

                var runDebug = presenter.GetBattleEntityDebugSnapshots();
                Assert.That(runDebug.Any(snapshot =>
                    snapshot.VisualState == NineKingsV2ScenePresenter.BattleVisualState.Run
                    || snapshot.VisualState == NineKingsV2ScenePresenter.BattleVisualState.Attack
                    || snapshot.VisualState == NineKingsV2ScenePresenter.BattleVisualState.Shoot), Is.True);
            }
        }

        [UnityTest]
        public IEnumerator ScenePresenter_BattleDeploy_KeepsStructuresBack_WhileUnitsFormAhead()
        {
            var database = NineKingsV2SampleContentFactory.CreateInMemoryDatabase();
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.transform.position = new Vector3(0f, 0f, -10f);

            var root = new GameObject("NineKingsV2FormationSpacingRoot");
            var controller = root.AddComponent<NineKingsV2GameController>();
            controller.SetDatabase(database);
            var presenter = root.GetComponent<NineKingsV2ScenePresenter>() ?? root.AddComponent<NineKingsV2ScenePresenter>();
            presenter.SetController(controller);

            yield return null;

            controller.RunState!.handCardIds.Clear();
            controller.RunState.handCardIds.AddRange(new[] { "nothing_castle", "nothing_farm", "nothing_soldier", "nothing_archer", "nothing_blacksmith" });
            controller.HandState.cardIds.Clear();
            controller.HandState.cardIds.AddRange(controller.RunState.handCardIds);

            Assert.That(controller.TryPlayCard("nothing_castle", new BoardCoord(2, 2)), Is.True);
            Assert.That(controller.TryPlayCard("nothing_farm", new BoardCoord(3, 1)), Is.True);
            Assert.That(controller.TryPlayCard("nothing_soldier", new BoardCoord(1, 1)), Is.True);
            yield return null;

            Assert.That(controller.RunState!.phase, Is.EqualTo(RunPhase.BattleDeploy));
            var castle = root.transform.Find("PlacedStructures/Structure_2_2");
            var farm = root.transform.Find("PlacedStructures/Structure_3_1");
            var soldier = root.transform.Find("BattleUnits/Battle_friendly-1-1");
            Assert.That(castle, Is.Not.Null);
            Assert.That(farm, Is.Not.Null);
            Assert.That(soldier, Is.Not.Null);
            Assert.That(soldier!.position.x, Is.GreaterThan(castle!.position.x + 1.5f));

            var castleDeployPosition = castle.position;
            var farmDeployPosition = farm!.position;
            for (var i = 0; i < 40 && controller.RunState.phase == RunPhase.BattleDeploy; i++)
            {
                controller.TickBattle(0.1f);
                yield return null;
            }

            Assert.That(controller.RunState.phase == RunPhase.BattleRun || controller.RunState.phase == RunPhase.BattleResolve || controller.RunState.phase == RunPhase.LootChoice, Is.True);
            Assert.That(Vector3.Distance(castle.position, castleDeployPosition), Is.LessThanOrEqualTo(0.001f));
            Assert.That(Vector3.Distance(farm.position, farmDeployPosition), Is.LessThanOrEqualTo(0.001f));
        }

        [UnityTest]
        public IEnumerator BattleDeploy_LeavesFriendlyUnitsAtMapCoords_WhileEnemiesStillDeploy()
        {
            var database = NineKingsV2SampleContentFactory.CreateInMemoryDatabase();
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.transform.position = new Vector3(0f, 0f, -10f);

            var root = new GameObject("NineKingsV2DeployMoveRoot");
            var controller = root.AddComponent<NineKingsV2GameController>();
            controller.SetDatabase(database);
            var presenter = root.GetComponent<NineKingsV2ScenePresenter>() ?? root.AddComponent<NineKingsV2ScenePresenter>();
            presenter.SetController(controller);

            yield return null;

            controller.RunState!.handCardIds.Clear();
            controller.RunState.handCardIds.AddRange(new[] { "nothing_castle", "nothing_soldier", "nothing_archer", "nothing_farm" });
            controller.HandState.cardIds.Clear();
            controller.HandState.cardIds.AddRange(controller.RunState.handCardIds);

            Assert.That(controller.TryPlayCard("nothing_soldier", new BoardCoord(1, 1)), Is.True);
            Assert.That(controller.TryPlayCard("nothing_archer", new BoardCoord(2, 1)), Is.True);
            yield return null;

            Assert.That(controller.RunState!.phase, Is.EqualTo(RunPhase.BattleDeploy));
            var friendly = controller.BattleSceneState.entities.First(entity => !entity.isEnemy);
            var enemy = controller.BattleSceneState.entities.First(entity => entity.isEnemy);
            var initialPosition = new Vector2(friendly.worldX, friendly.worldY);
            var deployTarget = new Vector2(friendly.deployTargetX, friendly.deployTargetY);
            var deployStart = new Vector2(friendly.deployStartX, friendly.deployStartY);
            var enemyInitialPosition = new Vector2(enemy.worldX, enemy.worldY);
            var enemyDeployTarget = new Vector2(enemy.deployTargetX, enemy.deployTargetY);
            var enemyInitialDistance = Vector2.Distance(enemyInitialPosition, enemyDeployTarget);

            Assert.That(Vector2.Distance(initialPosition, deployStart), Is.LessThanOrEqualTo(0.08f));
            Assert.That(Vector2.Distance(deployStart, deployTarget), Is.LessThanOrEqualTo(0.01f));
            Assert.That(enemyInitialDistance, Is.GreaterThan(0.2f));

            for (var i = 0; i < 8; i++)
            {
                controller.TickBattle(0.1f);
                yield return null;
            }

            var movedPosition = new Vector2(friendly.worldX, friendly.worldY);
            var movedEnemyPosition = new Vector2(enemy.worldX, enemy.worldY);
            var movedEnemyDistance = Vector2.Distance(movedEnemyPosition, enemyDeployTarget);
            Assert.That(Vector2.Distance(movedPosition, initialPosition), Is.LessThanOrEqualTo(0.01f));
            Assert.That(movedEnemyDistance, Is.LessThan(enemyInitialDistance));
            Assert.That(movedEnemyPosition, Is.Not.EqualTo(enemyInitialPosition));

            for (var i = 0; i < 40 && controller.RunState.phase == RunPhase.BattleDeploy; i++)
            {
                controller.TickBattle(0.1f);
                yield return null;
            }

            Assert.That(controller.RunState.phase == RunPhase.BattleRun || controller.RunState.phase == RunPhase.BattleResolve || controller.RunState.phase == RunPhase.LootChoice || controller.RunState.phase == RunPhase.CardPhase || controller.RunState.phase == RunPhase.RunOver, Is.True);
            Assert.That(Vector2.Distance(new Vector2(friendly.worldX, friendly.worldY), deployTarget), Is.LessThanOrEqualTo(0.01f));
            Assert.That(Vector2.Distance(new Vector2(enemy.worldX, enemy.worldY), enemyDeployTarget), Is.LessThanOrEqualTo(0.05f));
        }

        [UnityTest]
        public IEnumerator ScenePresenter_LootChoiceLayout_HidesHand_AndShowsOverlay()
        {
            var database = NineKingsV2SampleContentFactory.CreateInMemoryDatabase();
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.transform.position = new Vector3(0f, 0f, -10f);

            var root = new GameObject("NineKingsV2LootChoiceRoot");
            var controller = root.AddComponent<NineKingsV2GameController>();
            controller.SetDatabase(database);
            var presenter = root.GetComponent<NineKingsV2ScenePresenter>() ?? root.AddComponent<NineKingsV2ScenePresenter>();
            presenter.SetController(controller);

            yield return null;

            controller.RunState!.phase = RunPhase.LootChoice;
            var layout = presenter.CreateCurrentLayoutSnapshot(1920f, 1080f);
            Assert.That(layout.BottomHand.Visible, Is.False);
            Assert.That(layout.Overlay.Visible, Is.True);
            Assert.That(layout.LootCardRects.Count, Is.GreaterThanOrEqualTo(1));
        }

        [UnityTest]
        public IEnumerator ScenePresenter_BattleScene_UsesReducedStructureAndUnitScales()
        {
            var database = NineKingsV2SampleContentFactory.CreateInMemoryDatabase();
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.transform.position = new Vector3(0f, 0f, -10f);

            var root = new GameObject("NineKingsV2ScaleRoot");
            var controller = root.AddComponent<NineKingsV2GameController>();
            controller.SetDatabase(database);
            var presenter = root.GetComponent<NineKingsV2ScenePresenter>() ?? root.AddComponent<NineKingsV2ScenePresenter>();
            presenter.SetController(controller);

            yield return null;

            Assert.That(controller.TryPlayCard(controller.HandState.cardIds[0], new BoardCoord(1, 1)), Is.True);
            Assert.That(controller.TryPlayCard(controller.HandState.cardIds[0], new BoardCoord(2, 1)), Is.True);
            yield return null;

            var battleUnits = root.transform.Find("BattleUnits");
            Assert.That(battleUnits, Is.Not.Null);
            var firstBattleUnit = battleUnits!.GetComponentsInChildren<Transform>(true).FirstOrDefault(item => item.name.StartsWith("Battle_", System.StringComparison.Ordinal));
            Assert.That(firstBattleUnit, Is.Not.Null);

            var structureScale = NineKingsV2ScenePresenter.GetStructureSpriteScale("greed_palace", true);
            var memberScale = NineKingsV2ScenePresenter.GetSpriteMemberScale(false);
            Assert.That(structureScale.x, Is.LessThan(0.7f));
            Assert.That(memberScale.x, Is.LessThan(0.25f));
            var debugSnapshots = presenter.GetBattleEntityDebugSnapshots();
            Assert.That(debugSnapshots.Any(snapshot => snapshot.UsesAnimator), Is.True);
            Assert.That(debugSnapshots.All(snapshot => snapshot.ActiveMemberCount >= 1), Is.True);
        }
    }
}
