using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MmoTemplate.Rpg
{
    /// <summary>
    /// The entire game UI, built in code at runtime.
    ///
    /// Deliberately NOT authored as a serialized scene hierarchy: wiring uGUI
    /// from an Editor script kept producing subtly broken windows (components
    /// initialising before their references existed, and edit-time
    /// AddListener calls that never serialise). Building it here means the
    /// layout is deterministic and there are no references to lose.
    ///
    /// Public API: Notify, SetPrompt, ShowDialog, Close, AppendChat.
    /// </summary>
    public partial class RpgHud : MonoBehaviour
    {
        public static RpgHud Instance { get; private set; }

        private static readonly Color Parchment = new(0.89f, 0.82f, 0.66f);
        private static readonly Color Ink = new(0.91f, 0.91f, 0.87f);
        private static readonly Color PanelBg = new(0.06f, 0.055f, 0.05f, 0.93f);
        private static readonly Color PanelEdge = new(0.59f, 0.50f, 0.34f);

        private Font _font;

        // Gameplay chrome — hidden while a dialogue window is open.
        private readonly List<GameObject> _gameplayChrome = new();

        private Text _statsText, _questText, _promptText, _toastText, _chatLog;
        private Image _healthFill, _xpFill;
        private GameObject _dialogPanel, _questPanel;
        private Text _dialogTitle, _dialogBody;
        private RectTransform _choiceHolder;
        private InputField _chatInput;
        private float _toastTimer;

        public bool DialogOpen => _dialogPanel != null && _dialogPanel.activeSelf;
        public bool ChatFocused => _chatInput != null && _chatInput.isFocused;
        /// <summary>True when the UI is swallowing input, so gameplay should ignore it.</summary>
        public bool InputBlocked => DialogOpen || ChatFocused || (_journal != null && _journal.activeSelf);

        private void Awake()
        {
            Instance = this;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            EnsureEventSystem();
            Build();
            ConnectEvents();
        }

        private void OnDestroy()
        {
            DisconnectEvents();
            GameEvents.InputBlocked = false;
            if (Instance == this) Instance = null;
        }

        // ------------------------------------------------------------------
        // Construction
        // ------------------------------------------------------------------

        private void Build()
        {
            var canvasGo = new GameObject("HudCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;
            var root = (RectTransform)canvasGo.transform;

            BuildPlayerFrame(root);
            BuildQuestTracker(root);
            BuildPrompt(root);
            BuildToast(root);
            BuildChat(root);
            BuildDialog(root);
            BuildClassicChrome(root);
        }

        private void BuildPlayerFrame(RectTransform root)
        {
            var panel = Panel(root, new Vector2(280, 132), new Vector2(16, -16),
                new Vector2(0, 1), new Vector2(0, 1));
            _gameplayChrome.Add(panel.gameObject);

            Label(panel, "✦  THE WAYFARER", 15, Parchment, new Vector2(12, -10), new Vector2(240, 20));
            _healthFill = Bar(panel, new Vector2(12, -34), new Vector2(244, 16), new Color(0.42f, 0.57f, 0.26f));
            _xpFill = Bar(panel, new Vector2(12, -54), new Vector2(244, 7), new Color(0.53f, 0.45f, 0.70f));
            _statsText = Label(panel, "", 13, Ink, new Vector2(12, -88), new Vector2(244, 30));
        }

        private void BuildQuestTracker(RectTransform root)
        {
            var panel = Panel(root, new Vector2(280, 112), new Vector2(-16, -16),
                new Vector2(1, 1), new Vector2(1, 1));
            _questPanel = panel.gameObject;
            _gameplayChrome.Add(_questPanel);

            Label(panel, "QUEST", 15, Parchment, new Vector2(12, -10), new Vector2(250, 20));
            _questText = Label(panel, "", 14, Ink, new Vector2(12, -34), new Vector2(256, 70));
            _questText.alignment = TextAnchor.UpperLeft;
        }

        private void BuildPrompt(RectTransform root)
        {
            _promptText = Label(root, "", 19, new Color(1f, 0.89f, 0.65f),
                new Vector2(0, 132), new Vector2(700, 30), new Vector2(0.5f, 0), new Vector2(0.5f, 0));
            _promptText.alignment = TextAnchor.MiddleCenter;
            _gameplayChrome.Add(_promptText.gameObject);
        }

        private void BuildToast(RectTransform root)
        {
            _toastText = Label(root, "", 17, new Color(0.96f, 0.87f, 0.67f),
                new Vector2(0, -150), new Vector2(760, 28), new Vector2(0.5f, 1), new Vector2(0.5f, 1));
            _toastText.alignment = TextAnchor.MiddleCenter;
        }

        private void BuildChat(RectTransform root)
        {
            var panel = Panel(root, new Vector2(360, 168), new Vector2(16, 16),
                new Vector2(0, 0), new Vector2(0, 0));
            _gameplayChrome.Add(panel.gameObject);

            _chatLog = Label(panel, "", 13, Ink, new Vector2(12, -10), new Vector2(336, 116));
            _chatLog.alignment = TextAnchor.LowerLeft;

            // NOTE: the InputField component is added LAST, after its text and
            // placeholder children exist — adding it first makes it cache null
            // references in OnEnable and render incorrectly.
            var fieldGo = new GameObject("ChatInput", typeof(RectTransform), typeof(Image));
            fieldGo.transform.SetParent(panel, false);
            var frt = (RectTransform)fieldGo.transform;
            frt.anchorMin = new Vector2(0, 0);
            frt.anchorMax = new Vector2(0, 0);
            frt.pivot = new Vector2(0, 0);
            frt.anchoredPosition = new Vector2(12, 12);
            frt.sizeDelta = new Vector2(336, 26);
            fieldGo.GetComponent<Image>().color = new Color(1, 1, 1, 0.07f);

            var textC = Label((RectTransform)fieldGo.transform, "", 13, Ink, Vector2.zero, Vector2.zero);
            Stretch((RectTransform)textC.transform, 6);
            textC.supportRichText = false;
            textC.alignment = TextAnchor.MiddleLeft;

            var placeholder = Label((RectTransform)fieldGo.transform, "Press Enter to chat…", 13,
                new Color(1, 1, 1, 0.35f), Vector2.zero, Vector2.zero);
            Stretch((RectTransform)placeholder.transform, 6);
            placeholder.fontStyle = FontStyle.Italic;
            placeholder.alignment = TextAnchor.MiddleLeft;

            _chatInput = fieldGo.AddComponent<InputField>();
            _chatInput.textComponent = textC;
            _chatInput.placeholder = placeholder;
            _chatInput.targetGraphic = fieldGo.GetComponent<Image>();
            _chatInput.onSubmit.AddListener(OnChatSubmit); // runtime listener: fine, we're not serialising
        }

        private void BuildDialog(RectTransform root)
        {
            var panel = Panel(root, new Vector2(620, 320), Vector2.zero,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            _dialogPanel = panel.gameObject;

            _dialogTitle = Label(panel, "", 21, Parchment, new Vector2(24, -20), new Vector2(572, 28));
            _dialogBody = Label(panel, "", 16, Ink, new Vector2(24, -56), new Vector2(572, 150));
            _dialogBody.alignment = TextAnchor.UpperLeft;

            var holder = new GameObject("Choices", typeof(RectTransform), typeof(VerticalLayoutGroup));
            holder.transform.SetParent(panel, false);
            _choiceHolder = (RectTransform)holder.transform;
            _choiceHolder.anchorMin = new Vector2(0, 0);
            _choiceHolder.anchorMax = new Vector2(0, 0);
            _choiceHolder.pivot = new Vector2(0, 0);
            _choiceHolder.anchoredPosition = new Vector2(24, 20);
            _choiceHolder.sizeDelta = new Vector2(572, 110);
            var vlg = holder.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 6;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.LowerCenter;

            _dialogPanel.SetActive(false);
        }

        // ------------------------------------------------------------------
        // Public API
        // ------------------------------------------------------------------

        public void Notify(string text)
        {
            if (_toastText == null) return;
            _toastText.text = text;
            _toastTimer = 4.5f;
        }

        public void SetPrompt(string text)
        {
            if (_promptText != null) _promptText.text = text ?? "";
        }

        public void AppendChat(string line)
        {
            if (_chatLog == null) return;
            _chatLog.text += line + "\n";
            var lines = _chatLog.text.Split('\n');
            if (lines.Length > 9)
                _chatLog.text = string.Join("\n", lines, lines.Length - 9, 9);
        }

        /// <summary>Opens a dialogue window and hides the gameplay HUD behind it,
        /// so nothing overlaps the text or the choice buttons.</summary>
        public void ShowDialog(string title, string body, params (string label, Action action)[] options)
        {
            _dialogTitle.text = title;
            BeginTyping(body);

            for (int i = _choiceHolder.childCount - 1; i >= 0; i--)
                Destroy(_choiceHolder.GetChild(i).gameObject);

            foreach (var (label, action) in options)
            {
                var b = MakeButton(_choiceHolder, label);
                var captured = action;
                b.onClick.AddListener(() => captured?.Invoke());
            }

            _dialogPanel.SetActive(true);
            GameEvents.InputBlocked = true;
            SetChromeVisible(false);
        }

        public void Close()
        {
            if (_dialogPanel != null) _dialogPanel.SetActive(false);
            GameEvents.InputBlocked = ChatFocused;
            SetChromeVisible(true);
        }

        private void SetChromeVisible(bool visible)
        {
            foreach (var go in _gameplayChrome)
                if (go != null) go.SetActive(visible);
        }

        // ------------------------------------------------------------------
        // Per-frame refresh
        // ------------------------------------------------------------------

        public void Refresh(PlayerStats stats, string questTitle, string objective)
        {
            if (stats != null && _statsText != null)
            {
                float pct = stats.MaxHealth <= 0 ? 0 : Mathf.Clamp01(stats.Health.Value / (float)stats.MaxHealth);
                _healthFill.rectTransform.anchorMax = new Vector2(pct, 1);
                float need = stats.XpForNextLevel;
                _xpFill.rectTransform.anchorMax = new Vector2(need <= 0 ? 0 : Mathf.Clamp01(stats.Xp.Value / need), 1);
                _statsText.text =
                    $"Lv {stats.Level.Value}   {stats.Health.Value} / {stats.MaxHealth} HP\n" +
                    $"<color=#e5bf58>{stats.Gold.Value / 10000}g</color>  <color=#cbd0d6>{stats.Gold.Value / 100 % 100}s</color>  <color=#c58d61>{stats.Gold.Value % 100}c</color>   ·   {stats.Potions.Value} potions";
            }

            if (_questText != null) _questText.text = objective;

        }

        private void Update()
        {
            UpdateClassicChrome();
            GameEvents.InputBlocked = InputBlocked;
            if (Input.GetKeyDown(KeyCode.Escape)) { Close(); _journal.SetActive(false); _chatInput.DeactivateInputField(); }
            if (Input.GetKeyDown(KeyCode.L) && !ChatFocused && !DialogOpen) _journal.SetActive(!_journal.activeSelf);
            if (_toastTimer > 0)
            {
                _toastTimer -= Time.deltaTime;
                if (_toastTimer <= 0 && _toastText != null) _toastText.text = "";
            }

            // Enter focuses chat when it isn't already focused.
            if (!DialogOpen && !ChatFocused && Input.GetKeyDown(KeyCode.Return))
            {
                _chatInput.Select();
                _chatInput.ActivateInputField();
            }
        }

        private void OnChatSubmit(string text)
        {
            text = text?.Trim();
            _chatInput.text = "";
            if (string.IsNullOrEmpty(text)) return;
            ChatRelay.Instance?.Send(text);
            _chatInput.DeactivateInputField();
        }

        // ------------------------------------------------------------------
        // Small UI factory
        // ------------------------------------------------------------------

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private RectTransform Panel(RectTransform parent, Vector2 size, Vector2 offset, Vector2 anchor, Vector2 pivot)
        {
            var go = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = offset;
            rt.sizeDelta = size;
            go.GetComponent<Image>().color = PanelBg;

            var outline = go.AddComponent<Outline>();
            outline.effectColor = PanelEdge;
            outline.effectDistance = new Vector2(2, -2);
            foreach (Vector2 corner in new[] { Vector2.zero, Vector2.one, new Vector2(1,0), new Vector2(0,1) })
            {
                var stud = new GameObject("Gilded corner", typeof(RectTransform), typeof(Image));
                stud.transform.SetParent(rt,false);
                var sr = (RectTransform)stud.transform;
                sr.anchorMin = sr.anchorMax = corner; sr.sizeDelta = new Vector2(7,7);
                stud.GetComponent<Image>().color = PanelEdge;
                stud.GetComponent<Image>().raycastTarget = false;
            }
            return rt;
        }

        private Text Label(RectTransform parent, string value, int size, Color color, Vector2 offset, Vector2 dimensions,
            Vector2? anchor = null, Vector2? pivot = null)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor ?? new Vector2(0, 1);
            rt.pivot = pivot ?? new Vector2(0, 1);
            rt.anchoredPosition = offset;
            rt.sizeDelta = dimensions;
            var t = go.GetComponent<Text>();
            t.font = _font;
            t.fontSize = size;
            t.color = color;
            t.text = value;
            t.alignment = TextAnchor.UpperLeft;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        private Image Bar(RectTransform parent, Vector2 offset, Vector2 dimensions, Color color)
        {
            var back = new GameObject("BarBg", typeof(RectTransform), typeof(Image));
            back.transform.SetParent(parent, false);
            var brt = (RectTransform)back.transform;
            brt.anchorMin = brt.anchorMax = new Vector2(0, 1);
            brt.pivot = new Vector2(0, 1);
            brt.anchoredPosition = offset;
            brt.sizeDelta = dimensions;
            back.GetComponent<Image>().color = new Color(0, 0, 0, 0.55f);

            var fill = new GameObject("BarFill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(back.transform, false);
            var frt = (RectTransform)fill.transform;
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = Vector2.one;
            frt.offsetMin = Vector2.zero;
            frt.offsetMax = Vector2.zero;
            var img = fill.GetComponent<Image>();
            img.color = color;
            return img;
        }

        private Button MakeButton(RectTransform parent, string label)
        {
            var go = new GameObject("Choice", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0.21f, 0.20f, 0.16f, 1f);
            go.GetComponent<LayoutElement>().minHeight = 32;

            var t = Label((RectTransform)go.transform, label, 15, Parchment, Vector2.zero, Vector2.zero);
            Stretch((RectTransform)t.transform, 0);
            t.alignment = TextAnchor.MiddleCenter;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = go.GetComponent<Image>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.38f, 0.33f, 0.22f);
            colors.pressedColor = new Color(0.48f, 0.41f, 0.27f);
            btn.colors = colors;
            return btn;
        }

        private static void Stretch(RectTransform rt, float pad)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(pad, pad);
            rt.offsetMax = new Vector2(-pad, -pad);
        }
    }
}
