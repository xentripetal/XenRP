namespace WiredPlayers_Client.model
{
    class Procedure
    {
        string desc { get; set; }
        int price { get; set; }

        public Procedure(string desc, int price)
        {
            this.desc = desc;
            this.price = price;
        }
    }
}
