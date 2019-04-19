namespace XenRP.model {
    public class FactionModel {
        public FactionModel(string descriptionMale, string descriptionFemale, int faction, int rank, int salary) {
            this.descriptionMale = descriptionMale;
            this.descriptionFemale = descriptionFemale;
            this.faction = faction;
            this.rank = rank;
            this.salary = salary;
        }

        public string descriptionMale { get; set; }
        public string descriptionFemale { get; set; }
        public int faction { get; set; }
        public int rank { get; set; }
        public int salary { get; set; }
    }
}