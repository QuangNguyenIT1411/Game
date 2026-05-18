using DungeonCrawler.Dungeon;
using DungeonCrawler.Items;
using DungeonCrawler.Village;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonCrawler.UI
{
    public class ShopUI : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Button openShopButton;
        [SerializeField] private Button buyPotionButton;
        [SerializeField] private Button sellMaterialButton;
        [SerializeField] private Button closeShopButton;
        [SerializeField] private Inventory inventory;
        [SerializeField] private ShopManager shopManager;

        private void Awake()
        {
            BindReferences();
            BindUiByName();
            BindButtons();
            HideShop();
        }

        private void Start()
        {
            BindReferences();
            BindUiByName();
            BindButtons();
            UpdateVillageVisibility();
        }

        private void Update()
        {
            UpdateVillageVisibility();
        }

        public void Bind(GameObject shopPanel, Button openButton, Button buyButton, Button sellButton, Button closeButton)
        {
            panel = shopPanel;
            openShopButton = openButton;
            buyPotionButton = buyButton;
            sellMaterialButton = sellButton;
            closeShopButton = closeButton;
            BindReferences();
            BindButtons();
        }

        public void OpenShop()
        {
            Debug.Log("Open Shop clicked", this);
            BindReferences();
            BindUiByName();
            BindButtons();

            if (!IsVillageMode())
            {
                HideShop();
                return;
            }

            if (panel != null)
            {
                panel.SetActive(true);
                panel.transform.SetAsLastSibling();
            }

            Debug.Log("Shop UI opened", this);
        }

        public void HideShop()
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }

        public void CloseShop()
        {
            HideShop();
            Debug.Log("Shop UI closed", this);
        }

        private void BuyPotion()
        {
            BindReferences();
            shopManager?.BuyPotion();
        }

        private void SellMaterial()
        {
            BindReferences();
            shopManager?.SellMaterial();
        }

        private void BindReferences()
        {
            shopManager = shopManager != null ? shopManager : ShopManager.Instance;
            if (shopManager == null)
            {
                shopManager = FindAnyObjectByType<ShopManager>(FindObjectsInactive.Include);
            }

            if (inventory == null)
            {
                GameObject player = GameObject.Find("Player");
                inventory = player != null ? player.GetComponent<Inventory>() : null;
            }

            shopManager?.Bind(inventory);
        }

        private void BindUiByName()
        {
            if (panel == null)
            {
                Transform panelTransform = transform.Find("ShopPanel");
                if (panelTransform == null && transform.parent != null)
                {
                    panelTransform = transform.parent.Find("ShopPanel");
                }

                GameObject panelObject = GameObject.Find("ShopPanel");
                panel = panelTransform != null ? panelTransform.gameObject : panelObject;
            }

            if (openShopButton == null)
            {
                GameObject openObject = GameObject.Find("OpenShopButton");
                openShopButton = openObject != null ? openObject.GetComponent<Button>() : null;
            }

            if (buyPotionButton == null && panel != null)
            {
                Transform buttonTransform = panel.transform.Find("BuyPotionButton");
                buyPotionButton = buttonTransform != null ? buttonTransform.GetComponent<Button>() : null;
            }

            if (sellMaterialButton == null && panel != null)
            {
                Transform buttonTransform = panel.transform.Find("SellMaterialButton");
                sellMaterialButton = buttonTransform != null ? buttonTransform.GetComponent<Button>() : null;
            }

            if (closeShopButton == null && panel != null)
            {
                Transform buttonTransform = panel.transform.Find("CloseShopButton");
                closeShopButton = buttonTransform != null ? buttonTransform.GetComponent<Button>() : null;
            }
        }

        private void BindButtons()
        {
            if (openShopButton != null)
            {
                openShopButton.onClick.RemoveListener(OpenShop);
                openShopButton.onClick.AddListener(OpenShop);
            }

            if (buyPotionButton != null)
            {
                buyPotionButton.onClick.RemoveListener(BuyPotion);
                buyPotionButton.onClick.AddListener(BuyPotion);
            }

            if (sellMaterialButton != null)
            {
                sellMaterialButton.onClick.RemoveListener(SellMaterial);
                sellMaterialButton.onClick.AddListener(SellMaterial);
            }

            if (closeShopButton != null)
            {
                closeShopButton.onClick.RemoveListener(CloseShop);
                closeShopButton.onClick.AddListener(CloseShop);
            }
        }

        private void UpdateVillageVisibility()
        {
            bool isVillage = IsVillageMode();
            if (openShopButton != null)
            {
                openShopButton.gameObject.SetActive(isVillage);
            }

            if (!isVillage)
            {
                HideShop();
            }
        }

        private static bool IsVillageMode()
        {
            FloorManager floorManager = FloorManager.Instance;
            return floorManager == null || floorManager.IsVillageMode;
        }
    }
}
