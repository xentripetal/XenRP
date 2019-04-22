using System.Collections.Generic;

namespace XenRP.Client.model {
    internal class CarPiece {
        public CarPiece(int id, string desc) {
            this.id = id;
            this.desc = desc;
        }

        public CarPiece(int slot, string desc, int products) {
            this.slot = slot;
            this.desc = desc;
            this.products = products;
        }

        public int id { get; set; }
        public int slot { get; set; }
        public string desc { get; set; }
        public int products { get; set; }
        public List<CarPiece> components { get; set; }
    }
}