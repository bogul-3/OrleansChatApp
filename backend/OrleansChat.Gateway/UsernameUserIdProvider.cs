using Microsoft.AspNetCore.SignalR;

public class UsernameUserIdProvider : IUserIdProvider

{

    public string? GetUserId(HubConnectionContext connection)

    {

        return connection.GetHttpContext()?.Request.Query["username"];

    }

}

