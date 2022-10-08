using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RoboStockChat.Repository;

namespace RoboStockChat.Pages
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly ChatroomRepository _chatroomRepository;

        public IndexModel(ChatroomRepository chatroomRepository, ILogger<IndexModel> logger)
        {
            _chatroomRepository = chatroomRepository;
            _logger = logger;
        }

        public async Task OnGet()
        {
            var chatrooms = await _chatroomRepository.GetAll();
            ViewData["Chatrooms"] = chatrooms;
        }
    }
}