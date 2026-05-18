using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using DungeonCrawler.Pathfinding;

namespace DungeonCrawler.Dungeon
{
    public class DungeonData
    {
        public Vector3 PlayerSpawn;
        public Vector3 ExitPortal;
        public Vector3 BossSpawn;
        public List<Vector3> EnemySpawns = new List<Vector3>();
        public List<Vector3> ChestSpawns = new List<Vector3>();
    }

    public class DungeonMapGenerator : MonoBehaviour
    {
        public static DungeonMapGenerator Instance { get; private set; }

        [Header("General Settings")]
        public int seed;
        public bool useRandomSeed = true;
        public int mapWidth = 60;
        public int mapHeight = 40;

        [Header("Room Settings")]
        public int roomCountMin = 8;
        public int roomCountMax = 12;
        public int roomSizeMin = 5;
        public int roomSizeMax = 10;
        public int bossRoomSize = 14;

        [Header("Corridor Settings")]
        public int corridorWidth = 2;
        public int branchCount = 4;

        [Header("Tilemaps")]
        public Tilemap floorTilemap;
        public Tilemap wallTilemap;
        [SerializeField] private Tile floorTile;
        [SerializeField] private Tile wallTile;

        private void Awake()
        {
            Instance = this;
            FindTilemapsIfMissing();
        }

        public DungeonData GenerateMap(int floor, bool isBossFloor)
        {
            if (useRandomSeed)
            {
                seed = Random.Range(0, 100000);
            }
            
            DungeonData finalData = null;
            int attempts = 0;
            const int maxAttempts = 20;

            while (attempts < maxAttempts)
            {
                attempts++;
                Random.InitState(seed + floor + attempts);
                finalData = TryGenerateMap(isBossFloor);
                
                if (finalData != null)
                {
                    break;
                }
            }

            if (finalData == null)
            {
                Debug.LogWarning($"[DungeonMapGenerator] Failed to generate a valid dungeon after {maxAttempts} attempts. Using fallback.");
                ClearTilemaps();
                finalData = new DungeonData();
                finalData.PlayerSpawn = Vector3.zero;
                finalData.ExitPortal = new Vector3(5, 5, 0);
            }

            if (PathfindingGrid.Instance != null)
            {
                PathfindingGrid.Instance.RebuildGrid();
            }

            // Ensure physics geometry is updated after setting tiles
            if (wallTilemap != null)
            {
                if (wallTilemap.TryGetComponent(out CompositeCollider2D composite))
                {
                    composite.GenerateGeometry();
                }
            }

            return finalData;
}

        private DungeonData TryGenerateMap(bool isBossFloor)
        {
            ClearTilemaps();

            int targetRoomCount = Random.Range(roomCountMin, roomCountMax + 1);
            List<RectInt> rooms = new List<RectInt>();

            int placementRetries = 0;
            const int maxPlacementRetries = 100;

            while (rooms.Count < targetRoomCount && placementRetries < maxPlacementRetries)
            {
                placementRetries++;
                int w = Random.Range(roomSizeMin, roomSizeMax + 1);
                int h = Random.Range(roomSizeMin, roomSizeMax + 1);
                
                if (isBossFloor && rooms.Count == targetRoomCount - 1)
                {
                    w = bossRoomSize;
                    h = bossRoomSize;
                }

                int x = Random.Range(2, mapWidth - w - 2);
                int y = Random.Range(2, mapHeight - h - 2);
                
                RectInt newRoom = new RectInt(x, y, w, h);

                bool overlap = false;
                foreach (var r in rooms)
                {
                    if (newRoom.Overlaps(new RectInt(r.x - 1, r.y - 1, r.width + 2, r.height + 2)))
                    {
                        overlap = true;
                        break;
                    }
                }

                if (!overlap)
                {
                    rooms.Add(newRoom);
                }
            }

            if (rooms.Count < 5)
            {
                return null;
            }

            // Draw Rooms
            HashSet<Vector3Int> floorPositions = new HashSet<Vector3Int>();
            foreach (var room in rooms)
            {
                for (int x = room.x; x < room.xMax; x++)
                {
                    for (int y = room.y; y < room.yMax; y++)
                    {
                        Vector3Int pos = new Vector3Int(x, y, 0);
                        floorTilemap.SetTile(pos, floorTile);
                        floorPositions.Add(pos);
                    }
                }
            }

            // Connect Rooms with Corridors
            for (int i = 0; i < rooms.Count - 1; i++)
            {
                ConnectRooms(rooms[i], rooms[i + 1], floorPositions);
            }

            // Add extra branches
            for (int i = 0; i < branchCount; i++)
            {
                int r1 = Random.Range(0, rooms.Count);
                int r2 = Random.Range(0, rooms.Count);
                if (r1 != r2)
                {
                    ConnectRooms(rooms[r1], rooms[r2], floorPositions);
                }
            }

            // Walls around all floor tiles
            HashSet<Vector3Int> wallPositions = new HashSet<Vector3Int>();
            foreach (var fPos in floorPositions)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        Vector3Int wPos = fPos + new Vector3Int(dx, dy, 0);
                        if (!floorPositions.Contains(wPos))
                        {
                            wallPositions.Add(wPos);
                        }
                    }
                }
            }

            foreach (var wPos in wallPositions)
            {
                wallTilemap.SetTile(wPos, wallTile);
            }

            Vector2Int startCenter = new Vector2Int(rooms[0].x + rooms[0].width / 2, rooms[0].y + rooms[0].height / 2);
            RectInt furthestRoom = rooms[0];
            float maxDist = 0;

            foreach (var room in rooms)
            {
                Vector2Int center = new Vector2Int(room.x + room.width / 2, room.y + room.height / 2);
                float dist = Vector2.Distance(startCenter, center);
                if (dist > maxDist)
                {
                    maxDist = dist;
                    furthestRoom = room;
                }
            }

            DungeonData data = new DungeonData();
            data.PlayerSpawn = floorTilemap.GetCellCenterWorld(new Vector3Int(startCenter.x, startCenter.y, 0));
            data.ExitPortal = floorTilemap.GetCellCenterWorld(new Vector3Int(furthestRoom.x + furthestRoom.width / 2, furthestRoom.y + furthestRoom.height / 2, 0));
            
            if (isBossFloor)
            {
                data.BossSpawn = data.ExitPortal;
            }

            for (int i = 1; i < rooms.Count; i++)
            {
                RectInt room = rooms[i];
                int eCount = Random.Range(1, 4);
                for (int e = 0; e < eCount; e++)
                {
                    data.EnemySpawns.Add(floorTilemap.GetCellCenterWorld(new Vector3Int(Random.Range(room.x, room.xMax), Random.Range(room.y, room.yMax), 0)));
                }

                if (Random.value < 0.4f)
                {
                    data.ChestSpawns.Add(floorTilemap.GetCellCenterWorld(new Vector3Int(Random.Range(room.x, room.xMax), Random.Range(room.y, room.yMax), 0)));
                }
            }

            return data;
        }

        private void ConnectRooms(RectInt r1, RectInt r2, HashSet<Vector3Int> floorPositions)
        {
            Vector2Int start = new Vector2Int(r1.x + r1.width / 2, r1.y + r1.height / 2);
            Vector2Int end = new Vector2Int(r2.x + r2.width / 2, r2.y + r2.height / 2);

            int x = start.x;
            while (x != end.x)
            {
                DrawCorridorPoint(new Vector3Int(x, start.y, 0), floorPositions);
                x += (end.x > start.x) ? 1 : -1;
            }
            int y = start.y;
            while (y != end.y)
            {
                DrawCorridorPoint(new Vector3Int(end.x, y, 0), floorPositions);
                y += (end.y > start.y) ? 1 : -1;
            }
        }

        private void DrawCorridorPoint(Vector3Int pos, HashSet<Vector3Int> floorPositions)
        {
            for (int dx = 0; dx < corridorWidth; dx++)
            {
                for (int dy = 0; dy < corridorWidth; dy++)
                {
                    Vector3Int p = pos + new Vector3Int(dx, dy, 0);
                    floorTilemap.SetTile(p, floorTile);
                    floorPositions.Add(p);
                }
            }
        }

        public void ClearTilemaps()
        {
            FindTilemapsIfMissing();
            if (floorTilemap != null) floorTilemap.ClearAllTiles();
            if (wallTilemap != null) wallTilemap.ClearAllTiles();
        }

        private void FindTilemapsIfMissing()
        {
            if (floorTilemap == null)
            {
                GameObject obj = GameObject.Find("FloorTilemap");
                if (obj != null) floorTilemap = obj.GetComponent<Tilemap>();
            }
            if (wallTilemap == null)
            {
                GameObject obj = GameObject.Find("WallTilemap");
                if (obj != null) wallTilemap = obj.GetComponent<Tilemap>();
            }
            
            if (floorTile == null && floorTilemap != null)
            {
                foreach (var pos in floorTilemap.cellBounds.allPositionsWithin)
                {
                    Tile t = floorTilemap.GetTile<Tile>(pos);
                    if (t != null) { floorTile = t; break; }
                }
            }
            if (wallTile == null && wallTilemap != null)
            {
                foreach (var pos in wallTilemap.cellBounds.allPositionsWithin)
                {
                    Tile t = wallTilemap.GetTile<Tile>(pos);
                    if (t != null) { wallTile = t; break; }
                }
            }
        }
        
        public void Configure(Tile floorT, Tile wallT)
        {
            floorTile = floorT;
            wallTile = wallT;
        }
        }
        }