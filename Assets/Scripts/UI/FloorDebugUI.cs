using DungeonCrawler.Dungeon;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonCrawler.UI
{
    public class FloorDebugUI : MonoBehaviour
    {
        [SerializeField] private Text floorText;

        private void Awake()
        {
            if (floorText == null)
            {
                floorText = GetComponent<Text>();
            }
        }

        public void UpdateUI()
        {
            Refresh();
        }

        private void Refresh()
        {
            FloorManager floorManager = FloorManager.Instance;
            if (floorManager == null || floorText == null)
            {
                return;
            }

            floorText.text = $"Floor: {floorManager.CurrentFloor}";
        }

        private void Update()
        {
            Refresh();
        }
    }
}
