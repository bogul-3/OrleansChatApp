using Microsoft.AspNetCore.SignalR;
using Orleans;
using OrleansChat.Shared;

namespace OrleansChat.Gateway;

public class ServerHub(IGrainFactory grainFactory, ILogger<ServerHub> logger) : Hub
{
    private string GetUsername()
    {
        var queryUser = Context.GetHttpContext()?.Request.Query["username"].ToString();
        if (!string.IsNullOrEmpty(queryUser)) return queryUser;

        if (Context.Items.TryGetValue("Username", out var usernameObj) && usernameObj is string username)
        {
            return username;
        }

        throw new HubException("User Not Authorized.");
    }

   public async Task<List<ServerDto>> GetMyServers()
    {
        string username = GetUsername();
        var userGrain = grainFactory.GetGrain<IUserGrain>(username);

        var serverIds = await userGrain.GetMyServers();

        var serverTasks = serverIds.Select(async id =>
        {
            var serverGrain = grainFactory.GetGrain<IServerGrain>(id);
            var details = await serverGrain.GetServerDetails();

            await Groups.AddToGroupAsync(Context.ConnectionId, id);

            return new ServerDto
            {
                Id = details.Id,
                Name = details.Name,
                AdminId = details.AdminId,
                Icon = details.Name.Substring(0, Math.Min(2, details.Name.Length)).ToUpper(),
                Channels = details.Channels.Select(c => new ChannelDto
                {
                    Id = c.Id,
                    Name = c.Name
                }).ToList()
            };
        });

        var result = await Task.WhenAll(serverTasks);

        return result.ToList();
    }

    public async Task<Response> CreateServer(string serverName)
    {
        string username = GetUsername();
        if (string.IsNullOrWhiteSpace(username))
            return Response.Fail("Not logged in", "NOT_LOGGED_IN");

        string serverId = Guid.NewGuid().ToString().Substring(0, 8);
        var serverGrain = grainFactory.GetGrain<IServerGrain>(serverId);

        var createResponse = await serverGrain.CreateServer(serverName, username);
        if (!createResponse.Success) return createResponse;

        try
        {
            var userGrain = grainFactory.GetGrain<IUserGrain>(username);

            if (!await userGrain.CanCreateServer())
                return Response.Fail("You have reached a maximum of 5 owned servers.", "LIMIT_REACHED");

            var addOwnedResponse = await userGrain.AddOwnedServer(serverId);
            if (!addOwnedResponse.Success) throw new Exception("Failed to link server");

            await Groups.AddToGroupAsync(Context.ConnectionId, serverId);
            await Clients.User(username).SendAsync("ServerAdded", await serverGrain.GetServerDetails());
            return Response.Ok(serverId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[SERVERHUB]:Rollback: Deleting server {ServerId}.", serverId);

            await serverGrain.DeleteServer(username);

            return Response.Fail("System not available.", "SYSTEM_ERROR");
        }
    }

    public async Task<Response> InviteToServer(string serverId, string targetUsername)
    {
        string requester = GetUsername();
        if (string.IsNullOrWhiteSpace(requester))
            return Response.Fail("Not logged in", "NOT_LOGGED_IN");


        var serverGrain = grainFactory.GetGrain<IServerGrain>(serverId);
        var response = await serverGrain.InviteUser(requester, targetUsername);


        if (response.Success)
        {
            try
            {
                var serverDetails = await serverGrain.GetServerDetails();

                await Clients.User(targetUsername).SendAsync("ServerAdded", serverDetails);

                logger.LogInformation("[SERVERHUB]:Invite sent to user: {Target} for server {ServerId}", targetUsername, serverId);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[SERVERHUB]:Invite not sent to user: {Target}", targetUsername);
            }
        }

        return response;
    }

    public async Task<Response> AddChannel(string serverId, string channelName)
    {
        var currentUsername = GetUsername();
        if (string.IsNullOrWhiteSpace(currentUsername))
            return Response.Fail("Not logged in", "NOT_LOGGED_IN");

        if (string.IsNullOrWhiteSpace(channelName))
            return Response.Fail("Naziv kanala ne može biti prazan.", "INVALID_INPUT");

        var serverGrain = grainFactory.GetGrain<IServerGrain>(serverId);

        string adminId = await serverGrain.GetOwnerUsername();
        if (currentUsername != adminId)
        {
            return Response.Fail("Nemate administratorska prava.", "UNAUTHORIZED");
        }

        string channelId = $"c_{serverId}_{channelName.ToLower().Trim()}";
        var response = await serverGrain.AddChannel(channelId);

        if (response.Success)
        {
            await Clients.Group(serverId).SendAsync("ChannelAdded", new
            {
                serverId = serverId,
                channel = new
                {
                    id = channelId,
                    name = channelName,
                }
            });
        }

        return response;
    }

    public async Task<Response> DeleteServer(string serverId)
    {
        var currentUsername = GetUsername();
        if (string.IsNullOrWhiteSpace(currentUsername))
            return Response.Fail("Not logged in", "NOT_LOGGED_IN");
        var serverGrain = grainFactory.GetGrain<IServerGrain>(serverId);

        string owner = await serverGrain.GetOwnerUsername();
        if (currentUsername != owner)
            return Response.Fail("Nemate administratorska prava.", "UNAUTHORIZED");

        var response = await serverGrain.DeleteServer(currentUsername);

        if (response.Success)
        {
            await Clients.Group(serverId).SendAsync("ServerDeleted", serverId);
        }

        return response;
    }

    public override async Task OnConnectedAsync()
    {
        if (Context.User?.Identity?.Name is not { } username)
        {
            logger.LogWarning("[SERVERHUB]:Connecting without authenticated user: {ConnectionId}", Context.ConnectionId);
            await base.OnConnectedAsync();
            return;
        }

        try
        {
            Context.Items["Username"] = username;

            var userGrain = grainFactory.GetGrain<IUserGrain>(username);

            await userGrain.UpdateStatus("Online", Context.ConnectionId);

            var serverIds = await userGrain.GetMyServers();

            foreach (var serverId in serverIds)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, serverId);
            }

            logger.LogInformation("[SERVERHUB]:User {Username} connected successfully.", username);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[SERVERHUB]:Error initializing connection for user: {Username}.", username);
        }

        await base.OnConnectedAsync();
    }

    public async Task<Response> DeleteChannel(string serverId, string channelId)
    {
        var channelGrain = grainFactory.GetGrain<IChannelGrain>(channelId);
        var deleteResponse = await channelGrain.DeleteChannel();

        if (!deleteResponse.Success) return deleteResponse;

        var serverGrain = grainFactory.GetGrain<IServerGrain>(serverId);
        await serverGrain.RemoveChannel(channelId);

        await Clients.Group(serverId).SendAsync("ChannelDeleted", new
        {
            serverId = serverId,
            channelId = channelId
        });

        return Response.Ok("Channel deleted");
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var username = Context.Items["Username"] as string;
        if (!string.IsNullOrEmpty(username))
        {
            var userGrain = grainFactory.GetGrain<IUserGrain>(username);
            await userGrain.UpdateStatus("Offline", null);
        }
        await base.OnDisconnectedAsync(exception);
    }
}