using RoboStockChat.Data;

namespace RoboStockChat.Models
{
    public class Chatroom : IEntity
    {
        /// <summary>
        /// Unique ID of the Chat room
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Name of the Chat room
        /// </summary>
        public string Name { get; set; } = null!;

        /// <summary>
        /// Id of the creator
        /// </summary>
        public string CreatedBy { get; set; } = null!;
    }
}
