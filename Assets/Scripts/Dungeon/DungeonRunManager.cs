namespace DungeonCrawler.Dungeon
{
    public enum GameRunMode
    {
        Village,
        Dungeon
    }

    public class DungeonRunManager : UnityEngine.MonoBehaviour
    {
        public GameRunMode Mode => FloorManager.Instance != null && FloorManager.Instance.IsVillageMode
            ? GameRunMode.Village
            : GameRunMode.Dungeon;
    }
}
