using Systems.SimpleCore.Operations;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Systems.SimpleCore.Examples
{
    /// <summary>
    ///     Small uGUI builder used by package example scenes.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ExampleRuntimePanel : MonoBehaviour
    {
        private const float PANEL_WIDTH = 520f;
        private const float BUTTON_HEIGHT = 34f;

        private static readonly Color PanelColor = new Color32(24, 31, 42, 235);
        private static readonly Color HeaderColor = new Color32(245, 247, 250, 255);
        private static readonly Color BodyColor = new Color32(210, 218, 230, 255);
        private static readonly Color MutedColor = new Color32(166, 177, 194, 255);
        private static readonly Color ButtonColor = new Color32(46, 63, 82, 255);
        private static readonly Color SelectedButtonColor = new Color32(75, 132, 189, 255);

        private Font _font;
        private RectTransform _content;
        private Text _statusText;

        public static ExampleRuntimePanel Create(string title, string subtitle)
        {
            return Create(title, subtitle, new Vector2(32f, 0f));
        }

        public static ExampleRuntimePanel Create(string title, string subtitle, Vector2 panelAnchoredPosition)
        {
            EnsureEventSystem();

            GameObject canvasObject = new GameObject(title + " UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            ExampleRuntimePanel panel = canvasObject.AddComponent<ExampleRuntimePanel>();
            panel.Build(title, subtitle, panelAnchoredPosition);
            return panel;
        }

        public void AddSection(string label)
        {
            Text text = CreateText(label + " Section", _content, label, 18, FontStyle.Bold, HeaderColor);
            LayoutElement layout = text.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = 28f;
            layout.preferredHeight = 28f;
        }

        public Text AddBodyText(string label)
        {
            Text text = CreateText(label + " Text", _content, label, 15, FontStyle.Normal, BodyColor);
            LayoutElement layout = text.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = 24f;
            layout.preferredHeight = 24f;
            return text;
        }

        public Button AddButton(string label)
        {
            return AddButton(label, false);
        }

        public Button AddButton(string label, bool selected)
        {
            RectTransform rect = CreateRect(label + " Button", _content);
            LayoutElement layout = rect.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = BUTTON_HEIGHT;
            layout.preferredHeight = BUTTON_HEIGHT;

            Image image = rect.gameObject.AddComponent<Image>();
            image.color = selected ? SelectedButtonColor : ButtonColor;

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            Text text = CreateText("Label", rect, label, 15, FontStyle.Bold, HeaderColor);
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12f, 0f);
            textRect.offsetMax = new Vector2(-12f, 0f);
            text.alignment = TextAnchor.MiddleCenter;
            return button;
        }

        public void SetStatus(string status)
        {
            if (ReferenceEquals(_statusText, null))
            {
                return;
            }

            _statusText.text = status;
        }

        public static string FormatResult(in OperationResult result)
        {
            string state = OperationResult.IsSuccess(in result) ? "Success" : "Error";
            return state +
                   " system " + result.systemCode +
                   " result " + result.resultCode +
                   " user " + result.userCode;
        }

        private void Build(string title, string subtitle, Vector2 panelAnchoredPosition)
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (ReferenceEquals(_font, null))
            {
                _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            RectTransform panel = CreateRect("Panel", transform);
            panel.anchorMin = new Vector2(0f, 0.5f);
            panel.anchorMax = new Vector2(0f, 0.5f);
            panel.pivot = new Vector2(0f, 0.5f);
            panel.anchoredPosition = panelAnchoredPosition;
            panel.sizeDelta = new Vector2(PANEL_WIDTH, 0f);

            Image panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.color = PanelColor;

            VerticalLayoutGroup panelLayout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            panelLayout.padding = new RectOffset(24, 24, 22, 22);
            panelLayout.spacing = 8f;
            panelLayout.childControlWidth = true;
            panelLayout.childControlHeight = true;
            panelLayout.childForceExpandWidth = true;
            panelLayout.childForceExpandHeight = false;

            ContentSizeFitter fitter = panel.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Text titleText = CreateText("Title", panel, title, 24, FontStyle.Bold, HeaderColor);
            LayoutElement titleLayout = titleText.gameObject.AddComponent<LayoutElement>();
            titleLayout.minHeight = 34f;
            titleLayout.preferredHeight = 34f;

            Text subtitleText = CreateText("Subtitle", panel, subtitle, 14, FontStyle.Normal, MutedColor);
            subtitleText.alignment = TextAnchor.UpperLeft;
            LayoutElement subtitleLayout = subtitleText.gameObject.AddComponent<LayoutElement>();
            subtitleLayout.minHeight = 42f;
            subtitleLayout.preferredHeight = 42f;

            _content = CreateRect("Content", panel);
            VerticalLayoutGroup contentLayout = _content.gameObject.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 6f;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            _statusText = CreateText("Status", panel, string.Empty, 14, FontStyle.Normal, BodyColor);
            _statusText.alignment = TextAnchor.UpperLeft;
            LayoutElement statusLayout = _statusText.gameObject.AddComponent<LayoutElement>();
            statusLayout.minHeight = 54f;
            statusLayout.preferredHeight = 54f;
        }

        private static void EnsureEventSystem()
        {
            EventSystem eventSystem = FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include);
            if (!ReferenceEquals(eventSystem, null))
            {
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            InputSystemUIInputModule inputModule = eventSystemObject.AddComponent<InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject rectObject = new GameObject(name, typeof(RectTransform));
            rectObject.transform.SetParent(parent, false);
            return rectObject.GetComponent<RectTransform>();
        }

        private Text CreateText(string name, Transform parent, string value, int fontSize, FontStyle fontStyle, Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = _font;
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.color = color;
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }
    }
}
