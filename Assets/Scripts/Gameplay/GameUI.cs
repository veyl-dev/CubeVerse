using System;
using CubeVerse.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace CubeVerse.Gameplay
{
    public sealed class GameUI : MonoBehaviour
    {
        private Text timerText, scrambleText, moveText, messageText, modeCaption, soundLabel, hapticLabel, recordsLabel;
        private Button undoButton, redoButton, startButton;
        private Action<int> sizeAction;
        private Action<GameMode> modeAction;
        private Action scrambleAction, restartAction, undoAction, redoAction, soundAction, hapticAction, recordsAction;
        private int selectedSize = 3;
        private GameMode selectedMode = GameMode.Classic;

        public void Initialize(Action<int> onSize, Action<GameMode> onMode, Action onScramble, Action onRestart,
            Action onUndo, Action onRedo, Action onSound, Action onHaptics, Action onRecords)
        {
            sizeAction = onSize; modeAction = onMode; scrambleAction = onScramble; restartAction = onRestart;
            undoAction = onUndo; redoAction = onRedo; soundAction = onSound; hapticAction = onHaptics; recordsAction = onRecords;
            Build();
        }

        public void SetToggles(bool soundOn, bool hapticsOn)
        {
            if (soundLabel != null) soundLabel.text = soundOn ? "SOUND  ON" : "SOUND  OFF";
            if (hapticLabel != null) hapticLabel.text = hapticsOn ? "HAPTICS  ON" : "HAPTICS  OFF";
        }

        public void Refresh(GameMode mode, int size, int moveCount, string scramble, string message,
            bool canUndo, bool canRedo, bool soundOn, bool hapticsOn, int totalSolves)
        {
            selectedMode = mode;
            selectedSize = size;
            if (moveText != null) moveText.text = $"{moveCount:00}  MOVES";
            if (scrambleText != null) scrambleText.text = string.IsNullOrEmpty(scramble) ? "Tap Scramble to begin" : scramble;
            if (messageText != null && message != null) messageText.text = message;
            if (undoButton != null) undoButton.interactable = canUndo;
            if (redoButton != null) redoButton.interactable = canRedo;
            if (modeCaption != null) modeCaption.text = ModeName(mode) + "  /  " + size + "×" + size;
            if (soundLabel != null) soundLabel.text = soundOn ? "SOUND  ON" : "SOUND  OFF";
            if (hapticLabel != null) hapticLabel.text = hapticsOn ? "HAPTICS  ON" : "HAPTICS  OFF";
            if (recordsLabel != null) recordsLabel.text = "RECORDS  " + totalSolves;
            UpdateSelectedStates();
        }

        public void SetClock(double seconds, bool inspecting)
        {
            if (timerText == null) return;
            if (seconds < 0) seconds = 0;
            TimeSpan time = TimeSpan.FromSeconds(seconds);
            string formatted = time.TotalMinutes >= 1
                ? $"{(int)time.TotalMinutes:00}:{time.Seconds:00}.{time.Milliseconds / 10:00}"
                : $"{time.Seconds:00}.{time.Milliseconds / 10:00}";
            timerText.text = formatted;
            timerText.color = inspecting ? new Color(1f, 0.72f, 0.25f) : Color.white;
        }

        private void Build()
        {
            Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            RectTransform root = GetComponent<RectTransform>();
            Panel("Top rail", root, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -116f), new Vector2(0f, 0f), new Color(0.025f, 0.035f, 0.06f, 0.94f));

            RectTransform brand = Panel("Brand", root, new Vector2(0.035f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -93f), new Vector2(0f, -8f), Color.clear);
            Text wordmark = Label("Wordmark", brand, "CV  /  CUBEVERSE", font, 34, TextAnchor.MiddleLeft, Color.white);
            wordmark.fontStyle = FontStyle.Bold;
            Text tagline = Label("Tagline", brand, "TWIST. THINK. SOLVE.", font, 13, TextAnchor.MiddleLeft, new Color(0.47f, 0.57f, 0.70f));
            Stretch(tagline.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.38f), Vector2.zero, Vector2.zero);
            Stretch(wordmark.rectTransform, new Vector2(0f, 0.33f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

            RectTransform tools = Panel("Quick settings", root, new Vector2(0.52f, 1f), new Vector2(0.98f, 1f), new Vector2(0f, -92f), new Vector2(0f, -10f), Color.clear);
            soundLabel = Button("Sound toggle", tools, "SOUND  OFF", font, 15, new Vector2(0.50f, 0.5f), new Vector2(0.49f, 62f), () => soundAction?.Invoke(), false).GetComponentInChildren<Text>();
            hapticLabel = Button("Haptics toggle", tools, "HAPTICS  OFF", font, 15, new Vector2(0.98f, 0.5f), new Vector2(0.46f, 62f), () => hapticAction?.Invoke(), false).GetComponentInChildren<Text>();

            RectTransform modePanel = Panel("Mode selector", root, new Vector2(0.04f, 1f), new Vector2(0.96f, 1f), new Vector2(0f, -205f), new Vector2(0f, -121f), Color.clear);
            MakeModeButton(modePanel, "CLASSIC", GameMode.Classic, 0.18f, font);
            MakeModeButton(modePanel, "SPEED", GameMode.Speedcubing, 0.50f, font);
            MakeModeButton(modePanel, "PRACTICE", GameMode.Practice, 0.82f, font);

            RectTransform sizePanel = Panel("Cube size selector", root, new Vector2(0.12f, 1f), new Vector2(0.88f, 1f), new Vector2(0f, -274f), new Vector2(0f, -206f), Color.clear);
            for (int size = 2; size <= 5; size++)
            {
                int captured = size;
                float x = (size - 1) / 3f;
                Button button = Button("Size " + size, sizePanel, size + "×" + size, font, 19,
                    new Vector2(x, 0.5f), new Vector2(74f, 54f), () => sizeAction?.Invoke(captured), false);
                button.name = "Size " + size;
            }

            timerText = Label("Solve timer", root, "00.00", font, 66, TextAnchor.MiddleCenter, Color.white);
            timerText.fontStyle = FontStyle.Bold;
            timerText.rectTransform.anchorMin = new Vector2(0.18f, 0.5f);
            timerText.rectTransform.anchorMax = new Vector2(0.82f, 0.5f);
            timerText.rectTransform.sizeDelta = new Vector2(0f, 100f);
            timerText.rectTransform.anchoredPosition = new Vector2(0f, 361f);

            modeCaption = Label("Mode and size", root, "CLASSIC  /  3×3", font, 17, TextAnchor.MiddleCenter, new Color(0.42f, 0.81f, 0.76f));
            modeCaption.rectTransform.anchorMin = new Vector2(0.08f, 0.5f);
            modeCaption.rectTransform.anchorMax = new Vector2(0.92f, 0.5f);
            modeCaption.rectTransform.sizeDelta = new Vector2(0f, 34f);
            modeCaption.rectTransform.anchoredPosition = new Vector2(0f, 304f);

            moveText = Label("Move counter", root, "00  MOVES", font, 17, TextAnchor.MiddleCenter, new Color(0.72f, 0.77f, 0.85f));
            moveText.rectTransform.anchorMin = new Vector2(0.25f, 0.5f);
            moveText.rectTransform.anchorMax = new Vector2(0.75f, 0.5f);
            moveText.rectTransform.sizeDelta = new Vector2(0f, 30f);
            moveText.rectTransform.anchoredPosition = new Vector2(0f, 262f);

            RectTransform bottom = Panel("Play controls", root, new Vector2(0.045f, 0f), new Vector2(0.955f, 0f), new Vector2(0f, 84f), new Vector2(0f, 396f), Color.clear);
            scrambleText = Label("Scramble", bottom, "Tap Scramble to begin", font, 18, TextAnchor.MiddleCenter, new Color(0.83f, 0.87f, 0.93f));
            Stretch(scrambleText.rectTransform, new Vector2(0f, 0.69f), new Vector2(1f, 1f), new Vector2(10f, 0f), new Vector2(-10f, 0f));
            scrambleText.horizontalOverflow = HorizontalWrapMode.Wrap;
            scrambleText.verticalOverflow = VerticalWrapMode.Truncate;
            messageText = Label("Status", bottom, "", font, 16, TextAnchor.MiddleCenter, new Color(0.40f, 0.80f, 0.73f));
            Stretch(messageText.rectTransform, new Vector2(0f, 0.52f), new Vector2(1f, 0.70f), Vector2.zero, Vector2.zero);

            Button scrambleButton = Button("Scramble", bottom, "SCRAMBLE", font, 18, new Vector2(0.24f, 0.37f), new Vector2(270f, 72f), () => scrambleAction?.Invoke(), true);
            scrambleButton.name = "Scramble";
            Button restartButton = Button("Restart scramble", bottom, "RESTART", font, 16, new Vector2(0.76f, 0.37f), new Vector2(210f, 64f), () => restartAction?.Invoke(), false);
            restartButton.name = "Restart scramble";

            undoButton = Button("Undo", bottom, "UNDO", font, 15, new Vector2(0.30f, 0.14f), new Vector2(160f, 54f), () => undoAction?.Invoke(), false);
            redoButton = Button("Redo", bottom, "REDO", font, 15, new Vector2(0.53f, 0.14f), new Vector2(160f, 54f), () => redoAction?.Invoke(), false);
            recordsLabel = Button("Local records", bottom, "RECORDS  0", font, 13, new Vector2(0.84f, 0.14f), new Vector2(190f, 54f), () => recordsAction?.Invoke(), false).GetComponentInChildren<Text>();

            Text gestureHint = Label("Gesture hint", root, "DRAG A STICKER TO TURN  ·  DRAG OUTSIDE THE CUBE TO ROTATE VIEW", font, 13, TextAnchor.MiddleCenter, new Color(0.43f, 0.51f, 0.63f));
            gestureHint.rectTransform.anchorMin = new Vector2(0.04f, 0f);
            gestureHint.rectTransform.anchorMax = new Vector2(0.96f, 0f);
            gestureHint.rectTransform.sizeDelta = new Vector2(0f, 26f);
            gestureHint.rectTransform.anchoredPosition = new Vector2(0f, 42f);
            UpdateSelectedStates();
        }

        private void MakeModeButton(RectTransform parent, string text, GameMode mode, float x, Font font)
        {
            Button button = Button(text, parent, text, font, 16, new Vector2(x, 0.5f), new Vector2(242f, 58f), () => modeAction?.Invoke(mode), false);
            button.name = "Mode " + mode;
        }

        private void UpdateSelectedStates()
        {
            foreach (Button button in GetComponentsInChildren<Button>(true))
            {
                ColorBlock colors = button.colors;
                string n = button.name;
                bool selected = (n == "Mode " + selectedMode) || n == "Size " + selectedSize;
                if (n.StartsWith("Mode ") || n.StartsWith("Size "))
                {
                    Image image = button.GetComponent<Image>();
                    if (image != null) image.color = selected ? new Color(0.10f, 0.47f, 0.43f, 0.96f) : new Color(0.08f, 0.12f, 0.18f, 0.94f);
                    colors.normalColor = selected ? Color.white : new Color(0.78f, 0.84f, 0.91f);
                    button.colors = colors;
                }
            }
        }

        private static RectTransform Panel(string name, RectTransform parent, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = offsetMin; rect.offsetMax = offsetMax;
            Image image = go.GetComponent<Image>(); image.color = color; image.raycastTarget = false;
            return rect;
        }

        private static Text Label(string name, RectTransform parent, string value, Font font, int size, TextAnchor anchor, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            Text text = go.GetComponent<Text>();
            text.text = value; text.font = font; text.fontSize = size; text.alignment = anchor;
            text.color = color; text.raycastTarget = false; text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Math.Max(10, size - 3); text.resizeTextMaxSize = size;
            text.horizontalOverflow = HorizontalWrapMode.Overflow; text.verticalOverflow = VerticalWrapMode.Truncate;
            Stretch(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return text;
        }

        private static Button Button(string name, RectTransform parent, string label, Font font, int fontSize,
            Vector2 anchor, Vector2 size, Action action, bool primary)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor; rect.sizeDelta = size; rect.anchoredPosition = Vector2.zero;
            Image image = go.GetComponent<Image>();
            image.color = primary ? new Color(0.12f, 0.65f, 0.57f, 1f) : new Color(0.08f, 0.12f, 0.18f, 0.96f);
            Button button = go.GetComponent<Button>(); button.targetGraphic = image;
            ColorBlock block = button.colors;
            block.normalColor = Color.white; block.highlightedColor = new Color(0.83f, 1f, 0.96f);
            block.pressedColor = new Color(0.62f, 0.86f, 0.82f); block.disabledColor = new Color(0.35f, 0.39f, 0.46f, 0.62f);
            button.colors = block;
            button.onClick.AddListener(() => action?.Invoke());
            Text text = Label("Label", rect, label, font, fontSize, TextAnchor.MiddleCenter, Color.white);
            text.fontStyle = primary ? FontStyle.Bold : FontStyle.Normal;
            return button;
        }

        private static void Stretch(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = offsetMin; rect.offsetMax = offsetMax;
        }

        private static string ModeName(GameMode mode) => mode == GameMode.Speedcubing ? "SPEEDCUBING" : mode.ToString().ToUpperInvariant();
    }
}
