using GTANetworkAPI;

namespace XenRP.model {
    public class ReinforcesModel {
        public ReinforcesModel(int playerId, Vector3 position) {
            this.playerId = playerId;
            this.position = position;
        }

        public int playerId { get; set; }
        public Vector3 position { get; set; }
    }
}