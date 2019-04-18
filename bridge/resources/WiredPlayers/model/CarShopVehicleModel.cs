﻿using GTANetworkAPI;

namespace WiredPlayers.model {
    public class CarShopVehicleModel {
        public CarShopVehicleModel(VehicleHash hash, int carShop, int type, int price) {
            this.hash = hash;
            this.carShop = carShop;
            this.type = type;
            this.price = price;
        }

        public VehicleHash hash { get; set; }
        public int carShop { get; set; }
        public int type { get; set; }
        public int speed { get; set; }
        public int price { get; set; }
        public string model { get; set; }
    }
}