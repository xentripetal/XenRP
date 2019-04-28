using System;
using System.Collections.Generic;
using GTANetworkAPI;
using MySql.Data.MySqlClient;
using XenRP.database;
using XenRP.model;

namespace XenRP.vehicles {
    public static class DBMechanicCommands {
        public static List<TunningModel> LoadAllTunning() {
            var tunningList = new List<TunningModel>();

            using (var connection = Database.GetConnection()) {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM tunning";

                using (var reader = command.ExecuteReader()) {
                    while (reader.Read()) {
                        var tunning = new TunningModel();
                        {
                            tunning.id = reader.GetInt32("id");
                            tunning.vehicle = reader.GetInt32("vehicle");
                            tunning.slot = reader.GetInt32("slot");
                            tunning.component = reader.GetInt32("component");
                        }

                        tunningList.Add(tunning);
                    }
                }
            }

            return tunningList;
        }

        public static int AddTunning(TunningModel tunning) {
            var tunningId = 0;

            using (var connection = Database.GetConnection()) {
                try {
                    connection.Open();
                    var command = connection.CreateCommand();

                    command.CommandText =
                        "INSERT INTO tunning (vehicle, slot, component) VALUES (@vehicle, @slot, @component)";
                    command.Parameters.AddWithValue("@vehicle", tunning.vehicle);
                    command.Parameters.AddWithValue("@slot", tunning.slot);
                    command.Parameters.AddWithValue("@component", tunning.component);

                    command.ExecuteNonQuery();
                    tunningId = (int) command.LastInsertedId;
                }
                catch (Exception ex) {
                    NAPI.Util.ConsoleOutput("[EXCEPTION AddTunning] " + ex.Message);
                    NAPI.Util.ConsoleOutput("[EXCEPTION AddTunning] " + ex.StackTrace);
                }
            }

            return tunningId;
        }

        public static void RemoveTunning(int tunningId) {
            using (var connection = Database.GetConnection()) {
                try {
                    connection.Open();
                    var command = connection.CreateCommand();

                    command.CommandText = "DELETE FROM tunning WHERE id = @id LIMIT 1";
                    command.Parameters.AddWithValue("@id", tunningId);

                    command.ExecuteNonQuery();
                }
                catch (Exception ex) {
                    NAPI.Util.ConsoleOutput("[EXCEPTION RemoveTunning] " + ex.Message);
                    NAPI.Util.ConsoleOutput("[EXCEPTION RemoveTunning] " + ex.StackTrace);
                }
            }
        }
    }
}