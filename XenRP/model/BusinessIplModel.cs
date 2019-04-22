﻿using GTANetworkAPI;

namespace XenRP.model {
    public class BusinessIplModel {
        public BusinessIplModel(int type, string ipl, Vector3 position) {
            this.type = type;
            this.ipl = ipl;
            this.position = position;
        }

        public int type { get; set; }
        public string ipl { get; set; }
        public Vector3 position { get; set; }
    }
}