using DungeonCrawler.Dungeon;
using DungeonCrawler.UI;
using UnityEngine;

namespace DungeonCrawler.Village
{
    public class VillageManager : MonoBehaviour
    {
        private static VillageManager _instance;
        public static VillageManager Instance
        {
            get
            {
                if (_instance == null) _instance = FindAnyObjectByType<VillageManager>(FindObjectsInactive.Include);
                return _instance;
            }
        }

        [SerializeField] private GameObject villageVisuals;
        [SerializeField] private FloorSelectUI floorSelectUI;
        [SerializeField] private ShopUI shopUI;

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
            // Do not auto-enter dungeon. 
            // GameUIManager will handle showing the Main Menu on start.
        }

        public void Bind(GameObject visuals, FloorSelectUI ui)
        {
            villageVisuals = visuals;
            floorSelectUI = ui;
        }

        public void EnterVillage()
        {
            FloorManager floorManager = FloorManager.Instance;
            if (floorManager != null && !floorManager.IsVillageMode)
            {
                floorManager.ReturnToVillage();
                return;
            }

            SetVillageVisualsVisible(true);
            floorManager?.ClearRoomRuntimeObjectsPublic();
            floorSelectUI = floorSelectUI != null ? floorSelectUI : FindAnyObjectByType<FloorSelectUI>(FindObjectsInactive.Include);
            if (floorSelectUI != null)
            {
                floorSelectUI.ShowVillage();
            }

            shopUI = shopUI != null ? shopUI : FindAnyObjectByType<ShopUI>(FindObjectsInactive.Include);
            if (shopUI != null)
            {
                shopUI.HideShop();
            }
        }

        public void EnterDungeon()
        {
            EnterDungeonAtFloor(1);
        }

        public void EnterDungeonAtFloor(int floor)
        {
            SetVillageVisualsVisible(false);
            if (floorSelectUI != null)
            {
                floorSelectUI.Hide();
            }

            shopUI = shopUI != null ? shopUI : FindAnyObjectByType<ShopUI>(FindObjectsInactive.Include);
            if (shopUI != null)
            {
                shopUI.HideShop();
            }

            FloorManager.Instance?.EnterDungeonAtFloor(floor);
        }

        public void SetVillageVisualsVisible(bool visible)
        {
            if (villageVisuals != null)
            {
                villageVisuals.SetActive(visible);
            }
        }
    }
}
