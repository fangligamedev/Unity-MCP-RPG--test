#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NineKingsPrototype.V2
{
    public sealed class NineKingsV2ScenePresenter : MonoBehaviour
    {
        private sealed class CellEdgeView
        {
            public SpriteRenderer renderer = null!;
            public Vector3 localPosition;
            public Quaternion localRotation;
            public Vector3 localScale;
        }

        private sealed class CellView
        {
            public GameObject gameObject = null!;
            public SpriteRenderer fillRenderer = null!;
            public List<CellEdgeView> edgeViews = new();
        }

        private sealed class StructureView
        {
            public GameObject gameObject = null!;
            public SpriteRenderer bodyRenderer = null!;
            public SpriteRenderer accentRenderer = null!;
            public List<SpriteRenderer> memberRenderers = new();
            public TextMesh countLabel = null!;
        }

        internal readonly struct BoardGridSegment
        {
            public BoardGridSegment(Vector2 start, Vector2 end)
            {
                Start = start;
                End = end;
            }

            public Vector2 Start { get; }
            public Vector2 End { get; }
            public Vector2 Midpoint => (Start + End) * 0.5f;
            public float Length => Vector2.Distance(Start, End);
            public float AngleDegrees => Mathf.Atan2(End.y - Start.y, End.x - Start.x) * Mathf.Rad2Deg;
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
            public List<Animator> memberAnimators = new();
            public TextMesh stackLabel = null!;
        }

        internal enum BattleSide
        {
            Friendly,
            Enemy,
        }

        internal enum BattleVisualState
        {
            Hidden,
            Idle,
            Run,
            Attack,
            Shoot,
            Fallback,
        }

        private sealed class BattleEntityVisualCache
        {
            public bool initialized;
            public float previousWorldX;
            public float previousWorldY;
            public float previousAttackTimer;
            public float lastAttackVisualTime = -10f;
            public BattleVisualState currentState = BattleVisualState.Hidden;
            public string animatorKey = string.Empty;
            public bool usesAnimator;
        }

        [SerializeField] private NineKingsV2GameController? _controller;

        private readonly Dictionary<BoardCoord, CellView> _cellViews = new();
        private readonly Dictionary<BoardCoord, StructureView> _structureViews = new();
        private readonly Dictionary<string, BattleEntityView> _battleEntityViews = new();
        private readonly Dictionary<string, BattleEntityVisualCache> _battleEntityVisualCaches = new();

        private Camera? _camera;
        private Transform? _worldBackgroundRoot;
        private Transform? _boardCellsRoot;
        private Transform? _boardGridRoot;
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
        private static GUIStyle? s_TooltipTitleStyle;
        private static GUIStyle? s_TooltipBodyStyle;
        private static GUIStyle? s_TooltipStatStyle;
        private static readonly Dictionary<string, Sprite?> s_RuntimeSpriteCache = new(StringComparer.Ordinal);
        private static readonly Dictionary<string, RuntimeAnimatorController?> s_RuntimeAnimatorControllerCache = new(StringComparer.Ordinal);

        private const float DesignWidth = 1920f;
        private const float DesignHeight = 1080f;
        private const float CardCameraSize = 5.20f;
        private const float BattleCameraSize = 6.10f;
        private const int MaxDisplayedUnitMembers = 10;
        private const int MaxDisplayedMapUnitMembers = 6;
        private const float BoardCellHalfWidth = 1.08f;
        private const float BoardCellHalfHeight = 0.56f;
        private static readonly Vector3 s_BoardCellFillScale = new(1.50f, 0.76f, 1f);
        private static readonly Vector3 s_BoardCellEdgeScale = new(1.22f, 0.11f, 1f);
        private static readonly Vector3 s_CardCameraPosition = new(0.10f, 1.26f, -10f);
        private static readonly Vector3 s_BattleCameraPosition = new(0.68f, 0.84f, -10f);
        private static readonly int[] s_EventYears = { 4, 6, 8, 10, 12, 14, 16, 19, 21, 23, 25, 27, 29, 31, 33 };

        private readonly List<SpriteRenderer> _boardGridLineRenderers = new();
        private readonly List<SpriteRenderer> _boardGridAccentLineRenderers = new();
        private string _dragCardId = string.Empty;
        private int _hoveredHandIndex = -1;
        private BoardCoord? _hoveredPlotCoord;
        private BoardCoord? _hoveredPlotCoordOverride;
        private Vector2 _dragDesignPoint;
        private bool _suppressRuntimePointerInput;

        internal readonly struct BattleForceSummary
        {
            public BattleForceSummary(int friendlyTotal, int enemyTotal)
            {
                FriendlyTotal = friendlyTotal;
                EnemyTotal = enemyTotal;
            }

            public int FriendlyTotal { get; }
            public int EnemyTotal { get; }
        }

        internal readonly struct BattleStructureLayout
        {
            public BattleStructureLayout(Vector3 position, Vector3 scale)
            {
                Position = position;
                Scale = scale;
            }

            public Vector3 Position { get; }
            public Vector3 Scale { get; }
        }

        internal readonly struct UnitAnimatorSpec
        {
            public UnitAnimatorSpec(string controllerAssetPath, string idleStateName, string runStateName, string actionStateName, bool ranged)
            {
                ControllerAssetPath = controllerAssetPath;
                IdleStateName = idleStateName;
                RunStateName = runStateName;
                ActionStateName = actionStateName;
                Ranged = ranged;
            }

            public string ControllerAssetPath { get; }
            public string IdleStateName { get; }
            public string RunStateName { get; }
            public string ActionStateName { get; }
            public bool Ranged { get; }
            public bool IsValid => !string.IsNullOrEmpty(ControllerAssetPath);
            public string ControllerKey => $"{ControllerAssetPath}|{IdleStateName}|{RunStateName}|{ActionStateName}";
        }

        internal readonly struct BattleEntityDebugSnapshot
        {
            public BattleEntityDebugSnapshot(string entityId, BattleVisualState visualState, int activeMemberCount, bool usesAnimator)
            {
                EntityId = entityId;
                VisualState = visualState;
                ActiveMemberCount = activeMemberCount;
                UsesAnimator = usesAnimator;
            }

            public string EntityId { get; }
            public BattleVisualState VisualState { get; }
            public int ActiveMemberCount { get; }
            public bool UsesAnimator { get; }
        }

        internal readonly struct BoardCellOutlineStyle
        {
            public BoardCellOutlineStyle(Vector3 fillScale, Vector3 edgeScale, Color cardEdgeColor, Color battleEdgeColor)
            {
                FillScale = fillScale;
                EdgeScale = edgeScale;
                CardEdgeColor = cardEdgeColor;
                BattleEdgeColor = battleEdgeColor;
            }

            public Vector3 FillScale { get; }
            public Vector3 EdgeScale { get; }
            public Color CardEdgeColor { get; }
            public Color BattleEdgeColor { get; }
        }

        internal readonly struct BoardGridLineStyle
        {
            public BoardGridLineStyle(Color mainColor, Color accentColor, float mainWidth, float accentWidth)
            {
                MainColor = mainColor;
                AccentColor = accentColor;
                MainWidth = mainWidth;
                AccentWidth = accentWidth;
            }

            public Color MainColor { get; }
            public Color AccentColor { get; }
            public float MainWidth { get; }
            public float AccentWidth { get; }
        }

        internal readonly struct MapUnitVisualSpec
        {
            public MapUnitVisualSpec(
                string assetRelativePath,
                string cacheKey,
                int columns,
                int rows,
                int column,
                int rowFromTop,
                int spriteCount,
                Vector3 primaryOffset,
                Vector3 primaryScale,
                Vector3 secondaryOffset,
                Vector3 secondaryScale)
            {
                AssetRelativePath = assetRelativePath;
                CacheKey = cacheKey;
                Columns = columns;
                Rows = rows;
                Column = column;
                RowFromTop = rowFromTop;
                SpriteCount = spriteCount;
                PrimaryOffset = primaryOffset;
                PrimaryScale = primaryScale;
                SecondaryOffset = secondaryOffset;
                SecondaryScale = secondaryScale;
            }

            public string AssetRelativePath { get; }
            public string CacheKey { get; }
            public int Columns { get; }
            public int Rows { get; }
            public int Column { get; }
            public int RowFromTop { get; }
            public int SpriteCount { get; }
            public Vector3 PrimaryOffset { get; }
            public Vector3 PrimaryScale { get; }
            public Vector3 SecondaryOffset { get; }
            public Vector3 SecondaryScale { get; }
            public bool IsValid => !string.IsNullOrEmpty(AssetRelativePath);
        }

        internal sealed class HoveredPlotTooltipSnapshot
        {
            public BoardCoord coord;
            public string title = string.Empty;
            public string subtitle = string.Empty;
            public string footer = string.Empty;
            public readonly List<string> statLines = new();
        }

        internal readonly struct UiFrame
        {
            public UiFrame(float screenWidth, float screenHeight, float scale, Vector2 offset, Rect safeArea)
            {
                ScreenWidth = screenWidth;
                ScreenHeight = screenHeight;
                Scale = scale;
                Offset = offset;
                SafeArea = safeArea;
            }

            public float ScreenWidth { get; }
            public float ScreenHeight { get; }
            public float Scale { get; }
            public Vector2 Offset { get; }
            public Rect SafeArea { get; }
        }

        internal readonly struct CameraPreset
        {
            public CameraPreset(Vector3 position, float size)
            {
                Position = position;
                Size = size;
            }

            public Vector3 Position { get; }
            public float Size { get; }
        }

        internal readonly struct SectionLayout
        {
            public SectionLayout(Rect rect, bool visible, bool dimmed = false)
            {
                Rect = rect;
                Visible = visible;
                Dimmed = dimmed;
            }

            public Rect Rect { get; }
            public bool Visible { get; }
            public bool Dimmed { get; }
        }

        internal sealed class UiLayoutSnapshot
        {
            public UiLayoutSnapshot(UiFrame frame, CameraPreset cameraPreset)
            {
                Frame = frame;
                CameraPreset = cameraPreset;
            }

            public UiFrame Frame { get; }
            public CameraPreset CameraPreset { get; }
            public Rect BoardInteractionRect { get; set; }
            public Rect GoldRect { get; set; }
            public Rect LivesRect { get; set; }
            public Rect TimelineLabelRect { get; set; }
            public Rect TimelineYearBadgeRect { get; set; }
            public Rect TimelineSubtitleRect { get; set; }
            public Rect TimelineTrackRect { get; set; }
            public Rect AutoButtonRect { get; set; }
            public Rect PauseButtonRect { get; set; }
            public Rect Speed1ButtonRect { get; set; }
            public Rect Speed2ButtonRect { get; set; }
            public Rect Speed4ButtonRect { get; set; }
            public Rect WellDropRect { get; set; }
            public Rect WellLabelRect { get; set; }
            public Rect WellHintRect { get; set; }
            public Rect StatusTitleRect { get; set; }
            public Rect FriendlyForceRect { get; set; }
            public Rect EnemyForceRect { get; set; }
            public Rect FriendlyForceValueRect { get; set; }
            public Rect EnemyForceValueRect { get; set; }
            public Rect FriendlyForceLabelRect { get; set; }
            public Rect EnemyForceLabelRect { get; set; }
            public Rect OverlayTitleRect { get; set; }
            public Rect OverlaySubtitleRect { get; set; }
            public Rect OverlayButtonRect { get; set; }
            public SectionLayout TopLeftResources { get; set; }
            public SectionLayout TopCenterTimeline { get; set; }
            public SectionLayout TopRightControls { get; set; }
            public SectionLayout LeftWell { get; set; }
            public SectionLayout RightStatus { get; set; }
            public SectionLayout BottomHand { get; set; }
            public SectionLayout BottomBattleForces { get; set; }
            public SectionLayout Overlay { get; set; }
            public List<Rect> TimelineDotRects { get; } = new();
            public List<Rect> FriendlyForcePips { get; } = new();
            public List<Rect> EnemyForcePips { get; } = new();
            public List<Rect> StatusLineRects { get; } = new();
            public List<Rect> HandCardRects { get; } = new();
            public List<Rect> LootCardRects { get; } = new();
            public bool BoardCellsUseStrongOutline { get; set; }
            public int VisibleBuildCellCount { get; set; }
        }

        public void SetController(NineKingsV2GameController controller)
        {
            _controller = controller;
            if (isActiveAndEnabled)
            {
                EnsureCamera();
                SyncView();
            }
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
            var layout = CreateCurrentLayoutSnapshot(Screen.width, Screen.height);
            var previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(new Vector3(layout.Frame.Offset.x, layout.Frame.Offset.y, 0f), Quaternion.identity, new Vector3(layout.Frame.Scale, layout.Frame.Scale, 1f));

            if (_controller?.RunState == null)
            {
                GUI.Label(new Rect(layout.Frame.SafeArea.x, layout.Frame.SafeArea.y, 360f, 32f), "NineKings V2 加载中...", s_HeaderStyle ?? GUI.skin.label);
                GUI.matrix = previousMatrix;
                return;
            }

            DrawTopLeftResources(layout);
            DrawTopCenterTimeline(layout);
            DrawTopRightControls(layout);
            DrawLeftWellHint(layout);
            DrawRightStatus(layout);
            DrawHoveredPlotTooltip(layout);
            DrawBottomHandRibbon(layout);
            DrawBattleForceHud(layout);
            DrawOverlayIfNeeded(layout);
            DrawDraggedCard();

            GUI.matrix = previousMatrix;
        }

        private void HandleBoardInput()
        {
            if (_controller?.RunState == null || _camera == null || Mouse.current == null)
            {
                return;
            }

            if (_suppressRuntimePointerInput)
            {
                return;
            }

            var screen = Mouse.current.position.ReadValue();
            var layout = CreateCurrentLayoutSnapshot(Screen.width, Screen.height);
            var designPoint = ScreenToDesign(screen, layout.Frame);
            UpdateHoveredHandIndex(layout, designPoint);

            if (_controller.RunState.phase != RunPhase.CardPhase)
            {
                _hoveredPlotCoord = null;
                if (Mouse.current.rightButton.wasPressedThisFrame)
                {
                    CancelCardDrag();
                }

                return;
            }

            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                CancelCardDrag();
                return;
            }

            if (!string.IsNullOrEmpty(_dragCardId))
            {
                UpdateActiveDrag(layout, screen, designPoint);
                if (Mouse.current.leftButton.wasReleasedThisFrame)
                {
                    ReleaseActiveDrag(layout, screen, designPoint);
                }

                return;
            }

            if (!_controller.HandState.isLocked && Mouse.current.leftButton.wasPressedThisFrame && _hoveredHandIndex >= 0 && _hoveredHandIndex < _controller.HandState.cardIds.Count)
            {
                StartCardDrag(_controller.HandState.cardIds[_hoveredHandIndex], designPoint);
                return;
            }

            UpdateHoveredPlotCoord(layout, screen, designPoint);

            if (string.IsNullOrEmpty(_selectedCardId))
            {
                _controller.ClearPreview();
                return;
            }

            if (layout.WellDropRect.Contains(designPoint))
            {
                _controller.SetPreview(_selectedCardId, null, true);
                if (Mouse.current.leftButton.wasPressedThisFrame && _controller.TryDiscardToWell(_selectedCardId))
                {
                    _selectedCardId = string.Empty;
                }
                return;
            }

            if (IsPointerOverBlockingUi(layout, designPoint))
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

        private void UpdateHoveredHandIndex(UiLayoutSnapshot layout, Vector2 designPoint)
        {
            _hoveredHandIndex = -1;
            if (!layout.BottomHand.Visible)
            {
                return;
            }

            for (var i = layout.HandCardRects.Count - 1; i >= 0; i--)
            {
                if (layout.HandCardRects[i].Contains(designPoint))
                {
                    _hoveredHandIndex = i;
                    break;
                }
            }
        }

        private void UpdateHoveredPlotCoord(UiLayoutSnapshot layout, Vector2 screenPoint, Vector2 designPoint)
        {
            if (_hoveredPlotCoordOverride.HasValue)
            {
                _hoveredPlotCoord = _hoveredPlotCoordOverride;
                return;
            }

            _hoveredPlotCoord = null;
            if (IsPointerOverBlockingUi(layout, designPoint) || layout.WellDropRect.Contains(designPoint))
            {
                return;
            }

            if (!TryResolveHoveredCoord(layout, screenPoint, designPoint, out var hoveredCoord))
            {
                return;
            }

            var plot = _controller!.RunState!.GetPlot(hoveredCoord);
            if (!plot.IsEmpty)
            {
                _hoveredPlotCoord = hoveredCoord;
            }
        }

        private void StartCardDrag(string cardId, Vector2 designPoint)
        {
            _dragCardId = cardId;
            _selectedCardId = cardId;
            _dragDesignPoint = designPoint;
            _hoveredPlotCoord = null;
            _hoveredPlotCoordOverride = null;
        }

        private void CancelCardDrag()
        {
            _dragCardId = string.Empty;
            _selectedCardId = string.Empty;
            _hoveredHandIndex = -1;
            _hoveredPlotCoord = null;
            _hoveredPlotCoordOverride = null;
            _controller!.ClearPreview();
        }

        private void UpdateActiveDrag(UiLayoutSnapshot layout, Vector2 screenPoint, Vector2 designPoint)
        {
            _dragDesignPoint = designPoint;
            _hoveredPlotCoord = null;

            if (layout.WellDropRect.Contains(designPoint))
            {
                _controller!.SetPreview(_dragCardId, null, true);
                return;
            }

            if (IsPointerOverBlockingUi(layout, designPoint))
            {
                _controller!.ClearPreview();
                return;
            }

            if (!TryResolveHoveredCoord(layout, screenPoint, designPoint, out var coord))
            {
                _controller!.ClearPreview();
                return;
            }

            _hoveredPlotCoord = coord;
            _controller!.SetPreview(_dragCardId, coord, false);
        }

        private bool ReleaseActiveDrag(UiLayoutSnapshot layout, Vector2 screenPoint, Vector2 designPoint)
        {
            var draggedCardId = _dragCardId;
            var overWell = layout.WellDropRect.Contains(designPoint);
            var played = false;
            if (overWell)
            {
                played = _controller!.TryDiscardToWell(draggedCardId);
            }
            else if (!IsPointerOverBlockingUi(layout, designPoint)
                     && TryResolveHoveredCoord(layout, screenPoint, designPoint, out var coord)
                     && _controller!.PlacementPreviewState.targetCoord.HasValue
                     && _controller.PlacementPreviewState.targetCoord.Value.Equals(coord)
                     && _controller.PlacementPreviewState.isValid)
            {
                played = _controller.TryPlayCard(draggedCardId, coord);
            }

            _dragCardId = string.Empty;
            _dragDesignPoint = Vector2.zero;
            _hoveredPlotCoord = null;
            _hoveredPlotCoordOverride = null;

            if (played)
            {
                if (_controller!.HandState.cardIds.Contains(draggedCardId) == false)
                {
                    _selectedCardId = string.Empty;
                }
            }
            else
            {
                _selectedCardId = string.Empty;
                _controller!.ClearPreview();
            }

            return played;
        }

        private static bool IsPointerOverBlockingUi(UiLayoutSnapshot layout, Vector2 designPoint)
        {
            if (layout.Overlay.Visible && layout.Overlay.Rect.Contains(designPoint))
            {
                return true;
            }

            if (layout.BottomHand.Visible && layout.BottomHand.Rect.Contains(designPoint))
            {
                return true;
            }

            if (layout.TopCenterTimeline.Visible && layout.TopCenterTimeline.Rect.Contains(designPoint))
            {
                return true;
            }

            if (layout.TopRightControls.Visible && layout.TopRightControls.Rect.Contains(designPoint))
            {
                return true;
            }

            if (layout.RightStatus.Visible && layout.RightStatus.Rect.Contains(designPoint))
            {
                return true;
            }

            if (layout.LeftWell.Visible && layout.LeftWell.Rect.Contains(designPoint))
            {
                return true;
            }

            return false;
        }

        internal UiLayoutSnapshot CreateCurrentLayoutSnapshot(float screenWidth, float screenHeight)
        {
            var runState = _controller?.RunState;
            var phase = runState?.phase ?? RunPhase.MainMenu;
            var handCount = _controller?.HandState.cardIds.Count ?? 0;
            var showWellHint = phase == RunPhase.CardPhase && (!string.IsNullOrEmpty(_selectedCardId) || !string.IsNullOrEmpty(_dragCardId));
            var lootChoiceCount = phase == RunPhase.LootChoice ? _controller?.GetLootChoices().Count ?? 0 : 0;
            var snapshot = BuildLayoutSnapshot(screenWidth, screenHeight, phase, handCount, showWellHint, lootChoiceCount);
            snapshot.VisibleBuildCellCount = ShouldShowPlacementGrid()
                ? BuildVisibleBoardCellSet(runState, phase).Count
                : 0;
            return snapshot;
        }

        internal CameraPreset GetTargetCameraPreset()
        {
            return GetCameraPresetForPhase(_controller?.RunState?.phase ?? RunPhase.CardPhase);
        }

        internal BattleForceSummary GetBattleForceSummary()
        {
            return SummarizeBattleForces(_controller?.BattleSceneState);
        }

        internal IReadOnlyCollection<BoardCoord> GetVisibleBoardCellSet()
        {
            return ShouldShowPlacementGrid()
                ? BuildVisibleBoardCellSet(_controller?.RunState, _controller?.RunState?.phase ?? RunPhase.MainMenu)
                : Array.Empty<BoardCoord>();
        }

        internal HoveredPlotTooltipSnapshot? GetHoveredPlotTooltipSnapshot()
        {
            var hoveredCoord = _hoveredPlotCoordOverride ?? _hoveredPlotCoord;
            if (_controller?.RunState == null || hoveredCoord == null || !string.IsNullOrEmpty(_dragCardId))
            {
                return null;
            }

            var plot = _controller.RunState.GetPlot(hoveredCoord.Value);
            return CreateHoveredPlotTooltipSnapshot(_controller.Database, _controller.RunState, plot);
        }

        internal bool IsCardDragActive => !string.IsNullOrEmpty(_dragCardId);
        internal Vector2 DragDesignPoint => _dragDesignPoint;
        internal int HoveredHandIndex => _hoveredHandIndex;

        internal void SetHoveredHandIndexForTests(int hoveredHandIndex)
        {
            _suppressRuntimePointerInput = true;
            _hoveredHandIndex = hoveredHandIndex;
        }

        internal int GetDisplayedStructureMemberCount(BoardCoord coord)
        {
            if (!_structureViews.TryGetValue(coord, out var view))
            {
                return 0;
            }

            return view.memberRenderers.Count(renderer => renderer.gameObject.activeSelf);
        }

        internal string GetStructureCountLabelText(BoardCoord coord)
        {
            return _structureViews.TryGetValue(coord, out var view) ? view.countLabel.text : string.Empty;
        }

        internal Color GetStructureBodyColor(BoardCoord coord)
        {
            return _structureViews.TryGetValue(coord, out var view) ? view.bodyRenderer.color : Color.clear;
        }

        internal (int MainLines, int AccentLines) GetActiveBoardGridLineCounts()
        {
            return (
                _boardGridLineRenderers.Count(renderer => renderer.gameObject.activeSelf),
                _boardGridAccentLineRenderers.Count(renderer => renderer.gameObject.activeSelf));
        }

        internal void SetHoveredPlotForTests(BoardCoord? coord)
        {
            _hoveredPlotCoordOverride = coord;
            _hoveredPlotCoord = coord;
        }

        internal void BeginCardDragForTests(string cardId, Vector2 designPoint)
        {
            _suppressRuntimePointerInput = true;
            StartCardDrag(cardId, designPoint);
        }

        internal void UpdateCardDragForTests(Vector2 screenPoint, Vector2 designPoint)
        {
            var layout = CreateCurrentLayoutSnapshot(Screen.width <= 0 ? DesignWidth : Screen.width, Screen.height <= 0 ? DesignHeight : Screen.height);
            UpdateActiveDrag(layout, screenPoint, designPoint);
        }

        internal bool ReleaseCardDragForTests(Vector2 screenPoint, Vector2 designPoint)
        {
            var layout = CreateCurrentLayoutSnapshot(Screen.width <= 0 ? DesignWidth : Screen.width, Screen.height <= 0 ? DesignHeight : Screen.height);
            var result = ReleaseActiveDrag(layout, screenPoint, designPoint);
            _suppressRuntimePointerInput = false;
            return result;
        }

        internal (Vector2 Screen, Vector2 Design) ProjectBoardCoordPointerForTests(BoardCoord coord)
        {
            EnsureCamera();
            var layout = CreateCurrentLayoutSnapshot(Screen.width <= 0 ? DesignWidth : Screen.width, Screen.height <= 0 ? DesignHeight : Screen.height);
            var projected = _camera!.WorldToScreenPoint(BoardCoordToWorld(coord));
            var screen = new Vector2(projected.x, projected.y);
            var design = ScreenToDesign(screen, layout.Frame);
            return (screen, design);
        }

        internal float GetCurrentCameraSizeForTests()
        {
            EnsureCamera();
            return _camera?.orthographicSize ?? 0f;
        }

        internal void ForceCameraUpdateForTests()
        {
            UpdateCameraForPhase();
        }

        private bool TryResolveHoveredCoord(UiLayoutSnapshot layout, Vector2 screenPosition, Vector2 designPosition, out BoardCoord coord)
        {
            if (TryGetHoveredCoord(screenPosition, out coord))
            {
                return true;
            }

            coord = default;
            if (_camera == null || _controller?.RunState == null)
            {
                return false;
            }

            var bestDistance = float.MaxValue;
            foreach (var visibleCoord in BuildVisibleBoardCellSet(_controller.RunState, _controller.RunState.phase))
            {
                var world = BoardCoordToWorld(visibleCoord);
                var projected = _camera.WorldToScreenPoint(world);
                if (projected.z < 0f)
                {
                    continue;
                }

                var projectedDesign = ScreenToDesign(new Vector2(projected.x, projected.y), layout.Frame);
                var dx = Mathf.Abs(designPosition.x - projectedDesign.x);
                var dy = Mathf.Abs(designPosition.y - projectedDesign.y);
                var normalized = dx / 116f + dy / 70f;
                if (normalized > 1.08f)
                {
                    continue;
                }

                var sqrDistance = (designPosition - projectedDesign).sqrMagnitude;
                if (sqrDistance < bestDistance)
                {
                    bestDistance = sqrDistance;
                    coord = visibleCoord;
                }
            }

            return bestDistance < float.MaxValue;
        }

        internal IReadOnlyList<BattleEntityDebugSnapshot> GetBattleEntityDebugSnapshots()
        {
            var snapshots = new List<BattleEntityDebugSnapshot>(_battleEntityViews.Count);
            foreach (var pair in _battleEntityViews)
            {
                if (!pair.Value.gameObject.activeSelf)
                {
                    continue;
                }

                _battleEntityVisualCaches.TryGetValue(pair.Key, out var cache);
                var activeMemberCount = pair.Value.memberRenderers.Count(renderer => renderer.gameObject.activeSelf);
                snapshots.Add(new BattleEntityDebugSnapshot(
                    pair.Key,
                    cache?.currentState ?? BattleVisualState.Hidden,
                    activeMemberCount,
                    cache?.usesAnimator ?? false));
            }

            return snapshots;
        }

        internal static UiFrame BuildUiFrame(float screenWidth, float screenHeight)
        {
            var scale = Mathf.Min(screenWidth / DesignWidth, screenHeight / DesignHeight);
            var offset = new Vector2((screenWidth - DesignWidth * scale) * 0.5f, (screenHeight - DesignHeight * scale) * 0.5f);
            var safeArea = new Rect(40f, 24f, DesignWidth - 80f, DesignHeight - 48f);
            return new UiFrame(screenWidth, screenHeight, scale, offset, safeArea);
        }

        internal static BoardCellOutlineStyle GetBoardCellOutlineStyle()
        {
            return new BoardCellOutlineStyle(
                s_BoardCellFillScale,
                s_BoardCellEdgeScale,
                new Color(1f, 1f, 1f, 1f),
                new Color(1f, 0.97f, 0.88f, 0.60f));
        }

        internal static IReadOnlyList<BoardGridSegment> BuildBoardGridSegments(IReadOnlyCollection<BoardCoord> visibleCells)
        {
            if (visibleCells.Count == 0)
            {
                return Array.Empty<BoardGridSegment>();
            }

            var segments = new Dictionary<string, BoardGridSegment>(StringComparer.Ordinal);
            foreach (var coord in visibleCells)
            {
                var center = BoardCoordToWorld(coord);
                var top = new Vector2(center.x, center.y + BoardCellHalfHeight);
                var right = new Vector2(center.x + BoardCellHalfWidth, center.y);
                var bottom = new Vector2(center.x, center.y - BoardCellHalfHeight);
                var left = new Vector2(center.x - BoardCellHalfWidth, center.y);

                AddSegment(top, right);
                AddSegment(right, bottom);
                AddSegment(bottom, left);
                AddSegment(left, top);
            }

            return segments.Values.ToArray();

            void AddSegment(Vector2 start, Vector2 end)
            {
                var key = CreateSegmentKey(start, end);
                if (!segments.ContainsKey(key))
                {
                    segments[key] = new BoardGridSegment(start, end);
                }
            }
        }

        internal static MapUnitVisualSpec ResolveMapUnitVisualSpec(string cardId, WorldObjectType worldObjectType, bool battleVisible)
        {
            if (battleVisible || worldObjectType != WorldObjectType.UnitSource)
            {
                return default;
            }

            return cardId switch
            {
                "nothing_archer" => new MapUnitVisualSpec(
                    "Tiny Swords/Units/Blue Units/Archer/Archer_Idle.png",
                    "map-unit-nothing-archer",
                    6,
                    1,
                    0,
                    0,
                    2,
                    new Vector3(-0.14f, -0.02f, 0f),
                    new Vector3(0.34f, 0.34f, 1f),
                    new Vector3(0.14f, 0.04f, 0f),
                    new Vector3(0.31f, 0.31f, 1f)),
                "nothing_paladin" => new MapUnitVisualSpec(
                    "Tiny Swords/Units/Blue Units/Warrior/Warrior_Idle.png",
                    "map-unit-nothing-paladin",
                    8,
                    1,
                    0,
                    0,
                    1,
                    new Vector3(0f, 0.01f, 0f),
                    new Vector3(0.39f, 0.39f, 1f),
                    Vector3.zero,
                    Vector3.zero),
                "nothing_soldier" => new MapUnitVisualSpec(
                    "Tiny Swords/Units/Blue Units/Warrior/Warrior_Idle.png",
                    "map-unit-nothing-soldier",
                    8,
                    1,
                    0,
                    0,
                    2,
                    new Vector3(-0.14f, -0.03f, 0f),
                    new Vector3(0.35f, 0.35f, 1f),
                    new Vector3(0.15f, 0.04f, 0f),
                    new Vector3(0.32f, 0.32f, 1f)),
                "greed_thief" => new MapUnitVisualSpec(
                    "Tiny Swords/Units/Yellow Units/Warrior/Warrior_Idle.png",
                    "map-unit-greed-thief",
                    8,
                    1,
                    0,
                    0,
                    2,
                    new Vector3(-0.15f, -0.03f, 0f),
                    new Vector3(0.35f, 0.35f, 1f),
                    new Vector3(0.15f, 0.05f, 0f),
                    new Vector3(0.31f, 0.31f, 1f)),
                "greed_mercenary" => new MapUnitVisualSpec(
                    "Tiny Swords/Units/Yellow Units/Warrior/Warrior_Idle.png",
                    "map-unit-greed-mercenary",
                    8,
                    1,
                    0,
                    0,
                    2,
                    new Vector3(-0.15f, -0.03f, 0f),
                    new Vector3(0.36f, 0.36f, 1f),
                    new Vector3(0.15f, 0.05f, 0f),
                    new Vector3(0.33f, 0.33f, 1f)),
                _ => default,
            };
        }

        internal static UiLayoutSnapshot BuildLayoutSnapshot(float screenWidth, float screenHeight, RunPhase phase, int handCount, bool showWellHint, int lootChoiceCount)
        {
            var frame = BuildUiFrame(screenWidth, screenHeight);
            var snapshot = new UiLayoutSnapshot(frame, GetCameraPresetForPhase(phase));
            var battleLike = IsBattlePresentationPhase(phase);
            snapshot.BoardCellsUseStrongOutline = true;
            snapshot.VisibleBuildCellCount = phase is RunPhase.CardPhase or RunPhase.PlacementPreview or RunPhase.BattleDeploy or RunPhase.BattleRun or RunPhase.BattleResolve or RunPhase.LootChoice or RunPhase.RunOver
                ? 9
                : 0;

            snapshot.BoardInteractionRect = new Rect(300f, 128f, 1248f, 628f);
            snapshot.TopLeftResources = new SectionLayout(new Rect(frame.SafeArea.x + 8f, frame.SafeArea.y - 2f, 260f, 88f), true);
            snapshot.GoldRect = new Rect(snapshot.TopLeftResources.Rect.x, snapshot.TopLeftResources.Rect.y, 200f, 44f);
            snapshot.LivesRect = new Rect(snapshot.TopLeftResources.Rect.x + 4f, snapshot.TopLeftResources.Rect.y + 42f, 152f, 24f);

            snapshot.TopCenterTimeline = new SectionLayout(new Rect(DesignWidth * 0.5f - 338f, frame.SafeArea.y + 2f, 676f, 110f), true);
            snapshot.TimelineTrackRect = new Rect(snapshot.TopCenterTimeline.Rect.x + 46f, snapshot.TopCenterTimeline.Rect.y + 20f, snapshot.TopCenterTimeline.Rect.width - 92f, 14f);
            snapshot.TimelineYearBadgeRect = new Rect(DesignWidth * 0.5f - 72f, snapshot.TopCenterTimeline.Rect.y + 58f, 144f, 8f);
            snapshot.TimelineLabelRect = new Rect(snapshot.TopCenterTimeline.Rect.x + 176f, snapshot.TopCenterTimeline.Rect.y + 34f, 324f, 36f);
            snapshot.TimelineSubtitleRect = new Rect(snapshot.TopCenterTimeline.Rect.x + 168f, snapshot.TopCenterTimeline.Rect.y + 66f, 340f, 24f);
            var spacing = snapshot.TimelineTrackRect.width / (s_EventYears.Length - 1);
            for (var i = 0; i < s_EventYears.Length; i++)
            {
                snapshot.TimelineDotRects.Add(new Rect(snapshot.TimelineTrackRect.x + i * spacing - 6f, snapshot.TimelineTrackRect.y, 12f, 12f));
            }

            snapshot.TopRightControls = new SectionLayout(new Rect(frame.SafeArea.xMax - 250f, frame.SafeArea.y, 250f, 40f), true, !battleLike);
            snapshot.AutoButtonRect = new Rect(snapshot.TopRightControls.Rect.x, snapshot.TopRightControls.Rect.y, 72f, 32f);
            snapshot.PauseButtonRect = new Rect(snapshot.AutoButtonRect.xMax + 8f, snapshot.TopRightControls.Rect.y, 44f, 32f);
            snapshot.Speed1ButtonRect = new Rect(snapshot.PauseButtonRect.xMax + 12f, snapshot.TopRightControls.Rect.y, 30f, 32f);
            snapshot.Speed2ButtonRect = new Rect(snapshot.Speed1ButtonRect.xMax + 6f, snapshot.TopRightControls.Rect.y, 30f, 32f);
            snapshot.Speed4ButtonRect = new Rect(snapshot.Speed2ButtonRect.xMax + 6f, snapshot.TopRightControls.Rect.y, 36f, 32f);

            var showWell = phase == RunPhase.CardPhase || phase == RunPhase.PlacementPreview;
            snapshot.LeftWell = new SectionLayout(new Rect(frame.SafeArea.x - 12f, 520f, 252f, showWellHint ? 84f : 34f), showWell);
            snapshot.WellDropRect = new Rect(82f, 484f, 198f, 188f);
            snapshot.WellLabelRect = new Rect(snapshot.LeftWell.Rect.x + 24f, snapshot.LeftWell.Rect.y, 132f, 24f);
            snapshot.WellHintRect = new Rect(snapshot.LeftWell.Rect.x, snapshot.LeftWell.Rect.y + 24f, 230f, 34f);

            snapshot.RightStatus = new SectionLayout(new Rect(frame.SafeArea.xMax - 256f, 124f, 232f, 148f), phase != RunPhase.RunOver, battleLike);
            snapshot.StatusTitleRect = new Rect(snapshot.RightStatus.Rect.x + 16f, snapshot.RightStatus.Rect.y + 12f, snapshot.RightStatus.Rect.width - 32f, 24f);
            snapshot.StatusLineRects.Add(new Rect(snapshot.RightStatus.Rect.x + 16f, snapshot.RightStatus.Rect.y + 42f, snapshot.RightStatus.Rect.width - 32f, 22f));
            snapshot.StatusLineRects.Add(new Rect(snapshot.RightStatus.Rect.x + 16f, snapshot.RightStatus.Rect.y + 66f, snapshot.RightStatus.Rect.width - 32f, 22f));
            snapshot.StatusLineRects.Add(new Rect(snapshot.RightStatus.Rect.x + 16f, snapshot.RightStatus.Rect.y + 90f, snapshot.RightStatus.Rect.width - 32f, 22f));
            snapshot.StatusLineRects.Add(new Rect(snapshot.RightStatus.Rect.x + 16f, snapshot.RightStatus.Rect.y + 114f, snapshot.RightStatus.Rect.width - 32f, 22f));

            var showHand = phase == RunPhase.CardPhase || phase == RunPhase.PlacementPreview;
            snapshot.BottomHand = new SectionLayout(new Rect(DesignWidth * 0.5f - 496f, 728f, 992f, 320f), showHand);
            if (showHand && handCount > 0)
            {
                const float cardWidth = 170f;
                const float cardHeight = 252f;
                const float baseY = 748f;
                const float preferredGap = 10f;
                const float minGap = 0f;
                const float sidePadding = 124f;
                var cardSpacing = cardWidth + preferredGap;
                if (handCount > 1)
                {
                    var availableWidth = DesignWidth - sidePadding * 2f;
                    var maxSpacing = (availableWidth - cardWidth) / (handCount - 1f);
                    cardSpacing = Mathf.Clamp(maxSpacing, cardWidth + minGap, cardWidth + preferredGap);
                }

                var totalWidth = cardWidth + Math.Max(0, handCount - 1) * cardSpacing;
                var startX = DesignWidth * 0.5f - totalWidth * 0.5f;
                snapshot.BottomHand = new SectionLayout(new Rect(startX - 48f, 792f, totalWidth + 96f, 260f), true);
                for (var i = 0; i < handCount; i++)
                {
                    snapshot.HandCardRects.Add(new Rect(startX + i * cardSpacing, baseY, cardWidth, cardHeight));
                }
            }

            snapshot.BottomBattleForces = new SectionLayout(new Rect(0f, 926f, DesignWidth, 118f), battleLike);
            snapshot.FriendlyForceRect = new Rect(frame.SafeArea.x + 8f, 944f, 272f, 84f);
            snapshot.EnemyForceRect = new Rect(frame.SafeArea.xMax - 280f, 944f, 272f, 84f);
            snapshot.FriendlyForceLabelRect = new Rect(snapshot.FriendlyForceRect.x + 18f, snapshot.FriendlyForceRect.y + 12f, 118f, 22f);
            snapshot.EnemyForceLabelRect = new Rect(snapshot.EnemyForceRect.x + 18f, snapshot.EnemyForceRect.y + 12f, 118f, 22f);
            snapshot.FriendlyForceValueRect = new Rect(snapshot.FriendlyForceRect.x + 18f, snapshot.FriendlyForceRect.y + 30f, 88f, 40f);
            snapshot.EnemyForceValueRect = new Rect(snapshot.EnemyForceRect.x + 18f, snapshot.EnemyForceRect.y + 30f, 88f, 40f);
            for (var i = 0; i < 6; i++)
            {
                snapshot.FriendlyForcePips.Add(new Rect(snapshot.FriendlyForceRect.x + 116f + i * 22f, snapshot.FriendlyForceRect.y + 38f, 16f, 16f));
                snapshot.EnemyForcePips.Add(new Rect(snapshot.EnemyForceRect.x + 116f + i * 22f, snapshot.EnemyForceRect.y + 38f, 16f, 16f));
            }

            var showOverlay = phase == RunPhase.LootChoice || phase == RunPhase.RunOver;
            snapshot.Overlay = new SectionLayout(new Rect(0f, 0f, DesignWidth, DesignHeight), showOverlay);
            snapshot.OverlayTitleRect = new Rect(DesignWidth * 0.5f - 190f, 116f, 380f, 42f);
            snapshot.OverlaySubtitleRect = new Rect(DesignWidth * 0.5f - 260f, 156f, 520f, 28f);
            snapshot.OverlayButtonRect = new Rect(DesignWidth * 0.5f - 92f, DesignHeight * 0.5f + 12f, 184f, 38f);
            if (phase == RunPhase.LootChoice && lootChoiceCount > 0)
            {
                const float lootWidth = 210f;
                const float lootHeight = 294f;
                const float lootGap = 26f;
                var totalWidth = lootChoiceCount * lootWidth + Math.Max(0, lootChoiceCount - 1) * lootGap;
                var startX = DesignWidth * 0.5f - totalWidth * 0.5f;
                for (var i = 0; i < lootChoiceCount; i++)
                {
                    snapshot.LootCardRects.Add(new Rect(startX + i * (lootWidth + lootGap), 224f, lootWidth, lootHeight));
                }
            }

            return snapshot;
        }

        internal static CameraPreset GetCameraPresetForPhase(RunPhase phase)
        {
            return IsBattlePresentationPhase(phase)
                ? new CameraPreset(s_BattleCameraPosition, BattleCameraSize)
                : new CameraPreset(s_CardCameraPosition, CardCameraSize);
        }

        internal static bool IsBattlePresentationPhase(RunPhase phase)
        {
            return phase == RunPhase.BattleDeploy || phase == RunPhase.BattleRun || phase == RunPhase.BattleResolve || phase == RunPhase.LootChoice;
        }

        internal static BattleForceSummary SummarizeBattleForces(BattleSceneState? state)
        {
            if (state == null)
            {
                return new BattleForceSummary(0, 0);
            }

            var friendlyTotal = 0;
            var enemyTotal = 0;
            foreach (var entity in state.entities)
            {
                if (entity.isDead)
                {
                    continue;
                }

                if (entity.isEnemy)
                {
                    enemyTotal += Math.Max(1, entity.stackCount);
                }
                else
                {
                    friendlyTotal += Math.Max(1, entity.stackCount);
                }
            }

            return new BattleForceSummary(friendlyTotal, enemyTotal);
        }

        internal static IReadOnlyCollection<BoardCoord> BuildVisibleBoardCellSet(RunState? runState, RunPhase phase)
        {
            if (runState == null)
            {
                return Array.Empty<BoardCoord>();
            }

            if (phase is RunPhase.MainMenu or RunPhase.RunIntro)
            {
                return Array.Empty<BoardCoord>();
            }

            return runState.plots
                .Where(plot => plot.unlocked)
                .Select(plot => plot.coord)
                .ToArray();
        }

        internal static Vector2 ScreenToDesign(Vector2 screenPoint, UiFrame frame)
        {
            var guiY = frame.ScreenHeight - screenPoint.y;
            return new Vector2((screenPoint.x - frame.Offset.x) / frame.Scale, (guiY - frame.Offset.y) / frame.Scale);
        }

        internal static bool TryResolveHoveredCoordFromWorld(RunState? runState, RunPhase phase, Vector2 worldPoint, out BoardCoord coord)
        {
            coord = default;
            if (runState == null)
            {
                return false;
            }

            var visibleCells = new HashSet<BoardCoord>(BuildVisibleBoardCellSet(runState, phase));
            foreach (var plot in runState.plots)
            {
                if (!visibleCells.Contains(plot.coord))
                {
                    continue;
                }

                var center = BoardCoordToWorld(plot.coord);
                var local = worldPoint - new Vector2(center.x, center.y);
                var diamond = Mathf.Abs(local.x) / BoardCellHalfWidth + Mathf.Abs(local.y) / BoardCellHalfHeight;
                if (diamond <= 1.02f)
                {
                    coord = plot.coord;
                    return true;
                }
            }

            return false;
        }

        private void DrawTopLeftResources(UiLayoutSnapshot layout)
        {
            var runState = _controller!.RunState!;
            if (!layout.TopLeftResources.Visible)
            {
                return;
            }

            GUI.Label(layout.GoldRect, $"✦ {runState.gold}", s_TopValueStyle!);
            GUI.Label(layout.LivesRect, $"命 {runState.lives}", s_MutedLabelStyle!);
        }

        private void DrawTopCenterTimeline(UiLayoutSnapshot layout)
        {
            var runState = _controller!.RunState!;
            if (!layout.TopCenterTimeline.Visible)
            {
                return;
            }

            var previousColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.14f);
            GUI.Box(layout.TopCenterTimeline.Rect, GUIContent.none);
            GUI.color = previousColor;

            for (var i = 0; i < s_EventYears.Length; i++)
            {
                var year = s_EventYears[i];
                var dotRect = layout.TimelineDotRects[i];
                var prev = GUI.color;
                GUI.color = year == runState.year
                    ? new Color(1f, 0.95f, 0.78f, 1f)
                    : year < runState.year
                        ? new Color(0.86f, 0.70f, 0.34f, 1f)
                        : new Color(1f, 1f, 1f, 0.24f);
                GUI.Box(dotRect, GUIContent.none);
                GUI.color = prev;
            }

            GUI.Label(layout.TimelineLabelRect, $"YEAR {runState.year}", s_HeaderStyle!);
            GUI.color = new Color(0.58f, 0.38f, 0.16f, 0.90f);
            GUI.Box(layout.TimelineYearBadgeRect, GUIContent.none);
            GUI.color = Color.white;
            GUI.Label(layout.TimelineSubtitleRect, $"当前年份 / 第 {runState.year} 年", s_MutedLabelStyle!);
        }

        private void DrawTopRightControls(UiLayoutSnapshot layout)
        {
            if (!layout.TopRightControls.Visible)
            {
                return;
            }

            var previousColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, layout.TopRightControls.Dimmed ? 0.48f : 1f);

            if (GUI.Button(layout.AutoButtonRect, _controller!.AutoBattleEnabled ? "auto" : "手动", _controller.AutoBattleEnabled ? s_SelectedButtonStyle! : s_ButtonStyle!))
            {
                _controller.SetAutoBattleEnabled(!_controller.AutoBattleEnabled);
            }

            if (GUI.Button(layout.PauseButtonRect, _controller.IsPaused ? "▶" : "Ⅱ", _controller.IsPaused ? s_SelectedButtonStyle! : s_ButtonStyle!))
            {
                _controller.SetPausedState(!_controller.IsPaused);
            }

            if (GUI.Button(layout.Speed1ButtonRect, "1", Mathf.Abs(_controller.BattleSpeedMultiplier - 1f) < 0.01f ? s_SelectedButtonStyle! : s_ButtonStyle!))
            {
                _controller.SetBattleSpeedMultiplier(1f);
            }
            if (GUI.Button(layout.Speed2ButtonRect, "2", Mathf.Abs(_controller.BattleSpeedMultiplier - 2f) < 0.01f ? s_SelectedButtonStyle! : s_ButtonStyle!))
            {
                _controller.SetBattleSpeedMultiplier(2f);
            }
            if (GUI.Button(layout.Speed4ButtonRect, "4", Mathf.Abs(_controller.BattleSpeedMultiplier - 4f) < 0.01f ? s_SelectedButtonStyle! : s_ButtonStyle!))
            {
                _controller.SetBattleSpeedMultiplier(4f);
            }

            GUI.color = previousColor;
        }

        private void DrawLeftWellHint(UiLayoutSnapshot layout)
        {
            if (!layout.LeftWell.Visible)
            {
                return;
            }

            GUI.Label(layout.WellLabelRect, "井 / Well", s_HeaderStyle!);
            if (!string.IsNullOrEmpty(_selectedCardId) || !string.IsNullOrEmpty(_dragCardId))
            {
                GUI.Label(layout.WellHintRect, "拖到井里弃牌，获得 9 金币", s_MutedLabelStyle!);
            }
        }

        private void DrawRightStatus(UiLayoutSnapshot layout)
        {
            if (!layout.RightStatus.Visible)
            {
                return;
            }

            var runState = _controller!.RunState!;
            var previousColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, layout.RightStatus.Dimmed ? 0.14f : 0.22f);
            GUI.Box(layout.RightStatus.Rect, GUIContent.none);
            GUI.color = previousColor;

            GUI.Label(layout.StatusTitleRect, $"敌王 / {GetKingShortName(runState.currentEnemyKingId)}", s_PanelLabelStyle!);
            if (layout.StatusLineRects.Count >= 4)
            {
                GUI.Label(layout.StatusLineRects[0], $"阶段 {GetPhaseLabel(runState.phase)}", s_PanelLabelStyle!);
                GUI.Label(layout.StatusLineRects[1], $"手牌 {_controller.HandState.PlayableCount}", s_PanelLabelStyle!);
                GUI.Label(layout.StatusLineRects[2], GetStatusSummary(runState), s_PanelLabelStyle!);
                GUI.Label(layout.StatusLineRects[3], runState.phase == RunPhase.BattleDeploy ? "列队观察中 / 战前停顿" : IsBattlePresentationPhase(runState.phase) ? "左上固守 / 右下压入" : "拖牌放置或投入井中", s_MutedLabelStyle!);
            }
        }

        private string GetStatusSummary(RunState runState)
        {
            if (_controller!.PlacementPreviewState.targetCoord.HasValue)
            {
                var coord = _controller.PlacementPreviewState.targetCoord.Value;
                return _controller.PlacementPreviewState.isValid
                    ? $"格位 {coord.x},{coord.y} 可放置"
                    : $"格位 {coord.x},{coord.y} 不可放置";
            }

            if (!string.IsNullOrEmpty(_selectedCardId))
            {
                return $"已选 {GetCardName(_selectedCardId)}";
            }

            return IsBattlePresentationPhase(runState.phase)
                ? "观察战场与控速"
                : "布局庭院与兵力";
        }

        private void DrawHoveredPlotTooltip(UiLayoutSnapshot layout)
        {
            var snapshot = GetHoveredPlotTooltipSnapshot();
            if (snapshot == null || _camera == null)
            {
                return;
            }

            var worldCenter = BoardCoordToWorld(snapshot.coord);
            var screenPoint = _camera.WorldToScreenPoint(new Vector3(worldCenter.x, worldCenter.y, 0f));
            var designPoint = ScreenToDesign(new Vector2(screenPoint.x, screenPoint.y), layout.Frame);
            var height = 108f + snapshot.statLines.Count * 20f + (string.IsNullOrEmpty(snapshot.footer) ? 0f : 24f);
            var rect = new Rect(designPoint.x + 74f, designPoint.y - 108f, 312f, height);
            rect.x = Mathf.Clamp(rect.x, 24f, DesignWidth - rect.width - 24f);
            rect.y = Mathf.Clamp(rect.y, 24f, DesignHeight - rect.height - 24f);

            var previousColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.84f);
            GUI.Box(rect, GUIContent.none);
            GUI.color = GetCardWorldColor(_controller!.RunState!.GetPlot(snapshot.coord).cardId);
            GUI.Box(new Rect(rect.x + 12f, rect.y + 12f, 28f, 28f), GUIContent.none);
            GUI.color = previousColor;

            GUI.Label(new Rect(rect.x + 48f, rect.y + 10f, rect.width - 60f, 28f), snapshot.title, s_TooltipTitleStyle!);
            GUI.Label(new Rect(rect.x + 48f, rect.y + 36f, rect.width - 60f, 22f), snapshot.subtitle, s_TooltipBodyStyle!);

            for (var i = 0; i < snapshot.statLines.Count; i++)
            {
                GUI.Label(new Rect(rect.x + 14f, rect.y + 62f + i * 20f, rect.width - 28f, 20f), snapshot.statLines[i], s_TooltipStatStyle!);
            }

            if (!string.IsNullOrEmpty(snapshot.footer))
            {
                GUI.Label(new Rect(rect.x + 14f, rect.yMax - 24f, rect.width - 28f, 20f), snapshot.footer, s_TooltipBodyStyle!);
            }
        }

        private void DrawBottomHandRibbon(UiLayoutSnapshot layout)
        {
            if (!layout.BottomHand.Visible || layout.HandCardRects.Count == 0)
            {
                return;
            }

            var hand = _controller!.HandState.cardIds;
            GUI.color = new Color(0f, 0f, 0f, 0.30f);
            GUI.Box(layout.BottomHand.Rect, GUIContent.none);
            GUI.color = Color.white;

            for (var i = 0; i < hand.Count; i++)
            {
                if (i == _hoveredHandIndex || string.Equals(hand[i], _dragCardId, StringComparison.Ordinal))
                {
                    continue;
                }

                var cardId = hand[i];
                var isSelected = string.Equals(_selectedCardId, cardId, StringComparison.Ordinal);
                var baseRect = layout.HandCardRects[i];
                var rect = new Rect(baseRect.x, baseRect.y - (isSelected ? 38f : 0f), baseRect.width, baseRect.height);
                DrawCardButton(rect, cardId, isSelected, i >= hand.Count - 2);
            }

            if (_hoveredHandIndex >= 0 && _hoveredHandIndex < hand.Count && !string.Equals(hand[_hoveredHandIndex], _dragCardId, StringComparison.Ordinal))
            {
                var hoveredCardId = hand[_hoveredHandIndex];
                var isSelected = string.Equals(_selectedCardId, hoveredCardId, StringComparison.Ordinal);
                var rect = ScaleRect(layout.HandCardRects[_hoveredHandIndex], 1.14f);
                rect.y -= isSelected ? 42f : 32f;
                DrawCardButton(rect, hoveredCardId, isSelected, _hoveredHandIndex >= hand.Count - 2);
            }
        }

        private void DrawDraggedCard()
        {
            if (string.IsNullOrEmpty(_dragCardId))
            {
                return;
            }

            var rect = new Rect(_dragDesignPoint.x - 106f, _dragDesignPoint.y - 164f, 212f, 312f);
            DrawCardVisual(rect, _dragCardId, true, false);
        }

        private static Rect ScaleRect(Rect rect, float scale)
        {
            var width = rect.width * scale;
            var height = rect.height * scale;
            return new Rect(rect.center.x - width * 0.5f, rect.center.y - height * 0.5f, width, height);
        }

        private void DrawBattleForceHud(UiLayoutSnapshot layout)
        {
            if (!layout.BottomBattleForces.Visible)
            {
                return;
            }

            var summary = GetBattleForceSummary();
            DrawForcePanel(layout.FriendlyForceRect, layout.FriendlyForceLabelRect, layout.FriendlyForceValueRect, layout.FriendlyForcePips, "我方兵力", summary.FriendlyTotal, new Color(1f, 0.95f, 0.78f, 1f), new Color(0.86f, 0.72f, 0.30f, 1f));
            DrawForcePanel(layout.EnemyForceRect, layout.EnemyForceLabelRect, layout.EnemyForceValueRect, layout.EnemyForcePips, "敌方兵力", summary.EnemyTotal, new Color(1f, 0.86f, 0.90f, 1f), new Color(0.68f, 0.22f, 0.40f, 1f));
        }

        private void DrawForcePanel(Rect panelRect, Rect labelRect, Rect valueRect, IReadOnlyList<Rect> pipRects, string label, int total, Color valueColor, Color pipColor)
        {
            var previousColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.22f);
            GUI.Box(panelRect, GUIContent.none);
            GUI.color = previousColor;

            GUI.Label(labelRect, label, s_PanelLabelStyle!);

            var previousValueColor = s_TopValueStyle!.normal.textColor;
            s_TopValueStyle.normal.textColor = valueColor;
            GUI.Label(valueRect, total.ToString(), s_TopValueStyle);
            s_TopValueStyle.normal.textColor = previousValueColor;

            var pipCount = Mathf.Clamp(Mathf.CeilToInt(total / 2f), 0, pipRects.Count);
            for (var i = 0; i < pipRects.Count; i++)
            {
                GUI.color = i < pipCount ? pipColor : new Color(1f, 1f, 1f, 0.14f);
                GUI.Box(pipRects[i], GUIContent.none);
            }

            GUI.color = Color.white;
        }

        private void DrawOverlayIfNeeded(UiLayoutSnapshot layout)
        {
            var runState = _controller!.RunState!;
            if (!layout.Overlay.Visible)
            {
                return;
            }

            if (runState.phase == RunPhase.LootChoice)
            {
                var choices = _controller.GetLootChoices();
                GUI.color = new Color(0f, 0f, 0f, 0.76f);
                GUI.Box(layout.Overlay.Rect, GUIContent.none);
                GUI.color = Color.white;
                GUI.Label(layout.OverlayTitleRect, "BATTLE WON", s_OverlayTitleStyle!);
                GUI.Label(layout.OverlaySubtitleRect, "选择一张战利品 / PICK YOUR LOOT", s_HeaderStyle!);
                for (var i = 0; i < choices.Count && i < layout.LootCardRects.Count; i++)
                {
                    DrawLootCard(layout.LootCardRects[i], choices[i]);
                }
            }
            else if (runState.phase == RunPhase.RunOver)
            {
                GUI.color = new Color(0f, 0f, 0f, 0.78f);
                GUI.Box(layout.Overlay.Rect, GUIContent.none);
                GUI.color = Color.white;
                GUI.Label(new Rect(layout.OverlayTitleRect.x, DesignHeight * 0.5f - 44f, layout.OverlayTitleRect.width, layout.OverlayTitleRect.height), "本局结束", s_OverlayTitleStyle!);
                if (GUI.Button(layout.OverlayButtonRect, "重新开局", s_SelectedButtonStyle!))
                {
                    _controller.StartNewRun("king_greed");
                    _controller.EnterCardPhase();
                }
            }
        }

        private void DrawCardButton(Rect rect, string cardId, bool isSelected, bool markAutoBattle)
        {
            DrawCardVisual(rect, cardId, isSelected, markAutoBattle);
            if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
            {
                _selectedCardId = isSelected ? string.Empty : cardId;
            }
        }

        private void DrawCardVisual(Rect rect, string cardId, bool isSelected, bool markAutoBattle)
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
            SyncBoardGridSegments();
            SyncPlacedStructures();
            SyncBattleEntities();
        }

        private void UpdateCameraForPhase()
        {
            if (_camera == null || _controller?.RunState == null)
            {
                return;
            }

            var preset = GetTargetCameraPreset();
            var smoothDelta = Mathf.Max(Time.unscaledDeltaTime, 1f / 60f);
            _camera.orthographicSize = Mathf.Lerp(_camera.orthographicSize, preset.Size, smoothDelta * 6.5f);
            _camera.transform.position = Vector3.Lerp(_camera.transform.position, preset.Position, smoothDelta * 6.5f);
        }

        private void SyncBoardCells()
        {
            var runState = _controller!.RunState!;
            var visibleCells = ShouldShowPlacementGrid()
                ? new HashSet<BoardCoord>(BuildVisibleBoardCellSet(runState, runState.phase))
                : new HashSet<BoardCoord>();
            var inBattle = IsBattlePresentationPhase(runState.phase);
            foreach (var plot in runState.plots)
            {
                if (!_cellViews.TryGetValue(plot.coord, out var cellView))
                {
                    continue;
                }

                var visible = visibleCells.Contains(plot.coord);
                cellView.gameObject.SetActive(visible);
                if (!visible)
                {
                    continue;
                }

                var outlineStyle = GetBoardCellOutlineStyle();
                var edgeColor = inBattle ? outlineStyle.BattleEdgeColor : outlineStyle.CardEdgeColor;
                var fillColor = inBattle ? new Color(0.96f, 0.92f, 0.78f, 0.01f) : new Color(0.98f, 0.97f, 0.88f, 0.02f);
                var highlightActive = false;

                if (_controller.PlacementPreviewState.targetCoord.HasValue && _controller.PlacementPreviewState.targetCoord.Value.Equals(plot.coord) && !inBattle)
                {
                    edgeColor = _controller.PlacementPreviewState.isValid ? new Color(0.42f, 0.90f, 1f, 1f) : new Color(0.98f, 0.44f, 0.44f, 1f);
                    fillColor = _controller.PlacementPreviewState.isValid ? new Color(0.30f, 0.72f, 0.96f, 0.16f) : new Color(0.88f, 0.34f, 0.34f, 0.16f);
                    highlightActive = true;
                }

                foreach (var edgeView in cellView.edgeViews)
                {
                    edgeView.renderer.color = edgeColor;
                    edgeView.renderer.gameObject.SetActive(highlightActive);
                }

                cellView.fillRenderer.color = fillColor;
            }
        }

        private void SyncBoardGridSegments()
        {
            if (_boardGridRoot == null || _controller?.RunState == null)
            {
                return;
            }

            if (!ShouldShowPlacementGrid())
            {
                for (var i = 0; i < _boardGridLineRenderers.Count; i++)
                {
                    _boardGridLineRenderers[i].gameObject.SetActive(false);
                }

                for (var i = 0; i < _boardGridAccentLineRenderers.Count; i++)
                {
                    _boardGridAccentLineRenderers[i].gameObject.SetActive(false);
                }

                return;
            }

            var segments = BuildBoardGridSegments(BuildVisibleBoardCellSet(_controller.RunState, _controller.RunState.phase));
            EnsureBoardGridLineRenderers(segments.Count);

            var style = ResolveBoardGridLineStyle(_controller.RunState.phase);
            for (var i = 0; i < _boardGridLineRenderers.Count; i++)
            {
                var renderer = _boardGridLineRenderers[i];
                var accentRenderer = _boardGridAccentLineRenderers[i];
                if (i >= segments.Count)
                {
                    renderer.gameObject.SetActive(false);
                    accentRenderer.gameObject.SetActive(false);
                    continue;
                }

                var segment = segments[i];
                renderer.gameObject.SetActive(true);
                accentRenderer.gameObject.SetActive(true);
                renderer.color = style.MainColor;
                accentRenderer.color = style.AccentColor;
                renderer.transform.localPosition = new Vector3(segment.Midpoint.x, segment.Midpoint.y, 0f);
                renderer.transform.localRotation = Quaternion.Euler(0f, 0f, segment.AngleDegrees);
                renderer.transform.localScale = GetSquareSpriteWorldScale(segment.Length, style.MainWidth);
                accentRenderer.transform.localPosition = renderer.transform.localPosition;
                accentRenderer.transform.localRotation = renderer.transform.localRotation;
                accentRenderer.transform.localScale = GetSquareSpriteWorldScale(segment.Length, style.AccentWidth);
            }
        }

        private bool ShouldShowPlacementGrid()
        {
            if (_controller?.RunState == null)
            {
                return false;
            }

            var phase = _controller.RunState.phase;
            if (phase != RunPhase.CardPhase && phase != RunPhase.PlacementPreview)
            {
                return false;
            }

            if (_controller.HandState.isLocked || _controller.HandState.cardIds.Count == 0)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(_dragCardId) || !string.IsNullOrEmpty(_selectedCardId))
            {
                return true;
            }

            return _hoveredHandIndex >= 0 && _hoveredHandIndex < _controller.HandState.cardIds.Count;
        }

        private void SyncPlacedStructures()
        {
            var runState = _controller!.RunState!;
            var combatVisible = runState.phase == RunPhase.BattleRun || runState.phase == RunPhase.BattleResolve || runState.phase == RunPhase.LootChoice || runState.phase == RunPhase.BattleDeploy;
            var preserveMapUnitSources = runState.phase == RunPhase.BattleDeploy && _controller.IsBattleDeployCameraLeading;
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
                var worldObjectType = presentation?.worldObjectType ?? WorldObjectType.Building;
                view.gameObject.SetActive(true);
                view.gameObject.transform.position = combatVisible && worldObjectType != WorldObjectType.UnitSource
                    ? ResolveBattleStructureAnchor(plot.cardId, worldObjectType, plot.coord).Position
                    : ResolveMapPlotAnchor(plot.coord);
                var hideForBattle = combatVisible && !(preserveMapUnitSources && worldObjectType == WorldObjectType.UnitSource);
                UpdateStructureVisual(view, plot, worldObjectType, hideForBattle);
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
            var suppressDuringCameraLead = runState.phase == RunPhase.BattleDeploy && _controller.IsBattleDeployCameraLeading;
            var visibleEntityIds = new HashSet<string>();

            if (visibleBattle)
            {
                foreach (var entity in _controller.GetVisibleBattleEntities())
                {
                    if (suppressDuringCameraLead)
                    {
                        continue;
                    }

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
            if (_camera == null || _controller?.RunState == null)
            {
                return false;
            }

            var world = _camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -_camera.transform.position.z));
            return TryResolveHoveredCoordFromWorld(_controller.RunState, _controller.RunState.phase, new Vector2(world.x, world.y), out coord);
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

            var preset = GetCameraPresetForPhase(RunPhase.CardPhase);
            _camera.orthographic = true;
            _camera.orthographicSize = preset.Size;
            _camera.transform.position = preset.Position;
            _camera.backgroundColor = new Color(0.86f, 0.73f, 0.48f, 1f);
        }

        private void EnsureRoots()
        {
            _worldBackgroundRoot = EnsureChild("WorldBackground");
            _boardCellsRoot = EnsureChild("BoardCells");
            _boardGridRoot = EnsureChild("BoardGrid");
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
                    gameObject.transform.localRotation = Quaternion.identity;
                    gameObject.transform.localScale = Vector3.one;
                    gameObject.SetActive(false);

                    var fillObject = new GameObject("Fill");
                    fillObject.transform.SetParent(gameObject.transform, false);
                    fillObject.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                    fillObject.transform.localScale = GetSquareSpriteWorldScale(s_BoardCellFillScale.x, s_BoardCellFillScale.y);
                    var fillRenderer = fillObject.AddComponent<SpriteRenderer>();
                    fillRenderer.sprite = GetSquareSprite();
                    fillRenderer.sortingOrder = -1;
                    fillRenderer.color = new Color(0.98f, 0.97f, 0.88f, 0.02f);

                    var edgeViews = new List<CellEdgeView>(4);
                    CreateCellEdge(new Vector3(-BoardCellHalfWidth * 0.5f, BoardCellHalfHeight * 0.5f, 0f), -27.4f);
                    CreateCellEdge(new Vector3(BoardCellHalfWidth * 0.5f, BoardCellHalfHeight * 0.5f, 0f), 27.4f);
                    CreateCellEdge(new Vector3(-BoardCellHalfWidth * 0.5f, -BoardCellHalfHeight * 0.5f, 0f), 27.4f);
                    CreateCellEdge(new Vector3(BoardCellHalfWidth * 0.5f, -BoardCellHalfHeight * 0.5f, 0f), -27.4f);

                    _cellViews[coord] = new CellView
                    {
                        gameObject = gameObject,
                        fillRenderer = fillRenderer,
                        edgeViews = edgeViews,
                    };

                    void CreateCellEdge(Vector3 localPosition, float angle)
                    {
                        var edgeObject = new GameObject($"Edge_{edgeViews.Count}");
                        edgeObject.transform.SetParent(gameObject.transform, false);
                        edgeObject.transform.localPosition = localPosition;
                        edgeObject.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
                        edgeObject.transform.localScale = GetSquareSpriteWorldScale(s_BoardCellEdgeScale.x, s_BoardCellEdgeScale.y);
                        var edgeRenderer = edgeObject.AddComponent<SpriteRenderer>();
                        edgeRenderer.sprite = GetSquareSprite();
                        edgeRenderer.sortingOrder = 5;
                        edgeRenderer.color = GetBoardCellOutlineStyle().CardEdgeColor;
                        edgeObject.SetActive(false);
                        edgeViews.Add(new CellEdgeView
                        {
                            renderer = edgeRenderer,
                            localPosition = localPosition,
                            localRotation = edgeObject.transform.localRotation,
                            localScale = edgeObject.transform.localScale,
                        });
                    }
                }
            }
        }

        private void EnsureBoardGridLineRenderers(int count)
        {
            if (_boardGridRoot == null)
            {
                return;
            }

            while (_boardGridLineRenderers.Count < count)
            {
                var segmentObject = new GameObject($"GridLine_{_boardGridLineRenderers.Count}");
                segmentObject.transform.SetParent(_boardGridRoot, false);
                var renderer = segmentObject.AddComponent<SpriteRenderer>();
                renderer.sprite = GetSquareSprite();
                renderer.sortingOrder = 2;
                _boardGridLineRenderers.Add(renderer);
            }

            while (_boardGridAccentLineRenderers.Count < count)
            {
                var accentObject = new GameObject($"GridAccent_{_boardGridAccentLineRenderers.Count}");
                accentObject.transform.SetParent(_boardGridRoot, false);
                var renderer = accentObject.AddComponent<SpriteRenderer>();
                renderer.sprite = GetSquareSprite();
                renderer.sortingOrder = 1;
                _boardGridAccentLineRenderers.Add(renderer);
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

            var members = new List<SpriteRenderer>();
            for (var i = 0; i < MaxDisplayedMapUnitMembers; i++)
            {
                var memberObject = new GameObject($"Member_{i}");
                memberObject.transform.SetParent(gameObject.transform, false);
                var memberRenderer = memberObject.AddComponent<SpriteRenderer>();
                memberRenderer.sprite = GetSquareSprite();
                memberRenderer.sortingOrder = 14;
                memberObject.SetActive(false);
                members.Add(memberRenderer);
            }

            var labelObject = new GameObject("CountLabel");
            labelObject.transform.SetParent(gameObject.transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 0.46f, 0f);
            var countLabel = labelObject.AddComponent<TextMesh>();
            countLabel.anchor = TextAnchor.MiddleCenter;
            countLabel.alignment = TextAlignment.Center;
            countLabel.characterSize = 0.07f;
            countLabel.fontSize = 42;
            countLabel.color = Color.white;
            labelObject.GetComponent<MeshRenderer>().sortingOrder = 16;
            labelObject.SetActive(false);

            return new StructureView
            {
                gameObject = gameObject,
                bodyRenderer = bodyRenderer,
                accentRenderer = accentRenderer,
                memberRenderers = members,
                countLabel = countLabel,
            };
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
            var animators = new List<Animator>();
            for (var i = 0; i < MaxDisplayedUnitMembers; i++)
            {
                var memberObject = new GameObject($"Member_{i}");
                memberObject.transform.SetParent(gameObject.transform, false);
                var memberRenderer = memberObject.AddComponent<SpriteRenderer>();
                memberRenderer.sprite = GetSquareSprite();
                memberRenderer.sortingOrder = 21;
                members.Add(memberRenderer);
                animators.Add(memberObject.AddComponent<Animator>());
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
                memberAnimators = animators,
                stackLabel = label,
            };
        }

        private void UpdateStructureVisual(StructureView view, PlotState plot, WorldObjectType worldObjectType, bool battleVisible)
        {
            var cardId = plot.cardId;
            var runtimeStats = _controller!.ResolvePlotRuntimeStats(plot);
            if (battleVisible && worldObjectType == WorldObjectType.UnitSource)
            {
                view.bodyRenderer.gameObject.SetActive(false);
                view.accentRenderer.gameObject.SetActive(false);
                HideStructureMembers(view);
                return;
            }

            var primary = GetCardWorldColor(cardId);
            var accent = new Color(Mathf.Clamp01(primary.r * 0.70f), Mathf.Clamp01(primary.g * 0.62f), Mathf.Clamp01(primary.b * 0.54f), 1f);
            var mapUnitSpec = ResolveMapUnitVisualSpec(cardId, worldObjectType, battleVisible);
            if (mapUnitSpec.IsValid)
            {
                var unitSprite = LoadSheetFrame(
                    mapUnitSpec.AssetRelativePath,
                    mapUnitSpec.CacheKey,
                    mapUnitSpec.Columns,
                    mapUnitSpec.Rows,
                    mapUnitSpec.Column,
                    mapUnitSpec.RowFromTop);

                if (unitSprite != null)
                {
                    view.bodyRenderer.gameObject.SetActive(false);
                    view.accentRenderer.gameObject.SetActive(false);
                    var displayedMembers = ResolveMapUnitDisplayCount(plot, runtimeStats);
                    var offsets = GetMapUnitFormationOffsets(runtimeStats.EffectiveUnitCount, runtimeStats.AttackRange >= 1.6f);
                    for (var i = 0; i < view.memberRenderers.Count; i++)
                    {
                        var active = i < displayedMembers;
                        view.memberRenderers[i].gameObject.SetActive(active);
                        if (!active)
                        {
                            continue;
                        }

                        view.memberRenderers[i].sprite = unitSprite;
                        view.memberRenderers[i].color = Color.white;
                        view.memberRenderers[i].transform.localRotation = Quaternion.identity;
                        view.memberRenderers[i].transform.localPosition = offsets[i];
                        view.memberRenderers[i].transform.localScale = mapUnitSpec.PrimaryScale * 0.92f;
                    }

                    view.countLabel.text = runtimeStats.EffectiveUnitCount > MaxDisplayedMapUnitMembers ? $"{runtimeStats.EffectiveUnitCount}" : string.Empty;
                    view.countLabel.color = new Color(1f, 0.95f, 0.76f, 1f);
                    view.countLabel.transform.localPosition = new Vector3(0f, 0.38f, 0f);
                    view.countLabel.gameObject.SetActive(runtimeStats.EffectiveUnitCount > MaxDisplayedMapUnitMembers);
                    return;
                }
            }

            HideStructureMembers(view);
            var sprite = GetWorldSprite(cardId, worldObjectType);
            if (sprite != null)
            {
                view.bodyRenderer.sprite = sprite;
                view.bodyRenderer.color = worldObjectType == WorldObjectType.UnitSource ? Color.white : ResolveStructureLevelColor(plot.level);
                view.bodyRenderer.sortingOrder = 10;
                view.accentRenderer.gameObject.SetActive(false);
                view.bodyRenderer.gameObject.SetActive(true);
                view.bodyRenderer.transform.localPosition = Vector3.zero;
                view.bodyRenderer.transform.localRotation = Quaternion.identity;
                view.bodyRenderer.transform.localScale = battleVisible
                    ? ResolveBattleStructureLayout(cardId, worldObjectType, 0).Scale
                    : GetStructureSpriteScale(cardId, false);
                return;
            }

            view.accentRenderer.gameObject.SetActive(true);
            view.bodyRenderer.gameObject.SetActive(true);
            view.bodyRenderer.sortingOrder = 10;
            view.accentRenderer.sortingOrder = 11;
            view.bodyRenderer.sprite = GetSquareSprite();
            var structureLevelColor = worldObjectType == WorldObjectType.UnitSource ? primary : ResolveStructureLevelColor(plot.level);
            view.bodyRenderer.color = structureLevelColor;
            view.accentRenderer.color = worldObjectType == WorldObjectType.UnitSource ? accent : Color.Lerp(structureLevelColor, Color.white, 0.18f);
            view.bodyRenderer.transform.localPosition = Vector3.zero;
            view.bodyRenderer.transform.localRotation = Quaternion.identity;
            view.accentRenderer.transform.localRotation = Quaternion.identity;

            switch (worldObjectType)
            {
                case WorldObjectType.Palace:
                    view.bodyRenderer.transform.localScale = battleVisible ? new Vector3(0.76f, 0.48f, 1f) : new Vector3(0.56f, 0.34f, 1f);
                    view.accentRenderer.transform.localScale = battleVisible ? new Vector3(0.52f, 0.18f, 1f) : new Vector3(0.30f, 0.14f, 1f);
                    view.accentRenderer.transform.localPosition = battleVisible ? new Vector3(0f, 0.34f, 0f) : new Vector3(0f, 0.22f, 0f);
                    break;
                case WorldObjectType.Tower:
                    view.bodyRenderer.transform.localScale = battleVisible ? new Vector3(0.40f, 0.66f, 1f) : new Vector3(0.34f, 0.54f, 1f);
                    view.accentRenderer.transform.localScale = battleVisible ? new Vector3(0.18f, 0.18f, 1f) : new Vector3(0.14f, 0.14f, 1f);
                    view.accentRenderer.transform.localPosition = battleVisible ? new Vector3(0f, 0.34f, 0f) : new Vector3(0f, 0.30f, 0f);
                    break;
                case WorldObjectType.UnitSource:
                    view.bodyRenderer.transform.localScale = battleVisible ? new Vector3(0.46f, 0.30f, 1f) : new Vector3(0.40f, 0.28f, 1f);
                    view.accentRenderer.transform.localScale = battleVisible ? new Vector3(0.18f, 0.12f, 1f) : new Vector3(0.14f, 0.10f, 1f);
                    view.accentRenderer.transform.localPosition = battleVisible ? new Vector3(0f, 0.18f, 0f) : new Vector3(0f, 0.14f, 0f);
                    break;
                default:
                    view.bodyRenderer.transform.localScale = battleVisible ? new Vector3(0.34f, 0.34f, 1f) : new Vector3(0.30f, 0.30f, 1f);
                    view.accentRenderer.transform.localScale = new Vector3(0.16f, 0.16f, 1f);
                    view.accentRenderer.transform.localPosition = new Vector3(0f, 0.16f, 0f);
                    break;
            }
        }

        private static void HideStructureMembers(StructureView view)
        {
            for (var i = 0; i < view.memberRenderers.Count; i++)
            {
                view.memberRenderers[i].gameObject.SetActive(false);
            }

            view.countLabel.gameObject.SetActive(false);
        }

        internal static int ResolveMapUnitDisplayCount(PlotState plot, PlotRuntimeStats stats)
        {
            if (plot.IsEmpty || !stats.IsUnitSource || stats.EffectiveUnitCount <= 0)
            {
                return 0;
            }

            return Mathf.Clamp(stats.EffectiveUnitCount, 0, MaxDisplayedMapUnitMembers);
        }

        internal static Vector3 ResolveMapUnitDisplayAnchor(BoardCoord coord, int effectiveUnitCount, bool ranged)
        {
            var anchor = ResolveMapPlotAnchor(coord);
            var displayedMembers = Mathf.Clamp(effectiveUnitCount, 0, MaxDisplayedMapUnitMembers);
            if (displayedMembers <= 1)
            {
                return anchor;
            }

            var offsets = GetMapUnitFormationOffsets(effectiveUnitCount, ranged);
            var centroid = Vector3.zero;
            for (var i = 0; i < displayedMembers && i < offsets.Length; i++)
            {
                centroid += offsets[i];
            }

            centroid /= displayedMembers;
            return anchor + centroid;
        }

        private static Vector3[] GetMapUnitFormationOffsets(int effectiveUnitCount, bool ranged)
        {
            if (effectiveUnitCount <= 1)
            {
                return new[] { Vector3.zero };
            }

            if (ranged)
            {
                return new[]
                {
                    new Vector3(-0.12f, 0.05f, 0f),
                    new Vector3(0.12f, 0.05f, 0f),
                    new Vector3(0f, 0.12f, 0f),
                    new Vector3(-0.12f, -0.11f, 0f),
                    new Vector3(0.12f, -0.11f, 0f),
                    new Vector3(0f, -0.21f, 0f),
                };
            }

            return new[]
            {
                new Vector3(-0.14f, 0.06f, 0f),
                new Vector3(0.14f, 0.06f, 0f),
                new Vector3(0f, 0.14f, 0f),
                new Vector3(-0.14f, -0.11f, 0f),
                new Vector3(0.14f, -0.11f, 0f),
                new Vector3(0f, -0.22f, 0f),
            };
        }

        internal static Color ResolveStructureLevelColor(int level)
        {
            return Mathf.Clamp(level, 1, 5) switch
            {
                1 => new Color(0.97f, 0.83f, 0.24f, 1f),
                2 => new Color(0.90f, 0.30f, 0.22f, 1f),
                3 => new Color(0.56f, 0.36f, 0.82f, 1f),
                4 => new Color(0.26f, 0.58f, 0.92f, 1f),
                _ => new Color(0.18f, 0.18f, 0.18f, 1f),
            };
        }

        internal static HoveredPlotTooltipSnapshot? CreateHoveredPlotTooltipSnapshot(ContentDatabase? database, RunState? runState, PlotState? plot)
        {
            if (database == null || runState == null || plot == null || plot.IsEmpty)
            {
                return null;
            }

            var stats = NineKingsV2GameController.ResolvePlotRuntimeStats(database, runState, plot);
            var card = database.GetCard(plot.cardId);
            var snapshot = new HoveredPlotTooltipSnapshot
            {
                coord = plot.coord,
                title = card?.displayName.Get(true) ?? plot.cardId,
                subtitle = $"Lv.{stats.Level} / {GetStaticCardTypeLabel(card?.cardType ?? CardType.Building)}",
                footer = stats.AnnualAdjacencyBonus > 0
                    ? $"四邻增兵: 每年 +{stats.AnnualAdjacencyBonus} / 累计 +{stats.CumulativeBonusUnitCount}"
                    : stats.CumulativeBonusUnitCount > 0
                        ? $"累计增兵: +{stats.CumulativeBonusUnitCount}"
                        : "无相邻增兵",
            };

            snapshot.statLines.Add($"单位数: {stats.EffectiveUnitCount}");
            snapshot.statLines.Add($"生命: {stats.EffectiveMaxHp}");
            snapshot.statLines.Add($"伤害: {stats.EffectiveDamage}");
            snapshot.statLines.Add($"攻速: {stats.AttackInterval:0.##}");
            snapshot.statLines.Add($"范围: {stats.AttackRange:0.##}");
            snapshot.statLines.Add($"移速: {stats.MoveSpeed:0.##}");
            snapshot.statLines.Add($"护盾: {stats.Shield}");
            snapshot.statLines.Add($"附魔层数: {stats.EnchantmentStacks}");
            snapshot.statLines.Add($"累计伤害: {stats.TotalDamage}");
            snapshot.statLines.Add($"累计击杀: {stats.TotalKills}");
            return snapshot;
        }

        private void UpdateBattleEntityVisual(BattleEntityView view, BattleEntityState entity)
        {
            var ranged = entity.attackRange >= 1.6f;
            var baseColor = GetEntityColor(entity);
            view.shadowRenderer.color = new Color(0f, 0f, 0f, 0.22f);
            view.shadowRenderer.transform.localScale = ranged ? new Vector3(0.12f, 0.03f, 1f) : new Vector3(0.14f, 0.035f, 1f);

            var cache = GetOrCreateBattleEntityVisualCache(entity.entityId);
            var visualState = ResolveBattleVisualState(entity, cache);
            var animatorSpec = ResolveUnitAnimatorSpec(entity);
            if (TryApplyAnimatorVisual(view, entity, ranged, visualState, animatorSpec, cache))
            {
                cache.currentState = visualState;
                cache.usesAnimator = true;
                cache.animatorKey = animatorSpec.ControllerKey;
                cache.previousWorldX = entity.worldX;
                cache.previousWorldY = entity.worldY;
                cache.previousAttackTimer = entity.timeSinceLastAttack;
                cache.initialized = true;

                view.stackLabel.text = entity.stackCount > 1 ? $"{entity.stackCount}" : string.Empty;
                view.stackLabel.characterSize = 0.08f;
                view.stackLabel.fontSize = 52;
                view.stackLabel.color = entity.isEnemy ? new Color(1f, 0.82f, 0.88f, 1f) : new Color(1f, 0.94f, 0.72f, 1f);
                view.stackLabel.transform.localPosition = new Vector3(0f, 0.28f, 0f);
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
            view.bodyRenderer.transform.localScale = ranged ? new Vector3(0.045f, 0.07f, 1f) : new Vector3(0.055f, 0.085f, 1f);
            view.headRenderer.transform.localScale = new Vector3(0.025f, 0.025f, 1f);
            view.crestRenderer.transform.localScale = ranged ? new Vector3(0.03f, 0.01f, 1f) : new Vector3(0.0125f, 0.025f, 1f);
            view.weaponRenderer.transform.localScale = ranged ? new Vector3(0.055f, 0.01f, 1f) : new Vector3(0.01f, 0.055f, 1f);
            view.weaponRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, ranged ? -12f : -18f);

            var fallbackFormation = GetFormationOffsets(ranged);
            var fallbackCount = Mathf.Clamp(entity.stackCount, 3, fallbackFormation.Length + 1);
            for (var i = 0; i < view.memberRenderers.Count; i++)
            {
                var active = i < fallbackCount - 1;
                view.memberRenderers[i].gameObject.SetActive(active);
                view.memberAnimators[i].enabled = false;
                if (!active)
                {
                    continue;
                }

                view.memberRenderers[i].sprite = GetSquareSprite();
                view.memberRenderers[i].transform.localPosition = fallbackFormation[i];
                view.memberRenderers[i].transform.localScale = GetFallbackMemberScale(ranged);
                view.memberRenderers[i].color = memberColor;
            }

            view.stackLabel.text = entity.stackCount > 1 ? $"x{entity.stackCount}" : string.Empty;
            view.stackLabel.characterSize = 0.08f;
            view.stackLabel.fontSize = 52;
            view.stackLabel.transform.localPosition = new Vector3(0f, 0.28f, 0f);
            view.stackLabel.gameObject.SetActive(entity.stackCount > 1);
            cache.currentState = BattleVisualState.Fallback;
            cache.usesAnimator = false;
            cache.previousWorldX = entity.worldX;
            cache.previousWorldY = entity.worldY;
            cache.previousAttackTimer = entity.timeSinceLastAttack;
            cache.initialized = true;
        }

        private BattleEntityVisualCache GetOrCreateBattleEntityVisualCache(string entityId)
        {
            if (_battleEntityVisualCaches.TryGetValue(entityId, out var cache))
            {
                return cache;
            }

            cache = new BattleEntityVisualCache();
            _battleEntityVisualCaches[entityId] = cache;
            return cache;
        }

        private BattleVisualState ResolveBattleVisualState(BattleEntityState entity, BattleEntityVisualCache cache)
        {
            var phase = _controller?.RunState?.phase ?? RunPhase.CardPhase;
            if (entity.isDead)
            {
                return BattleVisualState.Hidden;
            }

            if (phase is RunPhase.BattleResolve or RunPhase.LootChoice or RunPhase.RunOver)
            {
                return BattleVisualState.Idle;
            }

            var movedDistance = cache.initialized
                ? Vector2.Distance(new Vector2(entity.worldX, entity.worldY), new Vector2(cache.previousWorldX, cache.previousWorldY))
                : 0f;

            if (phase == RunPhase.BattleDeploy)
            {
                return movedDistance > 0.010f ? BattleVisualState.Run : BattleVisualState.Idle;
            }

            if (phase != RunPhase.BattleRun)
            {
                return BattleVisualState.Idle;
            }

            var attackTriggered = cache.initialized && entity.timeSinceLastAttack + 0.02f < cache.previousAttackTimer;
            if (attackTriggered)
            {
                cache.lastAttackVisualTime = Time.unscaledTime;
            }

            if (Time.unscaledTime - cache.lastAttackVisualTime <= (entity.attackRange >= 1.6f ? 0.24f : 0.28f))
            {
                return entity.attackRange >= 1.6f ? BattleVisualState.Shoot : BattleVisualState.Attack;
            }

            return movedDistance > 0.010f ? BattleVisualState.Run : BattleVisualState.Idle;
        }

        private bool TryApplyAnimatorVisual(BattleEntityView view, BattleEntityState entity, bool ranged, BattleVisualState visualState, UnitAnimatorSpec spec, BattleEntityVisualCache cache)
        {
            if (!spec.IsValid || visualState == BattleVisualState.Hidden)
            {
                return false;
            }

            var controller = LoadAnimatorController(spec.ControllerAssetPath);
            if (controller == null)
            {
                return false;
            }

            view.bodyRenderer.gameObject.SetActive(false);
            view.headRenderer.gameObject.SetActive(false);
            view.crestRenderer.gameObject.SetActive(false);
            view.weaponRenderer.gameObject.SetActive(false);

            var formation = GetSpriteFormationOffsets(ranged);
            var visibleCount = Mathf.Clamp(entity.stackCount, 1, Mathf.Min(formation.Length, view.memberRenderers.Count));
            for (var i = 0; i < view.memberRenderers.Count; i++)
            {
                var active = i < visibleCount;
                view.memberRenderers[i].gameObject.SetActive(active);
                if (!active)
                {
                    continue;
                }

                view.memberRenderers[i].color = Color.white;
                view.memberRenderers[i].transform.localPosition = formation[i];
                view.memberRenderers[i].transform.localScale = GetSpriteMemberScale(ranged);
                view.memberRenderers[i].sortingOrder = 21 + Mathf.RoundToInt((0.44f - formation[i].y) * 10f);

                var animator = view.memberAnimators[i];
                animator.enabled = true;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.updateMode = AnimatorUpdateMode.UnscaledTime;
                if (animator.runtimeAnimatorController != controller)
                {
                    animator.runtimeAnimatorController = controller;
                    animator.Rebind();
                    animator.Update(0f);
                }

                if (cache.currentState != visualState || cache.animatorKey != spec.ControllerKey)
                {
                    animator.Play(GetAnimatorStateName(spec, visualState), 0, 0f);
                    animator.Update(0f);
                }
            }

            return true;
        }

        private static string GetAnimatorStateName(UnitAnimatorSpec spec, BattleVisualState visualState)
        {
            return visualState switch
            {
                BattleVisualState.Run => spec.RunStateName,
                BattleVisualState.Attack => spec.ActionStateName,
                BattleVisualState.Shoot => spec.ActionStateName,
                _ => spec.IdleStateName,
            };
        }

        internal static Vector3[] GetSpriteFormationOffsets(bool ranged)
        {
            if (ranged)
            {
                return new[]
                {
                    new Vector3(-0.14f, 0.08f, 0f),
                    new Vector3(0f, 0.08f, 0f),
                    new Vector3(0.14f, 0.08f, 0f),
                    new Vector3(-0.09f, -0.02f, 0f),
                    new Vector3(0.09f, -0.02f, 0f),
                    new Vector3(-0.18f, -0.12f, 0f),
                    new Vector3(0f, -0.12f, 0f),
                    new Vector3(0.18f, -0.12f, 0f),
                    new Vector3(-0.09f, -0.22f, 0f),
                    new Vector3(0.09f, -0.22f, 0f),
                };
            }

            return new[]
            {
                new Vector3(-0.22f, 0.08f, 0f),
                new Vector3(-0.08f, 0.08f, 0f),
                new Vector3(0.08f, 0.08f, 0f),
                new Vector3(0.22f, 0.08f, 0f),
                new Vector3(-0.22f, -0.02f, 0f),
                new Vector3(-0.08f, -0.02f, 0f),
                new Vector3(0.08f, -0.02f, 0f),
                new Vector3(0.22f, -0.02f, 0f),
                new Vector3(-0.08f, -0.14f, 0f),
                new Vector3(0.08f, -0.14f, 0f),
            };
        }

        internal static Vector3[] GetFormationOffsets(bool ranged)
        {
            if (ranged)
            {
                return new[]
                {
                    new Vector3(-0.10f, -0.02f, 0f),
                    new Vector3(0.10f, -0.02f, 0f),
                    new Vector3(0f, -0.08f, 0f),
                    new Vector3(-0.14f, -0.08f, 0f),
                    new Vector3(0.14f, -0.08f, 0f),
                    new Vector3(-0.06f, 0.06f, 0f),
                    new Vector3(0.06f, 0.06f, 0f),
                    new Vector3(-0.16f, 0.04f, 0f),
                    new Vector3(0.16f, 0.04f, 0f),
                    new Vector3(0f, 0.10f, 0f),
                };
            }

            return new[]
            {
                new Vector3(-0.09f, 0.00f, 0f),
                new Vector3(0.09f, 0.00f, 0f),
                new Vector3(-0.04f, -0.08f, 0f),
                new Vector3(0.04f, -0.08f, 0f),
                new Vector3(-0.14f, 0.06f, 0f),
                new Vector3(0.14f, 0.06f, 0f),
                new Vector3(0f, 0.08f, 0f),
                new Vector3(0f, -0.12f, 0f),
                new Vector3(-0.18f, -0.02f, 0f),
                new Vector3(0.18f, -0.02f, 0f),
            };
        }

        internal static Vector3 GetSpriteMemberScale(bool ranged)
        {
            return ranged ? new Vector3(0.18f, 0.18f, 1f) : new Vector3(0.20f, 0.20f, 1f);
        }

        internal static Vector3 GetFallbackMemberScale(bool ranged)
        {
            return ranged ? new Vector3(0.032f, 0.050f, 1f) : new Vector3(0.038f, 0.058f, 1f);
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

        internal static UnitAnimatorSpec ResolveUnitAnimatorSpec(BattleEntityState entity)
        {
            var ranged = entity.attackRange >= 1.6f;
            if (entity.isEnemy)
            {
                return ranged
                    ? new UnitAnimatorSpec(
                        "Assets/Tiny Swords/Units/Purple Units/Archer/Archer Purple Animations/Archer_Purple.controller",
                        "Archer_Idle_Purple",
                        "Archer_Run_Purple",
                        "Archer_Shoot_Purple",
                        true)
                    : new UnitAnimatorSpec(
                        "Assets/Tiny Swords/Units/Purple Units/Warrior/Warrior Purple Animations/Warrior_Purple.controller",
                        "Warrior_Idle_Purple",
                        "Warrior_Run_Purple",
                        "Warrior_Attack1_Purple",
                        false);
            }

            var greed = entity.sourceCardId.StartsWith("greed", StringComparison.Ordinal) || entity.unitArchetypeId.StartsWith("greed", StringComparison.Ordinal);
            if (greed)
            {
                return ranged
                    ? new UnitAnimatorSpec(
                        "Assets/Tiny Swords/Units/Yellow Units/Archer/Archer Yellow Animations/Archer_Yellow.controller",
                        "Archer_Idle_Yellow",
                        "Archer_Run_Yellow",
                        "Archer_Shoot_Yellow",
                        true)
                    : new UnitAnimatorSpec(
                        "Assets/Tiny Swords/Units/Yellow Units/Warrior/Warrior Yellow Animations/Warrior_Yellow.controller",
                        "Warrior_Idle_Yellow",
                        "Warrior_Run_Yellow",
                        "Warrior_Attack1_Yellow",
                        false);
            }

            return ranged
                ? new UnitAnimatorSpec(
                    "Assets/Tiny Swords/Units/Blue Units/Archer/Archer Blue Animations/Archer_Blue.controller",
                    "Archer_Idle_Blue",
                    "Archer_Run_Blue",
                    "Archer_Shoot_Blue",
                    true)
                : new UnitAnimatorSpec(
                    "Assets/Tiny Swords/Units/Blue Units/Warrior/Warrior Blue Animations/Warrior_Blue.controller",
                    "Warrior_Idle_Blue",
                    "Warrior_Run_Blue",
                    "Warrior_Attack1_Blue",
                    false);
        }

        private Sprite? GetFallbackUnitSprite(BattleEntityState entity)
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

        private static RuntimeAnimatorController? LoadAnimatorController(string assetPath)
        {
#if UNITY_EDITOR
            if (s_RuntimeAnimatorControllerCache.TryGetValue(assetPath, out var cached))
            {
                return cached;
            }

            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(assetPath);
            s_RuntimeAnimatorControllerCache[assetPath] = controller;
            return controller;
#else
            return null;
#endif
        }

        internal static Vector3 GetStructureSpriteScale(string cardId, bool battleVisible)
        {
            return cardId switch
            {
                "greed_palace" => battleVisible ? new Vector3(0.62f, 0.62f, 1f) : new Vector3(0.46f, 0.46f, 1f),
                "nothing_castle" => battleVisible ? new Vector3(0.58f, 0.58f, 1f) : new Vector3(0.44f, 0.44f, 1f),
                "greed_dispenser" or "greed_beacon" or "nothing_scout_tower" => battleVisible ? new Vector3(0.42f, 0.42f, 1f) : new Vector3(0.34f, 0.34f, 1f),
                _ => battleVisible ? new Vector3(0.38f, 0.38f, 1f) : new Vector3(0.30f, 0.30f, 1f),
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
                fontSize = 34,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(1f, 0.96f, 0.84f, 1f) },
            };
            s_SmallLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.96f, 0.93f, 0.84f, 1f) },
            };
            s_MutedLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.95f, 0.92f, 0.84f, 0.92f) },
            };
            s_PanelLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                wordWrap = true,
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = new Color(0.96f, 0.93f, 0.84f, 1f) },
            };
            s_HeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 30,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white },
            };
            s_CardTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                normal = { textColor = new Color(1f, 0.97f, 0.90f, 1f) },
            };
            s_CardBodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                alignment = TextAnchor.UpperCenter,
                wordWrap = true,
                normal = { textColor = new Color(1f, 0.95f, 0.85f, 1f) },
            };
            s_TooltipTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(1f, 0.97f, 0.90f, 1f) },
            };
            s_TooltipBodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                wordWrap = true,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(1f, 0.92f, 0.82f, 0.94f) },
            };
            s_TooltipStatStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                wordWrap = false,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.98f, 0.96f, 0.88f, 1f) },
            };
            s_OverlayTitleStyle = new GUIStyle(s_HeaderStyle) { fontSize = 38 };
            s_ButtonStyle = new GUIStyle(GUI.skin.button) { fontSize = 16, alignment = TextAnchor.MiddleCenter };
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

        private static Vector3 GetSquareSpriteWorldScale(float targetWidth, float targetHeight)
        {
            var bounds = GetSquareSprite().bounds.size;
            var safeWidth = bounds.x > 0.0001f ? bounds.x : 1f;
            var safeHeight = bounds.y > 0.0001f ? bounds.y : 1f;
            return new Vector3(targetWidth / safeWidth, targetHeight / safeHeight, 1f);
        }

        internal static Vector3 BoardCoordToWorld(BoardCoord coord)
        {
            var dx = coord.x - 2;
            var dy = coord.y - 2;
            return new Vector3(0.30f + (dx - dy) * 1.08f, 1.20f + (dx + dy) * 0.56f, 0f);
        }

        internal static Vector3 ResolveMapPlotAnchor(BoardCoord coord)
        {
            var center = BoardCoordToWorld(coord);
            return new Vector3(center.x, center.y, -0.06f);
        }

        private Vector3 CoordToWorld(BoardCoord coord)
        {
            return ResolveMapPlotAnchor(coord);
        }

        private static Vector3 BattleToWorld(BattleEntityState entity)
        {
            return new Vector3(entity.worldX, entity.worldY, entity.isEnemy ? -0.18f : -0.22f);
        }

        internal static BattleStructureLayout ResolveBattleStructureAnchor(string cardId, WorldObjectType worldObjectType, BoardCoord coord)
        {
            if (cardId is "greed_palace" or "nothing_castle" || worldObjectType == WorldObjectType.Palace)
            {
                return new BattleStructureLayout(new Vector3(-5.12f, 2.96f, -0.10f), GetStructureSpriteScale(cardId, true));
            }

            var castleAnchor = new Vector3(-5.12f, 2.96f, -0.10f);
            var boardCenter = BoardCoordToWorld(new BoardCoord(2, 2));
            var boardPosition = BoardCoordToWorld(coord);
            var delta = boardPosition - boardCenter;
            var offset = new Vector3(delta.x * 0.62f, delta.y * 0.82f - 0.88f, 0f);
            offset += worldObjectType switch
            {
                WorldObjectType.Tower => new Vector3(0f, 0.18f, 0f),
                _ => Vector3.zero,
            };
            return new BattleStructureLayout(castleAnchor + offset, GetStructureSpriteScale(cardId, true));
        }

        internal static BoardGridLineStyle ResolveBoardGridLineStyle(RunPhase phase)
        {
            return IsBattlePresentationPhase(phase)
                ? new BoardGridLineStyle(
                    new Color(1f, 1f, 1f, 0.84f),
                    new Color(0.22f, 0.82f, 0.98f, 0.70f),
                    0.072f,
                    0.132f)
                : new BoardGridLineStyle(
                    new Color(1f, 1f, 1f, 1f),
                    new Color(0.15f, 0.84f, 0.98f, 1f),
                    0.096f,
                    0.180f);
        }

        internal static BattleStructureLayout ResolveBattleStructureLayout(string cardId, WorldObjectType worldObjectType, int index)
        {
            var sampleCoords = new[]
            {
                new BoardCoord(1, 1),
                new BoardCoord(3, 1),
                new BoardCoord(1, 3),
                new BoardCoord(3, 3),
                new BoardCoord(2, 1),
                new BoardCoord(2, 3),
            };
            return ResolveBattleStructureAnchor(cardId, worldObjectType, sampleCoords[index % sampleCoords.Length]);
        }

        private static string CreateSegmentKey(Vector2 start, Vector2 end)
        {
            var a = new Vector2Int(Mathf.RoundToInt(start.x * 1000f), Mathf.RoundToInt(start.y * 1000f));
            var b = new Vector2Int(Mathf.RoundToInt(end.x * 1000f), Mathf.RoundToInt(end.y * 1000f));
            if (a.x > b.x || (a.x == b.x && a.y > b.y))
            {
                (a, b) = (b, a);
            }

            return $"{a.x}:{a.y}|{b.x}:{b.y}";
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

        private static string GetStaticCardTypeLabel(CardType cardType)
        {
            return cardType switch
            {
                CardType.Base => "基地",
                CardType.Troop => "兵种",
                CardType.Tower => "塔楼",
                CardType.Building => "建筑",
                CardType.Enchantment => "附魔",
                CardType.Tome => "法术",
                _ => cardType.ToString(),
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
