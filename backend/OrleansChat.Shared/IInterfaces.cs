using System;
using System.Collections.Generic;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace OrleansChat.Shared;

public interface IServerGrain : IGrainWithStringKey
{
    Task <Response>CreateServer(string name, string ownerUsername);
    Task<List<string>> GetChannels();
    Task<Response> AddChannel(string channelId);
    Task<Response> InviteUser(string requestingUsername, string targetUsername);
    Task<string> GetName();
    Task<string> GetOwnerUsername();
    Task<List<string>> GetMemberUsernames();
    Task<Response> DeleteServer(string requestingUsername);
    Task<ServerDto> GetServerDetails();
    Task<Response> RemoveChannel(string channelId);
}

public interface IChannelGrain : IGrainWithStringKey
{
    Task<Response> SendMessage(string senderId, string text);
    Task<List<MessageDto>> GetMessageHistory();
    Task<Response> DeleteChannel();
}

public interface IUserGrain : IGrainWithStringKey 
{
    Task<Response> Register(string password);
    Task<Response> Login(string password);
    Task<Response> Logout();
    Task<UserState> GetState();
    Task<bool> CanCreateServer();
    Task<Response> AddOwnedServer(string serverId);
    Task<Response> ReceiveInviteToServer(string serverId);
    Task<List<string>> GetMyServers();
    Task<Response> RemoveServerReference(string serverId);
    Task<Response> UpdateStatus(string status, string? ConnectionId);
    Task PrivateMessageReceived(string privateChatId, string senderId, string text);
    Task ChannelMessageReceived(string channelId, string senderId, string text);
    Task<Response> AddActiveChat(string chatKey);
    Task<List<string>> GetActiveChats();
    Task<bool> Exists();
}

public interface IPrivateChatGrain: IGrainWithStringKey
{
    Task<Response> SendPrivateMessage(string senderId, string receiverId, string text);
    Task<List<MessageDto>> GetMessageHistory();
}