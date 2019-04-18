using GTANetworkAPI;

namespace WiredPlayers.model {
    public class DeathModel {
        public DeathModel(Client player, Client killer, uint weapon) {
            this.player = player;
            this.killer = killer;
            this.weapon = weapon;
        }

        public Client player { get; set; }
        public Client killer { get; set; }
        public uint weapon { get; set; }
    }
}