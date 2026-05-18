using DungeonCrawler.AI;
using DungeonCrawler.CameraControls;
using DungeonCrawler.Dungeon;
using DungeonCrawler.Save;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonCrawler.UI
{
    public class PauseMenuUI : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button villageButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button loadButton;
        [SerializeField] private Button showAStarButton;
        [SerializeField] private Button showBFSButton;
        [SerializeField] private Button compareButton;
        [SerializeField] private Button clearVisualizationButton;
        [SerializeField] private Button nextStepButton;
        [SerializeField] private Button autoPlayButton;
        [SerializeField] private Button stopAutoButton;
        [SerializeField] private Button visualizationReturnButton;

        [Header("Visualization")]
        [SerializeField] private bool clearOnResume = true;

        private RectTransform cardRect;
        private RectTransform gameplayButtonsRoot;
        private RectTransform visualizationButtonsRoot;
        private Font cachedFont;

        public void Bind(Button resume, Button village, Button save, Button load)
        {
            resumeButton = resume;
            villageButton = village;
            saveButton = save;
            loadButton = load;

            RebuildLayout();
            SetupListeners();
        }

        private void Start()
        {
            RebuildLayout();
            SetupListeners();
            RefreshDungeonButtons();
            Debug.Log("Pause UI initialized");
        }

        private void OnEnable()
        {
            RebuildLayout();
            SetupListeners();
            RefreshDungeonButtons();
        }

        private void SetupListeners()
        {
            SetupButton(resumeButton, Resume);
            SetupButton(villageButton, ReturnToVillage);
            SetupButton(saveButton, SaveGame);
            SetupButton(loadButton, LoadGame);
            SetupButton(showAStarButton, ShowAStarSearch);
            SetupButton(showBFSButton, ShowBFSSearch);
            SetupButton(compareButton, CompareSearches);
            SetupButton(clearVisualizationButton, ClearVisualization);
            SetupButton(nextStepButton, AutoPlaySteps);
            SetupButton(autoPlayButton, AutoPlaySteps);
            SetupButton(stopAutoButton, StopAutoPlay);
            SetupButton(visualizationReturnButton, ReturnFromVisualizationMode);
        }

        private void SetupButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        public void Resume()
        {
            if (clearOnResume)
            {
                ClearVisualization();
            }

            visualizationReturnButton?.gameObject.SetActive(false);
            VisualizationCameraController.Instance?.ExitVisualizationMode(true);
            GameUIManager.Instance?.ResumeGame();
        }

        private void ReturnToVillage()
        {
            Debug.Log("Pause Return to Village clicked");
            ClearVisualization();
            visualizationReturnButton?.gameObject.SetActive(false);
            VisualizationCameraController.Instance?.ExitVisualizationMode(true);
            GameUIManager.Instance?.ResumeGame();
            FloorManager.Instance?.ReturnToVillage();
        }

        private void SaveGame()
        {
            Debug.Log("Pause Save clicked");
            SaveManager.Instance?.SaveGame();
        }

        private void LoadGame()
        {
            Debug.Log("Pause Load clicked");
            SaveManager.Instance?.LoadGame();
            Resume();
        }

        public void RefreshDungeonButtons()
        {
            bool isDungeon = IsDungeonPauseContext();
            SetVisualizationButtonsVisible(isDungeon);
            SetButtonVisible(villageButton, isDungeon);
        }

        private void ShowAStarSearch()
        {
            if (IsDungeonPauseContext())
            {
                PathfindingVisualizer.GetOrCreate().ShowAStarSearch();
                EnterVisualizationMode("Entered A* Visualization Mode");
            }
        }

        private void ShowBFSSearch()
        {
            if (IsDungeonPauseContext())
            {
                PathfindingVisualizer.GetOrCreate().ShowBFSSearch();
                EnterVisualizationMode("Entered BFS Visualization Mode");
            }
        }

        private void CompareSearches()
        {
            if (IsDungeonPauseContext())
            {
                PathfindingVisualizer.GetOrCreate().CompareSearches();
                EnterVisualizationMode("Entered Compare Visualization Mode");
            }
        }

        public void ReturnFromVisualizationMode()
        {
            VisualizationCameraController.Instance?.ExitVisualizationMode(true);
            visualizationReturnButton?.gameObject.SetActive(false);
            GameUIManager.Instance?.ReturnToPauseFromVisualization();
            Time.timeScale = 0f;
            Debug.Log("Returned to Pause Menu from Visualization");
        }

        private void ClearVisualization()
        {
            PathfindingVisualizer.Instance?.ClearVisualization();
        }

        private void AutoPlaySteps()
        {
            ShowAStarSearch();
        }

        private void StopAutoPlay()
        {
            PathfindingVisualizer.Instance?.StopAutoPlay();
        }

        private bool IsDungeonPauseContext()
        {
            if (GameUIManager.Instance != null && !GameUIManager.Instance.IsDungeonContext)
            {
                return false;
            }

            FloorManager floorManager = FloorManager.Instance;
            return floorManager == null || !floorManager.IsVillageMode;
        }

        private void SetVisualizationButtonsVisible(bool visible)
        {
            SetButtonVisible(showAStarButton, visible);
            SetButtonVisible(showBFSButton, visible);
            SetButtonVisible(compareButton, visible);
            SetButtonVisible(clearVisualizationButton, visible);
            SetButtonVisible(nextStepButton, visible);
            SetButtonVisible(autoPlayButton, visible);
            SetButtonVisible(stopAutoButton, visible);
        }

        private void SetButtonVisible(Button button, bool visible)
        {
            if (button != null)
            {
                button.gameObject.SetActive(visible);
            }
        }

        private void RebuildLayout()
        {
            SetupDimPanel();
            visualizationReturnButton = EnsureReturnButton();
            visualizationReturnButton.gameObject.SetActive(false);
            cardRect = EnsureRect("PauseMenuCard", transform);
            ConfigureCard(cardRect);

            Text title = EnsureText("Title", cardRect, "PAUSED", 32);
            ConfigureLayoutElement(title.gameObject, 280f, 48f);

            gameplayButtonsRoot = EnsureGroup("GameplayButtons", cardRect);
            visualizationButtonsRoot = EnsureGroup("VisualizationButtons", cardRect);

            Text subtitle = EnsureText("VisualizationTitle", cardRect, "Pathfinding Visualization", 22);
            ConfigureLayoutElement(subtitle.gameObject, 340f, 34f);

            MoveChildAfter(title.transform, cardRect, 0);
            MoveChildAfter(gameplayButtonsRoot, cardRect, 1);
            MoveChildAfter(subtitle.transform, cardRect, 2);
            MoveChildAfter(visualizationButtonsRoot, cardRect, 3);

            resumeButton = EnsureButton(resumeButton, "ResumeButton", "Resume", gameplayButtonsRoot, new Color(0.20f, 0.42f, 0.22f, 1f));
            villageButton = EnsureButton(villageButton, "VillageButton", "Return to Village", gameplayButtonsRoot, new Color(0.42f, 0.30f, 0.18f, 1f));
            saveButton = EnsureButton(saveButton, "SaveButton", "Save", gameplayButtonsRoot, new Color(0.22f, 0.24f, 0.48f, 1f));
            loadButton = EnsureButton(loadButton, "LoadButton", "Load", gameplayButtonsRoot, new Color(0.34f, 0.22f, 0.48f, 1f));

            showAStarButton = EnsureButton(showAStarButton, "ShowAStarSearchButton", "Show A* Search", visualizationButtonsRoot, new Color(0.18f, 0.42f, 0.20f, 1f));
            showBFSButton = EnsureButton(showBFSButton, "ShowBFSSearchButton", "Show BFS Search", visualizationButtonsRoot, new Color(0.18f, 0.30f, 0.55f, 1f));
            compareButton = EnsureButton(compareButton, "CompareSearchButton", "Compare A* vs BFS", visualizationButtonsRoot, new Color(0.24f, 0.35f, 0.46f, 1f));
            clearVisualizationButton = EnsureButton(clearVisualizationButton, "ClearVisualizationButton", "Clear Visualization", visualizationButtonsRoot, new Color(0.36f, 0.28f, 0.20f, 1f));
            nextStepButton = EnsureButton(nextStepButton, "NextStepButton", "Next Step", visualizationButtonsRoot, new Color(0.30f, 0.35f, 0.22f, 1f));
            autoPlayButton = EnsureButton(autoPlayButton, "AutoPlayStepsButton", "Auto Play Steps", visualizationButtonsRoot, new Color(0.20f, 0.38f, 0.24f, 1f));
            stopAutoButton = EnsureButton(stopAutoButton, "StopAutoButton", "Stop Auto", visualizationButtonsRoot, new Color(0.40f, 0.22f, 0.22f, 1f));

            DisableOldEmptyButtonsContainer();
            Debug.Log("Pause menu layout rebuilt");
            Debug.Log("Pause visualization buttons aligned");
        }

        private void EnterVisualizationMode(string logMessage)
        {
            GameUIManager.Instance?.EnterPathfindingVisualization();
            VisualizationCameraController.GetOrCreate()?.EnterVisualizationMode();
            visualizationReturnButton = EnsureReturnButton();
            SetupButton(visualizationReturnButton, ReturnFromVisualizationMode);
            visualizationReturnButton.gameObject.SetActive(true);
            visualizationReturnButton.transform.SetAsLastSibling();
            Time.timeScale = 0f;
            Debug.Log(logMessage);
        }

        private void SetupDimPanel()
        {
            RectTransform rectTransform = transform as RectTransform;
            if (rectTransform != null)
            {
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
            }

            Image image = GetComponent<Image>();
            if (image == null)
            {
                image = gameObject.AddComponent<Image>();
            }

            image.color = new Color(0f, 0f, 0f, 0.42f);
            image.raycastTarget = true;
        }

        private void ConfigureCard(RectTransform rectTransform)
        {
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(420f, 620f);

            Image image = rectTransform.GetComponent<Image>();
            if (image == null)
            {
                image = rectTransform.gameObject.AddComponent<Image>();
            }

            image.color = new Color(0.035f, 0.038f, 0.045f, 0.88f);
            image.raycastTarget = true;

            VerticalLayoutGroup layout = rectTransform.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = rectTransform.gameObject.AddComponent<VerticalLayoutGroup>();
            }

            layout.childAlignment = TextAnchor.UpperCenter;
            layout.padding = new RectOffset(0, 0, 24, 24);
            layout.spacing = 10f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        private RectTransform EnsureGroup(string name, Transform parent)
        {
            RectTransform group = EnsureRect(name, parent);
            group.sizeDelta = name == "GameplayButtons" ? new Vector2(320f, 159f) : new Vector2(320f, 282f);
            ConfigureLayoutElement(group.gameObject, 320f, group.sizeDelta.y);

            Image image = group.GetComponent<Image>();
            if (image != null)
            {
                image.enabled = false;
            }

            VerticalLayoutGroup layout = group.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = group.gameObject.AddComponent<VerticalLayoutGroup>();
            }

            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 5f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return group;
        }

        private RectTransform EnsureRect(string name, Transform parent)
        {
            Transform child = FindChildRecursive(transform, name);
            GameObject childObject;
            if (child != null)
            {
                childObject = child.gameObject;
            }
            else
            {
                childObject = new GameObject(name, typeof(RectTransform));
            }

            childObject.transform.SetParent(parent, false);
            RectTransform rectTransform = childObject.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                rectTransform = childObject.AddComponent<RectTransform>();
            }

            childObject.SetActive(true);
            return rectTransform;
        }

        private Text EnsureText(string name, Transform parent, string label, int fontSize)
        {
            RectTransform rectTransform = EnsureRect(name, parent);
            Text text = rectTransform.GetComponent<Text>();
            if (text == null)
            {
                text = rectTransform.gameObject.AddComponent<Text>();
            }

            text.text = label;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.font = GetFont();

            rectTransform.sizeDelta = new Vector2(360f, fontSize + 18f);
            return text;
        }

        private Button EnsureButton(Button existingButton, string objectName, string label, Transform parent, Color color)
        {
            Button button = existingButton != null ? existingButton : FindButton(objectName);
            if (button == null)
            {
                GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
                button = buttonObject.GetComponent<Button>();
            }

            button.gameObject.name = objectName;
            button.transform.SetParent(parent, false);
            button.gameObject.SetActive(true);

            RectTransform rectTransform = button.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(280f, 36f);
            ConfigureLayoutElement(button.gameObject, 280f, 36f);

            Image image = button.GetComponent<Image>();
            if (image == null)
            {
                image = button.gameObject.AddComponent<Image>();
            }

            image.color = color;
            image.raycastTarget = true;

            Text text = button.GetComponentInChildren<Text>(true);
            if (text == null)
            {
                GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
                textObject.transform.SetParent(button.transform, false);
                text = textObject.GetComponent<Text>();
            }

            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            text.text = label;
            text.fontSize = 15;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.font = GetFont();
            return button;
        }

        private Button EnsureReturnButton()
        {
            Transform parent = transform.parent != null ? transform.parent : transform;
            Transform existing = parent.Find("VisualizationReturnButton");
            GameObject buttonObject;
            if (existing != null)
            {
                buttonObject = existing.gameObject;
            }
            else
            {
                buttonObject = new GameObject("VisualizationReturnButton", typeof(RectTransform), typeof(Image), typeof(Button));
                buttonObject.transform.SetParent(parent, false);
            }

            RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(1f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(1f, 1f);
            rectTransform.anchoredPosition = new Vector2(-18f, -18f);
            rectTransform.sizeDelta = new Vector2(120f, 42f);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.12f, 0.12f, 0.14f, 0.92f);
            image.raycastTarget = true;

            Button button = buttonObject.GetComponent<Button>();
            Text text = button.GetComponentInChildren<Text>(true);
            if (text == null)
            {
                GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
                textObject.transform.SetParent(buttonObject.transform, false);
                text = textObject.GetComponent<Text>();
            }

            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            text.text = "Return";
            text.fontSize = 16;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.font = GetFont();
            return button;
        }

        private Button FindButton(string name)
        {
            Transform child = FindChildRecursive(transform, name);
            return child != null ? child.GetComponent<Button>() : null;
        }

        private Transform FindChildRecursive(Transform root, string childName)
        {
            foreach (Transform child in root)
            {
                if (child.name == childName)
                {
                    return child;
                }

                Transform nested = FindChildRecursive(child, childName);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        private void ConfigureLayoutElement(GameObject target, float width, float height)
        {
            LayoutElement layoutElement = target.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = target.AddComponent<LayoutElement>();
            }

            layoutElement.preferredWidth = width;
            layoutElement.preferredHeight = height;
            layoutElement.minWidth = width;
            layoutElement.minHeight = height;
            layoutElement.flexibleWidth = 0f;
            layoutElement.flexibleHeight = 0f;
        }

        private void MoveChildAfter(Transform child, Transform parent, int siblingIndex)
        {
            child.SetParent(parent, false);
            child.SetSiblingIndex(siblingIndex);
        }

        private void DisableOldEmptyButtonsContainer()
        {
            Transform oldButtons = transform.Find("Buttons");
            if (oldButtons != null && oldButtons != gameplayButtonsRoot && oldButtons.childCount == 0)
            {
                oldButtons.gameObject.SetActive(false);
            }
        }

        private Font GetFont()
        {
            if (cachedFont != null)
            {
                return cachedFont;
            }

            cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (cachedFont == null)
            {
                cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            return cachedFont;
        }
    }
}
