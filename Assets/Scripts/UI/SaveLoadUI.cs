using DungeonCrawler.Save;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonCrawler.UI
{
    public class SaveLoadUI : MonoBehaviour
    {
        [SerializeField] private Button saveButton;
        [SerializeField] private Button loadButton;
        [SerializeField] private Button deleteButton;
        [SerializeField] private SaveManager saveManager;

        private void Awake()
        {
            BindReferences();
            BindButtons();
        }

        private void Start()
        {
            BindReferences();
            BindButtons();
        }

        public void Bind(Button save, Button load, Button delete)
        {
            saveButton = save;
            loadButton = load;
            deleteButton = delete;
            BindReferences();
            BindButtons();
        }

        private void BindReferences()
        {
            saveManager = saveManager != null ? saveManager : SaveManager.Instance;
            if (saveManager == null)
            {
                saveManager = FindAnyObjectByType<SaveManager>(FindObjectsInactive.Include);
            }
        }

        private void BindButtons()
        {
            if (saveButton != null)
            {
                saveButton.onClick.RemoveListener(SaveGame);
                saveButton.onClick.AddListener(SaveGame);
            }

            if (loadButton != null)
            {
                loadButton.onClick.RemoveListener(LoadGame);
                loadButton.onClick.AddListener(LoadGame);
            }

            if (deleteButton != null)
            {
                deleteButton.onClick.RemoveListener(DeleteSave);
                deleteButton.onClick.AddListener(DeleteSave);
            }
        }

        private void SaveGame()
        {
            BindReferences();
            saveManager?.SaveGame();
        }

        private void LoadGame()
        {
            BindReferences();
            saveManager?.LoadGame();
        }

        private void DeleteSave()
        {
            BindReferences();
            saveManager?.DeleteSave();
        }
    }
}
