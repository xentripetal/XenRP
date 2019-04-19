namespace XenRP.model {
    public class TunningPriceModel {
        public TunningPriceModel(int slot, int products) {
            this.slot = slot;
            this.products = products;
        }

        public int slot { get; set; }
        public int products { get; set; }
    }
}