﻿namespace XenRP.model {
    public class AccountModel {
        public AccountModel() {
        }

        public AccountModel(string socialName, string forumName, int status, int lastCharacter) {
            this.socialName = socialName;
            this.forumName = forumName;
            this.status = status;
            this.lastCharacter = lastCharacter;
        }

        public string socialName { get; set; }
        public string forumName { get; set; }
        public int status { get; set; }
        public int lastCharacter { get; set; }
        public bool registered { get; set; }
    }
}