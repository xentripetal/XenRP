using GTANetworkAPI;

namespace WiredPlayers.model {
    public class CrateSpawnModel {
        public CrateSpawnModel(int spawnPoint, Vector3 position) {
            this.spawnPoint = spawnPoint;
            this.position = position;
        }

        public int spawnPoint { get; set; }
        public Vector3 position { get; set; }
    }
}