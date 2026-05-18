using DungeonCrawler.Combat;
using DungeonCrawler.Dungeon;
using DungeonCrawler.Player;
using DungeonCrawler.Village;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonCrawler.UI
{
    public class RespawnUI : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Button respawnButton;
        [SerializeField] private Button returnToVillageButton;

        private Health playerHealth;
        private FloorManager floorManager;
        private bool shownForCurrentDeath;

        private void Awake()
        {
            BindUI();
            BindSceneReferences();
            Hide();
        }

        private void Start()
        {
            BindSceneReferences();
            ShowIfPlayerAlreadyDead();
        }

        private void Update()
        {
            if (playerHealth == null || floorManager == null)
            {
                BindSceneReferences();
            }

            ShowIfPlayerAlreadyDead();
        }

        private void OnDestroy()
        {
            if (respawnButton != null)
            {
                respawnButton.onClick.RemoveListener(Respawn);
            }

            if (returnToVillageButton != null)
            {
                returnToVillageButton.onClick.RemoveListener(ReturnToVillage);
            }

            if (playerHealth != null)
            {
                playerHealth.Died -= OnPlayerDied;
                playerHealth.OnDied -= OnPlayerDied;
            }
        }

        public void Bind(GameObject respawnPanel, Button button)
        {
            panel = respawnPanel;
            respawnButton = button;
            BindUI();
        }

        private void BindUI()
        {
            if (panel == null)
            {
                Transform panelTransform = transform.Find("RespawnPanel");
                panel = panelTransform != null ? panelTransform.gameObject : gameObject;
            }

            if (respawnButton == null && panel != null)
            {
                Transform respawnButtonTransform = panel.transform.Find("RespawnCheckpointButton") ?? panel.transform.Find("RespawnButton");
                respawnButton = respawnButtonTransform != null
                    ? respawnButtonTransform.GetComponent<Button>()
                    : panel.GetComponentInChildren<Button>(true);
            }

            if (returnToVillageButton == null && panel != null)
            {
                Transform returnButtonTransform = panel.transform.Find("ReturnToVillageButton");
                returnToVillageButton = returnButtonTransform != null ? returnButtonTransform.GetComponent<Button>() : null;
            }

            if (respawnButton != null)
            {
                respawnButton.onClick.RemoveListener(Respawn);
                respawnButton.onClick.AddListener(Respawn);
            }

            if (returnToVillageButton != null)
            {
                returnToVillageButton.onClick.RemoveListener(ReturnToVillage);
                returnToVillageButton.onClick.AddListener(ReturnToVillage);
            }
        }

        private void BindSceneReferences()
        {
            floorManager = FloorManager.Instance != null ? FloorManager.Instance : FindAnyObjectByType<FloorManager>();

            if (playerHealth != null)
            {
                return;
            }

            GameObject player = GameObject.Find("Player");
            playerHealth = player != null ? player.GetComponent<Health>() : null;
            if (playerHealth != null)
            {
                playerHealth.Died -= OnPlayerDied;
                playerHealth.Died += OnPlayerDied;
                playerHealth.OnDied -= OnPlayerDied;
                playerHealth.OnDied += OnPlayerDied;
            }
        }

        private void OnPlayerDied(Health deadHealth)
        {
            DisablePlayerMovement(deadHealth);
            Show();
        }

        public void Respawn()
        {
            Debug.Log("Respawn button clicked", this);
            floorManager = floorManager != null ? floorManager : FloorManager.Instance;
            if (floorManager != null)
            {
                floorManager.RespawnAtCheckpoint();
            }

            Hide();
        }

        public void ReturnToVillage()
        {
            Debug.Log("Return to Village clicked", this);
            floorManager = floorManager != null ? floorManager : FloorManager.Instance;
            if (floorManager != null)
            {
                floorManager.ReturnToVillage();
            }
            else if (VillageManager.Instance != null)
            {
                VillageManager.Instance.EnterVillage();
            }

            Hide();
        }

        private void ShowIfPlayerAlreadyDead()
        {
            if (playerHealth != null && playerHealth.IsDead)
            {
                DisablePlayerMovement(playerHealth);
                Show();
            }
        }

        private void Show()
        {
            // If the game was paused when the player died, resume it to show the respawn panel properly
            if (GameUIManager.Instance != null)
            {
                GameUIManager.Instance.ResumeGame();
            }

            if (shownForCurrentDeath)
{
                if (panel != null && !panel.activeSelf)
                {
                    panel.SetActive(true);
                }

                return;
            }

            if (panel != null)
            {
                panel.SetActive(true);
            }

            shownForCurrentDeath = true;
            Debug.Log("Respawn UI shown", this);
        }

        private void Hide()
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }

            shownForCurrentDeath = false;
        }

        private static void DisablePlayerMovement(Health health)
        {
            if (health == null)
            {
                return;
            }

            PlayerMovement movement = health.GetComponent<PlayerMovement>();
            if (movement != null)
            {
                movement.enabled = false;
            }
        }
    }
}
