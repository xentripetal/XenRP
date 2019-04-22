using GTANetworkAPI;

namespace XenRP.model {
    public class GarbageModel {
        public GarbageModel(int route, int checkPoint, Vector3 position) {
            this.route = route;
            this.checkPoint = checkPoint;
            this.position = position;
        }

        public int route { get; set; }
        public int checkPoint { get; set; }
        public Vector3 position { get; set; }
    }
}