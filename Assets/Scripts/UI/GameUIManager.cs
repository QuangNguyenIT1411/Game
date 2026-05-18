using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using DungeonCrawler.AI;
using DungeonCrawler.Dungeon;

namespace DungeonCrawler.UI
{
    public enum UIState { MainMenu, Dungeon, Paused, PathfindingVisualization, Shop, FloorSelect }

    public class GameUIManager : MonoBehaviour
    {
        private static GameUIManager _instance;
        public static GameUIManager Instance
        {
            get
            {
                if (_instance == null) _instance = FindAnyObjectByType<GameUIManager>(FindObjectsInactive.Include);
                return _instance;
            }
        }

        [Header("State")]
        [SerializeField] private UIState currentState = UIState.MainMenu;

        public UIState CurrentState => currentState;
        public bool IsDungeonContext => currentState == UIState.Dungeon || currentState == UIState.Paused || currentState == UIState.PathfindingVisualization;

        [Header("Panels")]
        public GameObject statusPanel;
        public GameObject pausePanel;
        public GameObject equipmentPanel;
        public GameObject inventoryPanel;
        public GameObject saveLoadPanel;   // To show only in Hub
        
        [Header("Backgrounds")]
        public GameObject hubVisuals;      // Background and Logo for Hub/Menu
        public GameObject dungeonVisuals;  // Background for Dungeon floors
        public GameObject mainMenuPanel;   // Main Menu Panel explicitly
        public GameObject mainMenuHotspots; // Main Menu Hotspots container

        [Header("HUD")]
        public GameObject floorDebugText;
        public GameObject benchmarkPanel;
        public Button pauseButton;
        public Button resumeButton;

        private bool isHUDHiddenByPause = false;
        private GameObject[] cachedHudObjects;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                if (Application.isPlaying) Destroy(gameObject);
                else DestroyImmediate(gameObject);
                return;
            }
            _instance = this;
        }

        private void Start()
        {
            // Auto-assign references if missing - Robust search for potentially inactive objects
            if (mainMenuPanel == null) mainMenuPanel = FindObjectInactive("MainMenuPanel");
            if (mainMenuHotspots == null) mainMenuHotspots = FindObjectInactive("Buttons");
            if (hubVisuals == null) hubVisuals = FindObjectInactive("MainMenuVisuals") ?? FindObjectInactive("VillageVisuals");

            // Initial states for secondary panels
if (statusPanel != null) statusPanel.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(false);
            if (equipmentPanel != null) equipmentPanel.SetActive(false);
            if (inventoryPanel != null) inventoryPanel.SetActive(false);
            
            if (pauseButton != null)
            {
                pauseButton.onClick.RemoveAllListeners();
                pauseButton.onClick.AddListener(TogglePause);
            }

            CacheHudObjects();
            
            // Ensure EventSystem is valid for interaction
            CheckEventSystem();

            // Always show Main Menu at start unless requested otherwise
            ShowMainMenu();
            }

        private void CheckEventSystem()
        {
            UnityEngine.EventSystems.EventSystem es = Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (es == null)
            {
                GameObject go = new GameObject("EventSystem_Auto", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
                Debug.Log("GameUIManager: Created missing EventSystem.");
            }
            else
            {
                if (es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
                {
                    // Remove legacy if present
                    var legacy = es.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                    if (legacy != null) Destroy(legacy);
                    es.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                    Debug.Log("GameUIManager: Updated EventSystem to InputSystemUIInputModule.");
                }
            }
        }

        private void CacheHudObjects()
        {
            GameObject hudCanvas = GameObject.Find("HUDCanvas");
            if (hudCanvas == null) return;

            // Assign specific references if they are null
            if (floorDebugText == null)
            {
                Transform t = hudCanvas.transform.Find("FloorDebugText");
                if (t != null) floorDebugText = t.gameObject;
            }
            if (benchmarkPanel == null)
            {
                Transform t = hudCanvas.transform.Find("BenchmarkPanel");
                if (t != null) benchmarkPanel = t.gameObject;
            }

            // Only "always-on" gameplay HUD elements
            string[] hudElements = {
                "PlayerHealthBar_Background",
                "InventoryDebugText",
                "EquipmentDebugText",
                "LevelDebugText",
                "FloorDebugText",
                "Minimap",
                "MinimapPanel",
                "MinimapContainer",
                "MinimapImage",
                "StatusVisualPanel",
                "FloorInfoPanel",
                "EquipmentVisualPanel",
                "PauseButton",
                "BenchmarkPanel"
                };

            var foundObjects = new System.Collections.Generic.List<GameObject>();
            foreach (string name in hudElements)
            {
                Transform t = hudCanvas.transform.Find(name);
                if (t != null)
                {
                    foundObjects.Add(t.gameObject);
                }
                else
                {
                    // Try recursive search if not direct child
                    foreach (Transform child in hudCanvas.GetComponentsInChildren<Transform>(true))
                    {
                        if (child.name == name)
                        {
                            foundObjects.Add(child.gameObject);
                            break;
                        }
                    }
                }
            }
            cachedHudObjects = foundObjects.ToArray();
        }

        private void Update()
        {
            // Toggle HUD panels with hotkeys - Only if in Dungeon
            if (currentState == UIState.Dungeon)
            {
                if (Keyboard.current.eKey.wasPressedThisFrame)
                {
                    if (inventoryPanel != null) inventoryPanel.SetActive(!inventoryPanel.activeSelf);
                }

                if (Keyboard.current.tabKey.wasPressedThisFrame)
                {
                    if (statusPanel != null) statusPanel.SetActive(!statusPanel.activeSelf);
                }

                if (Keyboard.current.gKey.wasPressedThisFrame)
                {
                    if (equipmentPanel != null) equipmentPanel.SetActive(!equipmentPanel.activeSelf);
                }
            }

            // Esc for Pause
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (currentState == UIState.PathfindingVisualization)
                {
                    PauseMenuUI pauseMenu = pausePanel != null ? pausePanel.GetComponent<PauseMenuUI>() : FindAnyObjectByType<PauseMenuUI>(FindObjectsInactive.Include);
                    pauseMenu?.ReturnFromVisualizationMode();
                }
                else if (currentState == UIState.Dungeon || currentState == UIState.Paused)
                {
                    TogglePause();
                }
            }

            // Sync visuals based on mode
            UpdateBackgroundVisibility();
        }

        private void UpdateBackgroundVisibility()
        {
            FloorManager floorManager = FloorManager.Instance;
            if (floorManager == null) return;

            bool isVillage = floorManager.IsVillageMode;

            if (hubVisuals != null)
            {
                // Only show hub visuals if in village mode OR explicitly in main menu state
                // Don't show it just because we are paused in the dungeon
                bool shouldShowHub = isVillage || currentState == UIState.MainMenu;
                if (hubVisuals.activeSelf != shouldShowHub) hubVisuals.SetActive(shouldShowHub);
            }

            if (dungeonVisuals != null)
            {
                // Show dungeon visuals if not in village mode
                // We keep it visible even when paused so the game doesn't go black behind the menu
                bool shouldShowDungeon = !isVillage;
                if (dungeonVisuals.activeSelf != shouldShowDungeon) dungeonVisuals.SetActive(shouldShowDungeon);
            }
        }

        private GameObject FindObjectInactive(string name)
        {
            GameObject[] all = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var go in all)
            {
                if (go.name == name && go.scene.isLoaded) return go;
            }
            return null;
        }

        public void ShowMainMenu()
        {
            currentState = UIState.MainMenu;
            Time.timeScale = 1f;
            PathfindingVisualizer.Instance?.ClearVisualization();

            // Ensure references are valid
            if (mainMenuPanel == null) mainMenuPanel = FindObjectInactive("MainMenuPanel");
            if (mainMenuHotspots == null) mainMenuHotspots = FindObjectInactive("Buttons");
            if (hubVisuals == null) hubVisuals = FindObjectInactive("MainMenuVisuals") ?? FindObjectInactive("VillageVisuals");

            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
            if (mainMenuHotspots != null) mainMenuHotspots.SetActive(true);
            
            // Note: hubVisuals and dungeonVisuals are handled in UpdateBackgroundVisibility
            
            // Ensure Canvas visibility
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                canvas.enabled = true;
                canvas.sortingOrder = 100; // Bring to front
            }
            
            if (pausePanel != null) pausePanel.SetActive(false);
            if (pauseButton != null) pauseButton.gameObject.SetActive(false);
            
            // Hide Minimap specifically
            if (MinimapUI.Instance != null) MinimapUI.Instance.Hide();

            HideGameplayHUD();
            Debug.Log("Main menu UI clean: gameplay HUD hidden");
        }

        public void HideMainMenu()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (mainMenuHotspots != null) mainMenuHotspots.SetActive(false);
        }

        public void ShowGameplayHUD()
        {
            currentState = UIState.Dungeon;
            Time.timeScale = 1f;
            PathfindingVisualizer.Instance?.ClearVisualization();

            HideMainMenu();
            SetGameplayHUDVisible(true);
            
            // Explicitly show requested elements on ALL HUDCanvases in scene
            ToggleGameplayUIElements(true);
            
            // Force Minimap
            if (MinimapUI.Instance != null)
            {
                MinimapUI.Instance.Show();
            }

            RefreshAllHUD();
            Debug.Log("Gameplay HUD shown: benchmark and pause enabled");
        }

        public void HideGameplayHUD()
        {
            SetGameplayHUDVisible(false);
            ToggleGameplayUIElements(false);

            // Hide Minimap specifically
            MinimapUI minimap = MinimapUI.Instance;
            if (minimap != null) minimap.gameObject.SetActive(false);
        }

        private void ToggleGameplayUIElements(bool visible)
        {
            // Find all GameObjects that might be part of the HUD, including duplicates
            string[] targetNames = { 
                "FloorDebugText", 
                "BenchmarkPanel", 
                "PauseButton", 
                "PausePanel", 
                "AlgorithmLabel",
                "LabelText",
                "AStarStats",
                "BFSStats",
                "Minimap",
                "MinimapPanel",
                "MinimapContainer"
            };
            
            // 1. Handle all Canvases
            Canvas[] allCanvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            foreach (var canvas in allCanvases)
            {
                if (canvas.name == "HUDCanvas")
                {
                    foreach (string name in targetNames)
                    {
                        if (name == "AlgorithmLabel") continue;
                        Transform t = canvas.transform.Find(name);
                        if (t != null)
                        {
                            bool shouldBeActive = visible;
                            
                            // Specific logic for Pause state
                            if (name == "PausePanel")
                            {
                                // PausePanel should only be active when the UI state is explicitly Paused
                                shouldBeActive = (currentState == UIState.Paused);
                            }
                            else if (name == "PauseButton")
                            {
                                // PauseButton should be visible during dungeon gameplay but hidden when paused or at menu
                                shouldBeActive = (currentState == UIState.Dungeon);
                            }
                            
                            t.gameObject.SetActive(shouldBeActive);
                        }
                    }
                }
                else if (canvas.name == "AlgorithmLabel")
                {
                    // Show algorithm labels only during dungeon or pause
                    canvas.gameObject.SetActive(currentState == UIState.Dungeon || currentState == UIState.Paused || currentState == UIState.PathfindingVisualization);
                }
            }

            // 2. Hide enemies ONLY when at Main Menu
            if (currentState == UIState.MainMenu)
            {
                foreach (var enemy in Object.FindObjectsByType<Enemy.EnemyController>(FindObjectsInactive.Include))
                {
                    enemy.gameObject.SetActive(false);
                }
            }

            // 3. Comprehensive search for any stray objects by name (fallback)
            GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var go in allObjects)
            {
                if (go.scene.isLoaded)
                {
                    foreach (string name in targetNames)
                    {
                        if (go.name == name)
                        {
                            // Skip if it's part of the main menu panel to avoid recursive issues
                            if (mainMenuPanel != null && go.transform.IsChildOf(mainMenuPanel.transform)) continue;
                            
                            bool shouldBeActive = visible;
                            
                            if (name == "PausePanel")
                            {
                                shouldBeActive = (currentState == UIState.Paused);
                            }
                            else if (name == "PauseButton")
                            {
                                shouldBeActive = (currentState == UIState.Dungeon);
                            }
                            
                            go.SetActive(shouldBeActive);
                        }
                    }
                }
            }
        }

        public void RefreshAllHUD()
        {
            // Health Bar
            HealthBarUI healthBar = FindAnyObjectByType<HealthBarUI>(FindObjectsInactive.Include);
            healthBar?.Refresh();

            // Debug UIs
            InventoryDebugUI inventoryUI = FindAnyObjectByType<InventoryDebugUI>(FindObjectsInactive.Include);
            inventoryUI?.UpdateUI();

            EquipmentDebugUI equipmentUI = FindAnyObjectByType<EquipmentDebugUI>(FindObjectsInactive.Include);
            equipmentUI?.UpdateUI();

            LevelDebugUI levelUI = FindAnyObjectByType<LevelDebugUI>(FindObjectsInactive.Include);
            levelUI?.UpdateUI();

            FloorDebugUI floorUI = FindAnyObjectByType<FloorDebugUI>(FindObjectsInactive.Include);
            floorUI?.UpdateUI();

            // Minimap
            MinimapUI minimap = MinimapUI.Instance;
            if (minimap == null) minimap = FindAnyObjectByType<MinimapUI>(FindObjectsInactive.Include);
            
            if (minimap != null)
            {
                if (currentState == UIState.Dungeon)
                {
                    minimap.gameObject.SetActive(true);
                    minimap.enabled = true;
                    minimap.Show();
                    minimap.Rebuild();
                }
                else
                {
                    minimap.gameObject.SetActive(false);
                }
            }
        }

        private void SetGameplayHUDVisible(bool visible)
        {
            if (cachedHudObjects == null || cachedHudObjects.Length == 0) CacheHudObjects();
            
            if (cachedHudObjects == null || cachedHudObjects.Length == 0) return;

            foreach (GameObject obj in cachedHudObjects)
            {
                if (obj != null)
                {
                    // Avoid hiding panels that should be handled separately
                    if (obj == mainMenuPanel || obj == hubVisuals) continue;
                    obj.SetActive(visible);
                }
            }
        }

        public void TogglePause()
        {
            if (currentState != UIState.Dungeon && currentState != UIState.Paused) return;

            if (currentState == UIState.Dungeon)
            {
                currentState = UIState.Paused;
                if (pausePanel != null) pausePanel.SetActive(true);
                Time.timeScale = 0f;
                HideGameplayHUD();
                isHUDHiddenByPause = true;
                PauseMenuUI pauseMenu = pausePanel != null ? pausePanel.GetComponent<PauseMenuUI>() : FindAnyObjectByType<PauseMenuUI>(FindObjectsInactive.Include);
                pauseMenu?.RefreshDungeonButtons();
                Debug.Log("Game Paused");
            }
            else
            {
                currentState = UIState.Dungeon;
                if (pausePanel != null) pausePanel.SetActive(false);
                Time.timeScale = 1f;
                PathfindingVisualizer.Instance?.ClearVisualization();
                if (isHUDHiddenByPause)
                {
                    ShowGameplayHUD();
                    isHUDHiddenByPause = false;
                }
                Debug.Log("Game Resumed");
            }
        }

        public void ResumeGame()
        {
            if (currentState == UIState.Paused) TogglePause();
        }

        public void EnterPathfindingVisualization()
        {
            if (currentState != UIState.Paused)
            {
                return;
            }

            currentState = UIState.PathfindingVisualization;
            Time.timeScale = 0f;
            if (pausePanel != null) pausePanel.SetActive(false);
            if (pauseButton != null) pauseButton.gameObject.SetActive(false);
            if (benchmarkPanel != null) benchmarkPanel.SetActive(false);
            HideGameplayHUD();
        }

        public void ReturnToPauseFromVisualization()
        {
            if (currentState != UIState.PathfindingVisualization)
            {
                return;
            }

            currentState = UIState.Paused;
            Time.timeScale = 0f;
            if (pausePanel != null) pausePanel.SetActive(true);
            if (pauseButton != null) pauseButton.gameObject.SetActive(false);
            HideGameplayHUD();
        }
    }
}
