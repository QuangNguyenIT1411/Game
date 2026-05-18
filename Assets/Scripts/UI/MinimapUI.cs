using UnityEngine;
using UnityEngine.UI;
using DungeonCrawler.Dungeon;
using DungeonCrawler.Enemy;
using DungeonCrawler.Items;
using System.Collections.Generic;

namespace DungeonCrawler.UI
{
    public class MinimapUI : MonoBehaviour
    {
        private static MinimapUI _instance;
        public static MinimapUI Instance
        {
            get
            {
                if (_instance == null) _instance = Object.FindAnyObjectByType<MinimapUI>(FindObjectsInactive.Include);
                return _instance;
            }
        }

        [Header("Settings")]
        public float updateInterval = 0.15f;
        public int textureScale = 4;
        
        [Header("Colors")]
        public Color floorColor = new Color(0.7f, 0.7f, 0.7f);
        public Color wallColor = new Color(0.2f, 0.2f, 0.2f);
        public Color playerColor = Color.yellow;
        public Color enemyColor = Color.red;
        public Color chestColor = new Color(0.5f, 0.3f, 0.1f);
        public Color portalColor = Color.cyan;

        [Header("References")]
        [SerializeField] private RawImage minimapImage;
        [SerializeField] private RectTransform container;

        private Texture2D mapBaseTexture;
        private Texture2D dynamicTexture;
        private Color32[] basePixels;
        private float nextUpdateTime;
        private Vector2Int textureSize;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                if (Application.isPlaying) Object.Destroy(gameObject);
                else Object.DestroyImmediate(gameObject);
                return;
            }
            _instance = this;
        }

        private void Start()
        {
            FindReferencesIfMissing();
        }

        private void FindReferencesIfMissing()
        {
            if (container == null) container = GetComponent<RectTransform>();
            if (minimapImage == null) minimapImage = GetComponentInChildren<RawImage>(true);

            // Runtime fallback if still null
            if (minimapImage == null)
            {
                Transform imgTransform = transform.Find("MinimapImage");
                if (imgTransform != null)
                {
                    minimapImage = imgTransform.GetComponent<RawImage>();
                }
            }
        }

        private void OnEnable()
        {
            Show();
        }

        public void Show()
        {
            gameObject.SetActive(true);
            this.enabled = true;
            FindReferencesIfMissing();

            if (container != null)
            {
                container.gameObject.SetActive(true);
                // User requested dimensions: Anchor Top-Right (1,1), Pos (-160, -150), Size (260, 190)
                container.anchorMin = new Vector2(1f, 1f);
                container.anchorMax = new Vector2(1f, 1f);
                container.pivot = new Vector2(1f, 1f);
                container.anchoredPosition = new Vector2(-160f, -150f);
                container.sizeDelta = new Vector2(260f, 190f);
                container.localScale = Vector3.one;
                container.SetAsLastSibling();
            }

            if (minimapImage != null)
            {
                minimapImage.gameObject.SetActive(true);
                minimapImage.enabled = true;
                minimapImage.color = new Color(1, 1, 1, 1);
                
                // Ensure texture exists or create fallback
                if (minimapImage.texture == null)
                {
                    if (dynamicTexture != null)
                    {
                        minimapImage.texture = dynamicTexture;
                    }
                    else
                    {
                        Texture2D fallback = new Texture2D(128, 128);
                        Color[] pixels = new Color[128 * 128];
                        for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color(0, 0, 0, 0.5f);
                        fallback.SetPixels(pixels);
                        fallback.Apply();
                        minimapImage.texture = fallback;
                    }
                }
            }

            if (TryGetComponent<CanvasGroup>(out var group))
            {
                group.alpha = 1f;
                group.blocksRaycasts = true;
                group.interactable = true;
            }

            Debug.Log("Minimap forced visible");
            Rebuild();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (FloorManager.Instance == null) return;
            if (!gameObject.activeInHierarchy) return;

            // Minimap logic only in Dungeon
            if (FloorManager.Instance.IsVillageMode) return; 

            if (Time.time >= nextUpdateTime)
            {
                UpdateMarkers();
                nextUpdateTime = Time.time + updateInterval;
            }
        }

        public void Rebuild()
        {
            if (_instance == null) _instance = this;
            FindReferencesIfMissing();

            DungeonMapGenerator generator = DungeonMapGenerator.Instance;
            if (generator == null) generator = Object.FindAnyObjectByType<DungeonMapGenerator>();
            
            if (generator == null)
            {
                Debug.LogWarning("Minimap rebuild failed: missing DungeonMapGenerator");
                return;
            }

            if (generator.floorTilemap == null)
            {
                Debug.LogWarning("Minimap rebuild failed: missing FloorTilemap");
                return;
            }

            if (generator.wallTilemap == null)
            {
                Debug.LogWarning("Minimap rebuild failed: missing WallTilemap");
                return;
            }

            if (minimapImage == null)
            {
                Debug.LogWarning("Minimap rebuild failed: missing RawImage");
                return;
            }

            int width = generator.mapWidth;
            int height = generator.mapHeight;
            textureSize = new Vector2Int(width * textureScale, height * textureScale);

            if (mapBaseTexture != null) Object.Destroy(mapBaseTexture);
            if (dynamicTexture != null) Object.Destroy(dynamicTexture);

            mapBaseTexture = new Texture2D(textureSize.x, textureSize.y);
            mapBaseTexture.filterMode = FilterMode.Point;
            mapBaseTexture.wrapMode = TextureWrapMode.Clamp;
            
            dynamicTexture = new Texture2D(textureSize.x, textureSize.y);
            dynamicTexture.filterMode = FilterMode.Point;
            dynamicTexture.wrapMode = TextureWrapMode.Clamp;

            basePixels = new Color32[textureSize.x * textureSize.y];
            Color32 wallCol32 = wallColor;
            Color32 floorCol32 = floorColor;

            for (int y = 0; y < textureSize.y; y++)
            {
                for (int x = 0; x < textureSize.x; x++)
                {
                    int mapX = x / textureScale;
                    int mapY = y / textureScale;
                    Vector3Int cellPos = new Vector3Int(mapX, mapY, 0);

                    bool isFloor = generator.floorTilemap.HasTile(cellPos);
                    bool isWall = generator.wallTilemap.HasTile(cellPos);

                    if (isFloor && !isWall) basePixels[y * textureSize.x + x] = floorCol32;
                    else if (isWall) basePixels[y * textureSize.x + x] = wallCol32;
                    else basePixels[y * textureSize.x + x] = new Color32(0, 0, 0, 0);
                }
            }

            mapBaseTexture.SetPixels32(basePixels);
            mapBaseTexture.Apply();

            minimapImage.texture = dynamicTexture;
            
            // Layout map to fill container with padding
            minimapImage.rectTransform.anchorMin = Vector2.zero;
            minimapImage.rectTransform.anchorMax = Vector2.one;
            minimapImage.rectTransform.offsetMin = new Vector2(10, 10);
            minimapImage.rectTransform.offsetMax = new Vector2(-10, -35); // Room for "MINIMAP" label
            minimapImage.rectTransform.localScale = Vector3.one;
            minimapImage.rectTransform.anchoredPosition = new Vector2(0, -12);
            
            UpdateMarkers();
            Debug.Log("Minimap rebuilt successfully");
        }

        private void UpdateMarkers()
        {
            if (dynamicTexture == null || mapBaseTexture == null) return;

            Graphics.CopyTexture(mapBaseTexture, dynamicTexture);

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) DrawMarker(player.transform.position, playerColor, 3);

            foreach (var enemy in Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude))
                DrawMarker(enemy.transform.position, enemyColor, 2);

            foreach (var chest in Object.FindObjectsByType<Chest>(FindObjectsInactive.Exclude))
                DrawMarker(chest.transform.position, chestColor, 2);

            ExitPortal portal = Object.FindAnyObjectByType<ExitPortal>();
            if (portal != null) DrawMarker(portal.transform.position, portalColor, 3);

            dynamicTexture.Apply();
        }

        private void DrawMarker(Vector3 worldPos, Color color, int size)
        {
            DungeonMapGenerator generator = DungeonMapGenerator.Instance;
            if (generator == null || generator.floorTilemap == null) return;

            Vector3Int cellPos = generator.floorTilemap.WorldToCell(worldPos);
            int centerX = cellPos.x * textureScale + textureScale / 2;
            int centerY = cellPos.y * textureScale + textureScale / 2;

            for (int dy = -size; dy <= size; dy++)
            {
                for (int dx = -size; dx <= size; dx++)
                {
                    int px = centerX + dx;
                    int py = centerY + dy;
                    if (px >= 0 && px < textureSize.x && py >= 0 && py < textureSize.y)
                        dynamicTexture.SetPixel(px, py, color);
                }
            }
        }
    }
}