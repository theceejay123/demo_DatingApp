using System.Collections.Generic;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
  [Authorize]
  public class MessagesController : BaseApiController
  {
    private readonly IMessageRepository _messageRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public MessagesController(IUserRepository userRepository, IMessageRepository messageRepository, IMapper mapper)
    {
      _mapper = mapper;
      _userRepository = userRepository;
      _messageRepository = messageRepository;
    }

    [HttpPost]
    public async Task<ActionResult<MessageDTO>> CreateMessage(CreateMessageDTO createMessageDTO)
    {
      var username = User.GetUserName();
      if (username == createMessageDTO.RecipientUserName.ToLower())
        return BadRequest("You cannot send messages to yourself");

      var sender = await _userRepository.GetUserByUserNameAsync(username);
      var recipient = await _userRepository.GetUserByUserNameAsync(createMessageDTO.RecipientUserName);

      if (recipient == null) return NotFound();

      var message = new Message
      {
        Sender = sender,
        Recipient = recipient,
        SenderUsername = sender.UserName,
        RecipientUsername = recipient.UserName,
        Content = createMessageDTO.Content
      };

      _messageRepository.AddMessage(message);

      if (await _messageRepository.SaveAllAsync()) return Ok(_mapper.Map<MessageDTO>(message));

      return BadRequest("Failed to send a message");
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MessageDTO>>> GetMessagesForUser([FromQuery] MessageParams messageParams)
    {
      messageParams.UserName = User.GetUserName();

      var messages = await _messageRepository.GetMessagesForUser(messageParams);

      Response.AddPaginationHeader(messages.CurrentPage, messages.PageSize, messages.TotalCount, messages.TotalPages);

      return messages;
    }

    [HttpGet("thread/{recipient}")]
    public async Task<ActionResult<IEnumerable<MessageDTO>>> GetMessageThread(string recipient)
    {
      var currentUsername = User.GetUserName();

      return Ok(await _messageRepository.GetMessageThread(currentUsername, recipient));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteMessage(int id)
    {
      var username = User.GetUserName();
      var message = await _messageRepository.GetMessage(id);

      if (message.Sender.UserName != username && message.Recipient.UserName != username) return Unauthorized();
      if (message.Sender.UserName == username) message.SenderDeleted = true;
      if (message.Recipient.UserName == username) message.RecipientDeleted = true;
      if (message.SenderDeleted && message.RecipientDeleted) _messageRepository.DeleteMessage(message);

      if (await _messageRepository.SaveAllAsync()) return Ok();

      return BadRequest("Problem deleting the message");
    }
  }
}