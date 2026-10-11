using Microsoft.AspNetCore.SignalR;
using Orleans;
using OrleansChat.Shared;

namespace OrleansChat.Gateway;

public class ChatHub(IGrainFactory grainFactory, ILogger<ChatHub> logger) : Hub
{
    private string GetUsername() =>
    Context.GetHttpContext()?.Request.Query["username"].ToString()
    ?? throw new HubException("User Not Authorized.");

    public async Task<Response> JoinChannel(string channelId)
    {
        var username = GetUsername();
        if (string.IsNullOrEmpty(username))
            return Response.Fail("Not authenticated", "NOT_LOGGED_IN");

        try
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, channelId);

            logger.LogInformation("[CHATHUB]: User {Username} has joined a channel {ChannelId}", username, channelId);

            return Response.Ok("Joined channel successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[CHATHUB]:Error joining channel {ChannelId} for user {Username}", channelId, username);
            return Response.Fail("System error", "SYSTEM_ERROR");
        }
    }

    public async Task<Response> LeaveChannel(string channelId)
    {
        var username = GetUsername();
        if (string.IsNullOrEmpty(username))
            return Response.Fail("Not authenticated", "NOT_LOGGED_IN");

        try
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, channelId);

            logger.LogInformation("[CHATHUB]: User {Username} has left the channel {ChannelId}", username, channelId);

            return Response.Ok("Left channel successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[CHATHUB]:Error leaving channel {ChannelId} for user {Username}", channelId, username);
            return Response.Fail("System error", "SYSTEM_ERROR");
        }
    }

    public async Task<Response> SendChannelMessage(string channelId, string messageText)
    {
        var username = GetUsername();
        if (string.IsNullOrWhiteSpace(username))
            return Response.Fail("Not logged in", "NOT_LOGGED_IN");

        if (string.IsNullOrWhiteSpace(messageText))
            return Response.Fail("Message cannot be empty", "INVALID_INPUT");

        var channelGrain = grainFactory.GetGrain<IChannelGrain>(channelId);

        var response = await channelGrain.SendMessage(username, messageText);

        if (!response.Success)
            return response;

        var messageDto = new MessageDto
        {
            SenderId = username,
            Text = messageText,
            Timestamp = DateTime.UtcNow
        };

        await Clients.Group(channelId).SendAsync("ReceiveChannelMessage", channelId, messageDto);

        return Response.Ok("Message sent");
    }

    public async Task<Response> SendPrivateMessage(string receiverUsername, string messageText)
    {
        var senderUsername = GetUsername();

        if (string.IsNullOrWhiteSpace(senderUsername))
            return Response.Fail("Not logged in", "NOT_LOGGED_IN");

        if (string.IsNullOrWhiteSpace(receiverUsername))
            return Response.Fail("Receiver not specified", "INVALID_INPUT");

        if (string.IsNullOrWhiteSpace(messageText))
            return Response.Fail("Message cannot be empty", "INVALID_INPUT");

        var participants = new List<string> { senderUsername, receiverUsername };
        participants.Sort();
        string chatKey = $"{participants[0]}_{participants[1]}";

        var senderGrain = grainFactory.GetGrain<IUserGrain>(senderUsername);
        var receiverGrain = grainFactory.GetGrain<IUserGrain>(receiverUsername);
        if (!await receiverGrain.Exists())
        {
            return Response.Fail("User doesn't exist.", "USER_NOT_FOUND");
        }

        var senderChats = await senderGrain.GetActiveChats();
        var isNewChat = !senderChats.Contains(chatKey);

        if (isNewChat)
        {
            await senderGrain.AddActiveChat(chatKey);
            await receiverGrain.AddActiveChat(chatKey);

            await Clients.User(senderUsername).SendAsync("UpdatePrivateChatList", chatKey);
            await Clients.User(receiverUsername).SendAsync("UpdatePrivateChatList", chatKey);
        }

        var privateChatGrain = grainFactory.GetGrain<IPrivateChatGrain>(chatKey);

        var response = await privateChatGrain.SendPrivateMessage(senderUsername, receiverUsername, messageText);

        if (!response.Success)
            return response;

        var messageDto = new MessageDto
        {
            SenderId = senderUsername,
            Text = messageText,
            Timestamp = DateTime.UtcNow
        };
        await Clients.User(senderUsername).SendAsync("ReceivePrivateMessage", chatKey, messageDto);
        await Clients.User(receiverUsername).SendAsync("ReceivePrivateMessage", chatKey, messageDto);

        return Response.Ok("Message sent");
    }

    public async Task<List<MessageDto>> GetChannelHistory(string channelId)
    {
        var channelGrain = grainFactory.GetGrain<IChannelGrain>(channelId);

        return await channelGrain.GetMessageHistory();
    }

    public async Task<List<MessageDto>> GetPrivateHistory(string chatKey)
    {
        var currentUsername = GetUsername();
        if (!chatKey.Contains(currentUsername))
        {
            logger.LogWarning("[CHATHUB]: Unauthorized attempt to access private chat history by {Username}", currentUsername);
            return new List<MessageDto>();
        }

        var grain = grainFactory.GetGrain<IPrivateChatGrain>(chatKey);
        return await grain.GetMessageHistory();
    }

    public override async Task OnConnectedAsync()
    {
        var username = GetUsername();
        if (string.IsNullOrEmpty(username)) return;

        try
        {
            var userGrain = grainFactory.GetGrain<IUserGrain>(username);
            var serverIds = await userGrain.GetMyServers();

            foreach (var serverId in serverIds)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, serverId);

                var serverGrain = grainFactory.GetGrain<IServerGrain>(serverId);
                var channelIds = await serverGrain.GetChannels();

                foreach (var channelId in channelIds)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, channelId);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[CHATHUB]:Error initializing connection to channel.");
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var username = Context.Items["Username"] as string;

        if (!string.IsNullOrEmpty(username))
        {
            try
            {
                var userGrain = grainFactory.GetGrain<IUserGrain>(username);
                await userGrain.UpdateStatus("Offline", null);

                logger.LogInformation("[CHATHUB]: User {Username} has disconnected. Reason: {Reason}",
                    username, exception?.Message ?? "Normalno zatvaranje");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[CHATHUB]: Error disconnecting user {Username}.", username);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task<List<string>> GetMyPrivateChats()
    {
        var username = GetUsername();
        var userGrain = grainFactory.GetGrain<IUserGrain>(username);
        return await userGrain.GetActiveChats();
    }
}