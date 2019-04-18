﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GTANetworkAPI;
using WiredPlayers.database;
using WiredPlayers.globals;
using WiredPlayers.messages.error;
using WiredPlayers.messages.general;
using WiredPlayers.messages.information;
using WiredPlayers.messages.success;
using WiredPlayers.model;

namespace WiredPlayers.drivingschool {
    public class DrivingSchool : Script {
        private static Dictionary<int, Timer> drivingSchoolTimerList;

        public DrivingSchool() {
            // Initialize the variables
            drivingSchoolTimerList = new Dictionary<int, Timer>();
        }

        public static void OnPlayerDisconnected(Client player, DisconnectionType type, string reason) {
            if (drivingSchoolTimerList.TryGetValue(player.Value, out var drivingSchoolTimer)) {
                // We remove the timer
                drivingSchoolTimer.Dispose();
                drivingSchoolTimerList.Remove(player.Value);
            }
        }

        private void OnDrivingTimer(object playerObject) {
            // We get the player and his vehicle
            var player = (Client) playerObject;
            Vehicle vehicle = player.GetData(EntityData.PLAYER_VEHICLE);

            // We finish the exam
            FinishDrivingExam(player, vehicle);

            // Deleting timer from the list
            if (drivingSchoolTimerList.TryGetValue(player.Value, out var drivingSchoolTimer)) {
                drivingSchoolTimer.Dispose();
                drivingSchoolTimerList.Remove(player.Value);
            }

            // Confirmation message sent to the player
            player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.license_failed_not_in_vehicle);
        }

        private void FinishDrivingExam(Client player, Vehicle vehicle) {
            // Vehicle reseting
            vehicle.Repair();
            vehicle.Position = vehicle.GetData(EntityData.VEHICLE_POSITION);
            vehicle.Rotation = vehicle.GetData(EntityData.VEHICLE_ROTATION);

            // Checkpoint delete
            if (NAPI.Vehicle.GetVehicleDriver(vehicle) == player) {
                Checkpoint licenseCheckpoint = player.GetData(EntityData.PLAYER_DRIVING_COLSHAPE);
                player.TriggerEvent("deleteLicenseCheckpoint");
                licenseCheckpoint.Delete();
            }

            // Entity data cleanup
            player.ResetData(EntityData.PLAYER_VEHICLE);
            player.ResetData(EntityData.PLAYER_DRIVING_EXAM);
            player.ResetData(EntityData.PLAYER_DRIVING_COLSHAPE);
            player.ResetData(EntityData.PLAYER_DRIVING_CHECKPOINT);

            // Stop the vehicle's speedometer
            player.TriggerEvent("removeSpeedometer");

            // Remove player from vehicle
            player.WarpOutOfVehicle();
        }

        public static int GetPlayerLicenseStatus(Client player, int license) {
            string playerLicenses = player.GetData(EntityData.PLAYER_LICENSES);
            var licenses = playerLicenses.Split(',');
            return int.Parse(licenses[license]);
        }

        public static void SetPlayerLicense(Client player, int license, int value) {
            // We get player licenses
            string playerLicenses = player.GetData(EntityData.PLAYER_LICENSES);
            var licenses = playerLicenses.Split(',');

            // Changing license status
            licenses[license] = value.ToString();
            playerLicenses = string.Join(",", licenses);

            // Save the new licenses
            player.SetData(EntityData.PLAYER_LICENSES, playerLicenses);
        }

        [ServerEvent(Event.PlayerEnterVehicle)]
        public void OnPlayerEnterVehicle(Client player, Vehicle vehicle, sbyte seatId) {
            if (vehicle.GetData(EntityData.VEHICLE_FACTION) == Constants.FACTION_DRIVING_SCHOOL) {
                var vehicleHash = (VehicleHash) vehicle.Model;
                if (player.GetData(EntityData.PLAYER_DRIVING_EXAM) != null &&
                    player.GetData(EntityData.PLAYER_DRIVING_EXAM) == Constants.CAR_DRIVING_PRACTICE) {
                    // We check the class of the vehicle
                    if (NAPI.Vehicle.GetVehicleClass(vehicleHash) == Constants.VEHICLE_CLASS_SEDANS) {
                        int checkPoint = player.GetData(EntityData.PLAYER_DRIVING_CHECKPOINT);
                        if (drivingSchoolTimerList.TryGetValue(player.Value, out var drivingSchoolTimer)) {
                            drivingSchoolTimer.Dispose();
                            drivingSchoolTimerList.Remove(player.Value);
                        }

                        var newCheckpoint = NAPI.Checkpoint.CreateCheckpoint(0,
                            Constants.CAR_LICENSE_CHECKPOINTS[checkPoint],
                            Constants.CAR_LICENSE_CHECKPOINTS[checkPoint + 1], 2.5f, new Color(198, 40, 40, 200));
                        player.SetData(EntityData.PLAYER_DRIVING_COLSHAPE, newCheckpoint);
                        player.SetData(EntityData.PLAYER_VEHICLE, vehicle);

                        // We place a mark on the map
                        player.TriggerEvent("showLicenseCheckpoint", Constants.CAR_LICENSE_CHECKPOINTS[checkPoint]);
                    }
                    else {
                        // Stop the vehicle's speedometer
                        player.TriggerEvent("removeSpeedometer");

                        player.WarpOutOfVehicle();
                        player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.vehicle_driving_not_suitable);
                    }
                }
                else if (player.GetData(EntityData.PLAYER_DRIVING_EXAM) != null &&
                         player.GetData(EntityData.PLAYER_DRIVING_EXAM) == Constants.MOTORCYCLE_DRIVING_PRACTICE) {
                    // We check the class of the vehicle
                    if (NAPI.Vehicle.GetVehicleClass(vehicleHash) == Constants.VEHICLE_CLASS_MOTORCYCLES) {
                        int checkPoint = player.GetData(EntityData.PLAYER_DRIVING_CHECKPOINT);
                        if (drivingSchoolTimerList.TryGetValue(player.Value, out var drivingSchoolTimer)) {
                            drivingSchoolTimer.Dispose();
                            drivingSchoolTimerList.Remove(player.Value);
                        }

                        var newCheckpoint = NAPI.Checkpoint.CreateCheckpoint(0,
                            Constants.BIKE_LICENSE_CHECKPOINTS[checkPoint],
                            Constants.BIKE_LICENSE_CHECKPOINTS[checkPoint + 1], 2.5f, new Color(198, 40, 40, 200));
                        player.SetData(EntityData.PLAYER_DRIVING_COLSHAPE, newCheckpoint);
                        player.SetData(EntityData.PLAYER_VEHICLE, vehicle);

                        // We place a mark on the map
                        player.TriggerEvent("showLicenseCheckpoint", Constants.BIKE_LICENSE_CHECKPOINTS[checkPoint]);
                    }
                    else {
                        // Stop the vehicle's speedometer
                        player.TriggerEvent("removeSpeedometer");

                        player.WarpOutOfVehicle();
                        player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.vehicle_driving_not_suitable);
                    }
                }
                else {
                    // Stop the vehicle's speedometer
                    player.TriggerEvent("removeSpeedometer");

                    player.WarpOutOfVehicle();
                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.not_in_car_practice);
                }
            }
        }

        [ServerEvent(Event.PlayerExitVehicle)]
        public void OnPlayerExitVehicle(Client player, Vehicle vehicle) {
            if (player.GetData(EntityData.PLAYER_DRIVING_EXAM) != null &&
                player.GetData(EntityData.PLAYER_VEHICLE) != null)
                if (player.GetData(EntityData.PLAYER_VEHICLE) == vehicle &&
                    vehicle.GetData(EntityData.VEHICLE_FACTION) == Constants.FACTION_DRIVING_SCHOOL) {
                    var warn = string.Format(InfoRes.license_vehicle_exit, 15);
                    Checkpoint playerDrivingCheckpoint = player.GetData(EntityData.PLAYER_DRIVING_COLSHAPE);
                    playerDrivingCheckpoint.Delete();
                    player.SendChatMessage(Constants.COLOR_INFO + warn);

                    // Removing the checkpoint marker
                    player.TriggerEvent("deleteLicenseCheckpoint");

                    // When the timer finishes, the exam will be failed
                    var drivingSchoolTimer = new Timer(OnDrivingTimer, player, 15000, Timeout.Infinite);
                    drivingSchoolTimerList.Add(player.Value, drivingSchoolTimer);
                }
        }

        [ServerEvent(Event.PlayerEnterCheckpoint)]
        public void OnPlayerEnterCheckpoint(Checkpoint checkpoint, Client player) {
            if (player.GetData(EntityData.PLAYER_DRIVING_COLSHAPE) != null &&
                player.GetData(EntityData.PLAYER_DRIVING_EXAM) != null) {
                if (player.IsInVehicle &&
                    player.GetData(EntityData.PLAYER_DRIVING_EXAM) == Constants.CAR_DRIVING_PRACTICE) {
                    var vehicle = player.Vehicle;
                    if (checkpoint == player.GetData(EntityData.PLAYER_DRIVING_COLSHAPE) &&
                        vehicle.GetData(EntityData.VEHICLE_FACTION) == Constants.FACTION_DRIVING_SCHOOL) {
                        Checkpoint currentCheckpoint = player.GetData(EntityData.PLAYER_DRIVING_COLSHAPE);
                        int checkPoint = player.GetData(EntityData.PLAYER_DRIVING_CHECKPOINT);
                        currentCheckpoint.Delete();

                        if (checkPoint < Constants.CAR_LICENSE_CHECKPOINTS.Count - 2) {
                            currentCheckpoint = NAPI.Checkpoint.CreateCheckpoint(CheckpointType.CylinderSingleArrow,
                                Constants.CAR_LICENSE_CHECKPOINTS[checkPoint + 1],
                                Constants.CAR_LICENSE_CHECKPOINTS[checkPoint + 2], 2.5f, new Color(198, 40, 40, 200));
                            player.SetData(EntityData.PLAYER_DRIVING_CHECKPOINT, checkPoint + 1);

                            // We place a mark on the map
                            player.TriggerEvent("showLicenseCheckpoint", currentCheckpoint.Position);
                        }
                        else if (checkPoint == Constants.CAR_LICENSE_CHECKPOINTS.Count - 2) {
                            currentCheckpoint = NAPI.Checkpoint.CreateCheckpoint(CheckpointType.CylinderSingleArrow,
                                Constants.CAR_LICENSE_CHECKPOINTS[checkPoint + 1],
                                vehicle.GetData(EntityData.VEHICLE_POSITION), 2.5f, new Color(198, 40, 40, 200));
                            player.SetData(EntityData.PLAYER_DRIVING_CHECKPOINT, checkPoint + 1);

                            // We place a mark on the map
                            player.TriggerEvent("showLicenseCheckpoint", currentCheckpoint.Position);
                        }
                        else if (checkPoint == Constants.CAR_LICENSE_CHECKPOINTS.Count - 1) {
                            currentCheckpoint = NAPI.Checkpoint.CreateCheckpoint(CheckpointType.CylinderCheckerboard,
                                vehicle.GetData(EntityData.VEHICLE_POSITION), new Vector3(0.0f, 0.0f, 0.0f), 2.5f,
                                new Color(198, 40, 40, 200));
                            player.SetData(EntityData.PLAYER_DRIVING_CHECKPOINT, checkPoint + 1);

                            // We place a mark on the map
                            player.TriggerEvent("showLicenseCheckpoint", currentCheckpoint.Position);
                        }
                        else {
                            // Exam finished
                            FinishDrivingExam(player, vehicle);

                            // We add points to the license
                            SetPlayerLicense(player, Constants.LICENSE_CAR, 12);

                            // Confirmation message sent to the player
                            player.SendChatMessage(Constants.COLOR_SUCCESS + SuccRes.license_drive_passed);
                        }
                    }
                }
                else if (player.IsInVehicle && player.GetData(EntityData.PLAYER_DRIVING_EXAM) ==
                         Constants.MOTORCYCLE_DRIVING_PRACTICE) {
                    var vehicle = player.Vehicle;
                    if (checkpoint == player.GetData(EntityData.PLAYER_DRIVING_COLSHAPE) &&
                        vehicle.GetData(EntityData.VEHICLE_FACTION) == Constants.FACTION_DRIVING_SCHOOL) {
                        Checkpoint currentCheckpoint = player.GetData(EntityData.PLAYER_DRIVING_COLSHAPE);
                        int checkPoint = player.GetData(EntityData.PLAYER_DRIVING_CHECKPOINT);
                        currentCheckpoint.Delete();

                        if (checkPoint < Constants.BIKE_LICENSE_CHECKPOINTS.Count - 2) {
                            currentCheckpoint = NAPI.Checkpoint.CreateCheckpoint(CheckpointType.CylinderSingleArrow,
                                Constants.BIKE_LICENSE_CHECKPOINTS[checkPoint + 1],
                                Constants.BIKE_LICENSE_CHECKPOINTS[checkPoint + 2], 2.5f, new Color(198, 40, 40, 200));
                            player.SetData(EntityData.PLAYER_DRIVING_CHECKPOINT, checkPoint + 1);

                            // We place a mark on the map
                            player.TriggerEvent("showLicenseCheckpoint", currentCheckpoint.Position);
                        }
                        else if (checkPoint == Constants.BIKE_LICENSE_CHECKPOINTS.Count - 2) {
                            currentCheckpoint = NAPI.Checkpoint.CreateCheckpoint(CheckpointType.CylinderSingleArrow,
                                Constants.BIKE_LICENSE_CHECKPOINTS[checkPoint + 1],
                                vehicle.GetData(EntityData.VEHICLE_POSITION), 2.5f, new Color(198, 40, 40, 200));
                            player.SetData(EntityData.PLAYER_DRIVING_CHECKPOINT, checkPoint + 1);

                            // We place a mark on the map
                            player.TriggerEvent("showLicenseCheckpoint", currentCheckpoint.Position);
                        }
                        else if (checkPoint == Constants.BIKE_LICENSE_CHECKPOINTS.Count - 1) {
                            currentCheckpoint = NAPI.Checkpoint.CreateCheckpoint(CheckpointType.CylinderCheckerboard,
                                vehicle.GetData(EntityData.VEHICLE_POSITION), new Vector3(0.0f, 0.0f, 0.0f), 2.5f,
                                new Color(198, 40, 40, 200));
                            player.SetData(EntityData.PLAYER_DRIVING_CHECKPOINT, checkPoint + 1);

                            // We place a mark on the map
                            player.TriggerEvent("showLicenseCheckpoint", currentCheckpoint.Position);
                        }
                        else {
                            // Exam finished
                            FinishDrivingExam(player, vehicle);

                            // We add points to the license
                            SetPlayerLicense(player, Constants.LICENSE_MOTORCYCLE, 12);

                            // Confirmation message sent to the player
                            player.SendChatMessage(Constants.COLOR_SUCCESS + SuccRes.license_drive_passed);
                        }
                    }
                }
            }
        }

        [ServerEvent(Event.VehicleDamage)]
        public void OnVehicleDamage(Vehicle vehicle, float lossFirst, float lossSecond) {
            var player = NAPI.Vehicle.GetVehicleDriver(vehicle);
            if (player != null && player.GetData(EntityData.PLAYER_DRIVING_COLSHAPE) != null &&
                player.GetData(EntityData.PLAYER_DRIVING_EXAM) != null)
                if (lossFirst - vehicle.Health > 5.0f) {
                    // Exam finished
                    FinishDrivingExam(player, vehicle);

                    // Inform the player about his failure
                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.license_drive_failed);
                }
        }

        [ServerEvent(Event.Update)]
        public void OnUpdate() {
            foreach (var player in NAPI.Pools.GetAllPlayers())
                if (player.GetData(EntityData.PLAYER_PLAYING) != null &&
                    player.GetData(EntityData.PLAYER_DRIVING_EXAM) != null)
                    if (player.IsInVehicle && player.VehicleSeat == (int) VehicleSeat.Driver) {
                        var vehicle = player.Vehicle;
                        if (vehicle.GetData(EntityData.VEHICLE_FACTION) == Constants.FACTION_DRIVING_SCHOOL) {
                            var velocity = NAPI.Entity.GetEntityVelocity(vehicle);
                            var speed = Math.Sqrt(velocity.X * velocity.X + velocity.Y * velocity.Y +
                                                  velocity.Z * velocity.Z);
                            if (Math.Round(speed * 3.6f) > Constants.MAX_DRIVING_VEHICLE) {
                                // Exam finished
                                FinishDrivingExam(player, vehicle);

                                // Inform the player about his failure
                                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.license_drive_failed);
                            }
                        }
                    }
        }

        [RemoteEvent("checkAnswer")]
        public void CheckAnswerEvent(Client player, int answer) {
            Task.Factory.StartNew(() => {
                if (Database.CheckAnswerCorrect(answer)) {
                    // We add the correct answer
                    int nextQuestion = player.GetSharedData(EntityData.PLAYER_LICENSE_QUESTION) + 1;

                    if (nextQuestion < Constants.MAX_LICENSE_QUESTIONS) {
                        // Go for the next question
                        player.SetSharedData(EntityData.PLAYER_LICENSE_QUESTION, nextQuestion);
                        player.TriggerEvent("getNextTestQuestion");
                    }
                    else {
                        // Player passed the exam
                        int license = player.GetData(EntityData.PLAYER_LICENSE_TYPE);
                        SetPlayerLicense(player, license, 0);

                        // Reset the entity data
                        player.ResetData(EntityData.PLAYER_LICENSE_TYPE);
                        player.ResetSharedData(EntityData.PLAYER_LICENSE_QUESTION);

                        // Send the message to the player
                        player.SendChatMessage(Constants.COLOR_SUCCESS + SuccRes.license_exam_passed);

                        // Exam window close
                        player.TriggerEvent("finishLicenseExam");
                    }
                }
                else {
                    // Player failed the exam
                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.license_exam_failed);

                    // Reset the entity data
                    player.ResetData(EntityData.PLAYER_LICENSE_TYPE);
                    player.ResetSharedData(EntityData.PLAYER_LICENSE_QUESTION);

                    // Exam window close
                    player.TriggerEvent("finishLicenseExam");
                }
            });
        }

        [Command(Commands.COM_DRIVING_SCHOOL, Commands.HLP_DRIVING_SCHOOL_COMMAND)]
        public void DrivingSchoolCommand(Client player, string type) {
            var licenseStatus = 0;
            foreach (var interior in Constants.INTERIOR_LIST)
                if (interior.captionMessage == GenRes.driving_school &&
                    player.Position.DistanceTo(interior.entrancePosition) < 2.5f) {
                    var questions = new List<TestModel>();
                    var answers = new List<TestModel>();

                    // Get the player's money
                    int money = player.GetSharedData(EntityData.PLAYER_MONEY);

                    switch (type.ToLower()) {
                        case Commands.ARG_CAR:
                            // Check for the status if the license
                            licenseStatus = GetPlayerLicenseStatus(player, Constants.LICENSE_CAR);

                            switch (licenseStatus) {
                                case -1:
                                    // Check if the player has enough money
                                    if (money >= Constants.PRICE_DRIVING_THEORICAL) {
                                        Task.Factory.StartNew(() => {
                                            // Add the questions
                                            questions = Database.GetRandomQuestions(Constants.LICENSE_CAR + 1);
                                            foreach (var question in questions)
                                                answers.AddRange(Database.GetQuestionAnswers(question.id));

                                            player.SetData(EntityData.PLAYER_LICENSE_TYPE, Constants.LICENSE_CAR);
                                            player.SetSharedData(EntityData.PLAYER_LICENSE_QUESTION, 0);

                                            player.SetSharedData(EntityData.PLAYER_MONEY,
                                                money - Constants.PRICE_DRIVING_THEORICAL);

                                            // Start the exam
                                            player.TriggerEvent("startLicenseExam", NAPI.Util.ToJson(questions),
                                                NAPI.Util.ToJson(answers));
                                        });
                                    }
                                    else {
                                        var message = string.Format(ErrRes.driving_school_money,
                                            Constants.PRICE_DRIVING_THEORICAL);
                                        player.SendChatMessage(Constants.COLOR_ERROR + message);
                                    }

                                    break;
                                case 0:
                                    // Check if the player has enough money
                                    if (money >= Constants.PRICE_DRIVING_PRACTICAL) {
                                        player.SetData(EntityData.PLAYER_LICENSE_TYPE, Constants.LICENSE_CAR);
                                        player.SetData(EntityData.PLAYER_DRIVING_EXAM, Constants.CAR_DRIVING_PRACTICE);
                                        player.SetData(EntityData.PLAYER_DRIVING_CHECKPOINT, 0);

                                        player.SetSharedData(EntityData.PLAYER_MONEY,
                                            money - Constants.PRICE_DRIVING_PRACTICAL);

                                        player.SendChatMessage(
                                            Constants.COLOR_INFO + InfoRes.enter_license_car_vehicle);
                                    }
                                    else {
                                        var message = string.Format(ErrRes.driving_school_money,
                                            Constants.PRICE_DRIVING_PRACTICAL);
                                        player.SendChatMessage(Constants.COLOR_ERROR + message);
                                    }

                                    break;
                                default:
                                    // License up to date
                                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_already_license);
                                    break;
                            }

                            break;
                        case Commands.ARG_MOTORCYCLE:
                            // Check for the status if the license
                            licenseStatus = GetPlayerLicenseStatus(player, Constants.LICENSE_MOTORCYCLE);

                            switch (licenseStatus) {
                                case -1:
                                    // Check if the player has enough money
                                    if (money >= Constants.PRICE_DRIVING_THEORICAL) {
                                        Task.Factory.StartNew(() => {
                                            // Add the questions
                                            questions = Database.GetRandomQuestions(Constants.LICENSE_MOTORCYCLE + 1);
                                            foreach (var question in questions)
                                                answers.AddRange(Database.GetQuestionAnswers(question.id));

                                            player.SetData(EntityData.PLAYER_LICENSE_TYPE,
                                                Constants.LICENSE_MOTORCYCLE);
                                            player.SetSharedData(EntityData.PLAYER_LICENSE_QUESTION, 0);

                                            player.SetSharedData(EntityData.PLAYER_MONEY,
                                                money - Constants.PRICE_DRIVING_THEORICAL);

                                            // Start the exam
                                            player.TriggerEvent("startLicenseExam", NAPI.Util.ToJson(questions),
                                                NAPI.Util.ToJson(answers));
                                        });
                                    }
                                    else {
                                        var message = string.Format(ErrRes.driving_school_money,
                                            Constants.PRICE_DRIVING_THEORICAL);
                                        player.SendChatMessage(Constants.COLOR_ERROR + message);
                                    }

                                    break;
                                case 0:
                                    // Check if the player has enough money
                                    if (money >= Constants.PRICE_DRIVING_PRACTICAL) {
                                        player.SetData(EntityData.PLAYER_LICENSE_TYPE, Constants.LICENSE_MOTORCYCLE);
                                        player.SetData(EntityData.PLAYER_DRIVING_EXAM,
                                            Constants.MOTORCYCLE_DRIVING_PRACTICE);
                                        player.SetData(EntityData.PLAYER_DRIVING_CHECKPOINT, 0);

                                        player.SetSharedData(EntityData.PLAYER_MONEY,
                                            money - Constants.PRICE_DRIVING_PRACTICAL);

                                        player.SendChatMessage(
                                            Constants.COLOR_INFO + InfoRes.enter_license_bike_vehicle);
                                    }
                                    else {
                                        var message = string.Format(ErrRes.driving_school_money,
                                            Constants.PRICE_DRIVING_PRACTICAL);
                                        player.SendChatMessage(Constants.COLOR_ERROR + message);
                                    }

                                    break;
                                default:
                                    // License up to date
                                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_already_license);
                                    break;
                            }

                            break;
                        default:
                            player.SendChatMessage(Constants.COLOR_HELP + Commands.HLP_DRIVING_SCHOOL_COMMAND);
                            break;
                    }

                    return;
                }

            // Player's not in the driving school
            player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_driving_school);
        }

        [Command(Commands.COM_LICENSES)]
        public void LicensesCommand(Client player) {
            var currentLicense = 0;
            string playerLicenses = player.GetData(EntityData.PLAYER_LICENSES);
            var playerLicensesArray = playerLicenses.Split(',');
            player.SendChatMessage(Constants.COLOR_INFO + InfoRes.license_list);
            foreach (var license in playerLicensesArray) {
                var currentLicenseStatus = int.Parse(license);
                switch (currentLicense) {
                    case Constants.LICENSE_CAR:
                        switch (currentLicenseStatus) {
                            case -1:
                                player.SendChatMessage(Constants.COLOR_HELP + InfoRes.car_license_not_available);
                                break;
                            case 0:
                                player.SendChatMessage(Constants.COLOR_HELP + InfoRes.car_license_practical_pending);
                                break;
                            default:
                                var message = string.Format(InfoRes.car_license_points, currentLicenseStatus);
                                player.SendChatMessage(Constants.COLOR_HELP + message);
                                break;
                        }

                        break;
                    case Constants.LICENSE_MOTORCYCLE:
                        switch (currentLicenseStatus) {
                            case -1:
                                player.SendChatMessage(Constants.COLOR_HELP + InfoRes.motorcycle_license_not_available);
                                break;
                            case 0:
                                player.SendChatMessage(Constants.COLOR_HELP +
                                                       InfoRes.motorcycle_license_practical_pending);
                                break;
                            default:
                                var message = string.Format(InfoRes.motorcycle_license_points, currentLicenseStatus);
                                player.SendChatMessage(Constants.COLOR_HELP + message);
                                break;
                        }

                        break;
                    case Constants.LICENSE_TAXI:
                        if (currentLicenseStatus == -1)
                            player.SendChatMessage(Constants.COLOR_HELP + InfoRes.taxi_license_not_available);
                        else
                            player.SendChatMessage(Constants.COLOR_HELP + InfoRes.taxi_license_up_to_date);
                        break;
                }

                currentLicense++;
            }
        }
    }
}