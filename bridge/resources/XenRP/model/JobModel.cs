﻿namespace WiredPlayers.model {
    public class JobModel {
        public JobModel(string descriptionMale, string descriptionFemale, int job, int salary) {
            this.descriptionMale = descriptionMale;
            this.descriptionFemale = descriptionFemale;
            this.job = job;
            this.salary = salary;
        }

        public string descriptionMale { get; set; }
        public string descriptionFemale { get; set; }
        public int job { get; set; }
        public int salary { get; set; }
    }
}