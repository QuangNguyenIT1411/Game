using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
using DungeonCrawler.AI;
using DungeonCrawler.Combat;
using DungeonCrawler.Dungeon;
using DungeonCrawler.Enemy;
using DungeonCrawler.Items;
using DungeonCrawler.Pathfinding;
using DungeonCrawler.Player;
using DungeonCrawler.Progression;
using DungeonCrawler.Save;
using DungeonCrawler.Stats;
using DungeonCrawler.UI;
using DungeonCrawler.Village;
using System.Collections.Generic;

namespace DungeonCrawler.Editor
{
    /// <summary>
    /// Editor utility that creates a simple test room for dungeon prototyping.
    /// </summary>
    public static class DungeonRoomGenerator
    {
        private const int MapWidth = 60;
        private const int MapHeight = 40;

        private const string GridName = "Grid";
        private const string FloorTilemapName = "FloorTilemap";
        private const string WallTilemapName = "WallTilemap";
        private const string PlayerName = "Player";

        private static readonly Color FloorColor = new Color(0.70f, 0.70f, 0.70f, 1f);
        private static readonly Color WallColor = new Color(0.12f, 0.04f, 0.22f, 1f);
        private static readonly Color WallBorderColor = new Color(0f, 0f, 0f, 1f);
        private static readonly Color PlayerColor = new Color(1f, 0.9f, 0.1f);
        private static readonly Color CameraBackgroundColor = new Color(0.12f, 0.12f, 0.14f);

        [MenuItem("Tools/Dungeon/Generate Test Room")]
        public static void GenerateTestRoom()
        {
            EnemyVisualController.CleanOrphanSlimeVisuals();

            Grid grid = GetOrCreateGrid();
            if (grid == null)
            {
                Debug.LogWarning("Dungeon room generation stopped: Grid could not be created.");
                return;
            }

            Tilemap floorTilemap = GetOrCreateTilemap(grid.transform, FloorTilemapName, 0);
            Tilemap wallTilemap = GetOrCreateTilemap(grid.transform, WallTilemapName, 5);
            if (floorTilemap == null || wallTilemap == null)
            {
                Debug.LogWarning("Dungeon room generation stopped: required Tilemaps could not be created.");
                return;
            }

            EnsureWallCollider(wallTilemap.gameObject);
            RemoveMissingScriptsFromGeneratedObjects();
            SetupDungeonMapGenerator(floorTilemap, wallTilemap);
            GenerateRoom(floorTilemap, wallTilemap);
            
            // Force physics update
            if (wallTilemap.TryGetComponent(out TilemapCollider2D tileCol))
            {
                tileCol.compositeOperation = Collider2D.CompositeOperation.Merge;
            }
if (wallTilemap.TryGetComponent(out CompositeCollider2D compCol))
            {
                compCol.GenerateGeometry();
            }

            // SetupTilemapDebugVisuals removed as we are now using real tiles
EnemyVisualController.CleanOrphanSlimeVisuals();
SetupPathfindingGrid(floorTilemap, wallTilemap);
            GameObject player = GetOrCreatePlayerPlaceholder();
            SetupPlayerComponents(player);

            // Set Player Layer
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer != -1) player.layer = playerLayer;
            Debug.Log("Player collision configured (Editor Setup)");

            SetupPlayerHealthBar(player);
SetupInventoryDebugUI(player);
            SetupEquipmentDebugUI(player);
            SetupLevelDebugUI(player);
            SetupMainMenuUI();
            SetupFloorManager();
            SetupRuntimeVisibilityFixer();
            SetupVillageManager();
            SetupShopManager(player);
            SetupSaveManager();
            SetupFloorDebugUI();
            SetupEnemySpawnerObject();
            SetupChestSpawnerObject();
            EnsureEventSystem();
            SetupRespawnUI();
            SetupFloorSelectUI();
            SetupShopUI();
            SetupSaveLoadUI();
            DestroySpawnEnemyButton();
            SetupMinimapUI();
            SetupPauseUI();
SetupPathfindingBenchmarkUI();

            // Add Physics Sanity Checker
if (grid.GetComponent<PhysicsSanityChecker>() == null)
            {
                Undo.AddComponent<PhysicsSanityChecker>(grid.gameObject);
            }
            
            // Setup GameUIManager before finalizing
            MainMenuUI menuUI = Object.FindAnyObjectByType<MainMenuUI>(FindObjectsInactive.Include);
            Transform buttons = menuUI != null ? menuUI.transform.Find("Buttons") : null;
            SetupGameUIManager(menuUI != null ? menuUI.gameObject : null, buttons != null ? buttons.gameObject : null);

            // Set initial visibility for editor state
            if (menuUI != null) menuUI.gameObject.SetActive(true); // Show in editor so it's ready
            
            // Hide HUD elements in editor state if they exist
            Canvas hudCanvas = EnsureHudCanvas();
            if (hudCanvas != null)
            {
                foreach (Transform child in hudCanvas.transform)
                {
                    if (child.name != "MainMenuPanel")
                    {
                        child.gameObject.SetActive(false);
                    }
                }
            }

            EnemyVisualController.CleanOrphanSlimeVisuals();
            EnemySpawner.SpawnTestPair(GetEnemySpawnPosition(), 1);

            FocusCameraOnRoom();
RemoveMissingScriptsFromGeneratedObjects();

            Selection.activeGameObject = grid.gameObject;
            EditorUtility.SetDirty(grid.gameObject);
        }

        private static Grid GetOrCreateGrid()
        {
            Grid grid = Object.FindAnyObjectByType<Grid>();
            if (grid != null)
            {
                return grid;
            }

            GameObject gridObject = new GameObject(GridName);
            Undo.RegisterCreatedObjectUndo(gridObject, "Create Dungeon Grid");

            return gridObject.AddComponent<Grid>();
        }

        private static Tilemap GetOrCreateTilemap(Transform parent, string tilemapName, int sortingOrder)
        {
            if (parent == null)
            {
                Debug.LogWarning($"Cannot create {tilemapName}: parent transform is missing.");
                return null;
            }

            Transform tilemapTransform = parent.Find(tilemapName);
            if (tilemapTransform != null && tilemapTransform.TryGetComponent(out Tilemap tilemap))
            {
                TilemapRenderer existingRenderer = tilemapTransform.GetComponent<TilemapRenderer>();
                if (existingRenderer == null)
                {
                    existingRenderer = Undo.AddComponent<TilemapRenderer>(tilemapTransform.gameObject);
                }

                if (existingRenderer != null)
                {
                    existingRenderer.enabled = true;
                    existingRenderer.sortingOrder = sortingOrder;
                }

                tilemapTransform.gameObject.layer = 0;
                tilemap.color = Color.white;
                return tilemap;
            }

            GameObject tilemapObject = new GameObject(tilemapName);
            Undo.RegisterCreatedObjectUndo(tilemapObject, $"Create {tilemapName}");

            tilemapObject.transform.SetParent(parent);
            tilemapObject.transform.localPosition = Vector3.zero;
            tilemapObject.layer = 0;

            Tilemap newTilemap = tilemapObject.AddComponent<Tilemap>();
            newTilemap.color = Color.white;
            TilemapRenderer tilemapRenderer = tilemapObject.AddComponent<TilemapRenderer>();
            tilemapRenderer.enabled = true;
            tilemapRenderer.sortingOrder = sortingOrder;

            return newTilemap;
        }

        private static void EnsureWallCollider(GameObject wallTilemapObject)
        {
            if (wallTilemapObject == null)
            {
                Debug.LogWarning("Cannot add wall collider: WallTilemap GameObject is missing.");
                return;
            }

            // Setup Layer
            int wallLayer = LayerMask.NameToLayer("Wall");
            if (wallLayer != -1)
            {
                wallTilemapObject.layer = wallLayer;
            }

            if (!wallTilemapObject.TryGetComponent<TilemapCollider2D>(out TilemapCollider2D tilemapCollider))
            {
                tilemapCollider = Undo.AddComponent<TilemapCollider2D>(wallTilemapObject);
            }

            if (tilemapCollider != null)
            {
                tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
            }

            Rigidbody2D rb = wallTilemapObject.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = Undo.AddComponent<Rigidbody2D>(wallTilemapObject);
            }

            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Static;
            }

            if (!wallTilemapObject.TryGetComponent<CompositeCollider2D>(out CompositeCollider2D composite))
            {
                composite = Undo.AddComponent<CompositeCollider2D>(wallTilemapObject);
            }

            if (composite != null)
            {
                composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
                composite.generationType = CompositeCollider2D.GenerationType.Synchronous;
            }

            Debug.Log("Wall collision initialized: Added Composite/Tilemap Collider and set 'Wall' layer");
            SetupPhysicsLayers();
        }

        private static void SetupPhysicsLayers()
        {
            int playerLayer = LayerMask.NameToLayer("Player");
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            int wallLayer = LayerMask.NameToLayer("Wall");

            if (playerLayer != -1 && wallLayer != -1) Physics2D.SetLayerCollisionMask(playerLayer, Physics2D.GetLayerCollisionMask(playerLayer) | (1 << wallLayer));
            if (enemyLayer != -1 && wallLayer != -1) Physics2D.SetLayerCollisionMask(enemyLayer, Physics2D.GetLayerCollisionMask(enemyLayer) | (1 << wallLayer));
        }

        private static void GenerateRoom(Tilemap floorTilemap, Tilemap wallTilemap)
        {
            if (floorTilemap == null || wallTilemap == null)
            {
                Debug.LogWarning("Cannot generate room: floor or wall Tilemap is missing.");
                return;
            }

            DungeonMapGenerator generator = Object.FindAnyObjectByType<DungeonMapGenerator>(FindObjectsInactive.Include);
            if (generator != null)
            {
                generator.GenerateMap(1, false);
            }
            else
            {
                Debug.LogWarning("DungeonMapGenerator missing. Room not generated.");
            }
        }

        private static void SetupDungeonMapGenerator(Tilemap floorTilemap, Tilemap wallTilemap)
        {
            DungeonMapGenerator generator = Object.FindAnyObjectByType<DungeonMapGenerator>();
            if (generator == null)
            {
                GameObject obj = GameObject.Find("DungeonMapGenerator");
                if (obj != null) generator = obj.GetComponent<DungeonMapGenerator>();
            }

            if (generator == null)
            {
                GameObject generatorObject = new GameObject("DungeonMapGenerator");
                Undo.RegisterCreatedObjectUndo(generatorObject, "Create DungeonMapGenerator");
                generator = generatorObject.AddComponent<DungeonMapGenerator>();
            }

            Tile floorTile = null;
            Tile wallTile = null;

            if (floorTilemap != null)
            {
                floorTile = CreateRuntimeTile(FloorColor, "Generated Floor Tile");
            }

            if (wallTilemap != null)
            {
                wallTile = CreateRuntimeTile(WallColor, "Generated Wall Tile");
                if (wallTile != null)
                {
                    wallTile.sprite = CreateBorderedWallSprite(WallColor, WallBorderColor);
                    wallTile.color = Color.white;
                }
            }

            generator.Configure(floorTile, wallTile);
            EditorUtility.SetDirty(generator);
        }

        private static Tile CreateRuntimeTile(Color color, string tileName)
        {
            Tile tile = ScriptableObject.CreateInstance<Tile>();
            if (tile == null)
            {
                Debug.LogWarning($"Cannot create tile: {tileName}.");
                return null;
            }

            tile.name = tileName;
            tile.color = Color.white;
            tile.sprite = CreateSquareSprite(color);
            tile.colliderType = Tile.ColliderType.Grid; // CRITICAL: Enables collision for this tile

            return tile;
}

        private static void SetupTilemapDebugVisuals(Tilemap floorTilemap, Tilemap wallTilemap)
        {
            GameObject existingVisuals = GameObject.Find("TilemapDebugVisuals");
            if (existingVisuals != null)
            {
                Undo.DestroyObjectImmediate(existingVisuals);
            }

            GameObject root = new GameObject("TilemapDebugVisuals");
            Undo.RegisterCreatedObjectUndo(root, "Create Tilemap Debug Visuals");

            GameObject floorRoot = new GameObject("FloorVisuals");
            Undo.RegisterCreatedObjectUndo(floorRoot, "Create Floor Visuals");
            floorRoot.transform.SetParent(root.transform, false);

            GameObject wallRoot = new GameObject("WallVisuals");
            Undo.RegisterCreatedObjectUndo(wallRoot, "Create Wall Visuals");
            wallRoot.transform.SetParent(root.transform, false);

            Sprite floorSprite = CreateSquareSprite(FloorColor);
            Sprite wallBorderSprite = CreateSquareSprite(Color.black);
            Sprite wallFillSprite = CreateSquareSprite(WallColor);

            TilemapDebugVisuals runtimeVisuals = root.AddComponent<TilemapDebugVisuals>();
            EditorUtility.SetDirty(runtimeVisuals);

            int floorCount = CreateDebugTileVisuals(floorTilemap, floorRoot.transform, floorSprite, FloorColor, 1, false);
            int wallCount = CreateDebugTileVisuals(wallTilemap, wallRoot.transform, wallBorderSprite, Color.black, 6, true, wallFillSprite, WallColor);

            Debug.Log($"Generated debug floor/wall visuals: floor={floorCount} wall={wallCount}");
            EditorUtility.SetDirty(root);
        }

        private static int CreateDebugTileVisuals(
            Tilemap tilemap,
            Transform parent,
            Sprite sprite,
            Color color,
            int sortingOrder,
            bool addInnerFill,
            Sprite innerSprite = null,
            Color innerColor = default)
        {
            if (tilemap == null || parent == null || sprite == null)
            {
                return 0;
            }

            int count = 0;
            BoundsInt bounds = tilemap.cellBounds;
            foreach (Vector3Int cell in bounds.allPositionsWithin)
            {
                if (!tilemap.HasTile(cell))
                {
                    continue;
                }

                GameObject visual = new GameObject($"{tilemap.gameObject.name}_Visual_{cell.x}_{cell.y}");
                Undo.RegisterCreatedObjectUndo(visual, "Create Tile Debug Visual");
                visual.transform.SetParent(parent, false);
                visual.transform.position = tilemap.GetCellCenterWorld(cell);
                visual.layer = 0;

                SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.color = color;
                renderer.sortingOrder = sortingOrder;

                if (addInnerFill && innerSprite != null)
                {
                    GameObject fill = new GameObject("InnerFill");
                    Undo.RegisterCreatedObjectUndo(fill, "Create Wall Inner Fill");
                    fill.transform.SetParent(visual.transform, false);
                    fill.transform.localPosition = Vector3.zero;
                    fill.transform.localScale = new Vector3(0.74f, 0.74f, 1f);
                    fill.layer = 0;

                    SpriteRenderer fillRenderer = fill.AddComponent<SpriteRenderer>();
                    fillRenderer.sprite = innerSprite;
                    fillRenderer.color = innerColor;
                    fillRenderer.sortingOrder = sortingOrder + 1;
                }

                count++;
            }

            return count;
        }

        private static Sprite CreateBorderedWallSprite(Color fillColor, Color borderColor)
        {
            const int textureSize = 16;
            const int borderSize = 2;
            Texture2D texture = new Texture2D(textureSize, textureSize)
            {
                filterMode = FilterMode.Point,
                hideFlags = HideFlags.HideAndDontSave
            };

            for (int x = 0; x < textureSize; x++)
            {
                for (int y = 0; y < textureSize; y++)
                {
                    bool isBorder = x < borderSize
                        || y < borderSize
                        || x >= textureSize - borderSize
                        || y >= textureSize - borderSize;
                    texture.SetPixel(x, y, isBorder ? borderColor : fillColor);
                }
            }

            texture.Apply();

            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, textureSize, textureSize), new Vector2(0.5f, 0.5f), textureSize);
            if (sprite == null)
            {
                Debug.LogWarning("Cannot create bordered wall sprite.");
            }

            return sprite;
        }

        private static Sprite CreateSquareSprite(Color color)
        {
            Texture2D texture = new Texture2D(1, 1)
            {
                filterMode = FilterMode.Point,
                hideFlags = HideFlags.HideAndDontSave
            };

            texture.SetPixel(0, 0, color);
            texture.Apply();

            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            if (sprite == null)
            {
                Debug.LogWarning("Cannot create placeholder sprite.");
            }

            return sprite;
        }

        private static GameObject GetOrCreatePlayerPlaceholder()
        {
            GameObject existingPlayer = GameObject.Find(PlayerName);
            if (existingPlayer != null)
            {
                return existingPlayer;
            }

            GameObject player = new GameObject(PlayerName);
            Undo.RegisterCreatedObjectUndo(player, "Create Player Placeholder");

            player.transform.position = GetPlayerSpawnPosition();

            return player;
        }

        private static void SetupPathfindingGrid(Tilemap floorTilemap, Tilemap wallTilemap)
        {
            GameObject gridObject = GameObject.Find("PathfindingGrid");
            if (gridObject == null)
            {
                gridObject = new GameObject("PathfindingGrid");
                Undo.RegisterCreatedObjectUndo(gridObject, "Create Pathfinding Grid");
            }

            PathfindingGrid pathfindingGrid = gridObject.GetComponent<PathfindingGrid>();
            if (pathfindingGrid == null)
            {
                pathfindingGrid = Undo.AddComponent<PathfindingGrid>(gridObject);
            }

            if (pathfindingGrid == null)
            {
                Debug.LogWarning("Cannot setup PathfindingGrid component.");
                return;
            }

            pathfindingGrid.Configure(floorTilemap, wallTilemap);
            EditorUtility.SetDirty(pathfindingGrid);
        }

        private static void SetupPlayerComponents(GameObject player)
        {
            if (player == null)
            {
                Debug.LogWarning("Cannot setup Player: Player GameObject is missing.");
                return;
            }

            Undo.RecordObject(player, "Setup Player Placeholder");

            Remove3DRenderComponents(player);

            // Remove any old 3D collider from earlier placeholders so only 2D physics is used.
            foreach (Collider collider3D in player.GetComponents<Collider>())
            {
                Undo.DestroyObjectImmediate(collider3D);
            }

            // Rigidbody2D plus Collider2D lets the player collide with WallTilemap's TilemapCollider2D.
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = Undo.AddComponent<Rigidbody2D>(player);
            }

            if (rb == null)
            {
                Debug.LogWarning("Cannot setup Player: Rigidbody2D could not be created.", player);
                return;
            }

            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            if (!player.TryGetComponent<Collider2D>(out _))
            {
                CircleCollider2D circleCollider = Undo.AddComponent<CircleCollider2D>(player);
                if (circleCollider != null)
                {
                    circleCollider.radius = 0.35f;
                }
                else
                {
                    Debug.LogWarning("Cannot setup Player: Collider2D could not be created.", player);
                }
            }

            if (!player.TryGetComponent<SpriteRenderer>(out SpriteRenderer spriteRenderer))
            {
                spriteRenderer = Undo.AddComponent<SpriteRenderer>(player);
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = CreateSquareSprite(PlayerColor);
                spriteRenderer.color = Color.white;
                spriteRenderer.sortingOrder = 20;
            }
            else
            {
                Debug.LogWarning("Cannot setup Player: SpriteRenderer could not be created.", player);
            }

            player.transform.localScale = new Vector3(0.75f, 0.75f, 1f);
            player.transform.position = GetPlayerSpawnPosition();
            player.tag = "Player";

            CharacterStats stats = player.GetComponent<CharacterStats>();
            if (stats == null)
            {
                stats = Undo.AddComponent<CharacterStats>(player);
            }

            if (stats != null)
            {
                stats.ConfigurePlayerDefaults();
            }

            Health health = player.GetComponent<Health>();
            if (health == null)
            {
                health = Undo.AddComponent<Health>(player);
            }

            if (health != null)
            {
                health.Configure(stats != null ? stats.MaxHP : 100, stats != null ? stats.MaxHP : 100);
            }

            DamageDealer damageDealer = player.GetComponent<DamageDealer>();
            if (damageDealer == null)
            {
                damageDealer = Undo.AddComponent<DamageDealer>(player);
            }

            if (damageDealer != null)
            {
                damageDealer.Damage = stats != null ? stats.Attack : 10;
                damageDealer.Cooldown = 0.4f;
            }

            Inventory inventory = player.GetComponent<Inventory>();
            if (inventory == null)
            {
                inventory = Undo.AddComponent<Inventory>(player);
            }

            if (inventory != null)
            {
                inventory.ResetCounts();
            }

            if (!player.TryGetComponent<PlayerMovement>(out _))
            {
                Undo.AddComponent<PlayerMovement>(player);
            }

            SetupPlayerSpriteSheetVisual(player, spriteRenderer);

            if (!player.TryGetComponent<PlayerItemUse>(out _))
            {
                Undo.AddComponent<PlayerItemUse>(player);
            }

            if (!player.TryGetComponent<EquipmentInventory>(out _))
            {
                Undo.AddComponent<EquipmentInventory>(player);
            }

            if (!player.TryGetComponent<PlayerLevel>(out _))
            {
                Undo.AddComponent<PlayerLevel>(player);
            }

            PlayerLevel playerLevel = player.GetComponent<PlayerLevel>();
            if (playerLevel != null)
            {
                playerLevel.ResetProgress();
            }

            EditorUtility.SetDirty(player);
        }

        private static void SetupPlayerSpriteSheetVisual(GameObject player, SpriteRenderer rootRenderer)
        {
            if (player == null)
            {
                return;
            }

            Transform visual = player.transform.Find("PlayerVisual");
            if (visual == null)
            {
                GameObject visualObject = new GameObject("PlayerVisual");
                Undo.RegisterCreatedObjectUndo(visualObject, "Create Player Visual");
                visualObject.transform.SetParent(player.transform, false);
                visual = visualObject.transform;
            }

            visual.localPosition = Vector3.zero;
            visual.localRotation = Quaternion.identity;
            visual.localScale = new Vector3(8f, 8f, 1f);

            SpriteRenderer visualRenderer = visual.GetComponent<SpriteRenderer>();
            if (visualRenderer == null)
            {
                visualRenderer = Undo.AddComponent<SpriteRenderer>(visual.gameObject);
            }

            if (visualRenderer != null)
            {
                visualRenderer.sortingOrder = 20;
                visualRenderer.color = Color.white;
            }

            PlayerVisualController visualController = visual.GetComponent<PlayerVisualController>();
            if (visualController == null)
            {
                visualController = Undo.AddComponent<PlayerVisualController>(visual.gameObject);
            }

            Sprite[] playerFrames = LoadPlayerSprites();
            if (visualController != null)
            {
                visualController.SetFrames(playerFrames);
                EditorUtility.SetDirty(visualController);
            }

            bool hasSpriteSheet = playerFrames != null && playerFrames.Length > 0;
            if (rootRenderer != null)
            {
                rootRenderer.enabled = !hasSpriteSheet;
                rootRenderer.sortingOrder = 20;
            }

            Transform directionArrow = player.transform.Find("DirectionArrow");
            if (directionArrow != null)
            {
                directionArrow.gameObject.SetActive(!hasSpriteSheet);
            }

            Debug.Log(hasSpriteSheet ? $"Player visual loaded: {playerFrames.Length} sprites" : "Fallback placeholder used for Player", player);
            Debug.Log($"Player visual base scale = {visual.localScale}", player);
        }

        private static Sprite[] LoadPlayerSprites()
        {
            const string playerSpritePath = "Assets/Art/Characters/Player/characters_new.png";
            Debug.Log($"Player sprite asset path: {playerSpritePath}");
            Object[] assets = AssetDatabase.LoadAllAssetRepresentationsAtPath(playerSpritePath);
            List<Sprite> sprites = new List<Sprite>();
            foreach (Object asset in assets)
            {
                if (asset is Sprite sprite)
                {
                    sprites.Add(sprite);
                }
            }

            if (sprites.Count < 40)
            {
                Sprite singleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(playerSpritePath);
                if (singleSprite != null && !sprites.Contains(singleSprite))
                {
                    sprites.Add(singleSprite);
                }
            }

            sprites.Sort((a, b) =>
            {
                int yCompare = b.rect.y.CompareTo(a.rect.y);
                return yCompare != 0 ? yCompare : a.rect.x.CompareTo(b.rect.x);
            });

            Debug.Log($"Player sprite children loaded from characters_new.png: {sprites.Count}");
            for (int i = 0; i < Mathf.Min(5, sprites.Count); i++)
            {
                Debug.Log($"Player sprite[{i}]: {sprites[i].name}");
            }

            if (sprites.Count == 0)
            {
                Debug.LogWarning("Player sprite sheet not found or not sliced");
            }
            else if (sprites.Count < 40)
            {
                Debug.LogWarning($"Player sprite sheet has {sprites.Count} sprites, expected 40 for 4 directions x 10 columns.");
            }

            return sprites.ToArray();
        }

        private static void ConfigureEnemyVisualSorting(GameObject enemy)
        {
            if (enemy == null)
            {
                return;
            }

            SpriteRenderer[] renderers = enemy.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (SpriteRenderer renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                renderer.sortingOrder = Mathf.Max(renderer.sortingOrder, 11);
                renderer.color = new Color(renderer.color.r, renderer.color.g, renderer.color.b, 1f);
                EditorUtility.SetDirty(renderer);
            }
        }

        private static void SetupPlayerHealthBar(GameObject player)
        {
            if (player == null || !player.TryGetComponent(out Health playerHealth))
            {
                Debug.LogWarning("Cannot setup health bar: Player Health is missing.");
                return;
            }

            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObject = new GameObject("HUDCanvas");
                Undo.RegisterCreatedObjectUndo(canvasObject, "Create HUD Canvas");
                canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObject.AddComponent<CanvasScaler>();
                canvasObject.AddComponent<GraphicRaycaster>();
            }

            Transform existingBar = canvas.transform.Find("PlayerHealthBar_Background") ?? canvas.transform.Find("PlayerHealthBar");
            GameObject barObject = existingBar != null ? existingBar.gameObject : new GameObject("PlayerHealthBar_Background", typeof(RectTransform), typeof(Image));
            barObject.name = "PlayerHealthBar_Background";
            if (existingBar == null)
            {
                Undo.RegisterCreatedObjectUndo(barObject, "Create Player Health Bar");
                barObject.transform.SetParent(canvas.transform, false);
            }

            RectTransform barRect = barObject.GetComponent<RectTransform>();
            if (barRect == null)
            {
                Debug.LogWarning("Cannot setup health bar: PlayerHealthBar needs a RectTransform.");
                return;
            }

            barRect.localScale = Vector3.one;
            barRect.anchorMin = new Vector2(0f, 1f);
            barRect.anchorMax = new Vector2(0f, 1f);
            barRect.pivot = new Vector2(0f, 1f);
            barRect.anchoredPosition = new Vector2(20f, -20f);
            barRect.sizeDelta = new Vector2(320f, 28f);

            Image backgroundImage = barObject.GetComponent<Image>();
            if (backgroundImage == null)
            {
                backgroundImage = barObject.AddComponent<Image>();
            }

            backgroundImage.color = new Color(0.08f, 0.02f, 0.02f, 0.95f);

            // Clean up any stray children (like HPFrame) that shouldn't be here
            List<Transform> childrenToRemove = new List<Transform>();
            foreach (Transform child in barObject.transform)
            {
                if (child.name != "PlayerHealthBar_Fill" && child.name != "Fill" && child.name != "HPText")
                {
                    childrenToRemove.Add(child);
                }
            }
            foreach (Transform child in childrenToRemove)
            {
                Undo.DestroyObjectImmediate(child.gameObject);
            }

            Transform fillTransform = barObject.transform.Find("PlayerHealthBar_Fill") ?? barObject.transform.Find("Fill");
            GameObject fillObject = fillTransform != null ? fillTransform.gameObject : new GameObject("PlayerHealthBar_Fill", typeof(RectTransform), typeof(Image));
            fillObject.name = "PlayerHealthBar_Fill";
            if (fillTransform == null)
            {
                fillObject.transform.SetParent(barObject.transform, false);
            }

            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            if (fillRect == null)
            {
                Debug.LogWarning("Cannot setup health bar: Fill needs a RectTransform.");
                return;
            }

            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.offsetMin = new Vector2(2f, 2f);
            fillRect.offsetMax = new Vector2(-2f, -2f);
            
            Image fillImage = fillObject.GetComponent<Image>();
            if (fillImage == null)
            {
                fillImage = fillObject.AddComponent<Image>();
            }

            fillImage.color = new Color(0.1f, 0.9f, 0.25f, 1f);
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillAmount = playerHealth.Normalized;

            Transform hpTextTransform = barObject.transform.Find("HPText");
            GameObject hpTextObject = hpTextTransform != null ? hpTextTransform.gameObject : new GameObject("HPText", typeof(RectTransform), typeof(Text));
            if (hpTextTransform == null)
            {
                hpTextObject.transform.SetParent(barObject.transform, false);
            }

            RectTransform hpTextRect = hpTextObject.GetComponent<RectTransform>();
            if (hpTextRect != null)
            {
                hpTextRect.anchorMin = new Vector2(1f, 0.5f);
                hpTextRect.anchorMax = new Vector2(1f, 0.5f);
                hpTextRect.pivot = new Vector2(0f, 0.5f);
                hpTextRect.anchoredPosition = new Vector2(15f, 0f);
                hpTextRect.sizeDelta = new Vector2(200f, 32f);
            }

            Text hpText = hpTextObject.GetComponent<Text>();
            if (hpText != null)
            {
                hpText.text = $"HP: {playerHealth.CurrentHealth} / {playerHealth.MaxHealth}";
                hpText.color = Color.white;
                Font legacyFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (legacyFont != null)
                {
                    hpText.font = legacyFont;
                }
                else
                {
                    Debug.LogWarning("HUD HP text font missing: LegacyRuntime.ttf could not be loaded.");
                }

                hpText.fontSize = 28;
                hpText.alignment = TextAnchor.MiddleLeft;
            }

            HealthBarUI healthBarUI = barObject.GetComponent<HealthBarUI>();
            if (healthBarUI == null)
            {
                healthBarUI = barObject.AddComponent<HealthBarUI>();
            }

            healthBarUI.Bind(playerHealth, fillImage);
            EditorUtility.SetDirty(barObject);
            EditorUtility.SetDirty(healthBarUI);
        }

        private static void SetupInventoryDebugUI(GameObject player)
        {
            if (player == null || !player.TryGetComponent(out Inventory inventory))
            {
                Debug.LogWarning("Cannot setup inventory UI: Player Inventory is missing.");
                return;
            }

            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObject = new GameObject("HUDCanvas");
                Undo.RegisterCreatedObjectUndo(canvasObject, "Create HUD Canvas");
                canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObject.AddComponent<CanvasScaler>();
                canvasObject.AddComponent<GraphicRaycaster>();
            }

            Transform existingInventoryText = canvas.transform.Find("InventoryDebugText");
            GameObject inventoryObject = existingInventoryText != null
                ? existingInventoryText.gameObject
                : new GameObject("InventoryDebugText", typeof(RectTransform), typeof(Text));

            if (existingInventoryText == null)
            {
                Undo.RegisterCreatedObjectUndo(inventoryObject, "Create Inventory Debug UI");
                inventoryObject.transform.SetParent(canvas.transform, false);
            }

            RectTransform rectTransform = inventoryObject.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.localScale = Vector3.one;
                rectTransform.anchorMin = new Vector2(1f, 1f);
                rectTransform.anchorMax = new Vector2(1f, 1f);
                rectTransform.pivot = new Vector2(1f, 1f);
                rectTransform.anchoredPosition = new Vector2(-20f, -20f);
                rectTransform.sizeDelta = new Vector2(300f, 120f);
            }

            Text text = inventoryObject.GetComponent<Text>();
            if (text != null)
            {
                Font legacyFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (legacyFont != null)
                {
                    text.font = legacyFont;
                }

                text.fontSize = 26;
                text.color = Color.white;
                text.alignment = TextAnchor.UpperRight;
            }

            InventoryDebugUI inventoryDebugUI = inventoryObject.GetComponent<InventoryDebugUI>();
            if (inventoryDebugUI == null)
            {
                inventoryDebugUI = inventoryObject.AddComponent<InventoryDebugUI>();
            }

            if (inventoryDebugUI != null)
            {
                inventoryDebugUI.Bind(inventory, text);
                EditorUtility.SetDirty(inventoryDebugUI);
            }

            EditorUtility.SetDirty(inventoryObject);
        }

        private static void SetupEquipmentDebugUI(GameObject player)
        {
            if (player == null || !player.TryGetComponent(out EquipmentInventory equipmentInventory))
            {
                Debug.LogWarning("Cannot setup equipment UI: Player EquipmentInventory is missing.");
                return;
            }

            CharacterStats stats = player.GetComponent<CharacterStats>();
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObject = new GameObject("HUDCanvas");
                Undo.RegisterCreatedObjectUndo(canvasObject, "Create HUD Canvas");
                canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObject.AddComponent<CanvasScaler>();
                canvasObject.AddComponent<GraphicRaycaster>();
            }

            Transform existingEquipmentText = canvas.transform.Find("EquipmentDebugText");
            GameObject equipmentObject = existingEquipmentText != null
                ? existingEquipmentText.gameObject
                : new GameObject("EquipmentDebugText", typeof(RectTransform), typeof(Text));

            if (existingEquipmentText == null)
            {
                Undo.RegisterCreatedObjectUndo(equipmentObject, "Create Equipment Debug UI");
                equipmentObject.transform.SetParent(canvas.transform, false);
            }

            RectTransform rectTransform = equipmentObject.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.localScale = Vector3.one;
                rectTransform.anchorMin = new Vector2(1f, 0f);
                rectTransform.anchorMax = new Vector2(1f, 0f);
                rectTransform.pivot = new Vector2(1f, 0f);
                rectTransform.anchoredPosition = new Vector2(-20f, 20f);
                rectTransform.sizeDelta = new Vector2(320f, 150f);
            }

            Text text = equipmentObject.GetComponent<Text>();
            if (text != null)
            {
                Font legacyFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (legacyFont != null)
                {
                    text.font = legacyFont;
                }

                text.fontSize = 26;
                text.color = Color.white;
                text.alignment = TextAnchor.LowerRight;
            }

            EquipmentDebugUI equipmentDebugUI = equipmentObject.GetComponent<EquipmentDebugUI>();
            if (equipmentDebugUI == null)
            {
                equipmentDebugUI = equipmentObject.AddComponent<EquipmentDebugUI>();
            }

            if (equipmentDebugUI != null)
            {
                equipmentDebugUI.Bind(equipmentInventory, stats, text);
                EditorUtility.SetDirty(equipmentDebugUI);
            }

            EditorUtility.SetDirty(equipmentObject);
        }

        private static void SetupLevelDebugUI(GameObject player)
        {
            if (player == null || !player.TryGetComponent(out PlayerLevel playerLevel))
            {
                Debug.LogWarning("Cannot setup level UI: PlayerLevel is missing.");
                return;
            }

            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObject = new GameObject("HUDCanvas");
                Undo.RegisterCreatedObjectUndo(canvasObject, "Create HUD Canvas");
                canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObject.AddComponent<CanvasScaler>();
                canvasObject.AddComponent<GraphicRaycaster>();
            }

            Transform existingLevelText = canvas.transform.Find("LevelDebugText");
            GameObject levelObject = existingLevelText != null
                ? existingLevelText.gameObject
                : new GameObject("LevelDebugText", typeof(RectTransform), typeof(Text));

            if (existingLevelText == null)
            {
                Undo.RegisterCreatedObjectUndo(levelObject, "Create Level Debug UI");
                levelObject.transform.SetParent(canvas.transform, false);
            }

            RectTransform rectTransform = levelObject.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.localScale = Vector3.one;
                rectTransform.anchorMin = new Vector2(0f, 0f);
                rectTransform.anchorMax = new Vector2(0f, 0f);
                rectTransform.pivot = new Vector2(0f, 0f);
                rectTransform.anchoredPosition = new Vector2(20f, 20f);
                rectTransform.sizeDelta = new Vector2(320f, 120f);
            }

            Text text = levelObject.GetComponent<Text>();
            if (text != null)
            {
                Font legacyFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (legacyFont != null)
                {
                    text.font = legacyFont;
                }

                text.fontSize = 26;
                text.color = Color.white;
                text.alignment = TextAnchor.LowerLeft;
            }

            LevelDebugUI levelDebugUI = levelObject.GetComponent<LevelDebugUI>();
            if (levelDebugUI == null)
            {
                levelDebugUI = levelObject.AddComponent<LevelDebugUI>();
            }

            if (levelDebugUI != null)
            {
                levelDebugUI.Bind(playerLevel, text);
                EditorUtility.SetDirty(levelDebugUI);
            }

            EditorUtility.SetDirty(levelObject);
        }

        private static void SetupMainMenuUI()
        {
            Canvas canvas = EnsureHudCanvas();
            if (canvas == null) return;

            Transform existingMenu = canvas.transform.Find("MainMenuPanel");
            GameObject menuObject = existingMenu != null 
                ? existingMenu.gameObject 
                : new GameObject("MainMenuPanel", typeof(RectTransform), typeof(Image));

            if (existingMenu == null)
            {
                Undo.RegisterCreatedObjectUndo(menuObject, "Create Main Menu Panel");
                menuObject.transform.SetParent(canvas.transform, false);
            }

            RectTransform panelRect = menuObject.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image panelImage = menuObject.GetComponent<Image>();
            panelImage.color = Color.white;
            panelImage.type = Image.Type.Simple;
            panelImage.preserveAspect = false; 
            panelImage.raycastTarget = false; 

            Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/MainMenu/dungeon_crawler_rpg_menu_screen.png");
            if (bgSprite != null) panelImage.sprite = bgSprite;

            // Remove dual-layer complexity if it exists.
            Transform existingHotspots = menuObject.transform.Find("MainMenuHotspots");
            if (existingHotspots != null) Undo.DestroyObjectImmediate(existingHotspots.gameObject);

            Transform buttonContainerTransform = menuObject.transform.Find("Buttons");
            GameObject buttonContainer = buttonContainerTransform != null 
                ? buttonContainerTransform.gameObject 
                : new GameObject("Buttons", typeof(RectTransform));
            if (buttonContainerTransform == null) buttonContainer.transform.SetParent(menuObject.transform, false);

            RectTransform buttonsRect = buttonContainer.GetComponent<RectTransform>();
            if (buttonContainerTransform == null)
            {
                buttonsRect.anchorMin = new Vector2(0.5f, 0.5f);
                buttonsRect.anchorMax = new Vector2(0.5f, 0.5f);
                buttonsRect.pivot = new Vector2(0.5f, 0.5f);
                buttonsRect.anchoredPosition = Vector2.zero;
                buttonsRect.sizeDelta = new Vector2(300, 400);
            }

            // Ensure container doesn't block raycasts
            Image containerImage = buttonContainer.GetComponent<Image>();
            if (containerImage != null) containerImage.raycastTarget = false;

            // User requirement: Remove automatic layout
            VerticalLayoutGroup vlg = buttonContainer.GetComponent<VerticalLayoutGroup>();
            if (vlg != null) Undo.DestroyObjectImmediate(vlg);
            ContentSizeFitter csf = buttonContainer.GetComponent<ContentSizeFitter>();
            if (csf != null) Undo.DestroyObjectImmediate(csf);

            // Setup buttons - using default positions only if new
            Vector2 btnSize = new Vector2(260f, 40f);
            Button playBtn = CreateOrUpdateMainMenuButton(buttonContainer.transform, "PlayButton", "Play", new Vector2(0, 165), btnSize);
            Button shopBtn = CreateOrUpdateMainMenuButton(buttonContainer.transform, "ShopButton", "Open Shop", new Vector2(0, 110), btnSize);
            Button floorBtn = CreateOrUpdateMainMenuButton(buttonContainer.transform, "SelectFloorButton", "Select Floor", new Vector2(0, 55), btnSize);
            Button saveBtn = CreateOrUpdateMainMenuButton(buttonContainer.transform, "SaveButton", "Save", new Vector2(0, 0), btnSize);
            Button loadBtn = CreateOrUpdateMainMenuButton(buttonContainer.transform, "LoadButton", "Load", new Vector2(0, -55), btnSize);
            Button deleteBtn = CreateOrUpdateMainMenuButton(buttonContainer.transform, "DeleteSaveButton", "Delete Save", new Vector2(0, -110), btnSize);

            // Add Test button in top right
            Button testBtn = CreateOrUpdateMainMenuButton(menuObject.transform, "TestButton", "Test", new Vector2(-10, -10), new Vector2(100, 40));
RectTransform testRect = testBtn.GetComponent<RectTransform>();
            if (testRect != null)
            {
                testRect.anchorMin = new Vector2(1, 1);
                testRect.anchorMax = new Vector2(1, 1);
                testRect.pivot = new Vector2(1, 1);
                testRect.anchoredPosition = new Vector2(-20, -20);
            }
            // Ensure Test button has visible text since it's a test button
            Transform testTextTransform = testBtn.transform.Find("Text");
            if (testTextTransform == null)
            {
                GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
                Undo.RegisterCreatedObjectUndo(textObj, "Create Test Button Text");
                textObj.transform.SetParent(testBtn.transform, false);
                testTextTransform = textObj.transform;
            }
            
            if (testTextTransform != null && testTextTransform.gameObject != null)
            {
                testTextTransform.gameObject.SetActive(true);
                Text testText = testTextTransform.GetComponent<Text>();
                if (testText != null)
                {
                    testText.enabled = true;
                    testText.text = "Test";
                    testText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    testText.color = Color.white;
                    testText.alignment = TextAnchor.MiddleCenter;
                    
                    RectTransform textRect = testText.GetComponent<RectTransform>();
                    textRect.anchorMin = Vector2.zero;
                    textRect.anchorMax = Vector2.one;
                    textRect.sizeDelta = Vector2.zero;
                }
            }
Image testImg = testBtn.GetComponent<Image>();
            if (testImg != null)
            {
                testImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
                testImg.enabled = true;
            }

            MainMenuUI menuUI = menuObject.GetComponent<MainMenuUI>();
            if (menuUI == null) menuUI = Undo.AddComponent<MainMenuUI>(menuObject);
            
            menuUI.Bind(menuObject, playBtn, shopBtn, floorBtn, saveBtn, loadBtn, deleteBtn, testBtn);
menuObject.SetActive(false); // Hide by default in editor generation; GameUIManager will show it if needed

            // Re-bind other managers
            GameUIManager uiManager = Object.FindAnyObjectByType<GameUIManager>();
            if (uiManager != null)
            {
                uiManager.hubVisuals = menuObject;
                uiManager.mainMenuPanel = menuObject;
                uiManager.mainMenuHotspots = buttonContainer; 
                uiManager.saveLoadPanel = null;
                EditorUtility.SetDirty(uiManager);
            }
            
            Debug.Log("Removed stray menu preview image from gameplay HUD");
            EditorUtility.SetDirty(menuObject);
            }

        private static Button CreateOrUpdateMainMenuButton(Transform parent, string name, string label, Vector2 defaultPos, Vector2 size)
        {
            Transform existing = parent.Find(name);
            GameObject buttonObject = existing != null 
                ? existing.gameObject 
                : new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            
            if (existing == null)
            {
                Undo.RegisterCreatedObjectUndo(buttonObject, $"Create {name}");
                buttonObject.transform.SetParent(parent, false);
                RectTransform rect = buttonObject.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = defaultPos;
                rect.sizeDelta = size;
            }

            UnityEngine.UI.Image image = buttonObject.GetComponent<UnityEngine.UI.Image>();
            if (image != null)
            {
                // Only set default color if the image is brand new (no sprite and no color changed from default white)
                if (existing == null && image.sprite == null && image.color == Color.white)
                {
                    image.color = new Color(0.2f, 0.2f, 0.2f, 0.8f); // Default semi-dark look
                }
                image.raycastTarget = true;
            }

            UnityEngine.UI.Button button = buttonObject.GetComponent<UnityEngine.UI.Button>();
            button.transition = Selectable.Transition.None;

            Transform existingText = buttonObject.transform.Find("Text");
            GameObject textObj = existingText != null ? existingText.gameObject : new GameObject("Text", typeof(RectTransform), typeof(UnityEngine.UI.Text));
            if (existingText == null) textObj.transform.SetParent(buttonObject.transform, false);
            
            UnityEngine.UI.Text text = textObj.GetComponent<UnityEngine.UI.Text>();
            if (text != null)
            {
                text.text = label;
                text.fontSize = 20;
                text.color = Color.white;
                text.alignment = TextAnchor.MiddleCenter;
                Font legacyFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (legacyFont != null) text.font = legacyFont;
            }

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            EditorUtility.SetDirty(buttonObject);
            return button;
        }

        private static void SetupGameUIManager(GameObject menuPanel, GameObject hotspots)
        {
            GameUIManager uiManager = Object.FindAnyObjectByType<GameUIManager>(FindObjectsInactive.Include);
            if (uiManager == null)
            {
                GameObject obj = new GameObject("GameUIManager");
                Undo.RegisterCreatedObjectUndo(obj, "Create GameUIManager");
                uiManager = obj.AddComponent<GameUIManager>();
            }

            Canvas canvas = EnsureHudCanvas();
            
            uiManager.mainMenuPanel = menuPanel;
            uiManager.hubVisuals = menuPanel; // Using same object for now
            uiManager.mainMenuHotspots = hotspots;
            
            // Assign other panels if they exist
            if (canvas != null)
            {
                uiManager.inventoryPanel = canvas.transform.Find("InventoryDebugText")?.gameObject;
                uiManager.statusPanel = canvas.transform.Find("LevelDebugText")?.gameObject;
                uiManager.equipmentPanel = canvas.transform.Find("EquipmentDebugText")?.gameObject;
                
                uiManager.floorDebugText = canvas.transform.Find("FloorDebugText")?.gameObject;
                uiManager.benchmarkPanel = canvas.transform.Find("BenchmarkPanel")?.gameObject;

                // Find or create PausePanel
                Transform pause = canvas.transform.Find("PausePanel");
                if (pause != null) uiManager.pausePanel = pause.gameObject;

                Transform pauseBtn = canvas.transform.Find("PauseButton");
                if (pauseBtn != null) uiManager.pauseButton = pauseBtn.GetComponent<Button>();
            }

            EditorUtility.SetDirty(uiManager);
            Debug.Log("GameUIManager configured");
        }

        private static void SetupFloorManager()
        {
            FloorManager floorManager = Object.FindAnyObjectByType<FloorManager>();
            if (floorManager == null)
            {
                GameObject floorManagerObject = new GameObject("FloorManager");
                Undo.RegisterCreatedObjectUndo(floorManagerObject, "Create FloorManager");
                floorManager = floorManagerObject.AddComponent<FloorManager>();
            }

            if (floorManager != null)
            {
                if (floorManager.GetComponent<DungeonRunManager>() == null)
                {
                    Undo.AddComponent<DungeonRunManager>(floorManager.gameObject);
                }

                floorManager.ResetForGeneratedRoom();
                EditorUtility.SetDirty(floorManager);
            }
        }

        private static void SetupRuntimeVisibilityFixer()
        {
            RuntimeVisibilityFixer fixer = Object.FindAnyObjectByType<RuntimeVisibilityFixer>();
            if (fixer == null)
            {
                GameObject fixerObject = new GameObject("RuntimeVisibilityFixer");
                Undo.RegisterCreatedObjectUndo(fixerObject, "Create RuntimeVisibilityFixer");
                fixer = fixerObject.AddComponent<RuntimeVisibilityFixer>();
            }

            if (fixer != null)
            {
                EditorUtility.SetDirty(fixer);
            }
        }

        private static void SetupVillageManager()
        {
            VillageManager villageManager = Object.FindAnyObjectByType<VillageManager>();
            if (villageManager == null)
            {
                GameObject villageManagerObject = new GameObject("VillageManager");
                Undo.RegisterCreatedObjectUndo(villageManagerObject, "Create VillageManager");
                villageManager = villageManagerObject.AddComponent<VillageManager>();
            }

            GameObject villageVisuals = GameObject.Find("VillageVisuals");
            if (villageVisuals == null)
            {
                villageVisuals = new GameObject("VillageVisuals");
                Undo.RegisterCreatedObjectUndo(villageVisuals, "Create Village Visuals");
            }

            SpriteRenderer villageFloor = villageVisuals.GetComponent<SpriteRenderer>();
            if (villageFloor == null)
            {
                villageFloor = Undo.AddComponent<SpriteRenderer>(villageVisuals);
            }

            if (villageFloor != null)
            {
                villageFloor.sprite = CreateSquareSprite(new Color(0.22f, 0.40f, 0.28f, 1f));
                villageFloor.color = Color.white;
                villageFloor.sortingOrder = 0;
                villageVisuals.transform.position = new Vector3(4.5f, 5.5f, 0.2f);
                villageVisuals.transform.localScale = new Vector3(8f, 5f, 1f);
                EditorUtility.SetDirty(villageFloor);
            }

            if (villageManager != null)
            {
                villageManager.Bind(villageVisuals, Object.FindAnyObjectByType<FloorSelectUI>(FindObjectsInactive.Include));
                EditorUtility.SetDirty(villageManager);
            }
        }

        private static void SetupShopManager(GameObject player)
        {
            ShopManager shopManager = Object.FindAnyObjectByType<ShopManager>(FindObjectsInactive.Include);
            if (shopManager == null)
            {
                GameObject shopManagerObject = new GameObject("ShopManager");
                Undo.RegisterCreatedObjectUndo(shopManagerObject, "Create ShopManager");
                shopManager = shopManagerObject.AddComponent<ShopManager>();
            }

            Inventory inventory = player != null ? player.GetComponent<Inventory>() : null;
            if (shopManager != null)
            {
                shopManager.Bind(inventory);
                EditorUtility.SetDirty(shopManager);
            }
        }

        private static void SetupSaveManager()
        {
            SaveManager saveManager = Object.FindAnyObjectByType<SaveManager>(FindObjectsInactive.Include);
            if (saveManager == null)
            {
                GameObject saveManagerObject = new GameObject("SaveManager");
                Undo.RegisterCreatedObjectUndo(saveManagerObject, "Create SaveManager");
                saveManager = saveManagerObject.AddComponent<SaveManager>();
            }

            if (saveManager != null)
            {
                EditorUtility.SetDirty(saveManager);
            }
        }

        private static void RemoveMissingScriptsFromGeneratedObjects()
        {
            int removedCount = 0;
            foreach (GameObject gameObject in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (gameObject == null || !gameObject.scene.IsValid())
                {
                    continue;
                }

                removedCount += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(gameObject);
            }

            if (removedCount > 0)
            {
                Debug.Log($"Removed missing script components: {removedCount}");
            }
        }

        private static Canvas EnsureHudCanvas()
        {
            Canvas canvas = null;
            Canvas[] allCanvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var c in allCanvases)
            {
                if (c.name == "HUDCanvas" && c.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    canvas = c;
                    break;
                }
            }

            if (canvas == null)
            {
                GameObject canvasObject = new GameObject("HUDCanvas");
                Undo.RegisterCreatedObjectUndo(canvasObject, "Create HUD Canvas");
                canvas = canvasObject.AddComponent<Canvas>();
            }

            canvas.gameObject.name = "HUDCanvas";
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.transform.localScale = Vector3.one;

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = Undo.AddComponent<CanvasScaler>(canvas.gameObject);
            
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            if (canvas.GetComponent<GraphicRaycaster>() == null)
            {
                Undo.AddComponent<GraphicRaycaster>(canvas.gameObject);
            }

            return canvas;
        }

        private static void SetupFloorDebugUI()
        {
            Canvas canvas = EnsureHudCanvas();
            if (canvas == null) return;

            Transform existingFloorText = canvas.transform.Find("FloorDebugText");
            GameObject floorObject = existingFloorText != null
                ? existingFloorText.gameObject
                : new GameObject("FloorDebugText", typeof(RectTransform), typeof(Text));

            if (existingFloorText == null)
            {
                Undo.RegisterCreatedObjectUndo(floorObject, "Create Floor Debug UI");
                floorObject.transform.SetParent(canvas.transform, false);
            }

            RectTransform rectTransform = floorObject.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.localScale = Vector3.one;
                rectTransform.anchorMin = new Vector2(0f, 1f);
                rectTransform.anchorMax = new Vector2(0f, 1f);
                rectTransform.pivot = new Vector2(0f, 1f);
                rectTransform.anchoredPosition = new Vector2(20f, -55f);
                rectTransform.sizeDelta = new Vector2(300f, 100f);
            }

            Text text = floorObject.GetComponent<Text>();
            if (text != null)
            {
                Font legacyFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (legacyFont != null)
                {
                    text.font = legacyFont;
                }

                text.fontSize = 26;
                text.color = Color.white;
                text.alignment = TextAnchor.UpperLeft;
            }

            FloorDebugUI floorDebugUI = floorObject.GetComponent<FloorDebugUI>();
            if (floorDebugUI == null)
            {
                floorDebugUI = floorObject.AddComponent<FloorDebugUI>();
            }

            EditorUtility.SetDirty(floorObject);
            if (floorDebugUI != null)
            {
                EditorUtility.SetDirty(floorDebugUI);
            }
        }

        private static void SetupRespawnUI()
        {
            Canvas canvas = EnsureHudCanvas();
            if (canvas == null)
            {
                Debug.LogWarning("Cannot setup Respawn UI: HUDCanvas could not be created.");
                return;
            }

            RespawnUI respawnUI = canvas.GetComponent<RespawnUI>();
            if (respawnUI == null)
            {
                respawnUI = Undo.AddComponent<RespawnUI>(canvas.gameObject);
            }

            Transform existingPanel = canvas.transform.Find("RespawnPanel");
            GameObject panelObject = existingPanel != null
                ? existingPanel.gameObject
                : new GameObject("RespawnPanel", typeof(RectTransform), typeof(Image));

            if (existingPanel == null)
            {
                Undo.RegisterCreatedObjectUndo(panelObject, "Create Respawn Panel");
                panelObject.transform.SetParent(canvas.transform, false);
            }

            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            if (panelRect != null)
            {
                panelRect.anchorMin = new Vector2(0.5f, 0.5f);
                panelRect.anchorMax = new Vector2(0.5f, 0.5f);
                panelRect.pivot = new Vector2(0.5f, 0.5f);
                panelRect.anchoredPosition = Vector2.zero;
                panelRect.sizeDelta = new Vector2(300f, 170f);
            }

            Image panelImage = panelObject.GetComponent<Image>();
            if (panelImage == null)
            {
                panelImage = panelObject.AddComponent<Image>();
            }

            panelImage.color = new Color(0.02f, 0.02f, 0.02f, 0.82f);

            Transform existingButton = panelObject.transform.Find("RespawnCheckpointButton") ?? panelObject.transform.Find("RespawnButton");
            GameObject buttonObject = existingButton != null
                ? existingButton.gameObject
                : new GameObject("RespawnCheckpointButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.name = "RespawnCheckpointButton";

            if (existingButton == null)
            {
                Undo.RegisterCreatedObjectUndo(buttonObject, "Create Respawn Button");
                buttonObject.transform.SetParent(panelObject.transform, false);
            }

            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            if (buttonRect != null)
            {
                buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
                buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
                buttonRect.pivot = new Vector2(0.5f, 0.5f);
                buttonRect.anchoredPosition = new Vector2(0f, 34f);
                buttonRect.sizeDelta = new Vector2(230f, 46f);
            }

            Image buttonImage = buttonObject.GetComponent<Image>();
            if (buttonImage == null)
            {
                buttonImage = buttonObject.AddComponent<Image>();
            }

            buttonImage.color = new Color(0.18f, 0.32f, 0.42f, 1f);

            Button button = buttonObject.GetComponent<Button>();
            if (button == null)
            {
                button = buttonObject.AddComponent<Button>();
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(respawnUI.Respawn);

            Transform existingText = buttonObject.transform.Find("Text");
            GameObject textObject = existingText != null
                ? existingText.gameObject
                : new GameObject("Text", typeof(RectTransform), typeof(Text));

            if (existingText == null)
            {
                Undo.RegisterCreatedObjectUndo(textObject, "Create Respawn Button Text");
                textObject.transform.SetParent(buttonObject.transform, false);
            }

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            if (textRect != null)
            {
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;
            }

            Text text = textObject.GetComponent<Text>();
            if (text != null)
            {
                Font legacyFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (legacyFont != null)
                {
                    text.font = legacyFont;
                }

                text.text = "Respawn at Checkpoint";
                text.fontSize = 18;
                text.color = Color.white;
                text.alignment = TextAnchor.MiddleCenter;
            }

            Button returnButton = CreateOrUpdatePanelButton(
                panelObject.transform,
                "ReturnToVillageButton",
                new Vector2(0f, -34f),
                new Vector2(230f, 46f),
                "Return to Village",
                new Color(0.30f, 0.26f, 0.22f, 1f));
            if (returnButton != null)
            {
                returnButton.onClick.RemoveAllListeners();
                returnButton.onClick.AddListener(respawnUI.ReturnToVillage);
            }

            respawnUI.Bind(panelObject, button);
            panelObject.SetActive(false);

            EditorUtility.SetDirty(canvas.gameObject);
            EditorUtility.SetDirty(panelObject);
            EditorUtility.SetDirty(buttonObject);
            if (returnButton != null)
            {
                EditorUtility.SetDirty(returnButton);
            }
            EditorUtility.SetDirty(respawnUI);
        }

        private static Button CreateOrUpdatePanelButton(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, string label, Color color)
        {
            Transform existingButton = parent.Find(name);
            GameObject buttonObject = existingButton != null
                ? existingButton.gameObject
                : new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));

            if (existingButton == null)
            {
                Undo.RegisterCreatedObjectUndo(buttonObject, $"Create {name}");
                buttonObject.transform.SetParent(parent, false);
            }

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = anchoredPosition;
                rect.sizeDelta = size;
            }

            Image image = buttonObject.GetComponent<Image>();
            if (image != null)
            {
                image.color = color;
            }

            Button button = buttonObject.GetComponent<Button>();
            if (button == null)
            {
                button = buttonObject.AddComponent<Button>();
            }

            Transform existingText = buttonObject.transform.Find("Text");
            GameObject textObject = existingText != null
                ? existingText.gameObject
                : new GameObject("Text", typeof(RectTransform), typeof(Text));

            if (existingText == null)
            {
                Undo.RegisterCreatedObjectUndo(textObject, $"Create {name} Text");
                textObject.transform.SetParent(buttonObject.transform, false);
            }

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            if (textRect != null)
            {
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;
            }

            Text text = textObject.GetComponent<Text>();
            if (text != null)
            {
                Font legacyFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (legacyFont != null)
                {
                    text.font = legacyFont;
                }

                text.text = label;
                text.fontSize = 18;
                text.color = Color.white;
                text.alignment = TextAnchor.MiddleCenter;
            }

            return button;
        }

        private static void SetupFloorSelectUI()
        {
            Canvas canvas = EnsureHudCanvas();
            if (canvas == null)
            {
                return;
            }

            Transform existingPanel = canvas.transform.Find("FloorSelectPanel") ?? canvas.transform.Find("VillagePanel");
            GameObject panelObject = existingPanel != null
                ? existingPanel.gameObject
                : new GameObject("FloorSelectPanel", typeof(RectTransform), typeof(Image));
            panelObject.name = "FloorSelectPanel";

            if (existingPanel == null)
            {
                Undo.RegisterCreatedObjectUndo(panelObject, "Create Floor Select Panel");
                panelObject.transform.SetParent(canvas.transform, false);
            }

            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            if (panelRect != null)
            {
                panelRect.anchorMin = new Vector2(0.5f, 0.5f);
                panelRect.anchorMax = new Vector2(0.5f, 0.5f);
                panelRect.pivot = new Vector2(0.5f, 0.5f);
                panelRect.anchoredPosition = Vector2.zero;
                panelRect.sizeDelta = new Vector2(360f, 300f);
            }

            Image panelImage = panelObject.GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.color = new Color(0.04f, 0.07f, 0.06f, 0.95f);
            }

            Text titleText = CreateOrUpdatePanelText(panelObject.transform, "Title", new Vector2(0f, 112f), new Vector2(320f, 40f), "Select Floor", 24);
            
            // Close button for this panel
            Button closeButton = CreateOrUpdatePanelButton(panelObject.transform, "CloseButton", new Vector2(0, -112), new Vector2(100, 30), "Close", new Color(0.2f, 0.2f, 0.2f, 1f));

            Transform existingRoot = panelObject.transform.Find("FloorButtonRoot");
            GameObject rootObject = existingRoot != null
                ? existingRoot.gameObject
                : new GameObject("FloorButtonRoot", typeof(RectTransform), typeof(VerticalLayoutGroup));

            if (existingRoot == null)
            {
                Undo.RegisterCreatedObjectUndo(rootObject, "Create Floor Button Root");
                rootObject.transform.SetParent(panelObject.transform, false);
            }

            RectTransform rootRect = rootObject.GetComponent<RectTransform>();
            if (rootRect != null)
            {
                rootRect.anchorMin = new Vector2(0.5f, 0.5f);
                rootRect.anchorMax = new Vector2(0.5f, 0.5f);
                rootRect.pivot = new Vector2(0.5f, 0.5f);
                rootRect.anchoredPosition = new Vector2(0f, 0f); // Center it
                rootRect.sizeDelta = new Vector2(180f, 180f);
            }

            VerticalLayoutGroup layout = rootObject.GetComponent<VerticalLayoutGroup>();
            if (layout != null)
            {
                layout.childAlignment = TextAnchor.UpperCenter;
                layout.spacing = 8f;
                layout.childControlWidth = false;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
            }

            FloorSelectUI floorSelectUI = panelObject.GetComponent<FloorSelectUI>();
            if (floorSelectUI == null)
            {
                floorSelectUI = Undo.AddComponent<FloorSelectUI>(panelObject);
            }

            if (floorSelectUI != null)
            {
                floorSelectUI.Bind(panelObject, rootObject.transform, null, null, titleText);
                EditorUtility.SetDirty(floorSelectUI);
            }
            
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => panelObject.SetActive(false));

            panelObject.SetActive(false);
            EditorUtility.SetDirty(panelObject);
        }

        private static Text CreateOrUpdatePanelText(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, string label, int fontSize)
        {
            Transform existingText = parent.Find(name);
            GameObject textObject = existingText != null
                ? existingText.gameObject
                : new GameObject(name, typeof(RectTransform), typeof(Text));

            if (existingText == null)
            {
                Undo.RegisterCreatedObjectUndo(textObject, $"Create {name}");
                textObject.transform.SetParent(parent, false);
            }

            RectTransform rect = textObject.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = anchoredPosition;
                rect.sizeDelta = size;
            }

            Text text = textObject.GetComponent<Text>();
            if (text != null)
            {
                Font legacyFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (legacyFont != null)
                {
                    text.font = legacyFont;
                }

                text.text = label;
                text.fontSize = fontSize;
                text.color = Color.white;
                text.alignment = TextAnchor.MiddleCenter;
            }

            return text;
        }

        private static void SetupShopUI()
        {
            Canvas canvas = EnsureHudCanvas();
            if (canvas == null)
            {
                return;
            }

            Transform villagePanel = canvas.transform.Find("VillagePanel");
            Button openShopButton = null;
            if (villagePanel != null)
            {
                Transform openShopTransform = villagePanel.Find("OpenShopButton");
                openShopButton = openShopTransform != null ? openShopTransform.GetComponent<Button>() : null;
            }

            Transform existingPanel = canvas.transform.Find("ShopPanel");
            GameObject panelObject = existingPanel != null
                ? existingPanel.gameObject
                : new GameObject("ShopPanel", typeof(RectTransform), typeof(Image));

            if (existingPanel == null)
            {
                Undo.RegisterCreatedObjectUndo(panelObject, "Create Shop Panel");
                panelObject.transform.SetParent(canvas.transform, false);
            }

            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            if (panelRect != null)
            {
                panelRect.anchorMin = new Vector2(0.5f, 0.5f);
                panelRect.anchorMax = new Vector2(0.5f, 0.5f);
                panelRect.pivot = new Vector2(0.5f, 0.5f);
                panelRect.anchoredPosition = Vector2.zero;
                panelRect.sizeDelta = new Vector2(320f, 240f);
            }

            Image panelImage = panelObject.GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.color = new Color(0.05f, 0.04f, 0.03f, 0.94f);
            }

            CreateOrUpdatePanelText(panelObject.transform, "ShopTitle", new Vector2(0f, 96f), new Vector2(260f, 36f), "Shop", 26);
            Button buyButton = CreateOrUpdatePanelButton(panelObject.transform, "BuyPotionButton", new Vector2(0f, 42f), new Vector2(220f, 42f), "Buy Potion - 5 Gold", new Color(0.16f, 0.32f, 0.20f, 1f));
            Button sellButton = CreateOrUpdatePanelButton(panelObject.transform, "SellMaterialButton", new Vector2(0f, -14f), new Vector2(220f, 42f), "Sell Material +3 Gold", new Color(0.28f, 0.22f, 0.14f, 1f));
            Button closeButton = CreateOrUpdatePanelButton(panelObject.transform, "CloseShopButton", new Vector2(0f, -78f), new Vector2(220f, 42f), "Close Shop", new Color(0.20f, 0.20f, 0.24f, 1f));

            panelObject.transform.SetAsLastSibling();

            ShopUI oldPanelShopUI = panelObject.GetComponent<ShopUI>();
            if (oldPanelShopUI != null)
            {
                Undo.DestroyObjectImmediate(oldPanelShopUI);
            }

            ShopUI shopUI = canvas.GetComponent<ShopUI>();
            if (shopUI == null)
            {
                shopUI = Undo.AddComponent<ShopUI>(canvas.gameObject);
            }

            if (shopUI != null)
            {
                shopUI.Bind(panelObject, null, buyButton, sellButton, closeButton);
                EditorUtility.SetDirty(shopUI);
            }

            if (closeButton != null && shopUI != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(shopUI.CloseShop);
            }

            panelObject.SetActive(false);
            EditorUtility.SetDirty(panelObject);
        }

        private static void SetupSaveLoadUI()
        {
            Canvas canvas = EnsureHudCanvas();
            if (canvas == null) return;

            Transform existingPanel = canvas.transform.Find("SaveLoadPanel");
            if (existingPanel != null)
            {
                Undo.DestroyObjectImmediate(existingPanel.gameObject);
            }
        }

        private static void DestroySpawnEnemyButton()
        {
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            Transform existingButton = canvas.transform.Find("SpawnEnemyButton");
            if (existingButton != null)
            {
                Undo.DestroyObjectImmediate(existingButton.gameObject);
            }
        }

        private static void SetupPauseUI()
        {
            Canvas canvas = EnsureHudCanvas();
            if (canvas == null) return;

            // 1. Pause Button on HUD
            Transform existingPauseBtn = canvas.transform.Find("PauseButton");
            GameObject pauseBtnObj = existingPauseBtn != null ? existingPauseBtn.gameObject : new GameObject("PauseButton", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            if (existingPauseBtn == null)
            {
                Undo.RegisterCreatedObjectUndo(pauseBtnObj, "Create Pause Button");
                pauseBtnObj.transform.SetParent(canvas.transform, false);
            }

            RectTransform pauseBtnRect = pauseBtnObj.GetComponent<RectTransform>();
            pauseBtnRect.anchorMin = new Vector2(1f, 1f);
            pauseBtnRect.anchorMax = new Vector2(1f, 1f);
            pauseBtnRect.pivot = new Vector2(1f, 1f);
            pauseBtnRect.anchoredPosition = new Vector2(-20f, -320f); // Below Minimap
            pauseBtnRect.sizeDelta = new Vector2(100f, 40f);

            UnityEngine.UI.Image pauseBtnImg = pauseBtnObj.GetComponent<UnityEngine.UI.Image>();
            pauseBtnImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            UnityEngine.UI.Button pauseBtn = pauseBtnObj.GetComponent<UnityEngine.UI.Button>();
            
            Transform existingPauseText = pauseBtnObj.transform.Find("Text");
            GameObject pauseTextObj = existingPauseText != null ? existingPauseText.gameObject : new GameObject("Text", typeof(RectTransform), typeof(UnityEngine.UI.Text));
            if (existingPauseText == null) pauseTextObj.transform.SetParent(pauseBtnObj.transform, false);
            
            UnityEngine.UI.Text pauseText = pauseTextObj.GetComponent<UnityEngine.UI.Text>();
            pauseText.text = "Pause";
            pauseText.fontSize = 20;
            pauseText.color = Color.white;
            pauseText.alignment = TextAnchor.MiddleCenter;
            Font legacyFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (legacyFont != null) pauseText.font = legacyFont;
            
            RectTransform pauseTextRect = pauseTextObj.GetComponent<RectTransform>();
            pauseTextRect.anchorMin = Vector2.zero;
            pauseTextRect.anchorMax = Vector2.one;
            pauseTextRect.offsetMin = Vector2.zero;
            pauseTextRect.offsetMax = Vector2.zero;

            // 2. Pause Panel
            Transform existingPausePanel = canvas.transform.Find("PausePanel");
            GameObject pausePanelObj = existingPausePanel != null ? existingPausePanel.gameObject : new GameObject("PausePanel", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            if (existingPausePanel == null)
            {
                Undo.RegisterCreatedObjectUndo(pausePanelObj, "Create Pause Panel");
                pausePanelObj.transform.SetParent(canvas.transform, false);
            }

            RectTransform pausePanelRect = pausePanelObj.GetComponent<RectTransform>();
            pausePanelRect.anchorMin = Vector2.zero;
            pausePanelRect.anchorMax = Vector2.one;
            pausePanelRect.offsetMin = Vector2.zero;
            pausePanelRect.offsetMax = Vector2.zero;

            UnityEngine.UI.Image pausePanelImg = pausePanelObj.GetComponent<UnityEngine.UI.Image>();
            pausePanelImg.color = new Color(0f, 0f, 0f, 0.42f);

            Transform existingCard = pausePanelObj.transform.Find("PauseMenuCard");
            GameObject cardObj = existingCard != null ? existingCard.gameObject : new GameObject("PauseMenuCard", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(VerticalLayoutGroup));
            if (existingCard == null) cardObj.transform.SetParent(pausePanelObj.transform, false);

            RectTransform cardRect = cardObj.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = Vector2.zero;
            cardRect.sizeDelta = new Vector2(420f, 620f);

            UnityEngine.UI.Image cardImg = cardObj.GetComponent<UnityEngine.UI.Image>();
            cardImg.color = new Color(0.035f, 0.038f, 0.045f, 0.88f);

            VerticalLayoutGroup cardLayout = cardObj.GetComponent<VerticalLayoutGroup>();
            cardLayout.childAlignment = TextAnchor.UpperCenter;
            cardLayout.padding = new RectOffset(0, 0, 24, 24);
            cardLayout.spacing = 10f;
            cardLayout.childControlHeight = false;
            cardLayout.childControlWidth = false;
            cardLayout.childForceExpandHeight = false;
            cardLayout.childForceExpandWidth = false;

            Text title = CreateOrUpdatePanelText(cardObj.transform, "Title", Vector2.zero, new Vector2(360f, 48f), "PAUSED", 32);
            title.fontStyle = FontStyle.Bold;

            Transform existingGameplay = cardObj.transform.Find("GameplayButtons");
            GameObject gameplayObj = existingGameplay != null ? existingGameplay.gameObject : new GameObject("GameplayButtons", typeof(RectTransform), typeof(VerticalLayoutGroup));
            if (existingGameplay == null) gameplayObj.transform.SetParent(cardObj.transform, false);
            ConfigurePauseButtonGroup(gameplayObj.GetComponent<RectTransform>(), gameplayObj.GetComponent<VerticalLayoutGroup>(), 159f);

            Text visualizationTitle = CreateOrUpdatePanelText(cardObj.transform, "VisualizationTitle", Vector2.zero, new Vector2(360f, 34f), "Pathfinding Visualization", 22);
            visualizationTitle.fontStyle = FontStyle.Bold;

            Transform existingVisualization = cardObj.transform.Find("VisualizationButtons");
            GameObject visualizationObj = existingVisualization != null ? existingVisualization.gameObject : new GameObject("VisualizationButtons", typeof(RectTransform), typeof(VerticalLayoutGroup));
            if (existingVisualization == null) visualizationObj.transform.SetParent(cardObj.transform, false);
            ConfigurePauseButtonGroup(visualizationObj.GetComponent<RectTransform>(), visualizationObj.GetComponent<VerticalLayoutGroup>(), 282f);

            Vector2 btnSize = new Vector2(280, 36);
            UnityEngine.UI.Button resumeBtn = CreateOrUpdatePanelButton(gameplayObj.transform, "ResumeButton", Vector2.zero, btnSize, "Resume", new Color(0.2f, 0.4f, 0.2f));
            UnityEngine.UI.Button villageBtn = CreateOrUpdatePanelButton(gameplayObj.transform, "VillageButton", Vector2.zero, btnSize, "Return to Village", new Color(0.4f, 0.3f, 0.2f));
            UnityEngine.UI.Button saveBtn = CreateOrUpdatePanelButton(gameplayObj.transform, "SaveButton", Vector2.zero, btnSize, "Save", new Color(0.2f, 0.2f, 0.4f));
            UnityEngine.UI.Button loadBtn = CreateOrUpdatePanelButton(gameplayObj.transform, "LoadButton", Vector2.zero, btnSize, "Load", new Color(0.3f, 0.2f, 0.4f));
            UnityEngine.UI.Button visualizationReturnBtn = CreateOrUpdatePanelButton(canvas.transform, "VisualizationReturnButton", Vector2.zero, new Vector2(120f, 42f), "Return", new Color(0.12f, 0.12f, 0.14f, 0.92f));
            RectTransform visualizationReturnRect = visualizationReturnBtn.GetComponent<RectTransform>();
            if (visualizationReturnRect != null)
            {
                visualizationReturnRect.anchorMin = new Vector2(1f, 1f);
                visualizationReturnRect.anchorMax = new Vector2(1f, 1f);
                visualizationReturnRect.pivot = new Vector2(1f, 1f);
                visualizationReturnRect.anchoredPosition = new Vector2(-18f, -18f);
                visualizationReturnRect.sizeDelta = new Vector2(120f, 42f);
            }
            visualizationReturnBtn.gameObject.SetActive(false);

            // Component setup
            PauseMenuUI pauseMenu = pausePanelObj.GetComponent<PauseMenuUI>();
            if (pauseMenu == null) pauseMenu = Undo.AddComponent<PauseMenuUI>(pausePanelObj);
            pauseMenu.Bind(resumeBtn, villageBtn, saveBtn, loadBtn);

            // GameUIManager wiring
            GameUIManager uiManager = Object.FindAnyObjectByType<GameUIManager>(FindObjectsInactive.Include);
            if (uiManager != null)
            {
                uiManager.pausePanel = pausePanelObj;
                uiManager.pauseButton = pauseBtn;
                uiManager.resumeButton = resumeBtn;
                EditorUtility.SetDirty(uiManager);
            }

            pausePanelObj.SetActive(false);
            EditorUtility.SetDirty(pausePanelObj);
        }

        private static void ConfigurePauseButtonGroup(RectTransform rectTransform, VerticalLayoutGroup layout, float height)
        {
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = new Vector2(320f, height);
            }

            if (layout == null)
            {
                return;
            }

            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 5f;
            layout.childControlHeight = false;
            layout.childControlWidth = false;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;
        }

        private static void SetupMinimapUI()
        {
            Canvas canvas = EnsureHudCanvas();
            if (canvas == null) return;

            Transform existingMinimap = canvas.transform.Find("Minimap");
            GameObject minimapObject = existingMinimap != null 
                ? existingMinimap.gameObject 
                : new GameObject("Minimap", typeof(RectTransform), typeof(UnityEngine.UI.Image));

            if (existingMinimap == null)
            {
                Undo.RegisterCreatedObjectUndo(minimapObject, "Create Minimap UI");
                minimapObject.transform.SetParent(canvas.transform, false);
            }

            RectTransform rect = minimapObject.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.localScale = Vector3.one;
                rect.anchorMin = new Vector2(1f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(1f, 1f);
                rect.anchoredPosition = new Vector2(-160f, -150f); 
                rect.sizeDelta = new Vector2(260f, 190f); 
            }

            minimapObject.transform.SetAsLastSibling();

            UnityEngine.UI.Image bgImage = minimapObject.GetComponent<UnityEngine.UI.Image>();
            if (bgImage != null)
            {
                bgImage.color = new Color(0f, 0f, 0f, 0.65f);
            }

            // TitleText
            Transform existingTitle = minimapObject.transform.Find("TitleText");
            GameObject titleObject = existingTitle != null ? existingTitle.gameObject : new GameObject("TitleText", typeof(RectTransform), typeof(UnityEngine.UI.Text));
            if (existingTitle == null)
            {
                titleObject.transform.SetParent(minimapObject.transform, false);
            }
            
            UnityEngine.UI.Text titleText = titleObject.GetComponent<UnityEngine.UI.Text>();
            if (titleText != null)
            {
                titleText.text = "MINIMAP";
                titleText.fontSize = 18;
                titleText.color = Color.white;
                titleText.alignment = TextAnchor.MiddleCenter;
                Font legacyFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (legacyFont != null) titleText.font = legacyFont;
            }

            RectTransform titleRect = titleObject.GetComponent<RectTransform>();
            if (titleRect != null)
            {
                titleRect.anchorMin = new Vector2(0, 1);
                titleRect.anchorMax = new Vector2(1, 1);
                titleRect.pivot = new Vector2(0.5f, 1);
                titleRect.anchoredPosition = new Vector2(0, -5);
                titleRect.sizeDelta = new Vector2(0, 25);
            }

            // MinimapImage
            Transform existingImage = minimapObject.transform.Find("MinimapImage");
            GameObject imageObject = existingImage != null 
                ? existingImage.gameObject 
                : new GameObject("MinimapImage", typeof(RectTransform), typeof(UnityEngine.UI.RawImage));
            
            if (existingImage == null)
            {
                imageObject.transform.SetParent(minimapObject.transform, false);
            }

            RectTransform imageRect = imageObject.GetComponent<RectTransform>();
            if (imageRect != null)
            {
                imageRect.anchorMin = Vector2.zero;
                imageRect.anchorMax = Vector2.one;
                imageRect.pivot = new Vector2(0.5f, 0.5f);
                imageRect.offsetMin = new Vector2(10, 10);
                imageRect.offsetMax = new Vector2(-10, -35);
                imageRect.anchoredPosition = new Vector2(0, -12);
            }

            UnityEngine.UI.RawImage rawImg = imageObject.GetComponent<UnityEngine.UI.RawImage>();
            if (rawImg != null)
            {
                rawImg.color = Color.white;
                rawImg.raycastTarget = false;
            }

            MinimapUI minimapUI = minimapObject.GetComponent<MinimapUI>();
            if (minimapUI == null)
            {
                minimapUI = Undo.AddComponent<MinimapUI>(minimapObject);
            }

            // Wire references via SerializedObject to be safe
            SerializedObject minimapSo = new SerializedObject(minimapUI);
            minimapSo.FindProperty("minimapImage").objectReferenceValue = rawImg;
            minimapSo.FindProperty("container").objectReferenceValue = rect;
            minimapSo.ApplyModifiedProperties();

            EditorUtility.SetDirty(minimapObject);
        }

        private static void SetupEnemySpawnerObject()
        {
            EnemySpawner spawner = Object.FindAnyObjectByType<EnemySpawner>();
            if (spawner == null)
            {
                GameObject spawnerObject = new GameObject("EnemySpawner");
                Undo.RegisterCreatedObjectUndo(spawnerObject, "Create EnemySpawner");
                spawner = spawnerObject.AddComponent<EnemySpawner>();
            }

            if (spawner != null)
            {
                spawner.BindSlimePrefabs(
                    AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Characters/Enemy/Slime/Slime/Prefabs/Green_Slime.prefab"),
                    AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Characters/Enemy/Slime/Slime/Prefabs/Blue_Slime.prefab"),
                    AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Characters/Enemy/Slime/Slime/Prefabs/Purple_Slime.prefab"),
                    AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Characters/Enemy/Slime/Slime/Prefabs/Red_Slime.prefab"));
                EditorUtility.SetDirty(spawner);
            }
}

        private static void SetupChestSpawnerObject()
        {
            ChestSpawner spawner = Object.FindAnyObjectByType<ChestSpawner>();
            if (spawner != null)
            {
                return;
            }

            GameObject spawnerObject = new GameObject("ChestSpawner");
            Undo.RegisterCreatedObjectUndo(spawnerObject, "Create ChestSpawner");
            spawnerObject.AddComponent<ChestSpawner>();
        }

        private static void EnsureEventSystem()
        {
            EventSystem eventSystem = Object.FindAnyObjectByType<EventSystem>();
            if (eventSystem != null)
            {
                if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
                {
                    Undo.AddComponent<InputSystemUIInputModule>(eventSystem.gameObject);
                }

                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem");
            Undo.RegisterCreatedObjectUndo(eventSystemObject, "Create EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
        }

        private static void Remove3DRenderComponents(GameObject player)
        {
            foreach (MeshFilter meshFilter in player.GetComponents<MeshFilter>())
            {
                Undo.DestroyObjectImmediate(meshFilter);
            }

            foreach (MeshRenderer meshRenderer in player.GetComponents<MeshRenderer>())
            {
                Undo.DestroyObjectImmediate(meshRenderer);
            }
        }

        private static void FocusCameraOnRoom()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                Undo.RegisterCreatedObjectUndo(cameraObject, "Create Main Camera");

                camera = cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
            }

            if (camera == null)
            {
                Debug.LogWarning("Cannot focus camera: Main Camera is missing and could not be created.");
                return;
            }

            Undo.RecordObject(camera.transform, "Focus Camera On Dungeon Room");
            Undo.RecordObject(camera, "Configure Dungeon Camera");

            camera.orthographic = true;
            camera.orthographicSize = 12f;
            camera.backgroundColor = CameraBackgroundColor;
            camera.transform.position = GetRoomCenterPosition() + new Vector3(0f, 0f, -10f);
            camera.transform.rotation = Quaternion.identity;

            EditorUtility.SetDirty(camera);
            }

            private static void SetupPathfindingBenchmarkUI()
            {
                Canvas canvas = EnsureHudCanvas();
                if (canvas == null) return;

                Transform existingPanel = canvas.transform.Find("BenchmarkPanel");
                GameObject panelObject = existingPanel != null ? existingPanel.gameObject : new GameObject("BenchmarkPanel", typeof(RectTransform));
                if (existingPanel == null)
                {
                    Undo.RegisterCreatedObjectUndo(panelObject, "Create Benchmark Panel");
                    panelObject.transform.SetParent(canvas.transform, false);
                }

                RectTransform panelRect = panelObject.GetComponent<RectTransform>();
                panelRect.anchorMin = new Vector2(0f, 1f);
                panelRect.anchorMax = new Vector2(0f, 1f);
                panelRect.pivot = new Vector2(0f, 1f);
                panelRect.anchoredPosition = new Vector2(20f, -80f); 
                panelRect.sizeDelta = new Vector2(250f, 240f);

                Text astarText = CreateBenchmarkText(panelObject.transform, "AStarStats", new Vector2(0, 0), Color.green);
                Text bfsText = CreateBenchmarkText(panelObject.transform, "BFSStats", new Vector2(0, -110), new Color(0.6f, 0.4f, 1f));

                PathfindingBenchmarkUI benchmarkUI = panelObject.GetComponent<PathfindingBenchmarkUI>();
                if (benchmarkUI == null) benchmarkUI = Undo.AddComponent<PathfindingBenchmarkUI>(panelObject);
            
                SerializedObject so = new SerializedObject(benchmarkUI);
                so.FindProperty("astarStatsText").objectReferenceValue = astarText;
                so.FindProperty("bfsStatsText").objectReferenceValue = bfsText;
                so.ApplyModifiedProperties();

                EditorUtility.SetDirty(benchmarkUI);
            }

            private static Text CreateBenchmarkText(Transform parent, string name, Vector2 pos, Color color)
            {
                Transform existing = parent.Find(name);
                GameObject obj = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(Text));
                if (existing == null)
                {
                    obj.transform.SetParent(parent, false);
                }

                RectTransform rect = obj.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(1, 1);
                rect.pivot = new Vector2(0, 1);
                rect.anchoredPosition = pos;
                rect.sizeDelta = new Vector2(0, 100);

                Text text = obj.GetComponent<Text>();
                text.color = color;
                text.fontSize = 16;
                text.alignment = TextAnchor.UpperLeft;
                Font legacyFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (legacyFont != null) text.font = legacyFont;

                return text;
            }

            private static Vector3 GetRoomCenterPosition()
            {
                return new Vector3((MapWidth - 1) * 0.5f, (MapHeight - 1) * 0.5f, 0f);
            }

        private static Vector3 GetPlayerSpawnPosition()
        {
            return new Vector3(4.5f, 5.5f, 0f);
        }

        private static Vector3 GetEnemySpawnPosition()
        {
            return new Vector3(16.5f, 5.5f, 0f);
        }
    }
}
