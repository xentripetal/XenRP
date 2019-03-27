using GTANetworkAPI;

namespace WiredPlayers.model
{
    public class AttachmentModel
    {
        public int itemId { get; set; }
        public string hash { get; set; }
        public Vector3 offset { get; set; }
        public Vector3 rotation { get; set; }

        public AttachmentModel(int itemId, string hash, Vector3 offset, Vector3 rotation)
        {
            this.itemId = itemId;
            this.hash = hash;
            this.offset = offset;
            this.rotation = rotation;
        }
    }
}
