using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
  public class MessageRepository : IMessageRepository
  {
    private readonly DataContext _context;
    private readonly IMapper _mapper;
    public MessageRepository(DataContext context, IMapper mapper)
    {
      _mapper = mapper;
      _context = context;
    }

    public void AddMessage(Message message)
    {
      _context.Messages.Add(message);
    }

    public void DeleteMessage(Message message)
    {
      _context.Messages.Remove(message);
    }

    public async Task<Message> GetMessage(int id)
    {
      return await _context.Messages
        .Include(x => x.Sender)
        .Include(x => x.Recipient)
        .SingleOrDefaultAsync(x => x.Id == id);
    }

    public async Task<PagedList<MessageDTO>> GetMessagesForUser(MessageParams messageParams)
    {
      var query = _context.Messages
        .OrderByDescending(x => x.MessageSent)
        .AsQueryable();

      query = messageParams.Container switch
      {
        "Inbox" => query.Where(x => x.Recipient.UserName == messageParams.UserName && x.RecipientDeleted == false),
        "Outbox" => query.Where(x => x.Sender.UserName == messageParams.UserName && x.SenderDeleted == false),
        _ => query.Where(x => x.Recipient.UserName == messageParams.UserName && x.DateRead == null && x.RecipientDeleted == false)
      };

      var messages = query.ProjectTo<MessageDTO>(_mapper.ConfigurationProvider);

      return await PagedList<MessageDTO>.CreateAsync(messages, messageParams.PageNumber, messageParams.PageSize);
    }

    public async Task<IEnumerable<MessageDTO>> GetMessageThread(string currentUsername, string recipientUsername)
    {
      var messages = await _context.Messages
        .Include(u => u.Sender).ThenInclude(p => p.Photos)
        .Include(u => u.Recipient).ThenInclude(p => p.Photos)
        .Where(x => x.Recipient.UserName == currentUsername
                  && x.RecipientDeleted == false
                  && x.Sender.UserName == recipientUsername
                  || x.Recipient.UserName == recipientUsername
                  && x.Sender.UserName == currentUsername
                  && x.SenderDeleted == false)
        .OrderBy(x => x.MessageSent)
        .ToListAsync();

      var unreadMessages = messages.Where(m => m.DateRead == null && m.Recipient.UserName == currentUsername).ToList();

      if (unreadMessages.Any())
      {
        foreach (var message in unreadMessages)
        {
          message.DateRead = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
      }

      return _mapper.Map<IEnumerable<MessageDTO>>(messages);
    }

    public async Task<bool> SaveAllAsync()
    {
      return await _context.SaveChangesAsync() > 0;
    }

    public void AddGroup(Group group)
    {
      _context.Groups.Add(group);
    }

    public async Task<Connection> GetConnection(string connectionId)
    {
      return await _context.Connections.FindAsync(connectionId);
    }

    public async Task<Group> GetMessageGroup(string groupName)
    {
      return await _context.Groups.Include(x => x.Connections).FirstOrDefaultAsync(x => x.Name == groupName);
    }

    public void RemoveConnection(Connection connection)
    {
      _context.Connections.Remove(connection);
    }

    public async Task<Group> GetGroupForConnection(string connectionId)
    {
      return await _context.Groups
        .Include(c => c.Connections)
        .Where(c => c.Connections.Any(x => x.ConnectionId == connectionId))
        .FirstOrDefaultAsync();
    }
  }
}