﻿using GTANetworkAPI;

namespace XenRP.model {
    public class JobPickModel {
        public JobPickModel(int job, uint blip, string name, Vector3 position, string description) {
            this.job = job;
            this.blip = blip;
            this.name = name;
            this.position = position;
            this.description = description;
        }

        public int job { get; set; }
        public uint blip { get; set; }
        public string name { get; set; }
        public Vector3 position { get; set; }
        public string description { get; set; }
    }
}