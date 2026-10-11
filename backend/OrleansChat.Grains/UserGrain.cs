using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using OrleansChat.Shared;

namespace OrleansChat.Grains;

public class UserGrain(
    [PersistentState("userState", "PostgreSQLStore")] IPersistentState<UserState> state,
    ILogger<UserGrain> logger
    ) : Grain, IUserGrain
{
    public async Task<Response> Register(string password)
    {
        if (state.RecordExists)
        {
            logger.LogWarning("[USERGRAIN]:User already exists: {Username}", state.State.Username);
            return Response.Fail("Username already exists", "USER_EXISTS");
        }

        try
        {
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

            state.State = new UserState
            {
                Username = this.GetPrimaryKeyString(),
                PasswordHash = passwordHash,
                Status = "Offline",
                OwnedServerIds = new List<string>(),
                JoinedServerIds = new List<string>()
            };

            await state.WriteStateAsync();

            logger.LogInformation("[USERGRAIN]:User {Username} registered successfully.", state.State.Username);
            return Response.Ok("Login successful");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error registering user: {Username}", this.GetPrimaryKeyString());
            return Response.Fail("[USERGRAIN]:System not available.", "SYSTEM_ERROR");
        }
    }

    public async Task<Response> Login(string password)
    {
        if (!state.RecordExists)
        {
            logger.LogWarning("[USERGRAIN]:User for login doesn't exist: {Username}", this.GetPrimaryKeyString());
            return Response.Fail("User doesn't exist", "USER_NOT_EXIST");
        }

        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(password, state.State.PasswordHash);
        if (!isPasswordValid)
        {
            logger.LogWarning("[USERGRAIN]:Wrong password for user: {Username}", state.State.Username);
            return Response.Fail("Wrong password", "WRONG_PASS");
        }

        try
        {
            state.State.Status = "Online";
            await state.WriteStateAsync();
            logger.LogInformation("[USERGRAIN]:User {Username} login successful.", state.State.Username);
            return Response.Ok("Login successful");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[USERGRAIN]:Error logging in user: {Username}", this.GetPrimaryKeyString());
            return Response.Fail("System not available.", "SYSTEM_ERROR");
        }
    }

    public async Task<Response> Logout()
    {
        if (!state.RecordExists)
        {
            logger.LogWarning("[USERGRAIN]:User {Username} for logout doesnt exist", this.GetPrimaryKeyString());
            return Response.Fail("Username doesn't exist", "USER_NOT_EXIST");
        }

        if (state.State.Status == "Offline" && state.State.ConnectionId == null)
        {
            logger.LogWarning("[USERGRAIN]:User {Username} is not logged in.", state.State.Username);
            return Response.Ok("User is not logged in");
        }

        try
        {
            state.State.Status = "Offline";
            state.State.ConnectionId = null;

            await state.WriteStateAsync();

            logger.LogInformation("[USERGRAIN]:User {Username} logged out.", state.State.Username);
            return Response.Ok("User logged out");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[USERGRAIN]:Error loggin out user: {Username}", this.GetPrimaryKeyString());
            return Response.Fail("System not available.", "SYSTEM_ERROR");
        }
    }

    public async Task<Response> UpdateStatus(string status, string? connectionId)
    {
        if (!state.RecordExists)
        {
            logger.LogWarning("[USERGRAIN]:User {Username} for status update doesn't exist", this.GetPrimaryKeyString());
            return Response.Fail("User doesn't exist", "USER_NOT_EXIST");
        }

        if (state.State.Status == status && state.State.ConnectionId == connectionId)
        {
            return Response.Ok("User status is already up to date");
        }

        try
        {
            state.State.Status = status;
            state.State.ConnectionId = connectionId;

            await state.WriteStateAsync();

            logger.LogInformation("[USERGRAIN]:Status updated for user: {Username}. Status: {Status}, ConnectionId: {ConnectionId}",
                state.State.Username, status, connectionId ?? "NULL");

            return Response.Ok("User status updated successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[USERGRAIN]:Error updating user {Username}'s status", this.GetPrimaryKeyString());
            return Response.Fail("System not available.", "SYSTEM_ERROR");
        }
    }

    public Task<bool> CanCreateServer() => Task.FromResult(state.State.OwnedServerIds.Count < 5); 

    public async Task<Response> AddOwnedServer(string serverId)
    {
        if (!state.RecordExists)
        {
            logger.LogWarning("[USERGRAIN]:Attempting to add server {ServerId} for non-existent user: {Username}",
                serverId, this.GetPrimaryKeyString());
            return Response.Fail("User doesn't exist", "USER_NOT_EXIST");
        }

        if (state.State.OwnedServerIds.Contains(serverId))
        {
            logger.LogWarning("[USERGRAIN]:User {Username} already owns the server: {ServerId}. ",
                state.State.Username, serverId);
            return Response.Ok("User already owns the server");
        }

        try
        {
            state.State.OwnedServerIds.Add(serverId);
            await state.WriteStateAsync();

            logger.LogInformation("[USERGRAIN]:User {Username} now owns server: {ServerId}.",
                state.State.Username, serverId);

            return Response.Ok("User now owns the server");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[USERGRAIN]:Error loading owned server list (new server: {ServerId}) for user: {Username}",
                serverId, this.GetPrimaryKeyString());

            state.State.OwnedServerIds.Remove(serverId);
            return Response.Fail("System not available.", "SYSTEM_ERROR");
        }
    }

    public async Task<Response> ReceiveInviteToServer(string serverId)
    {
        if (!state.RecordExists)
        {
            logger.LogWarning("[USERGRAIN]:Invite to {ServerId} for non-existent user: {Username}",
                serverId, this.GetPrimaryKeyString());
            return Response.Fail("User doesn't exist", "USER_NOT_EXIST");
        }

        if (state.State.JoinedServerIds.Contains(serverId))
        {
            logger.LogInformation("[USERGRAIN]:User {Username} is already a member of server: {ServerId}.",
                state.State.Username, serverId);
            return Response.Ok("User is already a member of the server");
        }

        try
        {
            state.State.JoinedServerIds.Add(serverId);

            await state.WriteStateAsync();

            logger.LogInformation("[USERGRAIN]:User {Username} is invited to server: {ServerId}.",
                state.State.Username, serverId);

            return Response.Ok("User added to server successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[USERGRAIN]:Error loading joined server (new server:{ServerId}) list for user {Username}",
                serverId, this.GetPrimaryKeyString());

            state.State.JoinedServerIds.Remove(serverId);
            return Response.Fail("System not available.", "SYSTEM_ERROR");
        }
    }

    public async Task ChannelMessageReceived(string channelId, string senderId, string text)
    {
        if (!state.RecordExists)
        {
            logger.LogWarning("[USERGRAIN]:Channel message received for non-existent user: {Username}", this.GetPrimaryKeyString());
            return;
        }

        logger.LogInformation("[USERGRAIN]:Channel message sent to user {Username}", state.State.Username);
        return;
    }

    public Task<List<string>> GetMyServers()
    {
        if (!state.RecordExists)
        {
            logger.LogWarning("[USERGRAIN]:Pokušaj dobijanja servera za nepostojećeg korisnika: {Username}", this.GetPrimaryKeyString());
            return Task.FromResult(new List<string>());
        }

        var ownedServers = state.State.OwnedServerIds ?? new List<string>();
        var joinedServers = state.State.JoinedServerIds ?? new List<string>();
        var allMyServers = ownedServers.Concat(joinedServers).Distinct().ToList();

        logger.LogInformation("[USERGRAIN]:Returned all servers for user: {Username}.",
            state.State.Username);

        return Task.FromResult(allMyServers);
    }

    public async Task<Response> RemoveServerReference(string serverId)
    {
        if (!state.RecordExists)
        {
            logger.LogWarning("[USERGRAIN]:Removing server reference for server: {ServerId} for non-existent user: {Username}",
                serverId, this.GetPrimaryKeyString());
            return Response.Fail("User doesn't exist", "USER_NOT_EXIST");
        }

        bool removedFromOwned = state.State.OwnedServerIds.Contains(serverId);
        bool removedFromJoined = state.State.JoinedServerIds.Contains(serverId);
        if (!removedFromOwned && !removedFromJoined)
        {
            logger.LogDebug("[USERGRAIN]:Server {ServerId} is not found in user: {Username}'s server list.",
                serverId, state.State.Username);
            return Response.Ok("User isn't a member of the server");
        }

        try
        {
            if (removedFromOwned) state.State.OwnedServerIds.Remove(serverId);
            if (removedFromJoined) state.State.JoinedServerIds.Remove(serverId);

            await state.WriteStateAsync();

            logger.LogInformation("[USERGRAIN]:Removed reference for server: {ServerId} from user: {Username}.",
                serverId, state.State.Username);

            return Response.Ok("Removed server reference successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[USERGRAIN]:Error writing removed server: {ServerId} for user: {Username}",
                serverId, state.State.Username);

            if (removedFromOwned) state.State.OwnedServerIds.Add(serverId);
            if (removedFromJoined) state.State.JoinedServerIds.Add(serverId);

            return Response.Fail("System not available.", "SYSTEM_ERROR");
        }
    }

    public Task<UserState> GetState() => Task.FromResult(state.State);

    public async Task PrivateMessageReceived(string privateChatId, string senderId, string text)
    {
        if (!state.RecordExists)
        {
            logger.LogWarning("[USERGRAIN]:Private message received for non-existent user: {Username}", this.GetPrimaryKeyString());
            return;
        }

        logger.LogInformation("[USERGRAIN]:Private message received by user {Username}", state.State.Username);
        return;
    }

    public async Task<Response> AddActiveChat(string chatKey)
    {
        if (!state.RecordExists)
        {
            logger.LogWarning("[USERGRAIN]:Attempting to add active chat {ChatKey} for non-existent user: {Username}",
                chatKey, this.GetPrimaryKeyString());
            return Response.Fail("User doesn't exist", "USER_NOT_EXIST");
        }

        if (state.State.ActiveChatKeys.Contains(chatKey))
        {
            return Response.Ok("Chat already in active list");
        }

        try
        {
            state.State.ActiveChatKeys.Add(chatKey);
            await state.WriteStateAsync();

            logger.LogInformation("[USERGRAIN]:Added active chat {ChatKey} for user: {Username}",
                chatKey, state.State.Username);

            return Response.Ok("Chat added successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[USERGRAIN]:Error adding active chat {ChatKey} for user: {Username}",
                chatKey, state.State.Username);

            state.State.ActiveChatKeys.Remove(chatKey);
            return Response.Fail("System not available.", "SYSTEM_ERROR");
        }
    }

    public Task<List<string>> GetActiveChats() => Task.FromResult(state.State.ActiveChatKeys);

    public async Task<bool> Exists() => await Task.FromResult(state.RecordExists);
}