namespace WiredPlayers_Client.model
{
    class ClothesType
    {
        public int type { get; set; }
        public int slot { get; set; }
        public string desc { get; set; }

        public ClothesType() { }

        public ClothesType(int type, int slot, string desc)
        {
            this.type = type;
            this.slot = slot;
            this.desc = desc;
        }
    }
}
