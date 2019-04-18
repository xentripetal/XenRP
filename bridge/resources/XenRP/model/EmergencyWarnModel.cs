namespace WiredPlayers.model {
    public class EmergencyWarnModel {
        public EmergencyWarnModel(string patient, string paramedic, string time) {
            this.patient = patient;
            this.paramedic = paramedic;
            this.time = time;
        }

        public string patient { get; set; }
        public string paramedic { get; set; }
        public string time { get; set; }
    }
}