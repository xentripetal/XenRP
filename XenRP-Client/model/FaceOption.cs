namespace XenRP.Client.model {
    internal class FaceOption {
        public FaceOption() { }

        public FaceOption(string desc, int minValue, int maxValue) {
            this.desc = desc;
            this.minValue = minValue;
            this.maxValue = maxValue;
        }

        public string desc { get; set; }
        public int minValue { get; set; }
        public int maxValue { get; set; }
    }
}