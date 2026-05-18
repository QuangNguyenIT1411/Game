using DungeonCrawler.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonCrawler.UI
{
    public class LevelDebugUI : MonoBehaviour
    {
        [SerializeField] private PlayerLevel playerLevel;
        [SerializeField] private Text levelText;

        private void Awake()
        {
            FindLevelIfMissing();
            if (levelText == null)
            {
                levelText = GetComponent<Text>();
            }
        }

        private void OnEnable()
        {
            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void Bind(PlayerLevel targetLevel, Text targetText)
        {
            Unsubscribe();
            playerLevel = targetLevel;
            levelText = targetText;
            Subscribe();
            Refresh();
        }

        private void FindLevelIfMissing()
        {
            if (playerLevel != null)
            {
                return;
            }

            GameObject player = GameObject.Find("Player");
            if (player != null)
            {
                playerLevel = player.GetComponent<PlayerLevel>();
            }
        }

        private void Subscribe()
        {
            if (playerLevel != null)
            {
                playerLevel.Changed += OnLevelChanged;
            }
        }

        private void Unsubscribe()
        {
            if (playerLevel != null)
            {
                playerLevel.Changed -= OnLevelChanged;
            }
        }

        private void OnLevelChanged(PlayerLevel changedLevel)
        {
            Refresh();
        }

        public void UpdateUI()
        {
            Refresh();
        }

        private void Refresh()
        {
            FindLevelIfMissing();
            if (playerLevel == null || levelText == null)
            {
                return;
            }

            levelText.text =
                $"Level: {playerLevel.Level}\n" +
                $"EXP: {playerLevel.CurrentExp} / {playerLevel.ExpToNext}\n" +
                $"Stat Points: {playerLevel.StatPoints}\n" +
                "Z ATK, X DEF, C HP";
        }
    }
}
