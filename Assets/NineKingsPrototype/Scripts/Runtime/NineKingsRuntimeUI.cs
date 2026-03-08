#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace NineKingsPrototype
{
    public sealed class NineKingsRuntimeUI : MonoBehaviour
    {
        private const float CellSize = 96f;
        private const float CellSpacing = 6f;

        private NineKingsGameController? _controller;
        private Font? _font;
        private readonly Dictionary<Vector2Int, NKPlotView> _plotViews = new();
        private readonly List<NKCardView> _cardViews = new();
        private readonly List<GameObject> _battleViews = new();

        private Canvas? _canvas;
        private RectTransform? _boardGrid;
        private RectTransform? _battleOverlay;
        private RectTransform? _handRoot;
        private RectTransform? _modalRoot;
        private RectTransform? _gmRoot;
        private Text? _yearText;
        private Text? _livesText;
        private Text? _goldText;
        private Text? _enemyText;
        private Text? _phaseText;
        private Text? _detailsText;
        private Text? _logText;
        private Text? _gmSummaryText;
        private Text? _gmHelpText;
        private Text? _gmLogText;
        private InputField? _gmInput;
        private GameObject? _modalBlocker;
        private NKWellDropView? _wellDropView;

        public RectTransform BattleOverlay => _battleOverlay!;
        public Font UIFont => _font!;

        public void Initialize(NineKingsGameController controller)
        {
            _controller = controller;
            _font = LoadUIFont();
            BuildCanvas();
            Refresh(null, null, NKRunPhase.MainMenu, string.Empty, false);
        }

        public void Refresh(NineKingsContentDatabase? database, NKRunState? state, NKRunPhase phase, string currentEnemyName, bool isPaused)
        {
            _phaseText!.text = "阶段：" + NKChineseText.Phase(phase, isPaused);

            if (state == null)
            {
                _yearText!.text = "年份：-";
                _livesText!.text = "生命：-";
                _goldText!.text = "金币：-";
                _enemyText!.text = "敌方王国：-";
                _gmSummaryText!.text = "当前没有运行中的对局。";
                RebuildHand(database, state);
                RebuildBoard(database, state);
                return;
            }

            _yearText!.text = $"年份：{state.Year}/33";
            _livesText!.text = $"生命：{state.Lives}";
            _goldText!.text = $"金币：{state.Gold}";
            _enemyText!.text = string.IsNullOrEmpty(currentEnemyName) ? "敌方王国：-" : $"敌方王国：{currentEnemyName}";
            _gmSummaryText!.text = $"年 {state.Year}  |  命 {state.Lives}  |  金 {state.Gold}  |  手牌 {state.HandCardIds.Count} 张\n当前阶段：{NKChineseText.Phase(phase, isPaused)}";
            RebuildBoard(database, state);
            RebuildHand(database, state);
        }

        public void SetDetails(string content)
        {
            _detailsText!.text = content;
        }

        public void AppendLog(string message)
        {
            _logText!.text = message + "\n" + _logText.text;
            if (_logText.text.Length > 2400)
            {
                _logText.text = _logText.text[..2400];
            }
        }

        public void ToggleGM(bool visible)
        {
            if (_gmRoot == null)
            {
                return;
            }

            _gmRoot.gameObject.SetActive(visible);
            if (visible)
            {
                Canvas.ForceUpdateCanvases();
                _gmInput?.ActivateInputField();
                _gmInput?.Select();
                if (_gmLogText != null && string.IsNullOrWhiteSpace(_gmLogText.text))
                {
                    _gmLogText.text = GetDefaultGmLogText();
                }
            }
        }

        public void UpdateGmLog(IReadOnlyCollection<string> lines)
        {
            if (_gmLogText == null)
            {
                return;
            }

            _gmLogText.text = lines.Count == 0 ? GetDefaultGmLogText() : string.Join("\n", lines);
        }

        public void ShowMessageModal(string title, string body, string confirmLabel, Action onConfirm)
        {
            ShowChoiceModal(title, body, new[] { confirmLabel }, _ => onConfirm(), false, null, null);
        }

        public void ShowChoiceModal(string title, string body, IReadOnlyList<string> options, Action<int> onSelected, bool showCloseButton, string? secondaryLabel, Action? onSecondary)
        {
            ClearModalContent();
            _modalBlocker!.SetActive(true);

            CreateText(_modalRoot!, title, 28, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(680f, 48f));
            CreateText(_modalRoot!, body, 18, TextAnchor.UpperLeft, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -84f), new Vector2(680f, 120f));

            var buttonsRoot = CreateRect("Buttons", _modalRoot!, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(620f, 220f));
            var layout = buttonsRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(0, 0, 0, 0);

            for (var i = 0; i < options.Count; i++)
            {
                var index = i;
                CreateButton(buttonsRoot, options[i], new Color(0.22f, 0.29f, 0.42f), () =>
                {
                    _modalBlocker.SetActive(false);
                    onSelected(index);
                });
            }

            if (!string.IsNullOrEmpty(secondaryLabel) && onSecondary != null)
            {
                CreateButton(buttonsRoot, secondaryLabel, new Color(0.38f, 0.34f, 0.18f), onSecondary);
            }

            if (showCloseButton)
            {
                CreateButton(buttonsRoot, "关闭", new Color(0.22f, 0.22f, 0.22f), () => _modalBlocker.SetActive(false));
            }
        }

        public void ShowMerchantModal(string title, string body, IReadOnlyList<string> offers, Action<int> onBuy, Action onReroll, Action onClose, string rerollLabel)
        {
            ShowChoiceModal(title, body, offers, onBuy, true, rerollLabel, onReroll);
            var closeButton = CreateButton(_modalRoot!, "结束商人阶段", new Color(0.18f, 0.35f, 0.21f), () =>
            {
                _modalBlocker!.SetActive(false);
                onClose();
            });
            var rect = closeButton.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 26f);
            rect.sizeDelta = new Vector2(220f, 44f);
        }

        public Vector2 GetPlotAnchoredPosition(int x, int y)
        {
            var total = CellSize * 5f + CellSpacing * 4f;
            var startX = (-total * 0.5f) + (CellSize * 0.5f);
            var startY = (total * 0.5f) - (CellSize * 0.5f);
            return new Vector2(startX + x * (CellSize + CellSpacing), startY - y * (CellSize + CellSpacing));
        }

        public void ClearBattleViews()
        {
            foreach (var battleView in _battleViews)
            {
                if (battleView != null)
                {
                    Destroy(battleView);
                }
            }
            _battleViews.Clear();
        }

        public GameObject CreateBattleView(string label, Sprite? sprite, Color tint, Vector2 position)
        {
            var root = new GameObject($"Battle_{label}", typeof(RectTransform));
            root.transform.SetParent(_battleOverlay, false);
            var rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(42f, 42f);
            rect.anchoredPosition = position;

            var image = root.AddComponent<Image>();
            image.color = tint;
            image.raycastTarget = false;
            if (sprite != null)
            {
                image.sprite = sprite;
                image.preserveAspect = true;
            }

            var text = CreateText(rect, label, 14, TextAnchor.LowerCenter, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, -10f), new Vector2(60f, 20f));
            text.color = Color.white;
            _battleViews.Add(root);
            return root;
        }

        private void BuildCanvas()
        {
            var canvasRoot = new GameObject("NineKingsCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasRoot.transform.SetParent(transform, false);
            _canvas = canvasRoot.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.matchWidthOrHeight = 0.6f;

            EnsureEventSystem();

            var rootRect = canvasRoot.transform as RectTransform;

            var topBar = CreateRect("TopBar", rootRect, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(0f, 64f), new Vector2(0.5f, 1f));
            topBar.offsetMin = new Vector2(0f, -64f);
            topBar.offsetMax = Vector2.zero;
            AddImage(topBar, new Color(0.09f, 0.12f, 0.17f, 0.95f));
            _yearText = CreateText(topBar, "年份：-", 24, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(180f, 40f));
            _livesText = CreateText(topBar, "生命：-", 24, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(220f, 0f), new Vector2(180f, 40f));
            _goldText = CreateText(topBar, "金币：-", 24, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(410f, 0f), new Vector2(180f, 40f));
            _enemyText = CreateText(topBar, "敌方王国：-", 24, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(600f, 0f), new Vector2(360f, 40f));
            _phaseText = CreateText(topBar, "阶段：主菜单", 24, TextAnchor.MiddleRight, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(280f, 40f));

            var middleRoot = CreateRect("MiddleRoot", rootRect, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            middleRoot.offsetMin = new Vector2(0f, 150f);
            middleRoot.offsetMax = new Vector2(0f, -80f);

            var leftPanel = CreateRect("LeftPanel", middleRoot, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(18f, 0f), new Vector2(220f, 0f), new Vector2(0f, 0.5f));
            AddImage(leftPanel, new Color(0.11f, 0.14f, 0.19f, 0.92f));
            CreateText(leftPanel, "井", 24, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(180f, 32f));
            var wellRect = CreateRect("WellDrop", leftPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(150f, 150f));
            AddImage(wellRect, new Color(0.19f, 0.12f, 0.10f, 0.95f));
            _wellDropView = wellRect.gameObject.AddComponent<NKWellDropView>();
            _wellDropView.Initialize(this);
            CreateText(wellRect, "弃牌入井\n+9 金币", 20, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120f, 60f));

            var boardPanel = CreateRect("BoardPanel", middleRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 620f));
            AddImage(boardPanel, new Color(0.15f, 0.19f, 0.14f, 0.95f));
            _boardGrid = CreateRect("BoardGrid", boardPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(510f, 510f));
            var grid = _boardGrid.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;
            grid.cellSize = new Vector2(CellSize, CellSize);
            grid.spacing = new Vector2(CellSpacing, CellSpacing);
            grid.childAlignment = TextAnchor.UpperLeft;

            for (var y = 0; y < 5; y++)
            {
                for (var x = 0; x < 5; x++)
                {
                    var plotRect = CreateRect($"Plot_{x}_{y}", _boardGrid, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(CellSize, CellSize));
                    AddImage(plotRect, new Color(0.27f, 0.32f, 0.24f, 1f));
                    var plotView = plotRect.gameObject.AddComponent<NKPlotView>();
                    plotView.Initialize(this, x, y);
                    _plotViews[new Vector2Int(x, y)] = plotView;
                }
            }

            _battleOverlay = CreateRect("BattleOverlay", boardPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(510f, 510f));

            var rightPanel = CreateRect("RightPanel", middleRoot, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-18f, 0f), new Vector2(280f, 0f), new Vector2(1f, 0.5f));
            AddImage(rightPanel, new Color(0.11f, 0.14f, 0.19f, 0.92f));
            CreateText(rightPanel, "地块详情", 24, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(220f, 32f));
            _detailsText = CreateText(rightPanel, "悬停或点击一个地块查看详情。", 16, TextAnchor.UpperLeft, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -72f), new Vector2(230f, 300f));
            CreateText(rightPanel, "战斗日志", 24, TextAnchor.UpperCenter, new Vector2(0.5f, 0.45f), new Vector2(0.5f, 0.45f), new Vector2(0f, 0f), new Vector2(220f, 30f));
            _logText = CreateText(rightPanel, string.Empty, 14, TextAnchor.UpperLeft, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 180f), new Vector2(230f, 220f));

            _handRoot = CreateRect("HandRoot", rootRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 76f), new Vector2(980f, 140f));
            var handLayout = _handRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            handLayout.spacing = 12f;
            handLayout.childControlWidth = false;
            handLayout.childControlHeight = false;
            handLayout.childForceExpandWidth = false;
            handLayout.childForceExpandHeight = false;
            handLayout.childAlignment = TextAnchor.MiddleCenter;

            _modalBlocker = new GameObject("ModalBlocker", typeof(RectTransform), typeof(Image));
            _modalBlocker.transform.SetParent(rootRect, false);
            var modalBlockerRect = _modalBlocker.GetComponent<RectTransform>();
            modalBlockerRect.anchorMin = Vector2.zero;
            modalBlockerRect.anchorMax = Vector2.one;
            modalBlockerRect.offsetMin = Vector2.zero;
            modalBlockerRect.offsetMax = Vector2.zero;
            _modalBlocker.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
            _modalBlocker.SetActive(false);
            _modalRoot = CreateRect("ModalRoot", modalBlockerRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 480f));
            AddImage(_modalRoot, new Color(0.15f, 0.17f, 0.23f, 0.98f));

            _gmRoot = CreateRect("GMRoot", rootRect, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -82f), new Vector2(460f, 560f), new Vector2(1f, 1f));
            AddImage(_gmRoot, new Color(0.09f, 0.09f, 0.12f, 0.97f));
            CreateText(_gmRoot, "GM 调试面板", 24, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(240f, 32f));
            _gmSummaryText = CreateText(_gmRoot, "当前没有运行中的对局。", 15, TextAnchor.UpperLeft, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -64f), new Vector2(410f, 46f));

            var inputRoot = CreateRect("GMInput", _gmRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -126f), new Vector2(420f, 40f));
            AddImage(inputRoot, new Color(0.16f, 0.18f, 0.22f, 1f));
            _gmInput = CreateInputField(inputRoot, "例如：set_gold 99");

            CreateActionButton(_gmRoot, "执行", new Vector2(92f, 36f), new Vector2(-50f, -174f), new Color(0.25f, 0.38f, 0.22f), SubmitGmCommand).gameObject.name = "GMExecuteButton";
            CreateActionButton(_gmRoot, "清空", new Vector2(92f, 36f), new Vector2(58f, -174f), new Color(0.28f, 0.28f, 0.32f), () =>
            {
                if (_gmInput == null)
                {
                    return;
                }
                _gmInput.text = string.Empty;
                _gmInput.ActivateInputField();
                _gmInput.Select();
            }).gameObject.name = "GMClearButton";

            var quickButtonsRoot = CreateRect("GMQuickButtons", _gmRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -244f), new Vector2(420f, 120f));
            var quickGrid = quickButtonsRoot.gameObject.AddComponent<GridLayoutGroup>();
            quickGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            quickGrid.constraintCount = 3;
            quickGrid.cellSize = new Vector2(132f, 34f);
            quickGrid.spacing = new Vector2(10f, 8f);
            quickGrid.childAlignment = TextAnchor.UpperCenter;
            quickGrid.padding = new RectOffset(0, 0, 0, 0);
            CreateCommandButton(quickButtonsRoot, "暂停", "pause");
            CreateCommandButton(quickButtonsRoot, "继续", "resume");
            CreateCommandButton(quickButtonsRoot, "重置本局", "reset_run");
            CreateCommandButton(quickButtonsRoot, "+50 金币", "add_gold 50");
            CreateCommandButton(quickButtonsRoot, "+1 生命", "add_lives 1");
            CreateCommandButton(quickButtonsRoot, "第 33 年", "set_year 33");
            CreateCommandButton(quickButtonsRoot, "议会事件", "force_event royal_council");
            CreateCommandButton(quickButtonsRoot, "商人事件", "force_event merchant");
            CreateCommandButton(quickButtonsRoot, "扩地事件", "force_event tower_expand");

            _gmHelpText = CreateText(_gmRoot, GetDefaultGmHelpText(), 13, TextAnchor.UpperLeft, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -336f), new Vector2(420f, 128f));
            CreateText(_gmRoot, "执行记录", 18, TextAnchor.UpperLeft, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 196f), new Vector2(420f, 24f));
            _gmLogText = CreateText(_gmRoot, GetDefaultGmLogText(), 13, TextAnchor.UpperLeft, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(420f, 168f));
            _gmRoot.gameObject.SetActive(false);
        }

        private void EnsureEventSystem()
        {
            var eventSystem = FindFirstObjectByType<EventSystem>();
            if (eventSystem != null)
            {
                return;
            }

            var eventGo = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventGo.transform.SetParent(transform, false);
        }

        private void RebuildBoard(NineKingsContentDatabase? database, NKRunState? state)
        {
            foreach (var pair in _plotViews)
            {
                NKPlotState? plotState = state != null ? state.GetPlot(pair.Key.x, pair.Key.y) : null;
                NKCardDefinition? definition = plotState != null && database != null && !plotState.IsEmpty ? database.GetCard(plotState.cardId) : null;
                pair.Value.Refresh(plotState, definition);
            }
        }

        private void RebuildHand(NineKingsContentDatabase? database, NKRunState? state)
        {
            foreach (var view in _cardViews)
            {
                if (view != null)
                {
                    Destroy(view.gameObject);
                }
            }
            _cardViews.Clear();

            if (database == null || state == null)
            {
                return;
            }

            foreach (var cardId in state.HandCardIds)
            {
                var definition = database.GetCard(cardId);
                if (definition == null)
                {
                    continue;
                }

                var cardRect = CreateRect($"Card_{cardId}", _handRoot!, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(150f, 120f));
                AddImage(cardRect, new Color(0.25f, 0.29f, 0.38f, 1f));
                var cardView = cardRect.gameObject.AddComponent<NKCardView>();
                cardView.Initialize(this, definition);
                _cardViews.Add(cardView);
            }
        }

        private void ClearModalContent()
        {
            for (var i = _modalRoot!.childCount - 1; i >= 0; i--)
            {
                Destroy(_modalRoot.GetChild(i).gameObject);
            }
        }

        private void SubmitGmCommand()
        {
            if (_controller == null || _gmInput == null)
            {
                return;
            }

            var command = _gmInput.text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(command))
            {
                UpdateGmLog(Array.Empty<string>());
                _gmInput.ActivateInputField();
                _gmInput.Select();
                return;
            }

            _controller.ExecuteGmCommand(command);
            _gmInput.text = string.Empty;
            _gmInput.ActivateInputField();
            _gmInput.Select();
        }

        private string GetDefaultGmHelpText()
        {
            return "常用指令：\n"
                   + "pause / resume\n"
                   + "set_year 12\n"
                   + "add_gold 50\n"
                   + "set_lives 5\n"
                   + "force_event merchant\n"
                   + "force_enemy king_blood\n"
                   + "win_battle / lose_battle";
        }

        private string GetDefaultGmLogText()
        {
            return "GM 面板已开启。\n在输入框里输入英文指令，或直接点击上方快捷按钮。";
        }

        private static Font LoadUIFont()
        {
            try
            {
                var dynamicFont = Font.CreateDynamicFontFromOSFont(
                    new[] { "PingFang SC", "Hiragino Sans GB", "Arial Unicode MS", "Microsoft YaHei", "SimHei" },
                    18);
                if (dynamicFont != null)
                {
                    return dynamicFont;
                }
            }
            catch
            {
                // Ignore and fall back to built-in font.
            }

            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private static RectTransform CreateRect(string name, RectTransform? parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size, Vector2? pivot = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot ?? new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            return rect;
        }

        private Image AddImage(RectTransform rect, Color color)
        {
            var image = rect.gameObject.GetComponent<Image>() ?? rect.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private Text CreateText(RectTransform parent, string text, int fontSize, TextAnchor alignment, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size)
        {
            var rect = CreateRect("Text", parent, anchorMin, anchorMax, anchoredPosition, size);
            var component = rect.gameObject.AddComponent<Text>();
            component.font = _font;
            component.fontSize = fontSize;
            component.alignment = alignment;
            component.text = text;
            component.color = Color.white;
            component.horizontalOverflow = HorizontalWrapMode.Wrap;
            component.verticalOverflow = VerticalWrapMode.Overflow;
            return component;
        }

        private Button CreateButton(RectTransform parent, string label, Color color, Action onClick)
        {
            var rect = CreateRect(label.Replace(' ', '_'), parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(540f, 44f));
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick());
            var text = CreateText(rect, label, 18, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500f, 28f));
            text.color = Color.white;
            return button;
        }

        private Button CreateActionButton(RectTransform parent, string label, Vector2 size, Vector2 anchoredPosition, Color color, Action onClick)
        {
            var rect = CreateRect(label.Replace(' ', '_'), parent, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), anchoredPosition, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick());
            var text = CreateText(rect, label, 16, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size.x - 12f, size.y - 8f));
            text.color = Color.white;
            return button;
        }

        private void CreateCommandButton(RectTransform parent, string label, string command)
        {
            var go = new GameObject(label.Replace(' ', '_'), typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = new Color(0.22f, 0.31f, 0.42f, 1f);
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => _controller?.ExecuteGmCommand(command));
            CreateText(go.transform as RectTransform, label, 14, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(118f, 24f));
        }

        private InputField CreateInputField(RectTransform root, string placeholder)
        {
            var input = root.gameObject.AddComponent<InputField>();
            input.lineType = InputField.LineType.SingleLine;
            var text = CreateText(root, string.Empty, 16, TextAnchor.MiddleLeft, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            text.rectTransform.offsetMin = new Vector2(8f, 6f);
            text.rectTransform.offsetMax = new Vector2(-8f, -6f);
            var placeholderText = CreateText(root, placeholder, 16, TextAnchor.MiddleLeft, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            placeholderText.rectTransform.offsetMin = new Vector2(8f, 6f);
            placeholderText.rectTransform.offsetMax = new Vector2(-8f, -6f);
            placeholderText.color = new Color(1f, 1f, 1f, 0.35f);
            input.textComponent = text;
            input.placeholder = placeholderText;
            input.onEndEdit.AddListener(_ =>
            {
                if (_gmRoot == null || !_gmRoot.gameObject.activeInHierarchy)
                {
                    return;
                }

                var keyboard = Keyboard.current;
                if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))
                {
                    SubmitGmCommand();
                }
            });
            return input;
        }

        private sealed class NKCardView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
        {
            private NineKingsRuntimeUI? _ui;
            private NKCardDefinition? _definition;
            private RectTransform? _ghost;
            private bool _resolved;

            public string CardId => _definition?.cardId ?? string.Empty;

            public void Initialize(NineKingsRuntimeUI ui, NKCardDefinition definition)
            {
                _ui = ui;
                _definition = definition;
                var image = GetComponent<Image>();
                image.color = definition.ownerKingId == "king_nothing" ? new Color(0.29f, 0.38f, 0.53f) : new Color(0.39f, 0.24f, 0.24f);
                if (definition.plotSprite != null)
                {
                    image.sprite = definition.plotSprite;
                    image.preserveAspect = true;
                }
                ui.CreateText(transform as RectTransform, NKChineseText.CardName(definition), 16, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(130f, 40f));
                ui.CreateText(transform as RectTransform, NKChineseText.CardType(definition.cardType), 14, TextAnchor.LowerCenter, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(130f, 20f));
            }

            public void MarkResolved()
            {
                _resolved = true;
            }

            public void OnBeginDrag(PointerEventData eventData)
            {
                if (_ui == null || _definition == null)
                {
                    return;
                }

                _resolved = false;
                var ghostGo = new GameObject($"Ghost_{_definition.cardId}", typeof(RectTransform), typeof(Image));
                ghostGo.transform.SetParent(_ui.transform, false);
                _ghost = ghostGo.GetComponent<RectTransform>();
                _ghost.sizeDelta = new Vector2(140f, 100f);
                var image = ghostGo.GetComponent<Image>();
                image.color = new Color(1f, 1f, 1f, 0.85f);
                image.raycastTarget = false;
                if (_definition.plotSprite != null)
                {
                    image.sprite = _definition.plotSprite;
                    image.preserveAspect = true;
                }
                OnDrag(eventData);
            }

            public void OnDrag(PointerEventData eventData)
            {
                if (_ghost != null)
                {
                    _ghost.position = eventData.position;
                }
            }

            public void OnEndDrag(PointerEventData eventData)
            {
                if (_ghost != null)
                {
                    Destroy(_ghost.gameObject);
                }
                _ghost = null;
                _resolved = false;
            }
        }

        private sealed class NKPlotView : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerClickHandler
        {
            private NineKingsRuntimeUI? _ui;
            private int _x;
            private int _y;
            private Image? _image;
            private Image? _icon;
            private Text? _levelText;
            private NKPlotState? _plotState;
            private NKCardDefinition? _definition;

            public void Initialize(NineKingsRuntimeUI ui, int x, int y)
            {
                _ui = ui;
                _x = x;
                _y = y;
                _image = GetComponent<Image>();
                var iconRect = CreateRect("Icon", transform as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -4f), new Vector2(72f, 72f));
                _icon = iconRect.gameObject.AddComponent<Image>();
                _icon.raycastTarget = false;
                _levelText = ui.CreateText(transform as RectTransform, string.Empty, 18, TextAnchor.UpperRight, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-8f, -8f), new Vector2(28f, 24f));
            }

            public void Refresh(NKPlotState? plotState, NKCardDefinition? definition)
            {
                _plotState = plotState;
                _definition = definition;
                if (plotState == null)
                {
                    _image!.color = new Color(0.12f, 0.12f, 0.12f, 1f);
                    _icon!.enabled = false;
                    _levelText!.text = string.Empty;
                    return;
                }

                _image!.color = plotState.unlocked ? new Color(0.34f, 0.41f, 0.29f, 1f) : new Color(0.16f, 0.16f, 0.16f, 1f);
                if (plotState.blessingMarked)
                {
                    _image.color = new Color(0.55f, 0.44f, 0.18f, 1f);
                }

                if (!plotState.IsEmpty && definition != null)
                {
                    _icon!.enabled = true;
                    _icon.sprite = definition.plotSprite;
                    _icon.preserveAspect = true;
                    _levelText!.text = plotState.level.ToString();
                }
                else
                {
                    _icon!.enabled = false;
                    _levelText!.text = plotState.unlocked ? string.Empty : "锁";
                }
            }

            public void OnDrop(PointerEventData eventData)
            {
                var cardView = eventData.pointerDrag != null ? eventData.pointerDrag.GetComponent<NKCardView>() : null;
                if (_ui?._controller == null || cardView == null)
                {
                    return;
                }

                if (_ui._controller.TryPlayCardFromUI(cardView.CardId, _x, _y))
                {
                    cardView.MarkResolved();
                }
            }

            public void OnPointerEnter(PointerEventData eventData)
            {
                if (_plotState == null)
                {
                    _ui?.SetDetails("当前没有地块数据。");
                    return;
                }

                var text = $"地块 ({_x},{_y})\n已解锁：{NKChineseText.Bool(_plotState.unlocked)}\n卡牌：{(_definition == null ? "空" : NKChineseText.CardName(_definition))}\n等级：{_plotState.level}\n护盾：{_plotState.shieldCharges}\n单位加成：{_plotState.unitBonus}\n伤害倍率：x{_plotState.damageMultiplier:0.00}\n累计伤害：{_plotState.totalDamage}\n击杀：{_plotState.totalKills}";
                _ui?.SetDetails(text);
            }

            public void OnPointerClick(PointerEventData eventData)
            {
                _ui?._controller?.OnPlotClicked(_x, _y);
            }
        }

        private sealed class NKWellDropView : MonoBehaviour, IDropHandler
        {
            private NineKingsRuntimeUI? _ui;

            public void Initialize(NineKingsRuntimeUI ui)
            {
                _ui = ui;
            }

            public void OnDrop(PointerEventData eventData)
            {
                var cardView = eventData.pointerDrag != null ? eventData.pointerDrag.GetComponent<NKCardView>() : null;
                if (cardView == null)
                {
                    return;
                }

                _ui?._controller?.TryDiscardCardFromUI(cardView.CardId);
            }
        }
    }
}