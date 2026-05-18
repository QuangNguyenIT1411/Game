using UnityEngine;
using UnityEngine.Tilemaps;

namespace DungeonCrawler.UI
{
    public class PhysicsSanityChecker : MonoBehaviour
    {
        private void Start()
        {
            LogSanity();
        }

        public void LogSanity()
        {
            Debug.Log("--- Physics Sanity Check Start ---");
            
            GameObject wallObj = GameObject.Find("WallTilemap");
            int wallLayer = -1;
            if (wallObj != null)
            {
                wallLayer = wallObj.layer;
                Tilemap wallTilemap = wallObj.GetComponent<Tilemap>();
                TilemapCollider2D tileCol = wallObj.GetComponent<TilemapCollider2D>();
                CompositeCollider2D compCol = wallObj.GetComponent<CompositeCollider2D>();
                Rigidbody2D wallRb = wallObj.GetComponent<Rigidbody2D>();

                Debug.Log($"WallTilemap: {wallObj.name}, Active: {wallObj.activeSelf}, Layer: {wallLayer} ({LayerMask.LayerToName(wallLayer)})");
                Debug.Log($"Wall Tiles: {wallTilemap?.GetUsedTilesCount()}");
                Debug.Log($"TilemapCollider2D: exists={tileCol != null}, enabled={tileCol?.enabled}, isTrigger={tileCol?.isTrigger}, usedByComposite={tileCol?.usedByComposite}");
                Debug.Log($"CompositeCollider2D: exists={compCol != null}, enabled={compCol?.enabled}, isTrigger={compCol?.isTrigger}, geometryType={compCol?.geometryType}, paths={compCol?.pathCount}");
                Debug.Log($"Rigidbody2D (Wall): exists={wallRb != null}, bodyType={wallRb?.bodyType}");
            }

            GameObject playerObj = GameObject.Find("Player");
            if (playerObj != null)
            {
                int playerLayer = playerObj.layer;
                Collider2D playerCol = playerObj.GetComponent<Collider2D>();
                Rigidbody2D playerRb = playerObj.GetComponent<Rigidbody2D>();

                Debug.Log($"Player: {playerObj.name}, Layer: {playerLayer} ({LayerMask.LayerToName(playerLayer)})");
                Debug.Log($"Player Collider: exists={playerCol != null}, enabled={playerCol?.enabled}, isTrigger={playerCol?.isTrigger}");
                Debug.Log($"Player Rigidbody2D: exists={playerRb != null}, bodyType={playerRb?.bodyType}, collisionDetection={playerRb?.collisionDetectionMode}");
                
                if (wallLayer != -1)
                {
                    bool ignore = Physics2D.GetIgnoreLayerCollision(playerLayer, wallLayer);
                    Debug.Log($"Physics2D Layer Collision (Player vs Wall): Ignore={ignore}, Mask={System.Convert.ToString(Physics2D.GetLayerCollisionMask(playerLayer), 2)}");
                    
                    if (ignore)
                    {
                        Debug.LogWarning("Player and Wall layers are set to IGNORE collision. Fixing now...");
                        Physics2D.IgnoreLayerCollision(playerLayer, wallLayer, false);
                    }

                    if (playerCol != null && wallObj.GetComponent<CompositeCollider2D>() != null)
                    {
                        ContactFilter2D filter = new ContactFilter2D { layerMask = 1 << wallLayer, useLayerMask = true, useTriggers = true };
                        int count = playerCol.Overlap(filter, new Collider2D[1]);
                        Debug.Log($"Collision sanity check: Player overlaps wall = {count > 0}");
                        if (count > 0) Debug.LogError("ERROR: Player is inside wall collider at start!");
                    }
                }
            }

            Debug.Log("--- Physics Sanity Check End ---");
        }
    }
}
