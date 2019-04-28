using System;
using System.Collections.Generic;
using GTANetworkAPI;
using GTANetworkMethods;
using MySql.Data.MySqlClient;
using XenRP.database;
using XenRP.messages.general;
using XenRP.model;

namespace XenRP.bank {
    public static class DBBankCommands {
        
        public static void TransferMoneyToPlayer(string name, int amount) {
            using (var connection = Database.GetConnection()) {
                try {
                    connection.Open();
                    var command = connection.CreateCommand();

                    command.CommandText = "UPDATE users SET bank = bank + @amount WHERE name = @name LIMIT 1";
                    command.Parameters.AddWithValue("@name", name);
                    command.Parameters.AddWithValue("@amount", amount);

                    command.ExecuteNonQuery();
                }
                catch (Exception ex) {
                    NAPI.Util.ConsoleOutput("[EXCEPTION TransferMoneyToPlayer] " + ex.Message);
                    NAPI.Util.ConsoleOutput("[EXCEPTION TransferMoneyToPlayer] " + ex.StackTrace);
                }
            }
        }
        
        public static void LogPayment(string source, string receiver, string type, int amount) {
            using (var connection = Database.GetConnection()) {
                try {
                    connection.Open();
                    var command = connection.CreateCommand();

                    command.CommandText =
                        "INSERT INTO money VALUES (@source, @receiver, @type, @amount, CURDATE(), CURTIME())";
                    command.Parameters.AddWithValue("@source", source);
                    command.Parameters.AddWithValue("@receiver", receiver);
                    command.Parameters.AddWithValue("@type", type);
                    command.Parameters.AddWithValue("@amount", amount);

                    command.ExecuteNonQuery();
                }
                catch (Exception ex) {
                    NAPI.Util.ConsoleOutput("[EXCEPTION LogPayment] " + ex.Message);
                    NAPI.Util.ConsoleOutput("[EXCEPTION LogPayment] " + ex.StackTrace);
                }
            }
        }
        
        public static List<BankOperationModel> GetBankOperations(string playerName, int start, int count) {
            var operations = new List<BankOperationModel>();

            using (var connection = Database.GetConnection()) {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText =
                    "SELECT * FROM money WHERE (source = @playerName OR receiver = @playerName) AND (type = @opTransfer ";
                command.CommandText +=
                    "OR type = @opDeposit OR type = @opWithdraw) ORDER BY date DESC, hour DESC LIMIT @start, @count";
                command.Parameters.AddWithValue("@playerName", playerName);
                command.Parameters.AddWithValue("@opTransfer", GenRes.bank_op_transfer);
                command.Parameters.AddWithValue("@opDeposit", GenRes.bank_op_deposit);
                command.Parameters.AddWithValue("@opWithdraw", GenRes.bank_op_withdraw);
                command.Parameters.AddWithValue("@start", start);
                command.Parameters.AddWithValue("@count", count);

                using (var reader = command.ExecuteReader()) {
                    while (reader.Read()) {
                        var bankOperation = new BankOperationModel();
                        {
                            bankOperation.source = reader.GetString("source");
                            bankOperation.receiver = reader.GetString("receiver");
                            bankOperation.type = reader.GetString("type");
                            bankOperation.amount = reader.GetInt32("amount");
                            bankOperation.day = reader.GetString("date").Split(' ')[0];
                            bankOperation.time = reader.GetString("hour");
                        }

                        operations.Add(bankOperation);
                    }
                }
            }

            return operations;
        }
        
    }
}