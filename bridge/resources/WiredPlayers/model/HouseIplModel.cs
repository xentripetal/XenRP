using GTANetworkAPI;

namespace WiredPlayers.model {
    public class HouseIplModel {
        public HouseIplModel(string ipl, Vector3 position) {
            this.ipl = ipl;
            this.position = position;
        }

        public string ipl { get; set; }
        public Vector3 position { get; set; }
    }
}