using System;
using System.Collections.Generic;
using System.Text;

namespace OrleansChat.Shared;

[GenerateSerializer]
public class ServerState
{
    [Id(0)] public string Id { get; set; } = string.Empty;
    [Id(1)] public string Name { get; set; } = string.Empty;
    [Id(2)] public string OwnerUsername { get; set; } = string.Empty;
    [Id(3)] public List<string> MemberUsernames { get; set; } = new();
    [Id(4)] public List<string> ChannelIds { get; set; } = new();
}

[GenerateSerializer]
public class ChannelState
{
    [Id(0)] public string ServerId {  get; set; } = string.Empty;
    [Id(1)] public List<string> ActiveUserIds { get; set; } = new();
    [Id(2)] public List<MessageDto> MessageCache { get; set; } = new();
}

[GenerateSerializer]
public class UserState
{
    [Id(0)] public string Username { get; set; } = string.Empty;
    [Id(1)] public string PasswordHash {  get; set; } = string.Empty;
    [Id(2)] public List<string> OwnedServerIds { get; set; } = new();
    [Id(3)] public List<string> JoinedServerIds { get; set; } = new();
    [Id(4)] public string Status {  get; set; } = string.Empty;
    [Id(5)] public string? ConnectionId { get; set; }
    [Id(6)] public List<string> ActiveChatKeys { get; set; } = new();
}

[GenerateSerializer]
public class PrivateChatState
{
    [Id(0)] public List<MessageDto> Messages { get; set; } = new();
    [Id(1)] public string User1 { get; set; } = string.Empty;
    [Id(2)] public string User2 { get; set; } = string.Empty;
}

[GenerateSerializer]
public class MessageDto
{
    [Id(0)] public string SenderId { get; set; } = string.Empty;
    [Id(1)] public string Text { get; set; } = string.Empty;
    [Id(2)] public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

[GenerateSerializer]
public class ServerDto
{
    [Id(0)]public string Id { get; set; } = string.Empty;
    [Id(1)]public string Name { get; set; } = string.Empty;
    [Id(2)] public string Icon {  get; set; } = string.Empty;
    [Id(3)] public string AdminId { get; set; } = string.Empty;
     [Id(4)] public List<ChannelDto> Channels { get; set; } = new();
}

[GenerateSerializer]
public class ChannelDto
{
    [Id(0)] public string Id { get; set; } = string.Empty;
    [Id(1)] public string Name { get; set; } = string.Empty;
}

[GenerateSerializer]
public record Response(bool Success, string Message, string? ErrorCode = null)
{
    public static Response Ok(string message = "Uspešno") => new(true, message);
    public static Response Fail(string message, string? errorCode = null) => new(false, message, errorCode);
}
