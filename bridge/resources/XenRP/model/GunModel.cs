using GTANetworkAPI;

namespace WiredPlayers.model {
    public class GunModel {
        public GunModel(WeaponHash weapon, string ammunition, int capacity) {
            this.weapon = weapon;
            this.ammunition = ammunition;
            this.capacity = capacity;
        }

        public WeaponHash weapon { get; set; }
        public string ammunition { get; set; }
        public int capacity { get; set; }
    }
}