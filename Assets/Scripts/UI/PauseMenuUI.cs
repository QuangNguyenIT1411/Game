using UnityEngine;
using UnityEngine.UI;
using DungeonCrawler.AI;
using DungeonCrawler.Dungeon;
using DungeonCrawler.Save;

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

        [Header("Visualization")]
        [SerializeField] private bool clearOnResume = true;

        public void Bind(Button resume, Button village, Button save, Button load)
        {
            resumeButton = resume;
            villageButton = village;
            saveButton = save;
            loadButton = load;

            SetupListeners();
        }

        private void Start()
        {
            EnsureVisualizationButtons();
            ArrangeButtons();
            SetupListeners();
            RefreshDungeonButtons();
            Debug.Log("Pause UI initialized");
        }

        private void OnEnable()
        {
            EnsureVisualizationButtons();
            ArrangeButtons();
            SetupListeners();
            RefreshDungeonButtons();
        }

        private void SetupListeners()
        {
            if (resumeButton != null)
            {
                resumeButton.onClick.RemoveAllListeners();
                resumeButton.onClick.AddListener(Resume);
            }

            if (villageButton != null)
            {
                villageButton.onClick.RemoveAllListeners();
                villageButton.onClick.AddListener(ReturnToVillage);
            }

            if (saveButton != null)
            {
                saveButton.onClick.RemoveAllListeners();
                saveButton.onClick.AddListener(SaveGame);
            }

            if (loadButton != null)
            {
                loadButton.onClick.RemoveAllListeners();
                loadButton.onClick.AddListener(LoadGame);
            }

            if (showAStarButton != null)
            {
                showAStarButton.onClick.RemoveAllListeners();
                showAStarButton.onClick.AddListener(ShowAStarSearch);
            }

            if (showBFSButton != null)
            {
                showBFSButton.onClick.RemoveAllListeners();
                showBFSButton.onClick.AddListener(ShowBFSSearch);
            }

            if (compareButton != null)
            {
                compareButton.onClick.RemoveAllListeners();
                compareButton.onClick.AddListener(CompareSearches);
            }

            if (clearVisualizationButton != null)
            {
                clearVisualizationButton.onClick.RemoveAllListeners();
                clearVisualizationButton.onClick.AddListener(ClearVisualization);
            }

            if (nextStepButton != null)
            {
                nextStepButton.onClick.RemoveAllListeners();
                nextStepButton.onClick.AddListener(AutoPlaySteps);
            }

            if (autoPlayButton != null)
            {
                autoPlayButton.onClick.RemoveAllListeners();
                autoPlayButton.onClick.AddListener(AutoPlaySteps);
            }

            if (stopAutoButton != null)
            {
                stopAutoButton.onClick.RemoveAllListeners();
                stopAutoButton.onClick.AddListener(StopAutoPlay);
            }
        }

        public void Resume()
        {
            if (clearOnResume)
            {
                ClearVisualization();
            }

            if (GameUIManager.Instance != null)
            {
                GameUIManager.Instance.ResumeGame();
            }
        }

        private void ReturnToVillage()
        {
            Debug.Log("Pause Return to Village clicked");
            ClearVisualization();
            if (GameUIManager.Instance != null)
            {
                GameUIManager.Instance.ResumeGame();
            }

            if (FloorManager.Instance != null)
            {
                FloorManager.Instance.ReturnToVillage();
            }
        }

        private void SaveGame()
        {
            Debug.Log("Pause Save clicked");
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.SaveGame();
            }
        }

        private void LoadGame()
        {
            Debug.Log("Pause Load clicked");
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.LoadGame();
            }
            
            // Auto resume after load to prevent being stuck in pause menu with new data
            Resume();
        }

        public void RefreshDungeonButtons()
        {
            bool isDungeon = IsDungeonPauseContext();
            SetVisualizationButtonsVisible(isDungeon);

            if (villageButton != null)
            {
                villageButton.gameObject.SetActive(isDungeon);
            }
        }

        private void ShowAStarSearch()
        {
            if (!IsDungeonPauseContext())
            {
                return;
            }

            PathfindingVisualizer.GetOrCreate().ShowAStarSearch();
        }

        private void ShowBFSSearch()
        {
            if (!IsDungeonPauseContext())
            {
                return;
            }

            PathfindingVisualizer.GetOrCreate().ShowBFSSearch();
        }

        private void CompareSearches()
        {
            if (!IsDungeonPauseContext())
            {
                return;
            }

            PathfindingVisualizer.GetOrCreate().CompareSearches();
        }

        private void ClearVisualization()
        {
            PathfindingVisualizer visualizer = PathfindingVisualizer.Instance;
            if (visualizer != null)
            {
                visualizer.ClearVisualization();
            }
        }

        private void AutoPlaySteps()
        {
            ShowAStarSearch();
        }

        private void StopAutoPlay()
        {
            PathfindingVisualizer visualizer = PathfindingVisualizer.Instance;
            if (visualizer != null)
            {
                visualizer.StopAutoPlay();
            }
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

        private void EnsureVisualizationButtons()
        {
            showAStarButton = EnsureButton(showAStarButton, "ShowAStarSearchButton", "Show A* Search");
            showBFSButton = EnsureButton(showBFSButton, "ShowBFSSearchButton", "Show BFS Search");
            compareButton = EnsureButton(compareButton, "CompareSearchButton", "Compare A* vs BFS");
            clearVisualizationButton = EnsureButton(clearVisualizationButton, "ClearVisualizationButton", "Clear Visualization");
            nextStepButton = EnsureButton(nextStepButton, "NextStepButton", "Next Step");
            autoPlayButton = EnsureButton(autoPlayButton, "AutoPlayStepsButton", "Auto Play Steps");
            stopAutoButton = EnsureButton(stopAutoButton, "StopAutoButton", "Stop Auto");
        }

        private void ArrangeButtons()
        {
            RectTransform panelRect = transform as RectTransform;
            if (panelRect != null)
            {
                panelRect.sizeDelta = new Vector2(Mathf.Max(panelRect.sizeDelta.x, 620f), Mathf.Max(panelRect.sizeDelta.y, 620f));
            }

            float y = -72f;
            float spacing = 40f;
            PositionButton(resumeButton, y);
            y -= spacing;
            PositionButton(showAStarButton, y);
            y -= spacing;
            PositionButton(showBFSButton, y);
            y -= spacing;
            PositionButton(compareButton, y);
            y -= spacing;
            PositionButton(clearVisualizationButton, y);
            y -= spacing;
            PositionButton(nextStepButton, y);
            y -= spacing;
            PositionButton(autoPlayButton, y);
            y -= spacing;
            PositionButton(stopAutoButton, y);
            y -= spacing;
            PositionButton(villageButton, y);
            y -= spacing;
            PositionButton(saveButton, y);
            y -= spacing;
            PositionButton(loadButton, y);
        }

        private void PositionButton(Button button, float y)
        {
            if (button == null)
            {
                return;
            }

            RectTransform rectTransform = button.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = new Vector2(0.5f, 1f);
            rectTransform.anchorMax = new Vector2(0.5f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(0f, y);
            rectTransform.sizeDelta = new Vector2(230f, 34f);
            button.transform.SetAsLastSibling();
        }

        private Button EnsureButton(Button existingButton, string objectName, string label)
        {
            if (existingButton != null)
            {
                SetButtonLabel(existingButton, label);
                return existingButton;
            }

            Transform existingTransform = transform.Find(objectName);
            if (existingTransform != null && existingTransform.TryGetComponent(out Button foundButton))
            {
                SetButtonLabel(foundButton, label);
                return foundButton;
            }

            Button template = resumeButton != null ? resumeButton : villageButton;
            GameObject buttonObject;
            if (template != null)
            {
                buttonObject = Instantiate(template.gameObject, transform);
                buttonObject.name = objectName;
                buttonObject.transform.SetAsLastSibling();
            }
            else
            {
                buttonObject = CreatePlainButton(objectName);
            }

            Button button = buttonObject.GetComponent<Button>();
            SetButtonLabel(button, label);
            return button;
        }

        private GameObject CreatePlainButton(string objectName)
        {
            GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(transform, false);
            RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(220f, 36f);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.12f, 0.12f, 0.12f, 0.85f);

            GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(buttonObject.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Text text = textObject.GetComponent<Text>();
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.fontSize = 16;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            return buttonObject;
        }

        private void SetButtonLabel(Button button, string label)
        {
            if (button == null)
            {
                return;
            }

            Text text = button.GetComponentInChildren<Text>(true);
            if (text != null)
            {
                text.text = label;
            }
        }
    }
}
