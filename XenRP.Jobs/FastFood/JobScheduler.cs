using System;
using System.Collections.Generic;
using GTANetworkAPI;
using XenRP.globals;
using XenRP.house;
using XenRP.model;

namespace XenRP.Jobs.FastFood {
    public class JobScheduler : Script {
        public static List<FastfoodOrderModel> FastFoodOrderList;
        private static int orderGenerationTime;
        private static int fastFoodId = 1;
        public JobScheduler() {
            FastFoodOrderList = new List<FastfoodOrderModel>();
            var rnd = new Random();
            orderGenerationTime = Scheduler.GetTotalSeconds() + rnd.Next(0, 1) * 60; 
            Scheduler.Minute += OnMinute;
        }
        
        private void OnMinute() {
            var totalSeconds = Scheduler.GetTotalSeconds();
            
            if (orderGenerationTime <= totalSeconds && House.houseList.Count > 0) {
                var rnd = new Random();
                var generatedOrders = rnd.Next(7, 20);
                for (var i = 0; i < generatedOrders; i++) {
                    var order = new FastfoodOrderModel();
                    {
                        order.id = fastFoodId;
                        order.pizzas = rnd.Next(0, 4);
                        order.hamburgers = rnd.Next(0, 4);
                        order.sandwitches = rnd.Next(0, 4);
                        order.position = GetPlayerFastFoodDeliveryDestination();
                        order.limit = totalSeconds + 300;
                        order.taken = false;
                    }

                    FastFoodOrderList.Add(order);
                    fastFoodId++;
                }

                // Update the new timer time
                orderGenerationTime = totalSeconds + rnd.Next(2, 5) * 60;
            }

            // Remove old orders
            FastFoodOrderList.RemoveAll(order => !order.taken && order.limit <= totalSeconds);
        }

        private Vector3 GetPlayerFastFoodDeliveryDestination() {
            var random = new Random();
            var element = random.Next(House.houseList.Count);
            return House.houseList[element].position;
        }
    }
}