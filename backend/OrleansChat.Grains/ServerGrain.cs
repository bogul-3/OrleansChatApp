using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using OrleansChat.Shared;
using System.Diagnostics;
using System.Threading.Channels;

namespace OrleansChat.Grains;

public class ServerGrain(
    [PersistentState("serverState", "PostgreSQLStore")] IPersistentState<ServerState> state,
    ILogger<ServerGrain> logger
    ) : Grain, IServerGrain
{
    public async Task<Response> AddChannel(string channelId)
    {
        if (!state.RecordExists)
        {
            logger.LogWarning("Attempted to add channel: {ChannelId} to non-existant server: {ServerId}", channelId, this.GetPrimaryKeyString());
            return Response.Fail("Server doesn't exist to add channel", "SERVER_NOT_EXIST");
        }

        if (state.State.ChannelIds.Contains(channelId))
        {
            logger.LogWarning("Channel: {ChannelId} already exists on server: {ServerId}.", channelId, this.GetPrimaryKeyString());
            return Response.Ok("Channel already exists");
        }

        try
        {
            state.State.ChannelIds.Add(channelId);

            await state.WriteStateAsync();

            var channelGrain = GrainFactory.GetGrain<IChannelGrain>(channelId);
            string channelName = channelId.Split('_').Last();

            await channelGrain.SendMessage("System", $"Welcome to {channelName}");

            logger.LogInformation("User {Owner} added channel: {ChannelId} to server: {ServerId}.", state.State.OwnerUsername, channelId, this.GetPrimaryKeyString());
            return Response.Ok("Success adding channel");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error writing channel: {ChannelId} added to server: {ServerId}", channelId, this.GetPrimaryKeyString());

            state.State.ChannelIds.Remove(channelId);
            return Response.Fail("System not available.", "SYSTEM_ERROR");
        }
    }

    public async Task<Response> CreateServer(string name, string ownerUsername)
    {
        if (state.RecordExists)
        {
            logger.LogWarning("Attempted to create a server that already exists: {ServerId}", this.GetPrimaryKeyString());
            return Response.Fail("Server already exists", "SERVER_EXISTS");
        }

        try
        {
            state.State = new ServerState
            {
                Id = this.GetPrimaryKeyString(),
                Name = name,
                OwnerUsername = ownerUsername,
                MemberUsernames = new List<string> { ownerUsername },
                ChannelIds = new List<string> { $"c_{this.GetPrimaryKeyString()}_general" }
            };

            await state.WriteStateAsync();

            var generalChannelId = $"c_{this.GetPrimaryKeyString()}_general";
            var channelGrain = GrainFactory.GetGrain<IChannelGrain>(generalChannelId);

            await channelGrain.SendMessage("System", "Welcome to general");

            logger.LogInformation("Created server: {ServerName} by user: {Owner}.", name, ownerUsername);

            return Response.Ok("Created server successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error writing created server: '{ServerName}' (ID: {ServerId})", name, this.GetPrimaryKeyString());

            state.State = new ServerState();
            return Response.Fail("System not available.", "SYSTEM_ERROR");
        }
    }

    public async Task<Response> DeleteServer(string requestingUsername)
    {
        if (!state.RecordExists)
        {
            logger.LogWarning("Attempted to delete non-existent server: {ServerId}", this.GetPrimaryKeyString());
            return Response.Fail("Server doesn't exist to delete", "SERVER_NOT_EXIST");
        }
        if (state.State.OwnerUsername != requestingUsername)
        {
            logger.LogWarning("UNAUTHORIZED DELETE ATTEMPTED: User {Username} has attempted to delete server {ServerId}, but the owner is {Owner}.",
                requestingUsername, this.GetPrimaryKeyString(), state.State.OwnerUsername);
            return Response.Fail("Unauthorized delete attempt", "UNAUTHORIZED");
        }

        var serverId = this.GetPrimaryKeyString();
        var membersToClean = new List<string>(state.State.MemberUsernames ?? []);

        try
        {
            if (state.State.ChannelIds != null)
            {
                foreach (var channelId in state.State.ChannelIds)
                {
                    var channelGrain = GrainFactory.GetGrain<IChannelGrain>(channelId);
                    await channelGrain.DeleteChannel();
                }
            }
            foreach (var username in membersToClean)
            {
                try
                {
                    var userGrain = GrainFactory.GetGrain<IUserGrain>(username);
                    await userGrain.RemoveServerReference(serverId);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to remove server reference for user: {Username} while deleting server: {ServerId}",
                        username, serverId);
                }
            }

            await state.ClearStateAsync();

            state.State = new ServerState();
            DeactivateOnIdle();

            logger.LogInformation("Server '{ServerId}' Is deleted by owner: {Owner}.",
                serverId, requestingUsername);

            return Response.Ok("Deleted server successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while deleting {ServerId} from db.", serverId);
            return Response.Fail("System not available.", "SYSTEM_ERROR");
        }
    }

    public Task<string> GetOwnerUsername() => Task.FromResult(state.State.OwnerUsername);

    public Task<List<string>> GetChannels() => Task.FromResult(state.State.ChannelIds);

    public Task<string> GetName() => Task.FromResult(state.State.Name ?? "Unknown Server");

    public Task<List<string>> GetMemberUsernames() => Task.FromResult(state.State.MemberUsernames);

    public async Task<Response> InviteUser(string requestingUsername, string targetUsername)
    {
        if (!state.RecordExists)
        {
            logger.LogWarning("Attempted to invite user: {Target} to non-existent server: {ServerId}",
                targetUsername, this.GetPrimaryKeyString());
                return Response.Fail("Server doesn't exist to invite user", "SERVER_NOT_EXIST");
        }

        bool isOwner = state.State.OwnerUsername == requestingUsername;
        bool isMember = state.State.MemberUsernames.Contains(requestingUsername);

        if (!isOwner && !isMember)
        {
            logger.LogWarning("UNAUTHORISED INVITE: User {Requester} tried to invite {Target} to server {ServerId}, but isn't the owner.",
                requestingUsername, targetUsername, this.GetPrimaryKeyString());
            return Response.Fail("Unauthorized invite attempt", "UNAUTHORIZED");
        }

        if (state.State.MemberUsernames.Contains(targetUsername))
        {
            logger.LogDebug("User {Target} is already a member of server: {ServerId}.",
                targetUsername, this.GetPrimaryKeyString());
            return Response.Ok("User is already a member");
        }

        var targetUserGrain = GrainFactory.GetGrain<IUserGrain>(targetUsername);

        if (!await targetUserGrain.Exists())
        {
            logger.LogWarning("Attempted to invite non-existent user: {Target}", targetUsername);
            return Response.Fail("User Doesn't exist", "USER_NOT_FOUND");
        }

        var serverId = this.GetPrimaryKeyString();
        try
        {
            await targetUserGrain.ReceiveInviteToServer(serverId);

            state.State.MemberUsernames.Add(targetUsername);

            await state.WriteStateAsync();

            logger.LogInformation("User {Requester} has added {Target} to server '{ServerName}'.",
                requestingUsername, targetUsername, state.State.Name);

            return Response.Ok("User invited successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding {Target} to server {ServerId}",
                targetUsername, serverId);

            if (state.State.MemberUsernames.Contains(targetUsername))
            {
                state.State.MemberUsernames.Remove(targetUsername);
            }
            return Response.Fail("System not available.", "SYSTEM_ERROR");
        }
    }


    public async Task<ServerDto> GetServerDetails()
    {
        if (!state.RecordExists)
        {
            logger.LogWarning("Attempted to get details for non-existent server: {ServerId}", this.GetPrimaryKeyString());
            return new ServerDto { };
        }

        return new ServerDto
        {
            Id = state.State.Id,
            Name = state.State.Name,
            AdminId = state.State.OwnerUsername,
            Icon = state.State.Name.Substring(0, Math.Min(2, state.State.Name.Length)).ToUpper(),
            Channels = state.State.ChannelIds.Select(channelId => new ChannelDto
            {
                Id = channelId,
                Name = channelId.Split('_')[2]
            }).ToList()
        };
    }

    public async Task<Response> RemoveChannel(string channelId)
    {
        if (!state.RecordExists)
        {
            logger.LogWarning("Attempted to remove channel from non-existent server: {ServerId}", this.GetPrimaryKeyString());
            return Response.Fail("Server doesn't exist", "SERVER_NOT_EXIST");
        }

        if (state.State.ChannelIds == null || !state.State.ChannelIds.Contains(channelId))
        {
            logger.LogWarning("Attempted to remove non-existent channel {ChannelId} from server {ServerId}",
                channelId, this.GetPrimaryKeyString());
            return Response.Fail("Channel not found in server", "CHANNEL_NOT_FOUND");
        }

        try
        {
            state.State.ChannelIds.Remove(channelId);

            await state.WriteStateAsync();

            logger.LogInformation("Channel {ChannelId} successfully removed from server {ServerId}",
                channelId, this.GetPrimaryKeyString());

            return Response.Ok("Channel removed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while removing channel {ChannelId} from server {ServerId}",
                channelId, this.GetPrimaryKeyString());
            return Response.Fail("System not available.", "SYSTEM_ERROR");
        }
    }
}