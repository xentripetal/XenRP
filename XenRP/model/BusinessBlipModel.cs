namespace XenRP.model {
    public class BusinessBlipModel {
        public BusinessBlipModel(int id, int blip) {
            this.id = id;
            this.blip = blip;
        }

        public int id { get; set; }
        public int blip { get; set; }
    }
}