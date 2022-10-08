using RoboStockChat.Data;
using RoboStockChat.Models;

namespace RoboStockChat.Repository
{
    public class ChatroomRepository : Repository<Chatroom, ApplicationDbContext>
    {
        public ChatroomRepository(ApplicationDbContext context) : base(context)
        {

        }
    }
}
