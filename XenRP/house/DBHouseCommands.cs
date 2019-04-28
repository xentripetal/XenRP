using System;
using System.Collections.Generic;
using GTANetworkAPI;
using MySql.Data.MySqlClient;
using XenRP.database;
using XenRP.model;

namespace XenRP.house {
    public static class DBHouseCommands{
        public static List<HouseModel> LoadAllHouses() {
            var houseList = new List<HouseModel>();

            using (var connection = Database.GetConnection()) {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM houses";

                using (var reader = command.ExecuteReader()) {
                    while (reader.Read()) {
                        var house = new HouseModel();
                        var posX = reader.GetFloat("posX");
                        var posY = reader.GetFloat("posY");
                        var posZ = reader.GetFloat("posZ");

                        house.id = reader.GetInt32("id");
                        house.ipl = reader.GetString("ipl");
                        house.name = reader.GetString("name");
                        house.position = new Vector3(posX, posY, posZ);
                        house.dimension = reader.GetUInt32("dimension");
                        house.price = reader.GetInt32("price");
                        house.owner = reader.GetString("owner");
                        house.status = reader.GetInt32("status");
                        house.tenants = reader.GetInt32("tenants");
                        house.rental = reader.GetInt32("rental");
                        house.locked = reader.GetBoolean("locked");

                        houseList.Add(house);
                    }
                }
            }

            return houseList;
        }

        public static int AddHouse(HouseModel house) {
            var houseId = 0;

            using (var connection = Database.GetConnection()) {
                try {
                    connection.Open();
                    var command = connection.CreateCommand();

                    command.CommandText =
                        "INSERT INTO houses (ipl, posX, posY, posZ, dimension) VALUES (@ipl, @posX, @posY, @posZ, @dimension)";
                    command.Parameters.AddWithValue("@ipl", house.ipl);
                    command.Parameters.AddWithValue("@posX", house.position.X);
                    command.Parameters.AddWithValue("@posY", house.position.Y);
                    command.Parameters.AddWithValue("@posZ", house.position.Z);
                    command.Parameters.AddWithValue("@dimension", house.dimension);

                    command.ExecuteNonQuery();
                    houseId = (int) command.LastInsertedId;
                }
                catch (Exception ex) {
                    NAPI.Util.ConsoleOutput("[EXCEPTION AddHouse] " + ex.Message);
                    NAPI.Util.ConsoleOutput("[EXCEPTION AddHouse] " + ex.StackTrace);
                }
            }

            return houseId;
        }

        public static void UpdateHouse(HouseModel house) {
            using (var connection = Database.GetConnection()) {
                try {
                    connection.Open();
                    var command = connection.CreateCommand();

                    command.CommandText =
                        "UPDATE houses SET ipl = @ipl, posX = @posX, posY = @posY, posZ = @posZ, dimension = @dimension, name = @name, price = @price, ";
                    command.CommandText +=
                        "owner = @owner, status = @status, tenants = @tenants, rental = @rental, locked = @locked WHERE id = @id LIMIT 1";
                    command.Parameters.AddWithValue("@ipl", house.ipl);
                    command.Parameters.AddWithValue("@posX", house.position.X);
                    command.Parameters.AddWithValue("@posY", house.position.Y);
                    command.Parameters.AddWithValue("@posZ", house.position.Z);
                    command.Parameters.AddWithValue("@dimension", house.dimension);
                    command.Parameters.AddWithValue("@name", house.name);
                    command.Parameters.AddWithValue("@price", house.price);
                    command.Parameters.AddWithValue("@owner", house.owner);
                    command.Parameters.AddWithValue("@status", house.status);
                    command.Parameters.AddWithValue("@tenants", house.tenants);
                    command.Parameters.AddWithValue("@rental", house.rental);
                    command.Parameters.AddWithValue("@locked", house.locked);
                    command.Parameters.AddWithValue("@id", house.id);

                    command.ExecuteNonQuery();
                }
                catch (Exception ex) {
                    NAPI.Util.ConsoleOutput("[EXCEPTION UpdateHouse] " + ex.Message);
                    NAPI.Util.ConsoleOutput("[EXCEPTION UpdateHouse] " + ex.StackTrace);
                }
            }
        }

        public static void DeleteHouse(int houseId) {
            using (var connection = Database.GetConnection()) {
                try {
                    connection.Open();
                    var command = connection.CreateCommand();

                    command.CommandText = "DELETE FROM houses WHERE id = @id LIMIT 1";
                    command.Parameters.AddWithValue("@id", houseId);

                    command.ExecuteNonQuery();
                }
                catch (Exception ex) {
                    NAPI.Util.ConsoleOutput("[EXCEPTION DeleteHouse] " + ex.Message);
                    NAPI.Util.ConsoleOutput("[EXCEPTION DeleteHouse] " + ex.StackTrace);
                }
            }
        }
    }
}