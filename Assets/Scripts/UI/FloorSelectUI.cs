using DungeonCrawler.Dungeon;
using DungeonCrawler.Village;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonCrawler.UI
{
    public class FloorSelectUI : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Transform floorButtonRoot;
        [SerializeField] private Button enterDungeonButton;
        [SerializeField] private Button floorSelectButton;
        [SerializeField] private Text titleText;

        private Font legacyFont;

        private void Awake()
        {
            legacyFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BindButtons();
        }

        private void Start()
        {
            Refresh();
            if (FloorManager.Instance != null && FloorManager.Instance.IsVillageMode)
            {
                ShowVillage();
            }
            else
            {
                Hide();
            }
        }

        public void Bind(GameObject panelObject, Transform buttonRoot, Button enterButton, Button selectButton, Text title)
        {
            panel = panelObject;
            floorButtonRoot = buttonRoot;
            enterDungeonButton = enterButton;
            floorSelectButton = selectButton;
            titleText = title;
            BindButtons();
        }

        public void ShowVillage()
        {
            if (panel != null)
            {
                panel.SetActive(true);
            }

            Refresh();
        }

        public void Hide()
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }

        public void Refresh()
        {
            if (floorButtonRoot == null)
            {
                return;
            }

            for (int i = floorButtonRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(floorButtonRoot.GetChild(i).gameObject);
            }

            FloorManager floorManager = FloorManager.Instance;
            if (floorManager == null)
            {
                CreateFloorButton(1);
                return;
            }

            foreach (int floor in floorManager.UnlockedCheckpointFloors)
            {
                CreateFloorButton(floor);
            }
        }

        private void BindButtons()
        {
            if (enterDungeonButton != null)
            {
                enterDungeonButton.onClick.RemoveListener(EnterFloorOne);
                enterDungeonButton.onClick.AddListener(EnterFloorOne);
            }

            if (floorSelectButton != null)
            {
                floorSelectButton.onClick.RemoveListener(ShowVillage);
                floorSelectButton.onClick.AddListener(ShowVillage);
            }
        }

        private void EnterFloorOne()
        {
            EnterFloor(1);
        }

        private void EnterFloor(int floor)
        {
            VillageManager villageManager = VillageManager.Instance;
            if (villageManager != null)
            {
                villageManager.EnterDungeonAtFloor(floor);
                return;
            }

            FloorManager.Instance?.EnterDungeonAtFloor(floor);
            Hide();
        }

        private void CreateFloorButton(int floor)
        {
            GameObject buttonObject = new GameObject($"Floor{floor}Button", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(floorButtonRoot, false);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(160f, 36f);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.18f, 0.22f, 0.30f, 0.96f);

            Button button = buttonObject.GetComponent<Button>();
            int floorValue = floor;
            button.onClick.AddListener(() => EnterFloor(floorValue));

            GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(buttonObject.transform, false);

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Text text = textObject.GetComponent<Text>();
            text.font = legacyFont != null ? legacyFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = $"Floor {floor}";
            text.fontSize = 18;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
        }
    }
}
