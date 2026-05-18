using UnityEngine;
using UnityEngine.Tilemaps;

namespace DungeonCrawler.Dungeon
{
    public class TilemapDebugVisuals : MonoBehaviour
    {
        private static readonly Color FloorColor = new Color(0.80f, 0.80f, 0.80f, 1f); // Light gray
        private static readonly Color WallColor = new Color(0.10f, 0.05f, 0.20f, 1f); // Dark purple
        private static readonly Color WallBorderColor = Color.black;

        private void Start()
        {
            Rebuild();
        }

        public void Rebuild()
        {
            Tilemap floorTilemap = FindTilemap("FloorTilemap");
            Tilemap wallTilemap = FindTilemap("WallTilemap");
            if (floorTilemap == null || wallTilemap == null)
            {
                Debug.LogWarning("[TilemapDebugVisuals] Cannot rebuild: FloorTilemap or WallTilemap missing.", this);
                return;
            }

            ClearChildren(transform);

            GameObject floorRoot = CreateChildRoot("FloorVisuals");
            GameObject wallRoot = CreateChildRoot("WallVisuals");

            Sprite floorSprite = CreateSquareSprite(FloorColor);
            Sprite wallBorderSprite = CreateSquareSprite(WallBorderColor);
            Sprite wallFillSprite = CreateSquareSprite(WallColor);

            int floorCount = CreateVisuals(floorTilemap, floorRoot.transform, floorSprite, FloorColor, 1, false);
            int wallCount = CreateVisuals(wallTilemap, wallRoot.transform, wallBorderSprite, WallBorderColor, 6, true, wallFillSprite, WallColor);

            Debug.Log($"Dungeon visuals rebuilt | floor={floorCount} wall={wallCount}", this);
        }

        private GameObject CreateChildRoot(string rootName)
        {
            GameObject childRoot = new GameObject(rootName);
            childRoot.transform.SetParent(transform, false);
            childRoot.layer = 0;
            return childRoot;
        }

        private static int CreateVisuals(
            Tilemap tilemap,
            Transform parent,
            Sprite sprite,
            Color color,
            int sortingOrder,
            bool addInnerFill,
            Sprite innerSprite = null,
            Color innerColor = default)
        {
            int count = 0;
            Vector3 cellScale = tilemap.layoutGrid != null ? tilemap.layoutGrid.cellSize : Vector3.one;
            BoundsInt bounds = tilemap.cellBounds;
            foreach (Vector3Int cell in bounds.allPositionsWithin)
            {
                if (!tilemap.HasTile(cell))
                {
                    continue;
                }

                GameObject visual = new GameObject($"{tilemap.gameObject.name}_Visual_{cell.x}_{cell.y}");
                visual.transform.SetParent(parent, false);
                visual.transform.position = tilemap.GetCellCenterWorld(cell);
                visual.transform.localScale = new Vector3(cellScale.x, cellScale.y, 1f);
                visual.layer = 0;

                SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.color = color;
                renderer.sortingOrder = sortingOrder;

                if (addInnerFill && innerSprite != null)
                {
                    GameObject fill = new GameObject("InnerFill");
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

        private static Tilemap FindTilemap(string objectName)
        {
            GameObject tilemapObject = GameObject.Find(objectName);
            return tilemapObject != null ? tilemapObject.GetComponent<Tilemap>() : null;
        }

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Destroy(parent.GetChild(i).gameObject);
            }
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
            return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }
    }
}
