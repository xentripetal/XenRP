using System.Collections.Generic;
using GTANetworkAPI;
using XenRP.database;
using XenRP.model;

namespace XenRP.house {
    public static class DBFurnitureCommands {
        public static List<FurnitureModel> LoadAllFurniture() {
            var furnitureList = new List<FurnitureModel>();

            using (var connection = Database.GetConnection()) {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM furniture";

                using (var reader = command.ExecuteReader()) {
                    while (reader.Read()) {
                        var furniture = new FurnitureModel();
                        var posX = reader.GetFloat("posX");
                        var posY = reader.GetFloat("posY");
                        var posZ = reader.GetFloat("posZ");
                        var rot = reader.GetFloat("rotation");

                        furniture.id = reader.GetInt32("id");
                        furniture.hash = reader.GetUInt32("hash");
                        furniture.house = reader.GetUInt32("house");
                        furniture.position = new Vector3(posX, posY, posZ);
                        furniture.rotation = new Vector3(0.0f, 0.0f, rot);

                        furnitureList.Add(furniture);
                    }
                }
            }

            return furnitureList;
        }
    }
}