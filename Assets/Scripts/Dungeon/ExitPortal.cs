using UnityEngine;

namespace DungeonCrawler.Dungeon
{
    public class ExitPortal : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player") && other.gameObject.name != "Player")
            {
                return;
            }

            FloorManager floorManager = FloorManager.Instance;
            if (floorManager != null)
            {
                floorManager.AdvanceFloor();
            }
        }
    }
}
