using XenRP.model;

namespace XenRP.Jobs.FastFood.Models {
    public class FastfoodOrderModel : OrderModel {
        public int pizzas { get; set; }
        public int hamburgers { get; set; }
        public int sandwitches { get; set; }
    }
}