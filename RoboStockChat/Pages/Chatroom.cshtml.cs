using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Options;
using RoboStockChat.Framework;
using RoboStockChat.Models;
using RoboStockChat.Repository;

namespace RoboStockChat.Pages
{
    [Authorize]
    public class ChatroomModel : PageModel
    {
        private readonly ChatroomRepository _chatroomRepository;
        private readonly IOptions<AppSettings> _appSettings;

        public Chatroom Chatroom;
        public Guid ApplicationId;

        public ChatroomModel(ChatroomRepository chatroomRepository, IOptions<AppSettings> appSettings)
        {
            _chatroomRepository = chatroomRepository;
            _appSettings = appSettings;
            ApplicationId = appSettings.Value.ApplicationId;
        }

        public async Task<IActionResult> OnGet(Guid id)
        {
            if (id == Guid.Empty)
                return NotFound();

            Chatroom = await _chatroomRepository.Get(id);

            return Page();
        }
    }
}
