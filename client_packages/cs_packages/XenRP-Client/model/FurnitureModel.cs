using RAGE;
using RAGE.Elements;

namespace XenRP.Client.model {
    internal class FurnitureModel {
        public int id { get; set; }
        public uint hash { get; set; }
        public uint house { get; set; }
        public Vector3 position { get; set; }
        public Vector3 rotation { get; set; }
        public MapObject handle { get; set; }
    }
}