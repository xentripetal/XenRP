using System;
using System.Collections.Generic;
using GTANetworkAPI;
using GTANetworkMethods;
using MySql.Data.MySqlClient;
using XenRP.database;
using XenRP.model;

namespace XenRP.business {
    public static class DBBusinessCommands {
        public static List<BusinessModel> LoadAllBusiness() {
            var businessList = new List<BusinessModel>();

            using (var connection = Database.GetConnection()) {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM business";

                using (var reader = command.ExecuteReader()) {
                    while (reader.Read()) {
                        var business = new BusinessModel();
                        var posX = reader.GetFloat("posX");
                        var posY = reader.GetFloat("posY");
                        var posZ = reader.GetFloat("posZ");

                        business.id = reader.GetInt32("id");
                        business.type = reader.GetInt32("type");
                        business.ipl = reader.GetString("ipl");
                        business.name = reader.GetString("name");
                        business.position = new Vector3(posX, posY, posZ);
                        business.dimension = reader.GetUInt32("dimension");
                        business.owner = reader.GetString("owner");
                        business.multiplier = reader.GetFloat("multiplier");
                        business.locked = reader.GetBoolean("locked");

                        businessList.Add(business);
                    }
                }
            }

            return businessList;
        }

        public static void UpdateBusiness(BusinessModel business) {
            using (var connection = Database.GetConnection()) {
                try {
                    connection.Open();
                    var command = connection.CreateCommand();

                    command.CommandText =
                        "UPDATE business SET type = @type, ipl = @ipl, posX = @posX, posY = @posY, posZ = @posZ, dimension = @dimension, name = @name, ";
                    command.CommandText +=
                        "owner = @owner, funds = @funds, products = @products, multiplier = @multiplier, locked = @locked WHERE id = @id LIMIT 1";
                    command.Parameters.AddWithValue("@type", business.type);
                    command.Parameters.AddWithValue("@ipl", business.ipl);
                    command.Parameters.AddWithValue("@posX", business.position.X);
                    command.Parameters.AddWithValue("@posY", business.position.Y);
                    command.Parameters.AddWithValue("@posZ", business.position.Z);
                    command.Parameters.AddWithValue("@dimension", business.dimension);
                    command.Parameters.AddWithValue("@name", business.name);
                    command.Parameters.AddWithValue("@owner", business.owner);
                    command.Parameters.AddWithValue("@funds", business.funds);
                    command.Parameters.AddWithValue("@products", business.products);
                    command.Parameters.AddWithValue("@multiplier", business.multiplier);
                    command.Parameters.AddWithValue("@locked", business.locked);
                    command.Parameters.AddWithValue("@id", business.id);

                    command.ExecuteNonQuery();
                }
                catch (Exception ex) {
                    NAPI.Util.ConsoleOutput("[EXCEPTION UpdateBusiness] " + ex.Message);
                    NAPI.Util.ConsoleOutput("[EXCEPTION UpdateBusiness] " + ex.StackTrace);
                }
            }
        }

        public static void UpdateAllBusiness(List<BusinessModel> businessList) {
            using (var connection = Database.GetConnection()) {
                try {
                    connection.Open();
                    var command = connection.CreateCommand();

                    command.CommandText =
                        "UPDATE business SET type = @type, ipl = @ipl, posX = @posX, posY = @posY, posZ = @posZ, dimension = @dimension, name = @name, ";
                    command.CommandText +=
                        "owner = @owner, funds = @funds, products = @products, multiplier = @multiplier, locked = @locked WHERE id = @id LIMIT 1";

                    foreach (var business in businessList) {
                        command.Parameters.Clear();

                        command.Parameters.AddWithValue("@type", business.type);
                        command.Parameters.AddWithValue("@ipl", business.ipl);
                        command.Parameters.AddWithValue("@posX", business.position.X);
                        command.Parameters.AddWithValue("@posY", business.position.Y);
                        command.Parameters.AddWithValue("@posZ", business.position.Z);
                        command.Parameters.AddWithValue("@dimension", business.dimension);
                        command.Parameters.AddWithValue("@name", business.name);
                        command.Parameters.AddWithValue("@owner", business.owner);
                        command.Parameters.AddWithValue("@funds", business.funds);
                        command.Parameters.AddWithValue("@products", business.products);
                        command.Parameters.AddWithValue("@multiplier", business.multiplier);
                        command.Parameters.AddWithValue("@locked", business.locked);
                        command.Parameters.AddWithValue("@id", business.id);

                        command.ExecuteNonQuery();
                    }
                }
                catch (Exception ex) {
                    NAPI.Util.ConsoleOutput("[EXCEPTION UpdateAllBusiness] " + ex.Message);
                    NAPI.Util.ConsoleOutput("[EXCEPTION UpdateAllBusiness] " + ex.StackTrace);
                }
            }
        }

        public static int AddNewBusiness(BusinessModel business) {
            var businessId = 0;

            using (var connection = Database.GetConnection()) {
                try {
                    connection.Open();
                    var command = connection.CreateCommand();

                    command.CommandText =
                        "INSERT INTO business (type, ipl, posX, posY, posZ, dimension) VALUES (@type, @ipl, @posX, @posY, @posZ, @dimension)";
                    command.Parameters.AddWithValue("@type", business.type);
                    command.Parameters.AddWithValue("@ipl", business.ipl);
                    command.Parameters.AddWithValue("@posX", business.position.X);
                    command.Parameters.AddWithValue("@posY", business.position.Y);
                    command.Parameters.AddWithValue("@posZ", business.position.Z);
                    command.Parameters.AddWithValue("@dimension", business.dimension);

                    command.ExecuteNonQuery();
                    businessId = (int) command.LastInsertedId;
                }
                catch (Exception ex) {
                    NAPI.Util.ConsoleOutput("[EXCEPTION AddNewBusiness] " + ex.Message);
                    NAPI.Util.ConsoleOutput("[EXCEPTION AddNewBusiness] " + ex.StackTrace);
                }
            }

            return businessId;
        }

        public static void DeleteBusiness(int businessId) {
            using (var connection = Database.GetConnection()) {
                try {
                    connection.Open();
                    var command = connection.CreateCommand();

                    command.CommandText = "DELETE FROM business WHERE id = @id LIMIT 1";
                    command.Parameters.AddWithValue("@id", businessId);

                    command.ExecuteNonQuery();
                }
                catch (Exception ex) {
                    NAPI.Util.ConsoleOutput("[EXCEPTION AddNewBusiness] " + ex.Message);
                    NAPI.Util.ConsoleOutput("[EXCEPTION AddNewBusiness] " + ex.StackTrace);
                }
            }
        }
    }
}