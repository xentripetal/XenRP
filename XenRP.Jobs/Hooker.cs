using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GTANetworkAPI;
using XenRP.bank;
using XenRP.database;
using XenRP.globals;
using XenRP.messages.error;
using XenRP.messages.general;
using XenRP.messages.information;
using XenRP.messages.success;

namespace XenRP.jobs {
    public class Hooker : Script {
        public static Dictionary<int, Timer> sexTimerList;

        public Hooker() {
            // Initialize the variables
            sexTimerList = new Dictionary<int, Timer>();
        }

        [Command("accepthooker")]
        public void AcceptHookerCommand(Client player) {
            if (player.GetData(EntityData.HOOKER_TYPE_SERVICE) == null) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.no_service_offered);
            }
            else if (player.GetData(EntityData.PLAYER_ALREADY_FUCKING) != null) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.already_fucking);
            }
            else if (player.VehicleSeat != (int) VehicleSeat.Driver) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.not_vehicle_driving);
            }
            else {
                if (player.Vehicle.EngineStatus) {
                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.engine_on);
                }
                else {
                    Client target = player.GetData(EntityData.PLAYER_JOB_PARTNER);
                    if (player.GetData(EntityData.HOOKER_TYPE_SERVICE) != null) {
                        int amount = player.GetData(EntityData.JOB_OFFER_PRICE);
                        int money = player.GetSharedData(EntityData.PLAYER_MONEY);

                        if (target.GetData(EntityData.PLAYER_PLAYING) != null) {
                            if (amount > money) {
                                player.SendChatMessage(
                                    Constants.COLOR_ERROR + ErrRes.player_not_enough_money);
                            }
                            else {
                                int targetMoney = target.GetSharedData(EntityData.PLAYER_MONEY);
                                player.SetSharedData(EntityData.PLAYER_MONEY, money - amount);
                                target.SetSharedData(EntityData.PLAYER_MONEY, targetMoney + amount);

                                var playerMessage = string.Format(InfoRes.service_paid, amount);
                                var targetMessage = string.Format(InfoRes.service_received, amount);
                                player.SendChatMessage(Constants.COLOR_INFO + playerMessage);
                                target.SendChatMessage(Constants.COLOR_INFO + targetMessage);

                                player.SetData(EntityData.PLAYER_ANIMATION, target);
                                player.SetData(EntityData.PLAYER_ALREADY_FUCKING, target);
                                target.SetData(EntityData.PLAYER_ALREADY_FUCKING, player);

                                // Reset the entity data
                                player.ResetData(EntityData.JOB_OFFER_PRICE);
                                player.ResetData(EntityData.PLAYER_JOB_PARTNER);

                                // Check the type of the service
                                if (player.GetData(EntityData.HOOKER_TYPE_SERVICE) ==
                                    Constants.HOOKER_SERVICE_BASIC) {
                                    player.PlayAnimation("mini@prostitutes@sexlow_veh",
                                        "low_car_bj_loop_player", (int) Constants.AnimationFlags.Loop);
                                    target.PlayAnimation("mini@prostitutes@sexlow_veh",
                                        "low_car_bj_loop_female", (int) Constants.AnimationFlags.Loop);

                                    // Timer to finish the service
                                    var sexTimer = new Timer(OnSexServiceTimer, player, 120000,
                                        Timeout.Infinite);
                                    sexTimerList.Add(player.Value, sexTimer);
                                }
                                else {
                                    player.PlayAnimation("mini@prostitutes@sexlow_veh",
                                        "low_car_sex_loop_player", (int) Constants.AnimationFlags.Loop);
                                    target.PlayAnimation("mini@prostitutes@sexlow_veh",
                                        "low_car_sex_loop_female", (int) Constants.AnimationFlags.Loop);

                                    // Timer to finish the service
                                    var sexTimer = new Timer(OnSexServiceTimer, player, 180000,
                                        Timeout.Infinite);
                                    sexTimerList.Add(player.Value, sexTimer);
                                }

                                Task.Factory.StartNew(() => {
                                    // Save the log into the database
                                    DBBankCommands.LogPayment(player.Name, target.Name, GenRes.hooker, amount);
                                });
                            }
                        }
                        else {
                            player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_found);
                        }
                    }
                }
            }
        }

        [ServerEvent(Event.PlayerDisconnected)]
        public static void OnPlayerDisconnected(Client player, DisconnectionType type, string reason) {
            if (sexTimerList.TryGetValue(player.Value, out var sexTimer)) {
                sexTimer.Dispose();
                sexTimerList.Remove(player.Value);
            }
        }

        public static void OnSexServiceTimer(object playerObject) {
            var player = (Client) playerObject;
            Client target = player.GetData(EntityData.PLAYER_ALREADY_FUCKING);

            // We stop both animations
            player.StopAnimation();
            target.StopAnimation();

            // Health the player
            player.Health = 100;

            player.ResetData(EntityData.PLAYER_ANIMATION);
            player.ResetData(EntityData.HOOKER_TYPE_SERVICE);
            player.ResetData(EntityData.PLAYER_ALREADY_FUCKING);
            target.ResetData(EntityData.PLAYER_ALREADY_FUCKING);

            if (sexTimerList.TryGetValue(player.Value, out var sexTimer)) {
                sexTimer.Dispose();
                sexTimerList.Remove(player.Value);
            }

            // Send finish message to both players
            target.SendChatMessage(Constants.COLOR_SUCCESS + SuccRes.hooker_client_satisfied);
            player.SendChatMessage(Constants.COLOR_SUCCESS + SuccRes.hooker_service_finished);
        }

        [Command(Commands.COM_SERVICE, Commands.HLP_HOOKER_SERVICE_COMMAND)]
        public void ServiceCommand(Client player, string service, string targetString, int price) {
            if (player.GetData(EntityData.PLAYER_JOB) != Constants.JOB_HOOKER) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.not_hooker);
            }
            else if (player.GetData(EntityData.PLAYER_ALREADY_FUCKING) != null) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.already_fucking);
            }
            else if (player.VehicleSeat != (int) VehicleSeat.RightFront) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.not_vehicle_passenger);
            }
            else {
                NetHandle vehicle = player.Vehicle;
                var target = int.TryParse(targetString, out var targetId)
                    ? Globals.GetPlayerById(targetId)
                    : NAPI.Player.GetPlayerFromName(targetString);

                if (target.VehicleSeat != (int) VehicleSeat.Driver) {
                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.client_not_vehicle_driving);
                }
                else {
                    var playerMessage = string.Empty;
                    var targetMessage = string.Empty;

                    switch (service.ToLower()) {
                        case Commands.ARG_ORAL:
                            target.SetData(EntityData.PLAYER_JOB_PARTNER, player);
                            target.SetData(EntityData.JOB_OFFER_PRICE, price);
                            target.SetData(EntityData.HOOKER_TYPE_SERVICE, Constants.HOOKER_SERVICE_BASIC);

                            playerMessage = string.Format(InfoRes.oral_service_offer, target.Name, price);
                            targetMessage = string.Format(InfoRes.oral_service_receive, player.Name, price);
                            player.SendChatMessage(Constants.COLOR_INFO + playerMessage);
                            target.SendChatMessage(Constants.COLOR_INFO + targetMessage);
                            break;
                        case Commands.ARG_SEX:
                            target.SetData(EntityData.PLAYER_JOB_PARTNER, player);
                            target.SetData(EntityData.JOB_OFFER_PRICE, price);
                            target.SetData(EntityData.HOOKER_TYPE_SERVICE, Constants.HOOKER_SERVICE_FULL);

                            playerMessage = string.Format(InfoRes.sex_service_offer, target.Name, price);
                            targetMessage = string.Format(InfoRes.sex_service_receive, player.Name, price);
                            player.SendChatMessage(Constants.COLOR_INFO + playerMessage);
                            target.SendChatMessage(Constants.COLOR_INFO + targetMessage);
                            break;
                        default:
                            player.SendChatMessage(Constants.COLOR_ERROR + Commands.HLP_HOOKER_SERVICE_COMMAND);
                            break;
                    }
                }
            }
        }
    }
}