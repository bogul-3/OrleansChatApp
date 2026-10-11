using Microsoft.AspNetCore.SignalR;
using Orleans;
using OrleansChat.Shared;

namespace OrleansChat.Gateway;

public class AuthHub(IGrainFactory grainFactory, ILogger<AuthHub> logger) : Hub
{
    public async Task<Response> Register(string username, string password)
    {
        var trimmedUsername = username.Trim();
        if (string.IsNullOrWhiteSpace(trimmedUsername) || string.IsNullOrWhiteSpace(password))
        {
            return Response.Fail("Username or password can not be blank", "BLANK_USER_PASS");
        }

        try
        {
            var userGrain = grainFactory.GetGrain<IUserGrain>(trimmedUsername);
            return await userGrain.Register(password);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[AUTHHUB]:Error communicating to grain while registering user: {Username}", trimmedUsername);
            return Response.Fail("System is currently unavailable.", "SYSTEM_ERROR");
        }
    }

    public async Task<Response> Login(string username, string password)
    {
        var trimmedUsername = username?.Trim();
        if (string.IsNullOrWhiteSpace(trimmedUsername) || string.IsNullOrWhiteSpace(password))
            return Response.Fail("Username and password cannot be blank", "INVALID_INPUT");

        var userGrain = grainFactory.GetGrain<IUserGrain>(trimmedUsername);

        var response = await userGrain.Login(password);

        if (!response.Success)
        {
            return response;
        }

        try
        {
            Context.Items["Username"] = trimmedUsername;

            await Groups.AddToGroupAsync(Context.ConnectionId, trimmedUsername);

            await userGrain.UpdateStatus("Online", Context.ConnectionId);

            logger.LogInformation("[AUTHHUB]:Korisnik {Username} uspešno povezao sesiju.", trimmedUsername);
        }
        catch (Exception ex)
        { 
            logger.LogError(ex, "[AUTHHUB]:Login successful for user: {Username}, but error configuring user.", trimmedUsername);
        }
        return response;
    }

    public async Task<Response> Logout()
    {
        if (!Context.Items.TryGetValue("Username", out var usernameObj) || usernameObj is not string username)
            return Response.Fail("User is not logged in", "NOT_LOGGED_IN");

        try
        {
            var userGrain = grainFactory.GetGrain<IUserGrain>(username);
            var response = await userGrain.Logout();

            if (response.Success)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, username);

                Context.Items.Remove("Username");
            }

            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[AUTHHUB]:Error logging out user: {Username}", username);
            return Response.Fail("System is currently unavailable.", "SYSTEM_ERROR");
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (Context.Items.TryGetValue("Username", out var usernameObj) && usernameObj is string username)
        {
            var userGrain = grainFactory.GetGrain<IUserGrain>(username);
            await userGrain.UpdateStatus("Offline", null);
        }
        await base.OnDisconnectedAsync(exception);
    }
}