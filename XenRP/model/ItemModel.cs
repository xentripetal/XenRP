﻿using GTANetworkAPI;

namespace XenRP.model {
    public class ItemModel {
        public int id { get; set; }
        public string hash { get; set; }
        public string ownerEntity { get; set; }
        public int ownerIdentifier { get; set; }
        public int amount { get; set; }
        public Vector3 position { get; set; }
        public uint dimension { get; set; }
        public Object objectHandle { get; set; }

        public ItemModel Copy() {
            var itemModel = new ItemModel();

            itemModel.id = id;
            itemModel.hash = hash;
            itemModel.ownerEntity = ownerEntity;
            itemModel.ownerIdentifier = ownerIdentifier;
            itemModel.amount = amount;
            itemModel.position = position;
            itemModel.dimension = dimension;
            itemModel.objectHandle = objectHandle;

            return itemModel;
        }
    }
}