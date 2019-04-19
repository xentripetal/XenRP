namespace XenRP.Client.model {
    internal class Procedure {
        public Procedure(string desc, int price) {
            this.desc = desc;
            this.price = price;
        }

        public string desc { get; set; }
        public int price { get; set; }
    }
}