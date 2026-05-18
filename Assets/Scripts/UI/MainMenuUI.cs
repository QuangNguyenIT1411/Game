using UnityEngine;
using UnityEngine.UI;
using DungeonCrawler.Dungeon;
using DungeonCrawler.Village;
using DungeonCrawler.Save;

namespace DungeonCrawler.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        public static MainMenuUI Instance { get; private set; }

        [Header("Panels")]
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private ShopUI shopUI;
        [SerializeField] private FloorSelectUI floorSelectUI;

        [Header("Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button openShopButton;
        [SerializeField] private Button selectFloorButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button loadButton;
        [SerializeField] private Button deleteSaveButton;
        [SerializeField] private Button testButton;

        [Header("Scenes")]
        [SerializeField] private string performanceTestSceneName = "PerformanceTestScene";

        [Header("Debug")]
public bool showClickZonesDebug = false;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            SetupButtons();
            Debug.Log("MainMenuUI initialized");
            Debug.Log("Main menu hotspots configured");
        }

        private void Start()
        {
        }

        private void Update()
        {
        }

        #if UNITY_EDITOR
        private void OnValidate()
        {
            if (playButton != null)
            {
                SetupButtons();
            }
        }
        #endif

        private void SetupButtons()
        {
            ConfigureButton(playButton, OnPlayClicked);
            ConfigureButton(openShopButton, OnOpenShopClicked);
            ConfigureButton(selectFloorButton, OnSelectFloorClicked);
            ConfigureButton(saveButton, OnSaveClicked);
            ConfigureButton(loadButton, OnLoadClicked);
            ConfigureButton(deleteSaveButton, OnDeleteSaveClicked);
            ConfigureButton(testButton, OnTestClicked);

            int boundCount = 0;
            if (playButton != null) boundCount++;
            if (openShopButton != null) boundCount++;
            if (selectFloorButton != null) boundCount++;
            if (saveButton != null) boundCount++;
            if (loadButton != null) boundCount++;
            if (deleteSaveButton != null) boundCount++;
            if (testButton != null) boundCount++;
            
            Debug.Log($"MainMenu Buttons configured. Count = {boundCount}");
            }

            private void ConfigureButton(Button button, UnityEngine.Events.UnityAction action)
            {
                if (button == null) return;

                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(action);

                // Ensure button is active and interactable
                button.interactable = true;
                // User requirement: transition = None (unless they want to see hover, but we stick to requested stability)
                button.transition = Selectable.Transition.None;

                // Apply visualization
                Image img = button.GetComponent<Image>();
                if (img != null)
                {
                    img.enabled = true;
                    
                    // ONLY override color if debug mode is on. 
                    // Do NOT force white if the user set it to transparent.
                    if (showClickZonesDebug)
                    {
                        img.color = new Color(0f, 0.5f, 1f, 0.4f);
                    }
                
                    img.raycastTarget = true;
                }

                // Show child text
                Text text = button.GetComponentInChildren<Text>(true);
                if (text != null)
                {
                    text.enabled = true;
                    if (text.color.a < 0.1f) text.color = Color.white;
                }
            }

            public void HideMainMenuImmediate()
            {
            if (menuPanel != null)
            {
                menuPanel.SetActive(false);
                Debug.Log("Main menu hidden immediate");
            }
            
            // Also explicitly hide hotspots if they are separate in GameUIManager
            GameUIManager ui = GameUIManager.Instance;
            if (ui != null && ui.mainMenuHotspots != null)
            {
                ui.mainMenuHotspots.SetActive(false);
            }
            }

            private void OnTestClicked()
            {
                Debug.Log("<color=cyan>Main Menu TEST button clicked!</color> - Loading performance test scene: " + performanceTestSceneName);
                
                // Use GameUIManager to hide all menu-related visuals
                GameUIManager ui = GameUIManager.Instance;
                if (ui != null)
                {
                    ui.HideMainMenu();
                }
                
                // Also hide local panel just in case
                HideMainMenuImmediate();
                
                UnityEngine.SceneManagement.SceneManager.LoadScene(performanceTestSceneName);
            }

            private void OnPlayClicked()
{
            Debug.Log("Main Menu Play button pressed - starting transition");
            
            // 1. Trigger Dungeon state change FIRST
            VillageManager vm = VillageManager.Instance;
            if (vm == null) vm = FindAnyObjectByType<VillageManager>(FindObjectsInactive.Include);
            
            if (vm != null)
            {
                Debug.Log("VillageManager found, entering dungeon...");
                vm.EnterDungeon(); // This sets FloorManager.villageMode = false
            }
            else
            {
                Debug.LogError("VillageManager NOT FOUND! Falling back to FloorManager.");
                FloorManager fm = FloorManager.Instance;
                fm?.EnterDungeonAtFloor(1);
            }

            // 2. UI transition
            GameUIManager ui = GameUIManager.Instance;
            if (ui != null)
            {
                Debug.Log("GameUIManager found, showing gameplay HUD");
                ui.ShowGameplayHUD();
                HideMainMenuImmediate(); // Force hide the local panel
            }
            else
            {
                Debug.LogError("GameUIManager NOT FOUND! Force hiding panel manually.");
                HideMainMenuImmediate();
            }

            Debug.Log("Play transition completed");
        }

        private void OnOpenShopClicked()
        {
            Debug.Log("Main Menu Open Shop clicked");
            if (shopUI == null) shopUI = FindAnyObjectByType<ShopUI>(FindObjectsInactive.Include);
            shopUI?.OpenShop();
        }

        private void OnSelectFloorClicked()
        {
            Debug.Log("Main Menu Select Floor clicked");
            if (floorSelectUI == null) floorSelectUI = FindAnyObjectByType<FloorSelectUI>(FindObjectsInactive.Include);
            floorSelectUI?.ShowVillage();
        }

        private void OnSaveClicked()
        {
            Debug.Log("Main Menu Save clicked");
            SaveManager.Instance?.SaveGame();
        }

        private void OnLoadClicked()
        {
            Debug.Log("Main Menu Load clicked");
            SaveManager.Instance?.LoadGame();
        }

        private void OnDeleteSaveClicked()
        {
            Debug.Log("Main Menu Delete Save clicked");
            SaveManager.Instance?.DeleteSave();
        }

        public void Bind(GameObject panel, Button play, Button shop, Button select, Button save, Button load, Button delete, Button test)
        {
            menuPanel = panel;
            playButton = play;
            openShopButton = shop;
            selectFloorButton = select;
            saveButton = save;
            loadButton = load;
            deleteSaveButton = delete;
            testButton = test;
            
            SetupButtons();
        }
}
}
