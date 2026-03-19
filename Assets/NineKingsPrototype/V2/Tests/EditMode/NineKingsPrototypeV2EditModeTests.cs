#nullable enable
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

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

            Assert.That(run.plots.Count, Is.EqualTo(25));
            Assert.That(run.GetUnlockedPlots().Count, Is.EqualTo(9));
            Assert.That(run.GetUnlockedPlots().All(plot => plot.coord.x >= 1 && plot.coord.x <= 3 && plot.coord.y >= 1 && plot.coord.y <= 3), Is.True);
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

        [Test]
        public void ScenePresenter_LayoutSnapshot_StaysWithinDesignBounds()
        {
            var snapshot = NineKingsV2ScenePresenter.BuildLayoutSnapshot(1920f, 1080f, RunPhase.CardPhase, 4, true, 0);

            AssertRectWithinDesign(snapshot.TopLeftResources.Rect);
            AssertRectWithinDesign(snapshot.TopCenterTimeline.Rect);
            AssertRectWithinDesign(snapshot.TopRightControls.Rect);
            AssertRectWithinDesign(snapshot.LeftWell.Rect);
            AssertRectWithinDesign(snapshot.RightStatus.Rect);
            AssertRectWithinDesign(snapshot.BottomHand.Rect);
            AssertRectWithinDesign(snapshot.BoardInteractionRect);
            AssertRectWithinDesign(snapshot.TimelineSubtitleRect);
            AssertRectWithinDesign(snapshot.TimelineYearBadgeRect);
            Assert.That(snapshot.HandCardRects, Has.Count.EqualTo(4));
            Assert.That(snapshot.HandCardRects.All(IsRectWithinDesign), Is.True);
            Assert.That(snapshot.HandCardRects.Zip(snapshot.HandCardRects.Skip(1), (left, right) => left.xMax <= right.xMin + 0.01f).All(value => value), Is.True);
            Assert.That(snapshot.BoardCellsUseStrongOutline, Is.True);
            Assert.That(snapshot.VisibleBuildCellCount, Is.EqualTo(9));
        }

        [Test]
        public void ScenePresenter_BoardOutlineStyle_UsesClearContinuousWhiteLine()
        {
            var style = NineKingsV2ScenePresenter.GetBoardCellOutlineStyle();
            var cardGridStyle = NineKingsV2ScenePresenter.ResolveBoardGridLineStyle(RunPhase.CardPhase);
            var battleGridStyle = NineKingsV2ScenePresenter.ResolveBoardGridLineStyle(RunPhase.BattleDeploy);

            Assert.That(style.FillScale.x, Is.GreaterThan(1.4f));
            Assert.That(style.EdgeScale.x, Is.GreaterThan(1.15f));
            Assert.That(style.EdgeScale.y, Is.GreaterThan(0.10f));
            Assert.That(style.CardEdgeColor.a, Is.EqualTo(1f).Within(0.001f));
            Assert.That(style.CardEdgeColor.r, Is.GreaterThan(0.97f));
            Assert.That(cardGridStyle.MainWidth, Is.GreaterThan(0.09f));
            Assert.That(cardGridStyle.AccentWidth, Is.GreaterThan(0.17f));
            Assert.That(battleGridStyle.MainWidth, Is.GreaterThan(0.07f));
            Assert.That(battleGridStyle.AccentWidth, Is.GreaterThan(0.13f));
        }

        [Test]
        public void ScenePresenter_LayoutSnapshot_BuildsLetterboxFrameForWideAndTallScreens()
        {
            var wideFrame = NineKingsV2ScenePresenter.BuildUiFrame(2560f, 1080f);
            var tallFrame = NineKingsV2ScenePresenter.BuildUiFrame(1080f, 1920f);

            Assert.That(wideFrame.Scale, Is.EqualTo(1f).Within(0.001f));
            Assert.That(wideFrame.Offset.x, Is.GreaterThan(0f));
            Assert.That(wideFrame.Offset.y, Is.EqualTo(0f).Within(0.001f));

            Assert.That(tallFrame.Scale, Is.EqualTo(0.5625f).Within(0.001f));
            Assert.That(tallFrame.Offset.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(tallFrame.Offset.y, Is.GreaterThan(0f));
        }

        [Test]
        public void ScenePresenter_LayoutSnapshot_UsesPhaseVisibilityAndBattleCameraRules()
        {
            var cardSnapshot = NineKingsV2ScenePresenter.BuildLayoutSnapshot(1920f, 1080f, RunPhase.CardPhase, 4, false, 0);
            var deploySnapshot = NineKingsV2ScenePresenter.BuildLayoutSnapshot(1920f, 1080f, RunPhase.BattleDeploy, 2, false, 0);
            var battleSnapshot = NineKingsV2ScenePresenter.BuildLayoutSnapshot(1920f, 1080f, RunPhase.BattleRun, 2, false, 0);
            var resolveSnapshot = NineKingsV2ScenePresenter.BuildLayoutSnapshot(1920f, 1080f, RunPhase.BattleResolve, 2, false, 0);
            var lootSnapshot = NineKingsV2ScenePresenter.BuildLayoutSnapshot(1920f, 1080f, RunPhase.LootChoice, 2, false, 3);

            Assert.That(cardSnapshot.BottomHand.Visible, Is.True);
            Assert.That(cardSnapshot.Overlay.Visible, Is.False);
            Assert.That(cardSnapshot.TopRightControls.Dimmed, Is.True);
            Assert.That(cardSnapshot.BottomBattleForces.Visible, Is.False);
            Assert.That(cardSnapshot.TimelineSubtitleRect.width, Is.GreaterThan(0f));

            Assert.That(deploySnapshot.BottomBattleForces.Visible, Is.True);
            Assert.That(deploySnapshot.TopRightControls.Dimmed, Is.False);
            Assert.That(battleSnapshot.BottomHand.Visible, Is.False);
            Assert.That(battleSnapshot.Overlay.Visible, Is.False);
            Assert.That(battleSnapshot.TopRightControls.Dimmed, Is.False);
            Assert.That(battleSnapshot.BottomBattleForces.Visible, Is.True);
            AssertRectWithinDesign(battleSnapshot.FriendlyForceRect);
            AssertRectWithinDesign(battleSnapshot.EnemyForceRect);

            Assert.That(resolveSnapshot.BottomHand.Visible, Is.False);
            Assert.That(resolveSnapshot.Overlay.Visible, Is.True);
            Assert.That(resolveSnapshot.TopRightControls.Dimmed, Is.True);
            Assert.That(resolveSnapshot.BottomBattleForces.Visible, Is.True);

            Assert.That(lootSnapshot.BottomHand.Visible, Is.False);
            Assert.That(lootSnapshot.Overlay.Visible, Is.True);
            Assert.That(lootSnapshot.TopRightControls.Dimmed, Is.True);
            Assert.That(lootSnapshot.LootCardRects, Has.Count.EqualTo(3));

            Assert.That(battleSnapshot.CameraPreset.Size, Is.GreaterThan(cardSnapshot.CameraPreset.Size));
            Assert.That(cardSnapshot.CameraPreset.Size, Is.GreaterThan(5f));
            Assert.That(cardSnapshot.CameraPreset.Position.y, Is.GreaterThan(1.2f));
            Assert.That(battleSnapshot.CameraPreset.Position.y, Is.LessThan(cardSnapshot.CameraPreset.Position.y));
            Assert.That(lootSnapshot.CameraPreset.Size, Is.EqualTo(battleSnapshot.CameraPreset.Size).Within(0.001f));
        }

        [Test]
        public void ScenePresenter_ShouldSpawnProjectileForAttack_OnlyForLivingRangedAttackTriggers()
        {
            var ranged = new BattleEntityState
            {
                attackRange = 2.2f,
                isDead = false,
            };
            var melee = new BattleEntityState
            {
                attackRange = 1.2f,
                isDead = false,
            };
            var deadRanged = new BattleEntityState
            {
                attackRange = 2.2f,
                isDead = true,
            };

            Assert.That(NineKingsV2ScenePresenter.ShouldSpawnProjectileForAttack(ranged, true), Is.True);
            Assert.That(NineKingsV2ScenePresenter.ShouldSpawnProjectileForAttack(ranged, false), Is.False);
            Assert.That(NineKingsV2ScenePresenter.ShouldSpawnProjectileForAttack(melee, true), Is.False);
            Assert.That(NineKingsV2ScenePresenter.ShouldSpawnProjectileForAttack(deadRanged, true), Is.False);
        }

        [Test]
        public void ScenePresenter_BattleForceSummary_UsesAliveStackCounts()
        {
            var state = new BattleSceneState();
            state.entities.Add(new BattleEntityState { isEnemy = false, stackCount = 3 });
            state.entities.Add(new BattleEntityState { isEnemy = false, stackCount = 2, isDead = true });
            state.entities.Add(new BattleEntityState { isEnemy = true, stackCount = 4 });
            state.entities.Add(new BattleEntityState { isEnemy = true, stackCount = 1 });

            var summary = NineKingsV2ScenePresenter.SummarizeBattleForces(state);

            Assert.That(summary.FriendlyTotal, Is.EqualTo(3));
            Assert.That(summary.EnemyTotal, Is.EqualTo(5));
        }

        [Test]
        public void ScenePresenter_BattleScales_AreReduced_ForStageOneFixes()
        {
            Assert.That(NineKingsV2ScenePresenter.GetStructureSpriteScale("greed_palace", false).x, Is.LessThan(0.5f));
            Assert.That(NineKingsV2ScenePresenter.GetStructureSpriteScale("greed_palace", true).x, Is.LessThan(0.7f));
            Assert.That(NineKingsV2ScenePresenter.GetStructureSpriteScale("nothing_castle", true).x, Is.LessThan(0.7f));
            Assert.That(NineKingsV2ScenePresenter.ResolveBattleStructureAnchor("greed_palace", WorldObjectType.Palace, new BoardCoord(2, 2)).Scale.x, Is.LessThan(0.7f));
            Assert.That(NineKingsV2ScenePresenter.ResolveBattleStructureAnchor("nothing_farm", WorldObjectType.Building, new BoardCoord(1, 1)).Position.x, Is.LessThan(-4.0f));
            Assert.That(NineKingsV2ScenePresenter.GetSpriteMemberScale(false).x, Is.EqualTo(0.20f).Within(0.01f));
            Assert.That(NineKingsV2ScenePresenter.GetSpriteMemberScale(true).x, Is.EqualTo(0.18f).Within(0.01f));
            Assert.That(NineKingsV2ScenePresenter.GetFallbackMemberScale(false).x, Is.LessThan(0.05f));
        }

        [Test]
        public void ScenePresenter_BattleStructureAnchor_IsStablePerPlotCoord()
        {
            var deployAnchor = NineKingsV2ScenePresenter.ResolveBattleStructureAnchor("nothing_farm", WorldObjectType.Building, new BoardCoord(1, 1));
            var runAnchor = NineKingsV2ScenePresenter.ResolveBattleStructureAnchor("nothing_farm", WorldObjectType.Building, new BoardCoord(1, 1));
            var differentAnchor = NineKingsV2ScenePresenter.ResolveBattleStructureAnchor("nothing_farm", WorldObjectType.Building, new BoardCoord(3, 1));

            Assert.That(deployAnchor.Position, Is.EqualTo(runAnchor.Position));
            Assert.That(deployAnchor.Scale, Is.EqualTo(runAnchor.Scale));
            Assert.That(differentAnchor.Position, Is.Not.EqualTo(deployAnchor.Position));
        }

        [Test]
        public void ScenePresenter_VisibleBoardCellSet_UsesUnlockedThreeByThreeCore()
        {
            var database = NineKingsV2SampleContentFactory.CreateInMemoryDatabase();
            var run = RunState.CreateNew(database, "king_greed");

            var visible = NineKingsV2ScenePresenter.BuildVisibleBoardCellSet(run, RunPhase.CardPhase).ToArray();

            Assert.That(visible, Has.Length.EqualTo(9));
            Assert.That(visible, Does.Contain(new BoardCoord(1, 1)));
            Assert.That(visible, Does.Contain(new BoardCoord(2, 2)));
            Assert.That(visible, Does.Contain(new BoardCoord(3, 3)));
            Assert.That(visible.Any(coord => coord.Equals(new BoardCoord(0, 0))), Is.False);
        }

        [Test]
        public void ScenePresenter_BoardGridSegments_RenderOnlyUnlockedThreeByThreeNetwork()
        {
            var database = NineKingsV2SampleContentFactory.CreateInMemoryDatabase();
            var run = RunState.CreateNew(database, "king_greed");

            var visible = NineKingsV2ScenePresenter.BuildVisibleBoardCellSet(run, RunPhase.CardPhase);
            var segments = NineKingsV2ScenePresenter.BuildBoardGridSegments(visible);

            Assert.That(segments.Count, Is.EqualTo(24));
            Assert.That(segments.All(segment => Mathf.Abs(Mathf.Abs(segment.End.x - segment.Start.x) - 1.08f) < 0.02f), Is.True);
            Assert.That(segments.All(segment => Mathf.Abs(Mathf.Abs(segment.End.y - segment.Start.y) - 0.56f) < 0.02f), Is.True);
        }

        [Test]
        public void ScenePresenter_HoverResolver_MatchesCenterCell_AndScreenConversion_UsesTopLeftGuiSpace()
        {
            var database = NineKingsV2SampleContentFactory.CreateInMemoryDatabase();
            var run = RunState.CreateNew(database, "king_greed");
            var centerWorld = NineKingsV2ScenePresenter.BoardCoordToWorld(new BoardCoord(2, 2));

            var hit = NineKingsV2ScenePresenter.TryResolveHoveredCoordFromWorld(run, RunPhase.CardPhase, new UnityEngine.Vector2(centerWorld.x, centerWorld.y), out var coord);
            var frame = NineKingsV2ScenePresenter.BuildUiFrame(1920f, 1080f);
            var topLeftPoint = NineKingsV2ScenePresenter.ScreenToDesign(new UnityEngine.Vector2(0f, 1080f), frame);

            Assert.That(hit, Is.True);
            Assert.That(coord, Is.EqualTo(new BoardCoord(2, 2)));
            Assert.That(topLeftPoint.x, Is.EqualTo(0f).Within(0.01f));
            Assert.That(topLeftPoint.y, Is.EqualTo(0f).Within(0.01f));
        }

        [Test]
        public void GameController_ResolveLootChoice_PutsRewardIntoNextYearHand()
        {
            var database = NineKingsV2SampleContentFactory.CreateInMemoryDatabase();
            var gameObject = new UnityEngine.GameObject("LootChoiceController");
            try
            {
                var controller = gameObject.AddComponent<NineKingsV2GameController>();
                controller.SetDatabase(database);
                controller.StartNewRun("king_nothing");
                controller.RunState!.handCardIds.Clear();
                controller.RunState.handCardIds.AddRange(new[] { "nothing_castle", "nothing_soldier" });
                controller.RunState.deckCardIds.Clear();
                controller.RunState.deckCardIds.AddRange(new[] { "nothing_archer", "nothing_blacksmith" });
                controller.RunState.phase = RunPhase.LootChoice;
                controller.ResolveLootChoice("nothing_farm");

                Assert.That(controller.RunState.year, Is.EqualTo(2));
                Assert.That(controller.RunState.handCardIds, Does.Contain("nothing_farm"));
                Assert.That(controller.HandState.cardIds, Does.Contain("nothing_farm"));
                Assert.That(controller.RunState.handCardIds.Count, Is.EqualTo(4));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void GameController_ResolveYearlyAdjacencyEffects_AddsFarmBonus_ToOrthogonalTroopsOnly()
        {
            var database = NineKingsV2SampleContentFactory.CreateInMemoryDatabase();
            var run = RunState.CreateNew(database, "king_nothing");
            PlacementValidator.TryApply(database, run, "nothing_farm", new BoardCoord(2, 2));
            PlacementValidator.TryApply(database, run, "nothing_paladin", new BoardCoord(2, 1));
            PlacementValidator.TryApply(database, run, "nothing_archer", new BoardCoord(1, 2));
            PlacementValidator.TryApply(database, run, "nothing_blacksmith", new BoardCoord(3, 2));
            PlacementValidator.TryApply(database, run, "nothing_soldier", new BoardCoord(1, 1));
            run.GetPlot(new BoardCoord(2, 2)).level = 2;

            NineKingsV2GameController.ResolveYearlyAdjacencyEffects(database, run);

            Assert.That(run.GetPlot(new BoardCoord(2, 1)).bonusUnitCount, Is.EqualTo(2));
            Assert.That(run.GetPlot(new BoardCoord(1, 2)).bonusUnitCount, Is.EqualTo(2));
            Assert.That(run.GetPlot(new BoardCoord(3, 2)).bonusUnitCount, Is.EqualTo(0));
            Assert.That(run.GetPlot(new BoardCoord(1, 1)).bonusUnitCount, Is.EqualTo(0));
        }

        [Test]
        public void GameController_ResolvePlotRuntimeStats_ReflectsBonusUnits_AndCurrentAdjacency()
        {
            var database = NineKingsV2SampleContentFactory.CreateInMemoryDatabase();
            var run = RunState.CreateNew(database, "king_nothing");
            PlacementValidator.TryApply(database, run, "nothing_farm", new BoardCoord(2, 2));
            PlacementValidator.TryApply(database, run, "nothing_paladin", new BoardCoord(2, 1));
            var farmPlot = run.GetPlot(new BoardCoord(2, 2));
            var paladinPlot = run.GetPlot(new BoardCoord(2, 1));
            farmPlot.level = 3;
            paladinPlot.level = 2;
            paladinPlot.bonusUnitCount = 4;
            paladinPlot.damageMultiplier = 1.5f;
            paladinPlot.shield = 2;
            paladinPlot.enchantmentStacks = 1;
            paladinPlot.totalDamage = 19;
            paladinPlot.totalKills = 3;

            var stats = NineKingsV2GameController.ResolvePlotRuntimeStats(database, run, paladinPlot);

            Assert.That(stats.Level, Is.EqualTo(2));
            Assert.That(stats.EffectiveUnitCount, Is.EqualTo(6));
            Assert.That(stats.CumulativeBonusUnitCount, Is.EqualTo(4));
            Assert.That(stats.AnnualAdjacencyBonus, Is.EqualTo(3));
            Assert.That(stats.EffectiveMaxHp, Is.EqualTo(15));
            Assert.That(stats.EffectiveDamage, Is.EqualTo(8));
            Assert.That(stats.Shield, Is.EqualTo(2));
            Assert.That(stats.EnchantmentStacks, Is.EqualTo(1));
            Assert.That(stats.TotalDamage, Is.EqualTo(19));
            Assert.That(stats.TotalKills, Is.EqualTo(3));
        }

        [Test]
        public void ScenePresenter_MapDisplayCount_And_LevelColor_FollowRuntimeStats()
        {
            var plot = new PlotState { cardId = "nothing_soldier", level = 1 };
            var stats = new PlotRuntimeStats(1, 9, 6, 3, 8, 2, 0.9f, 1.2f, 1f, 0, 0, 0, 0, true);

            Assert.That(NineKingsV2ScenePresenter.ResolveMapUnitDisplayCount(plot, stats), Is.EqualTo(6));
            Assert.That(NineKingsV2ScenePresenter.ResolveStructureLevelColor(1), Is.EqualTo(new Color(0.97f, 0.83f, 0.24f, 1f)));
            Assert.That(NineKingsV2ScenePresenter.ResolveStructureLevelColor(3), Is.EqualTo(new Color(0.56f, 0.36f, 0.82f, 1f)));
            Assert.That(NineKingsV2ScenePresenter.ResolveStructureLevelColor(5), Is.EqualTo(new Color(0.18f, 0.18f, 0.18f, 1f)));
        }

        [Test]
        public void ScenePresenter_MapUnitDisplayAnchor_UsesVisibleFormationCentroid()
        {
            var centered = NineKingsV2ScenePresenter.ResolveMapUnitDisplayAnchor(new BoardCoord(2, 2), 1, false);
            var meleeGroup = NineKingsV2ScenePresenter.ResolveMapUnitDisplayAnchor(new BoardCoord(2, 2), 6, false);
            var rangedGroup = NineKingsV2ScenePresenter.ResolveMapUnitDisplayAnchor(new BoardCoord(2, 2), 6, true);

            Assert.That(centered, Is.EqualTo(NineKingsV2ScenePresenter.ResolveMapPlotAnchor(new BoardCoord(2, 2))));
            Assert.That(meleeGroup, Is.Not.EqualTo(centered));
            Assert.That(rangedGroup, Is.Not.EqualTo(centered));
            Assert.That(Mathf.Abs(meleeGroup.x - centered.x), Is.LessThan(0.05f));
            Assert.That(Mathf.Abs(rangedGroup.x - centered.x), Is.LessThan(0.05f));
            Assert.That(meleeGroup.y, Is.LessThan(centered.y));
            Assert.That(rangedGroup.y, Is.LessThan(centered.y));
        }

        [Test]
        public void ScenePresenter_TooltipSnapshot_UsesRuntimeStats()
        {
            var database = NineKingsV2SampleContentFactory.CreateInMemoryDatabase();
            var run = RunState.CreateNew(database, "king_nothing");
            PlacementValidator.TryApply(database, run, "nothing_farm", new BoardCoord(2, 2));
            PlacementValidator.TryApply(database, run, "nothing_paladin", new BoardCoord(2, 1));
            var farmPlot = run.GetPlot(new BoardCoord(2, 2));
            farmPlot.level = 2;
            var paladinPlot = run.GetPlot(new BoardCoord(2, 1));
            paladinPlot.bonusUnitCount = 3;

            var snapshot = NineKingsV2ScenePresenter.CreateHoveredPlotTooltipSnapshot(database, run, paladinPlot);

            Assert.That(snapshot, Is.Not.Null);
            Assert.That(snapshot!.title, Is.EqualTo("圣骑士"));
            Assert.That(snapshot.statLines.Any(line => line.Contains("单位数", System.StringComparison.Ordinal)), Is.True);
            Assert.That(snapshot.footer, Does.Contain("四邻增兵"));
        }

        [Test]
        public void ScenePresenter_AnimatorSpec_ResolvesExistingControllers()
        {
            var friendlyMelee = NineKingsV2ScenePresenter.ResolveUnitAnimatorSpec(new BattleEntityState { sourceCardId = "nothing_soldier", unitArchetypeId = "nothing-soldier", attackRange = 1.2f });
            var friendlyRanged = NineKingsV2ScenePresenter.ResolveUnitAnimatorSpec(new BattleEntityState { sourceCardId = "greed_archer", unitArchetypeId = "greed-archer", attackRange = 2.2f });
            var enemyMelee = NineKingsV2ScenePresenter.ResolveUnitAnimatorSpec(new BattleEntityState { isEnemy = true, sourceCardId = "enemy-melee", unitArchetypeId = "enemy-melee", attackRange = 1.2f });

            Assert.That(friendlyMelee.IsValid, Is.True);
            Assert.That(friendlyRanged.IsValid, Is.True);
            Assert.That(enemyMelee.IsValid, Is.True);
            Assert.That(File.Exists(Path.Combine(Directory.GetCurrentDirectory(), friendlyMelee.ControllerAssetPath)), Is.True);
            Assert.That(File.Exists(Path.Combine(Directory.GetCurrentDirectory(), friendlyRanged.ControllerAssetPath)), Is.True);
            Assert.That(File.Exists(Path.Combine(Directory.GetCurrentDirectory(), enemyMelee.ControllerAssetPath)), Is.True);
        }

        [Test]
        public void ScenePresenter_MapUnitVisualSpec_ResolvesTroopSourcesOnlyInCardPhase()
        {
            var soldier = NineKingsV2ScenePresenter.ResolveMapUnitVisualSpec("nothing_soldier", WorldObjectType.UnitSource, false);
            var archer = NineKingsV2ScenePresenter.ResolveMapUnitVisualSpec("nothing_archer", WorldObjectType.UnitSource, false);
            var battleSoldier = NineKingsV2ScenePresenter.ResolveMapUnitVisualSpec("nothing_soldier", WorldObjectType.UnitSource, true);
            var vault = NineKingsV2ScenePresenter.ResolveMapUnitVisualSpec("greed_vault", WorldObjectType.Building, false);

            Assert.That(soldier.IsValid, Is.True);
            Assert.That(archer.IsValid, Is.True);
            Assert.That(soldier.PrimaryScale.x, Is.GreaterThan(0.3f));
            Assert.That(File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Assets", soldier.AssetRelativePath)), Is.True);
            Assert.That(File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Assets", archer.AssetRelativePath)), Is.True);
            Assert.That(battleSoldier.IsValid, Is.False);
            Assert.That(vault.IsValid, Is.False);
        }

        [Test]
        public void CombatSimulation_FormationSlots_KeepMeleeAheadOfRanged()
        {
            var friendlyMelee = CombatSimulation.ResolveFormationSlots(false, CombatRole.Melee, 4);
            var friendlyRanged = CombatSimulation.ResolveFormationSlots(false, CombatRole.Ranged, 3);
            var enemyMelee = CombatSimulation.ResolveFormationSlots(true, CombatRole.Melee, 4);
            var enemyRanged = CombatSimulation.ResolveFormationSlots(true, CombatRole.Ranged, 3);

            Assert.That(friendlyMelee.Average(slot => slot.x), Is.GreaterThan(friendlyRanged.Average(slot => slot.x)));
            Assert.That(enemyMelee.Average(slot => slot.x), Is.LessThan(enemyRanged.Average(slot => slot.x)));
            Assert.That(friendlyRanged.Average(slot => slot.x), Is.GreaterThan(0f));
            Assert.That(enemyRanged.Average(slot => slot.x), Is.GreaterThan(4.0f));
            Assert.That(friendlyMelee[0], Is.EqualTo(CombatSimulation.ResolveFormationSlots(false, CombatRole.Melee, 4)[0]));
        }

        [Test]
        public void CombatSimulation_EnemyWave_SpawnsClearlyBeyondFriendlyFront()
        {
            var database = NineKingsV2SampleContentFactory.CreateInMemoryDatabase();
            var run = RunState.CreateNew(database, "king_nothing");
            PlacementValidator.TryApply(database, run, "nothing_soldier", new BoardCoord(1, 1));
            PlacementValidator.TryApply(database, run, "nothing_archer", new BoardCoord(2, 1));
            var simulation = new CombatSimulation(database);

            var battle = simulation.CreateBattleScene(run);
            var maxFriendlyX = battle.entities.Where(entity => !entity.isEnemy).Max(entity => entity.worldX);
            var minEnemyX = battle.entities.Where(entity => entity.isEnemy).Min(entity => entity.worldX);
            var minEnemyY = battle.entities.Where(entity => entity.isEnemy).Min(entity => entity.worldY);

            Assert.That(minEnemyX, Is.GreaterThan(maxFriendlyX + 1.2f));
            Assert.That(minEnemyY, Is.LessThan(-1.7f).Or.EqualTo(-1.96f).Within(0.2f));
        }

        [Test]
        public void CombatSimulation_CreateBattleScene_StartsDeployFromSourceAndEntryPoints()
        {
            var database = NineKingsV2SampleContentFactory.CreateInMemoryDatabase();
            var run = RunState.CreateNew(database, "king_nothing");
            PlacementValidator.TryApply(database, run, "nothing_soldier", new BoardCoord(1, 1));
            PlacementValidator.TryApply(database, run, "nothing_archer", new BoardCoord(2, 1));
            var simulation = new CombatSimulation(database);

            var battle = simulation.CreateBattleScene(run);
            var friendly = battle.entities.Where(entity => !entity.isEnemy).ToArray();
            var enemy = battle.entities.Where(entity => entity.isEnemy).ToArray();
            var entryPoint = CombatSimulation.GetEnemyDeployEntryPoint();
            var soldierEntity = friendly.First(entity => entity.sourceCoord.x == 1 && entity.sourceCoord.y == 1);
            var soldierStart = NineKingsV2ScenePresenter.ResolveMapUnitDisplayAnchor(soldierEntity.sourceCoord, soldierEntity.stackCount, false);

            Assert.That(friendly, Is.Not.Empty);
            Assert.That(enemy, Is.Not.Empty);
            Assert.That(friendly.All(entity => Mathf.Approximately(entity.worldX, entity.deployStartX) && Mathf.Approximately(entity.worldY, entity.deployStartY)), Is.True);
            Assert.That(friendly.Any(entity => !Mathf.Approximately(entity.deployStartX, entity.deployTargetX) || !Mathf.Approximately(entity.deployStartY, entity.deployTargetY)), Is.True);
            Assert.That(Mathf.Approximately(soldierEntity.deployStartX, soldierStart.x) && Mathf.Approximately(soldierEntity.deployStartY, soldierStart.y), Is.True);
            Assert.That(enemy.All(entity => Mathf.Approximately(entity.deployStartX, entryPoint.x) && Mathf.Approximately(entity.deployStartY, entryPoint.y)), Is.True);
            Assert.That(enemy.Any(entity => !Mathf.Approximately(entity.deployStartX, entity.deployTargetX) || !Mathf.Approximately(entity.deployStartY, entity.deployTargetY)), Is.True);
        }

        [Test]
        public void CombatSimulation_FriendlyFormationSlots_LockToRightBottomQuadrant()
        {
            var meleeSlots = CombatSimulation.ResolveFormationSlots(false, CombatRole.Melee, 3);
            var rangedSlots = CombatSimulation.ResolveFormationSlots(false, CombatRole.Ranged, 3);
            var axis = CombatSimulation.ResolveFriendlyFormationDebugAxis();

            Assert.That(meleeSlots, Is.Not.Empty);
            Assert.That(rangedSlots, Is.Not.Empty);
            Assert.That(meleeSlots.All(slot => Vector2.Dot(slot - axis.Start, axis.Forward) > 0f), Is.True);
            Assert.That(rangedSlots.All(slot => Vector2.Dot(slot - axis.Start, axis.Forward) > 0f), Is.True);
            Assert.That(meleeSlots.Average(slot => Vector2.Dot(slot - axis.Start, axis.Forward)), Is.GreaterThan(rangedSlots.Average(slot => Vector2.Dot(slot - axis.Start, axis.Forward))));
            Assert.That(meleeSlots.All(slot => Mathf.Abs((slot - axis.Start).x * axis.Forward.y - (slot - axis.Start).y * axis.Forward.x) < 0.08f), Is.True);
            Assert.That(rangedSlots.All(slot => Mathf.Abs((slot - axis.Start).x * axis.Forward.y - (slot - axis.Start).y * axis.Forward.x) < 0.08f), Is.True);
        }

        [Test]
        public void CombatSimulation_FriendlyFormationCenter_StaysOnDebugAxis()
        {
            var meleeSlots = CombatSimulation.ResolveFormationSlots(false, CombatRole.Melee, 2);
            var rangedSlots = CombatSimulation.ResolveFormationSlots(false, CombatRole.Ranged, 2);
            var allSlots = meleeSlots.Concat(rangedSlots).ToArray();
            var center = allSlots.Aggregate(Vector2.zero, (sum, slot) => sum + slot) / allSlots.Length;
            var axis = CombatSimulation.ResolveFriendlyFormationDebugAxis();
            var offset = center - axis.Start;
            var cross = Mathf.Abs(offset.x * axis.Forward.y - offset.y * axis.Forward.x);

            Assert.That(cross, Is.LessThan(0.08f));
        }

        [Test]
        public void CombatSimulation_EnemyFormationCenter_StaysOnDebugAxis()
        {
            var meleeSlots = CombatSimulation.ResolveFormationSlots(true, CombatRole.Melee, 2);
            var rangedSlots = CombatSimulation.ResolveFormationSlots(true, CombatRole.Ranged, 2);
            var allSlots = meleeSlots.Concat(rangedSlots).ToArray();
            var center = allSlots.Aggregate(Vector2.zero, (sum, slot) => sum + slot) / allSlots.Length;
            var axis = CombatSimulation.ResolveFriendlyFormationDebugAxis();
            var offset = center - axis.Start;
            var cross = Mathf.Abs(offset.x * axis.Forward.y - offset.y * axis.Forward.x);

            Assert.That(cross, Is.LessThan(0.08f));
        }

        [Test]
        public void CombatSimulation_EnemyFormationSlots_StayOnDebugAxis()
        {
            var meleeSlots = CombatSimulation.ResolveFormationSlots(true, CombatRole.Melee, 3);
            var rangedSlots = CombatSimulation.ResolveFormationSlots(true, CombatRole.Ranged, 3);
            var axis = CombatSimulation.ResolveFriendlyFormationDebugAxis();

            Assert.That(meleeSlots, Is.Not.Empty);
            Assert.That(rangedSlots, Is.Not.Empty);
            Assert.That(meleeSlots.All(slot => Mathf.Abs((slot - axis.Start).x * axis.Forward.y - (slot - axis.Start).y * axis.Forward.x) < 0.08f), Is.True);
            Assert.That(rangedSlots.All(slot => Mathf.Abs((slot - axis.Start).x * axis.Forward.y - (slot - axis.Start).y * axis.Forward.x) < 0.08f), Is.True);
        }

        [Test]
        public void ScenePresenter_BattleFormationOffsets_AreCenteredOnRoot()
        {
            var melee = NineKingsV2ScenePresenter.GetCenteredFormationOffsets(NineKingsV2ScenePresenter.GetSpriteFormationOffsets(false), 2);
            var ranged = NineKingsV2ScenePresenter.GetCenteredFormationOffsets(NineKingsV2ScenePresenter.GetSpriteFormationOffsets(true), 2);

            var meleeCentroid = (melee[0] + melee[1]) * 0.5f;
            var rangedCentroid = (ranged[0] + ranged[1]) * 0.5f;

            Assert.That(meleeCentroid.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(meleeCentroid.y, Is.EqualTo(0f).Within(0.001f));
            Assert.That(rangedCentroid.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(rangedCentroid.y, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void CombatSimulation_Melee_UsesContactDistance_WhileRangedKeepsSpacing()
        {
            var melee = new BattleEntityState { attackRange = 1f };
            var ranged = new BattleEntityState { attackRange = 2.2f };

            Assert.That(CombatSimulation.ResolveEngageDistance(melee), Is.LessThan(0.3f));
            Assert.That(CombatSimulation.ResolveAttackTriggerDistance(melee), Is.LessThan(0.3f));
            Assert.That(CombatSimulation.ResolveEngageDistance(ranged), Is.GreaterThan(1f));
            Assert.That(CombatSimulation.ResolveAttackTriggerDistance(ranged), Is.GreaterThan(ranged.attackRange));
        }

        [Test]
        public void CombatSimulation_EnemyWave_GrowsByYear_InsteadOfStartingAtFullPressure()
        {
            var earlyWave = CombatSimulation.BuildEnemyWaveArchetypes("king_blood", 1, false);
            var lateWave = CombatSimulation.BuildEnemyWaveArchetypes("king_blood", 8, false);
            var yearOneProfile = CombatSimulation.ResolveEnemyWaveProfile(1, "king_blood", false);
            var yearThreeProfile = CombatSimulation.ResolveEnemyWaveProfile(3, "king_blood", false);

            Assert.That(CombatSimulation.ResolveEnemyWaveCountForYear(1, false), Is.EqualTo(2));
            Assert.That(CombatSimulation.ResolveEnemyWaveCountForYear(8, false), Is.EqualTo(6));
            Assert.That(CombatSimulation.ResolveEnemyStackCountForYear(3, 1, false), Is.EqualTo(1));
            Assert.That(CombatSimulation.ResolveEnemyStackCountForYear(3, 3, false), Is.EqualTo(2));
            Assert.That(yearOneProfile.AllowDasher, Is.False);
            Assert.That(yearThreeProfile.AllowDasher, Is.False);
            Assert.That(yearOneProfile.HealthMultiplier, Is.EqualTo(0.70f).Within(0.001f));
            Assert.That(yearThreeProfile.GroupCount, Is.EqualTo(3));
            Assert.That(earlyWave.Count, Is.LessThan(lateWave.Count));
            Assert.That(earlyWave.Any(id => id == "enemy-dasher"), Is.False);
        }

        private static void AssertRectWithinDesign(UnityEngine.Rect rect)
        {
            Assert.That(IsRectWithinDesign(rect), Is.True, $"Rect out of design bounds: {rect}");
        }

        private static bool IsRectWithinDesign(UnityEngine.Rect rect)
        {
            return rect.xMin >= 0f
                && rect.yMin >= 0f
                && rect.xMax <= 1920f
                && rect.yMax <= 1080f;
        }
    }
}
