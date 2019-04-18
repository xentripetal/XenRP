﻿using System.Collections.Generic;
using System.Linq;
using GTANetworkAPI;
using WiredPlayers.globals;
using WiredPlayers.messages.error;

namespace WiredPlayers.jobs {
    public class Trucker : Script {
        public static void CheckTruckerOrders(Client player) {
            // Get the deliverable orders
            var truckerOrders = Globals.truckerOrderList.Where(o => !o.taken).ToList();

            if (truckerOrders.Count == 0) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.order_none);
                return;
            }

            var distancesList = new List<float>();

            foreach (var order in truckerOrders) {
                var distance = player.Position.DistanceTo(order.position);
                distancesList.Add(distance);
            }

            player.TriggerEvent("showTruckerOrders", NAPI.Util.ToJson(truckerOrders), NAPI.Util.ToJson(distancesList));
        }

        [Command(Commands.COM_DELIVER)]
        public void DeliverCommand(Client player) {
            if (player.GetData(EntityData.PLAYER_JOB) != Constants.JOB_TRUCKER) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.not_trucker);
                return;
            }

            if (player.GetData(EntityData.PLAYER_JOB_CHECKPOINT) != null) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.already_in_route);
                return;
            }

            // Create the delivery crates
            player.TriggerEvent("createTruckerCrates");
        }
    }
}