#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NineKingsPrototype.V2
{
    public sealed class NineKingsV2ScenePresenter : MonoBehaviour
    {
        private sealed class CellView
        {
            public GameObject gameObject = null!;
            public SpriteRenderer renderer = null!;
        }

        private sealed class StructureView
        {
            public GameObject gameObject = null!;
            public SpriteRenderer bodyRenderer = null!;
            public SpriteRenderer accentRenderer = null!;
        }

        private sealed class BattleEntityView
        {
            public GameObject gameObject = null!;
            public SpriteRenderer shadowRenderer = null!;
            public SpriteRenderer bodyRenderer = null!;
            public SpriteRenderer headRenderer = null!;
            public SpriteRenderer crestRenderer = null!;
            public SpriteRenderer weaponRenderer = null!;
            public List<SpriteRenderer> memberRenderers = new();
            public TextMesh stackLabel = null!;
        }

        [SerializeField] private NineKingsV2GameController? _controller;

        private readonly Dictionary<BoardCoord, CellView> _cellViews = new();
        private readonly Dictionary<BoardCoord, StructureView> _structureViews = new();
        private readonly Dictionary<string, BattleEntityView> _battleEntityViews = new();

        private Camera? _camera;
        private Transform? _worldBackgroundRoot;
        private Transform? _boardCellsRoot;
        private Transform? _placedStructuresRoot;
        private Transform? _battleUnitsRoot;
        private Transform? _worldPropsRoot;

        private string _selectedCardId = string.Empty;

        private static Sprite? s_SquareSprite;
        private static GUIStyle? s_TopValueStyle;
        private static GUIStyle? s_SmallLabelStyle;
        private static GUIStyle? s_HeaderStyle;
        private static GUIStyle? s_CardTitleStyle;
        private static GUIStyle? s_CardBodyStyle;
        private static GUIStyle? s_OverlayTitleStyle;
        private static GUIStyle? s_ButtonStyle;
        private static GUIStyle? s_SelectedButtonStyle;
        private static GUIStyle? s_PanelLabelStyle;
        private static GUIStyle? s_MutedLabelStyle;
        private static readonly Dictionary<string, Sprite?> s_RuntimeSpriteCache = new(StringComparer.Ordinal);

        private const float DesignWidth = 1920f;
        private const float DesignHeight = 1080f;

        public void SetController(NineKingsV2GameController controller)
        {
            _controller = controller;
        }

        private void Awake()
        {
            if (_controller == null)
            {
                _controller = GetComponent<NineKingsV2GameController>();
            }

            EnsureCamera();
            EnsureRoots();
            EnsureBackground();
            EnsureArenaProps();
            EnsureBoardCells();
        }

        private void Start()
        {
            SyncView();
        }

        private void Update()
        {
            HandleBoardInput();
        }

        private void LateUpdate()
        {
            UpdateCameraForPhase();
            SyncView();
        }

        private void OnGUI()
        {
            EnsureStyles();

            var scale = GetUiScale();
            var offset = GetUiOffset(scale);
            var previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(new Vector3(offset.x, offset.y, 0f), Quaternion.identity, new Vector3(scale, scale, 1f));

            if (_controller?.RunState == null)
            {
                GUI.Label(new Rect(32f, 28f, 360f, 32f), "NineKings V2 加载中...", s_HeaderStyle ?? GUI.skin.label);
                GUI.matrix = previousMatrix;
                return;
            }

            DrawTopLeftResources();
            DrawTopCenterTimeline();
            DrawTopRightControls();
            DrawLeftWellHint();
            DrawCompactStatus();
            DrawBottomHandRibbon();
            DrawBattleHint();
            DrawOverlayIfNeeded();

            GUI.matrix = previousMatrix;
        }

        private void HandleBoardInput()
        {
            if (_controller?.RunState == null || _camera == null || Mouse.current == null)
            {
                return;
            }

            var screen = Mouse.current.position.ReadValue();
            var scale = GetUiScale();
            var designPoint = ScreenToDesign(screen, scale);

            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                _selectedCardId = string.Empty;
                _controller.ClearPreview();
                return;
            }

            if (_controller.RunState.phase != RunPhase.CardPhase)
            {
                return;
            }

            if (string.IsNullOrEmpty(_selectedCardId))
            {
                return;
            }

            var wellRect = new Rect(96f, 486f, 182f, 182f);
            if (wellRect.Contains(designPoint))
            {
                _controller.SetPreview(_selectedCardId, null, true);
                if (Mouse.current.leftButton.wasPressedThisFrame && _controller.TryDiscardToWell(_selectedCardId))
                {
                    _selectedCardId = string.Empty;
                }
                return;
            }

            var insideBoard = designPoint.x > 300f && designPoint.x < 1548f && designPoint.y > 124f && designPoint.y < 744f;
            if (!insideBoard)
            {
                _controller.ClearPreview();
                return;
            }

            if (!TryGetHoveredCoord(screen, out var coord))
            {
                _controller.ClearPreview();
                return;
            }

            _controller.SetPreview(_selectedCardId, coord, false);
            if (Mouse.current.leftButton.wasPressedThisFrame && _controller.TryPlayCard(_selectedCardId, coord))
            {
                if (_controller.HandState.cardIds.Contains(_selectedCardId) == false)
                {
                    _selectedCardId = string.Empty;
                }
            }
        }

        private void DrawTopLeftResources()
        {
            var runState = _controller!.RunState!;
            GUI.Label(new Rect(26f, 16f, 160f, 34f), $"✦ {runState.gold}", s_TopValueStyle!);
            GUI.Label(new Rect(28f, 48f, 132f, 18f), $"命 {runState.lives}", s_MutedLabelStyle!);
        }

        private void DrawTopCenterTimeline()
        {
            var runState = _controller!.RunState!;
            var eventYears = new[] { 4, 6, 8, 10, 12, 14, 16, 19, 21, 23, 25, 27, 29, 31, 33 };
            var width = 468f;
            var startX = DesignWidth * 0.5f - width * 0.5f;
            var y = 16f;
            var spacing = width / (eventYears.Length - 1);

            for (var i = 0; i < eventYears.Length; i++)
            {
                var year = eventYears[i];
                var x = startX + i * spacing;
                var prev = GUI.color;
                GUI.color = year == runState.year
                    ? new Color(1f, 0.95f, 0.78f, 1f)
                    : year < runState.year
                        ? new Color(0.86f, 0.70f, 0.34f, 1f)
                        : new Color(1f, 1f, 1f, 0.24f);
                GUI.Box(new Rect(x - 5f, y, 10f, 10f), GUIContent.none);
                GUI.color = prev;
            }

            GUI.Label(new Rect(DesignWidth * 0.5f - 120f, 38f, 240f, 30f), $"YEAR {runState.year}", s_HeaderStyle!);
        }

        private void DrawTopRightControls()
        {
            var totalWidth = 214f;
            var baseX = DesignWidth - totalWidth - 28f;
            var y = 14f;
            var autoRect = new Rect(baseX, y, 60f, 28f);
            var pauseRect = new Rect(autoRect.xMax + 8f, y, 38f, 28f);
            var speed1Rect = new Rect(pauseRect.xMax + 10f, y, 28f, 28f);
            var speed2Rect = new Rect(speed1Rect.xMax + 6f, y, 28f, 28f);
            var speed4Rect = new Rect(speed2Rect.xMax + 6f, y, 36f, 28f);

            if (GUI.Button(autoRect, _controller!.AutoBattleEnabled ? "auto" : "手动", _controller.AutoBattleEnabled ? s_SelectedButtonStyle! : s_ButtonStyle!))
            {
                _controller.SetAutoBattleEnabled(!_controller.AutoBattleEnabled);
            }

            if (GUI.Button(pauseRect, _controller.IsPaused ? "▶" : "Ⅱ", _controller.IsPaused ? s_SelectedButtonStyle! : s_ButtonStyle!))
            {
                _controller.SetPausedState(!_controller.IsPaused);
            }

            if (GUI.Button(speed1Rect, "1", Mathf.Abs(_controller.BattleSpeedMultiplier - 1f) < 0.01f ? s_SelectedButtonStyle! : s_ButtonStyle!))
            {
                _controller.SetBattleSpeedMultiplier(1f);
            }
            if (GUI.Button(speed2Rect, "2", Mathf.Abs(_controller.BattleSpeedMultiplier - 2f) < 0.01f ? s_SelectedButtonStyle! : s_ButtonStyle!))
            {
                _controller.SetBattleSpeedMultiplier(2f);
            }
            if (GUI.Button(speed4Rect, "4", Mathf.Abs(_controller.BattleSpeedMultiplier - 4f) < 0.01f ? s_SelectedButtonStyle! : s_ButtonStyle!))
            {
                _controller.SetBattleSpeedMultiplier(4f);
            }
        }

        private void DrawLeftWellHint()
        {
            GUI.Label(new Rect(34f, 548f, 112f, 18f), "井 / Well", s_HeaderStyle!);
            if (_controller!.RunState!.phase == RunPhase.CardPhase && !string.IsNullOrEmpty(_selectedCardId))
            {
                GUI.Label(new Rect(18f, 568f, 228f, 32f), "拖到井里弃牌，获得 9 金币", s_MutedLabelStyle!);
            }
        }

        private void DrawCompactStatus()
        {
            var runState = _controller!.RunState!;
            var text = $"敌王 {GetKingShortName(runState.currentEnemyKingId)}  ·  {GetPhaseLabel(runState.phase)}  ·  手牌 {Mathf.Max(0, _controller.HandState.PlayableCount)}";
            GUI.Label(new Rect(DesignWidth * 0.5f - 250f, 72f, 500f, 20f), text, s_MutedLabelStyle!);
        }

        private void DrawBottomHandRibbon()
        {
            if (_controller!.RunState!.phase == RunPhase.BattleDeploy || _controller.RunState.phase == RunPhase.BattleRun || _controller.RunState.phase == RunPhase.BattleResolve || _controller.RunState.phase == RunPhase.LootChoice)
            {
                return;
            }

            var hand = _controller.HandState.cardIds;
            if (hand.Count == 0)
            {
                return;
            }

            var cardWidth = 176f;
            var cardHeight = 258f;
            var overlap = 144f;
            var totalWidth = cardWidth + Math.Max(0, hand.Count - 1) * overlap;
            var startX = DesignWidth * 0.5f - totalWidth * 0.5f;
            var baseY = 744f;
            var backgroundWidth = totalWidth + 136f;
            var backgroundX = DesignWidth * 0.5f - backgroundWidth * 0.5f;

            GUI.color = new Color(0f, 0f, 0f, 0.30f);
            GUI.Box(new Rect(backgroundX, 796f, backgroundWidth, 284f), GUIContent.none);
            GUI.color = Color.white;

            for (var i = 0; i < hand.Count; i++)
            {
                var cardId = hand[i];
                var isSelected = string.Equals(_selectedCardId, cardId, StringComparison.Ordinal);
                var rect = new Rect(startX + i * overlap, baseY - (isSelected ? 44f : 0f), cardWidth, cardHeight);
                DrawCardButton(rect, cardId, isSelected, i >= hand.Count - 2);
            }
        }

        private void DrawBattleHint()
        {
            if (_controller!.RunState!.phase != RunPhase.BattleDeploy && _controller.RunState.phase != RunPhase.BattleRun)
            {
                return;
            }

            GUI.Label(new Rect(118f, 100f, 700f, 22f), _controller.RunState.phase == RunPhase.BattleDeploy ? "列阵：我方左上驻防，敌军由右下压入；近战顶前，远程靠后。" : "战斗：敌军自右下推进，我方左上固守；可用右上角控速观察战局。", s_MutedLabelStyle!);
        }

        private void DrawOverlayIfNeeded()
        {
            var runState = _controller!.RunState!;
            if (runState.phase == RunPhase.LootChoice)
            {
                var choices = _controller.GetLootChoices();
                GUI.color = new Color(0f, 0f, 0f, 0.76f);
                GUI.Box(new Rect(0f, 0f, DesignWidth, DesignHeight), GUIContent.none);
                GUI.color = Color.white;
                GUI.Label(new Rect(DesignWidth * 0.5f - 160f, 124f, 320f, 36f), "BATTLE WON", s_OverlayTitleStyle!);
                GUI.Label(new Rect(DesignWidth * 0.5f - 220f, 162f, 440f, 28f), "选择一张战利品 / PICK YOUR LOOT", s_HeaderStyle!);
                for (var i = 0; i < choices.Count; i++)
                {
                    var rect = new Rect(DesignWidth * 0.5f - 330f + i * 230f, 232f, 204f, 288f);
                    DrawLootCard(rect, choices[i]);
                }
            }
            else if (runState.phase == RunPhase.RunOver)
            {
                GUI.color = new Color(0f, 0f, 0f, 0.78f);
                GUI.Box(new Rect(0f, 0f, DesignWidth, DesignHeight), GUIContent.none);
                GUI.color = Color.white;
                GUI.Label(new Rect(DesignWidth * 0.5f - 160f, DesignHeight * 0.5f - 44f, 320f, 36f), "本局结束", s_OverlayTitleStyle!);
                if (GUI.Button(new Rect(DesignWidth * 0.5f - 84f, DesignHeight * 0.5f + 10f, 168f, 36f), "重新开局", s_SelectedButtonStyle!))
                {
                    _controller.StartNewRun("king_greed");
                    _controller.EnterCardPhase();
                }
            }
        }

        private void DrawCardButton(Rect rect, string cardId, bool isSelected, bool markAutoBattle)
        {
            var background = new Color(0.36f, 0.15f, 0.05f, 0.98f);
            var header = GetCardWorldColor(cardId);
            var headerRect = new Rect(rect.x + 8f, rect.y + 12f, rect.width - 16f, 28f);
            var artRect = new Rect(rect.x + 12f, rect.y + 46f, rect.width - 24f, 112f);
            var textRect = new Rect(rect.x + 12f, rect.y + 166f, rect.width - 24f, 64f);

            var previousColor = GUI.color;
            GUI.color = isSelected ? new Color(1f, 0.94f, 0.78f, 1f) : background;
            GUI.Box(rect, GUIContent.none);
            GUI.color = header;
            GUI.Box(headerRect, GUIContent.none);
            GUI.color = new Color(header.r * 0.76f, header.g * 0.62f, header.b * 0.34f, 1f);
            GUI.Box(artRect, GUIContent.none);
            GUI.color = previousColor;

            GUI.Label(headerRect, GetCardName(cardId), s_CardTitleStyle!);
            GUI.Label(textRect, GetCardShortDescription(cardId), s_CardBodyStyle!);
            if (markAutoBattle)
            {
                GUI.Label(new Rect(rect.x + 8f, rect.y + rect.height - 22f, rect.width - 16f, 18f), "保留到两张后自动开战", s_MutedLabelStyle!);
            }

            if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
            {
                _selectedCardId = isSelected ? string.Empty : cardId;
            }
        }

        private void DrawLootCard(Rect rect, string cardId)
        {
            GUI.Box(rect, GUIContent.none);
            GUI.color = new Color(0.33f, 0.23f, 0.54f, 1f);
            GUI.Box(new Rect(rect.x + 10f, rect.y + 12f, rect.width - 20f, 34f), GUIContent.none);
            GUI.Box(new Rect(rect.x + 12f, rect.y + 56f, rect.width - 24f, 112f), GUIContent.none);
            GUI.color = Color.white;
            GUI.Label(new Rect(rect.x + 12f, rect.y + 14f, rect.width - 24f, 28f), GetCardName(cardId), s_CardTitleStyle!);
            GUI.Label(new Rect(rect.x + 16f, rect.y + 178f, rect.width - 32f, 72f), GetCardShortDescription(cardId), s_CardBodyStyle!);
            if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
            {
                _controller!.ResolveLootChoice(cardId);
            }
        }

        private void SyncView()
        {
            if (_controller?.RunState == null)
            {
                return;
            }

            SyncBoardCells();
            SyncPlacedStructures();
            SyncBattleEntities();
        }

        private void UpdateCameraForPhase()
        {
            if (_camera == null || _controller?.RunState == null)
            {
                return;
            }

            var runPhase = _controller.RunState.phase;
            var targetSize = runPhase == RunPhase.BattleDeploy || runPhase == RunPhase.BattleRun || runPhase == RunPhase.BattleResolve || runPhase == RunPhase.LootChoice
                ? 3.65f
                : 2.94f;
            var targetPosition = runPhase == RunPhase.BattleDeploy || runPhase == RunPhase.BattleRun || runPhase == RunPhase.BattleResolve || runPhase == RunPhase.LootChoice
                ? new Vector3(0.10f, 0.54f, -10f)
                : new Vector3(0.04f, 0.96f, -10f);

            _camera.orthographicSize = Mathf.Lerp(_camera.orthographicSize, targetSize, Time.unscaledDeltaTime * 6.5f);
            _camera.transform.position = Vector3.Lerp(_camera.transform.position, targetPosition, Time.unscaledDeltaTime * 6.5f);
        }

        private void SyncBoardCells()
        {
            var runState = _controller!.RunState!;
            var inBattle = runState.phase == RunPhase.BattleRun || runState.phase == RunPhase.BattleResolve || runState.phase == RunPhase.LootChoice;
            foreach (var plot in runState.plots)
            {
                if (!_cellViews.TryGetValue(plot.coord, out var cellView))
                {
                    continue;
                }

                cellView.renderer.color = plot.unlocked
                    ? (inBattle ? new Color(0.92f, 0.94f, 1f, 0.06f) : new Color(0.89f, 0.95f, 1f, 0.18f))
                    : new Color(0.34f, 0.31f, 0.26f, inBattle ? 0.02f : 0.06f);

                if (_controller.PlacementPreviewState.targetCoord.HasValue && _controller.PlacementPreviewState.targetCoord.Value.Equals(plot.coord) && !inBattle)
                {
                    cellView.renderer.color = _controller.PlacementPreviewState.isValid ? new Color(0.42f, 0.86f, 0.58f, 0.45f) : new Color(0.88f, 0.34f, 0.34f, 0.45f);
                }
            }
        }

        private void SyncPlacedStructures()
        {
            var runState = _controller!.RunState!;
            var combatVisible = runState.phase == RunPhase.BattleRun || runState.phase == RunPhase.BattleResolve || runState.phase == RunPhase.LootChoice || runState.phase == RunPhase.BattleDeploy;
            var occupiedPlots = runState.plots.Where(plot => plot.unlocked && !plot.IsEmpty).OrderBy(plot => plot.coord.y).ThenBy(plot => plot.coord.x).ToList();
            var occupiedCoords = new HashSet<BoardCoord>();

            for (var i = 0; i < occupiedPlots.Count; i++)
            {
                var plot = occupiedPlots[i];
                occupiedCoords.Add(plot.coord);
                if (!_structureViews.TryGetValue(plot.coord, out var view))
                {
                    view = CreateStructureView(plot.coord);
                    _structureViews[plot.coord] = view;
                }

                var presentation = _controller.Database?.GetPresentationConfig(plot.cardId);
                view.gameObject.SetActive(true);
                view.gameObject.transform.position = combatVisible ? GetStructureBattlePosition(plot, i) : CoordToWorld(plot.coord) + new Vector3(0f, 0.12f, -0.06f);
                UpdateStructureVisual(view, plot.cardId, presentation?.worldObjectType ?? WorldObjectType.Building, combatVisible);
            }

            foreach (var pair in _structureViews)
            {
                if (!occupiedCoords.Contains(pair.Key))
                {
                    pair.Value.gameObject.SetActive(false);
                }
            }
        }

        private void SyncBattleEntities()
        {
            var runState = _controller!.RunState!;
            var visibleBattle = runState.phase == RunPhase.BattleDeploy || runState.phase == RunPhase.BattleRun || runState.phase == RunPhase.BattleResolve || runState.phase == RunPhase.LootChoice;
            var visibleEntityIds = new HashSet<string>();

            if (visibleBattle)
            {
                foreach (var entity in _controller.GetVisibleBattleEntities())
                {
                    visibleEntityIds.Add(entity.entityId);
                    if (!_battleEntityViews.TryGetValue(entity.entityId, out var view))
                    {
                        view = CreateBattleEntityView(entity.entityId);
                        _battleEntityViews[entity.entityId] = view;
                    }

                    view.gameObject.SetActive(true);
                    view.gameObject.transform.position = BattleToWorld(entity);
                    UpdateBattleEntityVisual(view, entity);
                }
            }

            foreach (var pair in _battleEntityViews)
            {
                if (!visibleEntityIds.Contains(pair.Key))
                {
                    pair.Value.gameObject.SetActive(false);
                }
            }
        }

        private bool TryGetHoveredCoord(Vector2 screenPosition, out BoardCoord coord)
        {
            coord = default;
            if (_camera == null)
            {
                return false;
            }

            var world = _camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -_camera.transform.position.z));
            foreach (var plot in _controller!.RunState!.plots)
            {
                var center = CoordToWorld(plot.coord);
                var local = world - center;
                var diamond = Mathf.Abs(local.x) / 1.22f + Mathf.Abs(local.y) / 0.61f;
                if (diamond <= 1.02f)
                {
                    coord = plot.coord;
                    return true;
                }
            }

            return false;
        }

        private void EnsureCamera()
        {
            _camera = Camera.main;
            if (_camera == null)
            {
                _camera = FindAnyObjectByType<Camera>();
            }

            if (_camera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                _camera = cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
            }

            _camera.orthographic = true;
            _camera.orthographicSize = 2.94f;
            _camera.transform.position = new Vector3(0.04f, 0.96f, -10f);
            _camera.backgroundColor = new Color(0.86f, 0.73f, 0.48f, 1f);
        }

        private void EnsureRoots()
        {
            _worldBackgroundRoot = EnsureChild("WorldBackground");
            _boardCellsRoot = EnsureChild("BoardCells");
            _placedStructuresRoot = EnsureChild("PlacedStructures");
            _battleUnitsRoot = EnsureChild("BattleUnits");
            _worldPropsRoot = EnsureChild("WorldProps");
        }

        private void EnsureBackground()
        {
            if (_worldBackgroundRoot == null || _worldBackgroundRoot.childCount > 0)
            {
                return;
            }

            var ground = CreateSpriteObject("ArenaGround", _worldBackgroundRoot, new Vector3(0.18f, 0.72f, 4f), new Vector3(12.8f, 7.8f, 1f), new Color(0.89f, 0.77f, 0.53f, 1f));
            ground.sprite.sortingOrder = -30;
            CreateWall("TopWall", new Vector3(0.42f, 3.30f, 0f), new Vector3(12.0f, 0.76f, 1f));
            CreateWall("RightWall", new Vector3(4.84f, 1.82f, 0f), new Vector3(1.18f, 4.12f, 1f));
            CreateWall("LeftCorner", new Vector3(-4.62f, -1.42f, 0f), new Vector3(1.26f, 1.82f, 1f));
        }

        private void EnsureArenaProps()
        {
            if (_worldPropsRoot == null || _worldPropsRoot.childCount > 0)
            {
                return;
            }

            CreateProp("Well", new Vector3(-4.72f, -0.14f, -0.1f), new Vector3(1.08f, 1.08f, 1f), new Color(0.64f, 0.73f, 0.83f, 1f));
            CreateProp("StatueLeft", new Vector3(-4.04f, 2.28f, -0.1f), new Vector3(1.22f, 0.84f, 1f), new Color(0.52f, 0.40f, 0.24f, 1f));
            CreateProp("StoneA", new Vector3(-5.12f, 0.48f, -0.2f), new Vector3(0.56f, 0.34f, 1f), new Color(0.47f, 0.39f, 0.24f, 1f));
            CreateProp("StoneB", new Vector3(5.12f, 1.02f, -0.2f), new Vector3(0.56f, 0.34f, 1f), new Color(0.47f, 0.39f, 0.24f, 1f));
            CreateProp("StoneC", new Vector3(5.08f, -2.36f, -0.2f), new Vector3(0.54f, 0.34f, 1f), new Color(0.47f, 0.39f, 0.24f, 1f));
            CreateProp("Coins", new Vector3(-3.44f, 2.96f, -0.18f), new Vector3(0.24f, 0.24f, 1f), new Color(0.96f, 0.84f, 0.32f, 1f));
        }

        private void EnsureBoardCells()
        {
            if (_boardCellsRoot == null || _cellViews.Count > 0)
            {
                return;
            }

            for (var y = 0; y < 5; y++)
            {
                for (var x = 0; x < 5; x++)
                {
                    var coord = new BoardCoord(x, y);
                    var gameObject = new GameObject($"Cell_{x}_{y}");
                    gameObject.transform.SetParent(_boardCellsRoot, false);
                    gameObject.transform.position = CoordToWorld(coord);
                    gameObject.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                    gameObject.transform.localScale = new Vector3(1.86f, 0.96f, 1f);

                    var renderer = gameObject.AddComponent<SpriteRenderer>();
                    renderer.sprite = GetSquareSprite();
                    renderer.sortingOrder = -5;
                    renderer.color = new Color(0.90f, 0.94f, 1f, 0.18f);

                    _cellViews[coord] = new CellView { gameObject = gameObject, renderer = renderer };
                }
            }
        }

        private StructureView CreateStructureView(BoardCoord coord)
        {
            var gameObject = new GameObject($"Structure_{coord.x}_{coord.y}");
            gameObject.transform.SetParent(_placedStructuresRoot, false);

            var bodyObject = new GameObject("Body");
            bodyObject.transform.SetParent(gameObject.transform, false);
            var bodyRenderer = bodyObject.AddComponent<SpriteRenderer>();
            bodyRenderer.sprite = GetSquareSprite();
            bodyRenderer.sortingOrder = 10;

            var accentObject = new GameObject("Accent");
            accentObject.transform.SetParent(gameObject.transform, false);
            var accentRenderer = accentObject.AddComponent<SpriteRenderer>();
            accentRenderer.sprite = GetSquareSprite();
            accentRenderer.sortingOrder = 11;

            return new StructureView { gameObject = gameObject, bodyRenderer = bodyRenderer, accentRenderer = accentRenderer };
        }

        private BattleEntityView CreateBattleEntityView(string entityId)
        {
            var gameObject = new GameObject($"Battle_{entityId}");
            gameObject.transform.SetParent(_battleUnitsRoot, false);

            var shadowObject = new GameObject("Shadow");
            shadowObject.transform.SetParent(gameObject.transform, false);
            shadowObject.transform.localPosition = new Vector3(0f, -0.20f, 0f);
            var shadowRenderer = shadowObject.AddComponent<SpriteRenderer>();
            shadowRenderer.sprite = GetSquareSprite();
            shadowRenderer.sortingOrder = 18;
            shadowRenderer.color = new Color(0f, 0f, 0f, 0.22f);

            var bodyObject = new GameObject("Body");
            bodyObject.transform.SetParent(gameObject.transform, false);
            var bodyRenderer = bodyObject.AddComponent<SpriteRenderer>();
            bodyRenderer.sprite = GetSquareSprite();
            bodyRenderer.sortingOrder = 22;

            var headObject = new GameObject("Head");
            headObject.transform.SetParent(gameObject.transform, false);
            headObject.transform.localPosition = new Vector3(0f, 0.26f, 0f);
            var headRenderer = headObject.AddComponent<SpriteRenderer>();
            headRenderer.sprite = GetSquareSprite();
            headRenderer.sortingOrder = 24;

            var crestObject = new GameObject("Crest");
            crestObject.transform.SetParent(gameObject.transform, false);
            crestObject.transform.localPosition = new Vector3(0f, 0.48f, 0f);
            var crestRenderer = crestObject.AddComponent<SpriteRenderer>();
            crestRenderer.sprite = GetSquareSprite();
            crestRenderer.sortingOrder = 25;

            var weaponObject = new GameObject("Weapon");
            weaponObject.transform.SetParent(gameObject.transform, false);
            weaponObject.transform.localPosition = new Vector3(0.22f, 0.10f, 0f);
            var weaponRenderer = weaponObject.AddComponent<SpriteRenderer>();
            weaponRenderer.sprite = GetSquareSprite();
            weaponRenderer.sortingOrder = 23;

            var members = new List<SpriteRenderer>();
            for (var i = 0; i < 10; i++)
            {
                var memberObject = new GameObject($"Member_{i}");
                memberObject.transform.SetParent(gameObject.transform, false);
                var memberRenderer = memberObject.AddComponent<SpriteRenderer>();
                memberRenderer.sprite = GetSquareSprite();
                memberRenderer.sortingOrder = 21;
                members.Add(memberRenderer);
            }

            var labelObject = new GameObject("StackLabel");
            labelObject.transform.SetParent(gameObject.transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 0.70f, 0f);
            var label = labelObject.AddComponent<TextMesh>();
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = 0.11f;
            label.fontSize = 34;
            label.color = Color.white;

            return new BattleEntityView
            {
                gameObject = gameObject,
                shadowRenderer = shadowRenderer,
                bodyRenderer = bodyRenderer,
                headRenderer = headRenderer,
                crestRenderer = crestRenderer,
                weaponRenderer = weaponRenderer,
                memberRenderers = members,
                stackLabel = label,
            };
        }

        private void UpdateStructureVisual(StructureView view, string cardId, WorldObjectType worldObjectType, bool battleVisible)
        {
            var primary = GetCardWorldColor(cardId);
            var accent = new Color(Mathf.Clamp01(primary.r * 0.70f), Mathf.Clamp01(primary.g * 0.62f), Mathf.Clamp01(primary.b * 0.54f), 1f);
            var sprite = GetWorldSprite(cardId, worldObjectType);
            if (sprite != null)
            {
                view.bodyRenderer.sprite = sprite;
                view.bodyRenderer.color = Color.white;
                view.accentRenderer.gameObject.SetActive(false);
                view.bodyRenderer.transform.localPosition = Vector3.zero;
                view.bodyRenderer.transform.localRotation = Quaternion.identity;
                view.bodyRenderer.transform.localScale = GetStructureSpriteScale(cardId, battleVisible);
                return;
            }

            view.accentRenderer.gameObject.SetActive(true);
            view.bodyRenderer.sprite = GetSquareSprite();
            view.bodyRenderer.color = primary;
            view.accentRenderer.color = accent;

            switch (worldObjectType)
            {
                case WorldObjectType.Palace:
                    view.bodyRenderer.transform.localScale = battleVisible ? new Vector3(1.56f, 1.04f, 1f) : new Vector3(0.88f, 0.60f, 1f);
                    view.accentRenderer.transform.localScale = battleVisible ? new Vector3(0.76f, 0.24f, 1f) : new Vector3(0.30f, 0.14f, 1f);
                    view.accentRenderer.transform.localPosition = battleVisible ? new Vector3(0f, 0.48f, 0f) : new Vector3(0f, 0.22f, 0f);
                    break;
                case WorldObjectType.Tower:
                    view.bodyRenderer.transform.localScale = battleVisible ? new Vector3(0.72f, 1.18f, 1f) : new Vector3(0.46f, 0.80f, 1f);
                    view.accentRenderer.transform.localScale = battleVisible ? new Vector3(0.22f, 0.22f, 1f) : new Vector3(0.14f, 0.14f, 1f);
                    view.accentRenderer.transform.localPosition = battleVisible ? new Vector3(0f, 0.44f, 0f) : new Vector3(0f, 0.30f, 0f);
                    break;
                case WorldObjectType.UnitSource:
                    view.bodyRenderer.transform.localScale = battleVisible ? new Vector3(0.86f, 0.58f, 1f) : new Vector3(0.54f, 0.36f, 1f);
                    view.accentRenderer.transform.localScale = battleVisible ? new Vector3(0.24f, 0.16f, 1f) : new Vector3(0.14f, 0.10f, 1f);
                    view.accentRenderer.transform.localPosition = battleVisible ? new Vector3(0f, 0.22f, 0f) : new Vector3(0f, 0.14f, 0f);
                    break;
                default:
                    view.bodyRenderer.transform.localScale = new Vector3(0.42f, 0.42f, 1f);
                    view.accentRenderer.transform.localScale = new Vector3(0.16f, 0.16f, 1f);
                    view.accentRenderer.transform.localPosition = new Vector3(0f, 0.16f, 0f);
                    break;
            }
        }

        private void UpdateBattleEntityVisual(BattleEntityView view, BattleEntityState entity)
        {
            var ranged = entity.attackRange >= 1.6f;
            var baseColor = GetEntityColor(entity);
            view.shadowRenderer.color = new Color(0f, 0f, 0f, 0.22f);
            view.shadowRenderer.transform.localScale = ranged ? new Vector3(1.10f, 0.26f, 1f) : new Vector3(1.30f, 0.30f, 1f);

            var unitSprite = GetUnitSprite(entity);
            if (unitSprite != null)
            {
                view.bodyRenderer.sprite = unitSprite;
                view.bodyRenderer.color = Color.white;
                view.bodyRenderer.gameObject.SetActive(false);
                view.headRenderer.gameObject.SetActive(false);
                view.crestRenderer.gameObject.SetActive(false);
                view.weaponRenderer.gameObject.SetActive(false);

                var formation = GetSpriteFormationOffsets(ranged);
                var visibleCount = Mathf.Clamp(entity.stackCount, 1, formation.Length);
                for (var i = 0; i < view.memberRenderers.Count; i++)
                {
                    var active = i < visibleCount;
                    view.memberRenderers[i].gameObject.SetActive(active);
                    if (!active)
                    {
                        continue;
                    }

                    view.memberRenderers[i].sprite = unitSprite;
                    view.memberRenderers[i].color = Color.white;
                    view.memberRenderers[i].transform.localPosition = formation[i];
                    view.memberRenderers[i].transform.localScale = ranged ? new Vector3(1.46f, 1.46f, 1f) : new Vector3(1.58f, 1.58f, 1f);
                }

                view.stackLabel.text = entity.stackCount > 1 ? $"{entity.stackCount}" : string.Empty;
                view.stackLabel.characterSize = 0.10f;
                view.stackLabel.fontSize = 42;
                view.stackLabel.color = entity.isEnemy ? new Color(1f, 0.82f, 0.88f, 1f) : new Color(1f, 0.94f, 0.72f, 1f);
                view.stackLabel.transform.localPosition = new Vector3(0f, 1.12f, 0f);
                view.stackLabel.gameObject.SetActive(entity.stackCount > 1);
                return;
            }

            view.bodyRenderer.gameObject.SetActive(true);
            var memberColor = new Color(baseColor.r * 0.94f, baseColor.g * 0.94f, baseColor.b * 0.94f, 1f);
            view.bodyRenderer.sprite = GetSquareSprite();
            view.bodyRenderer.color = baseColor;
            view.headRenderer.gameObject.SetActive(true);
            view.crestRenderer.gameObject.SetActive(true);
            view.weaponRenderer.gameObject.SetActive(true);
            view.headRenderer.color = entity.isEnemy ? new Color(0.18f, 0.10f, 0.16f, 1f) : new Color(0.96f, 0.88f, 0.74f, 1f);
            view.crestRenderer.color = ranged ? new Color(0.94f, 0.96f, 1f, 1f) : new Color(0.90f, 0.76f, 0.34f, 1f);
            view.weaponRenderer.color = ranged ? new Color(0.94f, 0.92f, 0.78f, 1f) : new Color(0.58f, 0.34f, 0.16f, 1f);
            view.bodyRenderer.transform.localScale = ranged ? new Vector3(0.34f, 0.52f, 1f) : new Vector3(0.42f, 0.64f, 1f);
            view.headRenderer.transform.localScale = new Vector3(0.18f, 0.18f, 1f);
            view.crestRenderer.transform.localScale = ranged ? new Vector3(0.22f, 0.06f, 1f) : new Vector3(0.08f, 0.18f, 1f);
            view.weaponRenderer.transform.localScale = ranged ? new Vector3(0.44f, 0.06f, 1f) : new Vector3(0.08f, 0.42f, 1f);
            view.weaponRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, ranged ? -12f : -18f);

            var fallbackFormation = GetFormationOffsets(ranged);
            var fallbackCount = Mathf.Clamp(entity.stackCount, 3, fallbackFormation.Length + 1);
            for (var i = 0; i < view.memberRenderers.Count; i++)
            {
                var active = i < fallbackCount - 1;
                view.memberRenderers[i].gameObject.SetActive(active);
                if (!active)
                {
                    continue;
                }

                view.memberRenderers[i].sprite = GetSquareSprite();
                view.memberRenderers[i].transform.localPosition = fallbackFormation[i];
                view.memberRenderers[i].transform.localScale = ranged ? new Vector3(0.26f, 0.40f, 1f) : new Vector3(0.30f, 0.46f, 1f);
                view.memberRenderers[i].color = memberColor;
            }

            view.stackLabel.text = entity.stackCount > 1 ? $"x{entity.stackCount}" : string.Empty;
            view.stackLabel.gameObject.SetActive(entity.stackCount > 1);
        }

        private static Vector3[] GetSpriteFormationOffsets(bool ranged)
        {
            if (ranged)
            {
                return new[]
                {
                    new Vector3(-0.78f, -0.06f, 0f),
                    new Vector3(0.78f, -0.06f, 0f),
                    new Vector3(0f, -0.48f, 0f),
                    new Vector3(-1.08f, -0.36f, 0f),
                    new Vector3(1.08f, -0.36f, 0f),
                    new Vector3(-0.42f, 0.38f, 0f),
                    new Vector3(0.42f, 0.38f, 0f),
                    new Vector3(-1.30f, 0.18f, 0f),
                    new Vector3(1.30f, 0.18f, 0f),
                };
            }

            return new[]
            {
                new Vector3(-0.74f, 0.08f, 0f),
                new Vector3(0.74f, 0.08f, 0f),
                new Vector3(-0.32f, -0.44f, 0f),
                new Vector3(0.32f, -0.44f, 0f),
                new Vector3(-1.02f, 0.28f, 0f),
                new Vector3(1.02f, 0.28f, 0f),
                new Vector3(0f, 0.44f, 0f),
                new Vector3(0f, -0.68f, 0f),
                new Vector3(-1.24f, -0.18f, 0f),
                new Vector3(1.24f, -0.18f, 0f),
            };
        }

        private static Vector3[] GetFormationOffsets(bool ranged)
        {
            if (ranged)
            {
                return new[]
                {
                    new Vector3(-0.34f, -0.02f, 0f),
                    new Vector3(0.34f, -0.02f, 0f),
                    new Vector3(0f, -0.22f, 0f),
                    new Vector3(-0.46f, -0.22f, 0f),
                    new Vector3(0.46f, -0.22f, 0f),
                    new Vector3(-0.16f, 0.18f, 0f),
                    new Vector3(0.16f, 0.18f, 0f),
                    new Vector3(-0.54f, 0.10f, 0f),
                    new Vector3(0.54f, 0.10f, 0f),
                    new Vector3(0f, 0.30f, 0f),
                };
            }

            return new[]
            {
                new Vector3(-0.30f, 0.00f, 0f),
                new Vector3(0.30f, 0.00f, 0f),
                new Vector3(-0.14f, -0.22f, 0f),
                new Vector3(0.14f, -0.22f, 0f),
                new Vector3(-0.46f, 0.18f, 0f),
                new Vector3(0.46f, 0.18f, 0f),
                new Vector3(0f, 0.22f, 0f),
                new Vector3(0f, -0.38f, 0f),
                new Vector3(-0.58f, -0.08f, 0f),
                new Vector3(0.58f, -0.08f, 0f),
            };
        }

        private static Color GetEntityColor(BattleEntityState entity)
        {
            if (entity.isEnemy)
            {
                return entity.unitArchetypeId.Contains("boss", StringComparison.Ordinal)
                    ? new Color(0.72f, 0.18f, 0.54f, 1f)
                    : new Color(0.46f, 0.10f, 0.26f, 1f);
            }

            if (entity.sourceCardId.StartsWith("nothing", StringComparison.Ordinal) || entity.unitArchetypeId.StartsWith("nothing", StringComparison.Ordinal))
            {
                return new Color(0.80f, 0.90f, 1f, 1f);
            }

            return new Color(1f, 0.92f, 0.74f, 1f);
        }

        private Sprite? GetWorldSprite(string cardId, WorldObjectType worldObjectType)
        {
            return cardId switch
            {
                "greed_palace" => LoadFullSprite("Tiny Swords/Buildings/Yellow Buildings/Castle.png", "structure-greed-palace"),
                "greed_vault" => LoadFullSprite("Tiny Swords/Buildings/Yellow Buildings/House1.png", "structure-greed-vault"),
                "greed_dispenser" => LoadFullSprite("Tiny Swords/Buildings/Yellow Buildings/Tower.png", "structure-greed-dispenser"),
                "greed_beacon" => LoadFullSprite("Tiny Swords/Buildings/Yellow Buildings/Tower.png", "structure-greed-beacon"),
                "nothing_castle" => LoadFullSprite("Tiny Swords/Buildings/Blue Buildings/Castle.png", "structure-nothing-castle"),
                "nothing_scout_tower" => LoadFullSprite("Tiny Swords/Buildings/Blue Buildings/Tower.png", "structure-nothing-scout-tower"),
                "nothing_blacksmith" => LoadFullSprite("Tiny Swords/Buildings/Blue Buildings/House1.png", "structure-nothing-blacksmith"),
                "nothing_farm" => LoadFullSprite("Tiny Swords/Buildings/Blue Buildings/House1.png", "structure-nothing-farm"),
                _ => worldObjectType == WorldObjectType.Palace ? LoadFullSprite("Tiny Swords/Buildings/Yellow Buildings/Castle.png", "structure-fallback-palace") : null,
            };
        }

        private Sprite? GetUnitSprite(BattleEntityState entity)
        {
            if (entity.isEnemy)
            {
                return entity.attackRange >= 1.6f
                    ? LoadSheetFrame("Tiny Swords/Units/Purple Units/Archer/Archer_Run.png", "unit-enemy-ranged", 6, 1, 1, 0)
                    : LoadSheetFrame("Tiny Swords/Units/Purple Units/Warrior/Warrior_Run.png", "unit-enemy-melee", 8, 1, 1, 0);
            }

            var greed = entity.sourceCardId.StartsWith("greed", StringComparison.Ordinal) || entity.unitArchetypeId.StartsWith("greed", StringComparison.Ordinal);
            if (greed)
            {
                return entity.attackRange >= 1.6f
                    ? LoadSheetFrame("Tiny Swords/Units/Yellow Units/Archer/Archer_Run.png", "unit-greed-ranged", 6, 1, 1, 0)
                    : LoadSheetFrame("Tiny Swords/Units/Yellow Units/Warrior/Warrior_Run.png", "unit-greed-melee", 8, 1, 1, 0);
            }

            return entity.attackRange >= 1.6f
                ? LoadSheetFrame("Tiny Swords/Units/Blue Units/Archer/Archer_Run.png", "unit-nothing-ranged", 6, 1, 1, 0)
                : LoadSheetFrame("Tiny Swords/Units/Blue Units/Warrior/Warrior_Run.png", "unit-nothing-melee", 8, 1, 1, 0);
        }

        private static Vector3 GetStructureSpriteScale(string cardId, bool battleVisible)
        {
            return cardId switch
            {
                "greed_palace" => battleVisible ? new Vector3(1.76f, 1.76f, 1f) : new Vector3(0.92f, 0.92f, 1f),
                "nothing_castle" => battleVisible ? new Vector3(1.48f, 1.48f, 1f) : new Vector3(0.88f, 0.88f, 1f),
                "greed_dispenser" or "greed_beacon" or "nothing_scout_tower" => battleVisible ? new Vector3(1.36f, 1.36f, 1f) : new Vector3(0.68f, 0.68f, 1f),
                _ => battleVisible ? new Vector3(1.14f, 1.14f, 1f) : new Vector3(0.60f, 0.60f, 1f),
            };
        }

        private static Sprite? LoadFullSprite(string assetRelativePath, string cacheKey)
        {
            if (s_RuntimeSpriteCache.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }

            var fullPath = Path.Combine(Application.dataPath, assetRelativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(fullPath))
            {
                s_RuntimeSpriteCache[cacheKey] = null;
                return null;
            }

            var bytes = File.ReadAllBytes(fullPath);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = cacheKey,
            };
            if (!texture.LoadImage(bytes))
            {
                UnityEngine.Object.Destroy(texture);
                s_RuntimeSpriteCache[cacheKey] = null;
                return null;
            }

            var sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.08f), 100f);
            s_RuntimeSpriteCache[cacheKey] = sprite;
            return sprite;
        }

        private static Sprite? LoadSheetFrame(string assetRelativePath, string cacheKey, int columns, int rows, int column, int rowFromTop)
        {
            if (s_RuntimeSpriteCache.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }

            var fullPath = Path.Combine(Application.dataPath, assetRelativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(fullPath))
            {
                s_RuntimeSpriteCache[cacheKey] = null;
                return null;
            }

            var bytes = File.ReadAllBytes(fullPath);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = cacheKey,
            };
            if (!texture.LoadImage(bytes))
            {
                UnityEngine.Object.Destroy(texture);
                s_RuntimeSpriteCache[cacheKey] = null;
                return null;
            }

            var cellWidth = texture.width / columns;
            var cellHeight = texture.height / rows;
            var yFromBottom = rows - 1 - rowFromTop;
            var rect = new Rect(column * cellWidth, yFromBottom * cellHeight, cellWidth, cellHeight);
            var sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.08f), 100f);
            s_RuntimeSpriteCache[cacheKey] = sprite;
            return sprite;
        }

        private void CreateWall(string name, Vector3 position, Vector3 scale)
        {
            var wall = CreateSpriteObject(name, _worldBackgroundRoot!, position, scale, new Color(0.56f, 0.41f, 0.23f, 1f));
            wall.sprite.sortingOrder = -26;
        }

        private Transform EnsureChild(string name)
        {
            var child = transform.Find(name);
            if (child != null)
            {
                return child;
            }

            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(transform, false);
            return gameObject.transform;
        }

        private (GameObject gameObject, SpriteRenderer sprite) CreateSpriteObject(string name, Transform parent, Vector3 position, Vector3 scale, Color color)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = position;
            gameObject.transform.localScale = scale;
            var renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = GetSquareSprite();
            renderer.color = color;
            return (gameObject, renderer);
        }

        private void CreateProp(string name, Vector3 position, Vector3 scale, Color color)
        {
            var prop = CreateSpriteObject(name, _worldPropsRoot!, position, scale, color);
            prop.sprite.sortingOrder = -4;
        }

        private void EnsureStyles()
        {
            if (s_HeaderStyle != null)
            {
                return;
            }

            s_TopValueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(1f, 0.96f, 0.84f, 1f) },
            };
            s_SmallLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.96f, 0.93f, 0.84f, 1f) },
            };
            s_MutedLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.95f, 0.92f, 0.84f, 0.92f) },
            };
            s_PanelLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true,
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = new Color(0.96f, 0.93f, 0.84f, 1f) },
            };
            s_HeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white },
            };
            s_CardTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                normal = { textColor = new Color(1f, 0.97f, 0.90f, 1f) },
            };
            s_CardBodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                alignment = TextAnchor.UpperCenter,
                wordWrap = true,
                normal = { textColor = new Color(1f, 0.95f, 0.85f, 1f) },
            };
            s_OverlayTitleStyle = new GUIStyle(s_HeaderStyle) { fontSize = 28 };
            s_ButtonStyle = new GUIStyle(GUI.skin.button) { fontSize = 12, alignment = TextAnchor.MiddleCenter };
            s_SelectedButtonStyle = new GUIStyle(s_ButtonStyle) { fontStyle = FontStyle.Bold };
        }

        private static Sprite GetSquareSprite()
        {
            if (s_SquareSprite != null)
            {
                return s_SquareSprite;
            }

            s_SquareSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height), new Vector2(0.5f, 0.5f), 100f);
            return s_SquareSprite;
        }

        private static float GetUiScale()
        {
            return Mathf.Min(Screen.width / DesignWidth, Screen.height / DesignHeight);
        }

        private static Vector2 GetUiOffset(float scale)
        {
            return new Vector2((Screen.width - DesignWidth * scale) * 0.5f, (Screen.height - DesignHeight * scale) * 0.5f);
        }

        private static Vector2 ScreenToDesign(Vector2 screenPoint, float scale)
        {
            var offset = GetUiOffset(scale);
            return new Vector2((screenPoint.x - offset.x) / scale, (screenPoint.y - offset.y) / scale);
        }

        private Vector3 CoordToWorld(BoardCoord coord)
        {
            var dx = coord.x - 2;
            var dy = coord.y - 2;
            return new Vector3(0.30f + (dx - dy) * 1.08f, 1.20f + (dx + dy) * 0.56f, 0f);
        }

        private static Vector3 BattleToWorld(BattleEntityState entity)
        {
            return new Vector3(entity.worldX, entity.worldY, entity.isEnemy ? -0.18f : -0.22f);
        }

        private Vector3 GetStructureBattlePosition(PlotState plot, int index)
        {
            var card = _controller!.Database?.GetCard(plot.cardId);
            if (card?.cardType == CardType.Base)
            {
                return new Vector3(-4.28f, 2.36f, -0.1f);
            }

            var positions = new[]
            {
                new Vector3(-5.08f, 2.82f, -0.1f),
                new Vector3(-3.70f, 2.78f, -0.1f),
                new Vector3(-2.82f, 2.40f, -0.1f),
                new Vector3(-4.78f, 1.72f, -0.1f),
                new Vector3(-3.42f, 1.66f, -0.1f),
                new Vector3(-5.24f, 3.30f, -0.1f),
            };
            return positions[index % positions.Length];
        }

        private static string GetPhaseLabel(RunPhase phase)
        {
            return phase switch
            {
                RunPhase.MainMenu => "主菜单",
                RunPhase.RunIntro => "开局",
                RunPhase.YearStart => "年开始",
                RunPhase.CardPhase => "出牌",
                RunPhase.PlacementPreview => "预览",
                RunPhase.BattleDeploy => "列阵",
                RunPhase.BattleRun => "战斗",
                RunPhase.BattleResolve => "结算",
                RunPhase.LootChoice => "战利品",
                RunPhase.EventModal => "事件",
                RunPhase.RunOver => "结束",
                _ => phase.ToString(),
            };
        }

        private string GetCardName(string cardId)
        {
            return _controller?.Database?.GetCard(cardId)?.displayName.Get(true) ?? cardId;
        }

        private string GetKingShortName(string kingId)
        {
            return kingId switch
            {
                "king_greed" => "贪欲",
                "king_nothing" => "虚无",
                "king_blood" => "鲜血",
                "king_nature" => "自然",
                _ => kingId,
            };
        }

        private string GetCardShortDescription(string cardId)
        {
            var card = _controller?.Database?.GetCard(cardId);
            if (card == null)
            {
                return cardId;
            }

            return card.cardType switch
            {
                CardType.Base => "王国核心，负责驻守与胜负结算。",
                CardType.Troop => "会出兵并参与自动战斗。",
                CardType.Tower => "固定射击，提供持续火力支援。",
                CardType.Building => "驻场建筑，提供资源或战斗支援。",
                CardType.Enchantment => "附着到现有地块，强化目标。",
                CardType.Tome => "即时效果牌，不会持续驻场。",
                _ => card.displayName.Get(true),
            };
        }

        private static Color GetCardWorldColor(string cardId)
        {
            if (cardId.StartsWith("greed", StringComparison.Ordinal))
            {
                return new Color(0.70f, 0.42f, 0.10f, 1f);
            }
            if (cardId.StartsWith("nothing", StringComparison.Ordinal))
            {
                return new Color(0.28f, 0.54f, 0.88f, 1f);
            }
            return new Color(0.62f, 0.62f, 0.62f, 1f);
        }
    }
}
