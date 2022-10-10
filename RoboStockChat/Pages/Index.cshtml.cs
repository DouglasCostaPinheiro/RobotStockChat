using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RoboStockChat.Models;
using RoboStockChat.Repository;
using System.Security.Claims;

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

        public async Task<IActionResult> OnPostAdd(Chatroom chatroom)
        {
            chatroom.Id = Guid.NewGuid();
            chatroom.CreatedBy = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
            await _chatroomRepository.Add(chatroom);

            return RedirectToPage("Index");
        }

        public async Task<IActionResult> OnPostDelete(Chatroom chatroom)
        {
            await _chatroomRepository.Delete(chatroom.Id);
            return RedirectToPage("Index");
        }
    }
}