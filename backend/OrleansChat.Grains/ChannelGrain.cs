using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using OrleansChat.Shared;

namespace OrleansChat.Grains;

public class ChannelGrain(
    [PersistentState("channelState", "PostgreSQLStore")] IPersistentState<ChannelState> state,
    ILogger<ChannelGrain> logger
    ) : Grain, IChannelGrain
{
    public async Task<Response> SendMessage(string senderId, string text)
    {
        if (!state.RecordExists)
        {
            state.State = new ChannelState
            {
                MessageCache = new List<MessageDto>(),
                ServerId = state.State.ServerId
            };
        }

        var msg = new MessageDto
        {
            SenderId = senderId,
            Text = text,
            Timestamp = DateTime.UtcNow
        };

        try
        {
            state.State.MessageCache.Add(msg);

            if (state.State.MessageCache.Count > 50)
            {
                state.State.MessageCache.RemoveAt(0);
            }

            await state.WriteStateAsync();

            logger.LogInformation("[PROCESUIRANO]: Message sent to channel: {ChannelId} by user: {SenderId}.", this.GetPrimaryKeyString(), senderId);

            if (!string.IsNullOrEmpty(state.State.ServerId))
            {
                var serverGrain = GrainFactory.GetGrain<IServerGrain>(state.State.ServerId);

                var memberUsernames = await serverGrain.GetMemberUsernames();

                var notificationTasks = memberUsernames
                    .Where(username => username != senderId)
                    .Select(username =>
                    {
                        var userGrain = GrainFactory.GetGrain<IUserGrain>(username);
                        return userGrain.ChannelMessageReceived(this.GetPrimaryKeyString(), senderId, text);
                    });
                await Task.WhenAll(notificationTasks);
            }

            return Response.Ok("Message sent successfully to channel");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending message to channel: {ChannelId} by user: {SenderId}", this.GetPrimaryKeyString(), senderId);

            if (state.State.MessageCache.Contains(msg))
            {
                state.State.MessageCache.Remove(msg);
            }
            return Response.Fail("System not available.", "SYSTEM_ERROR");
        }
    }

    public Task<List<MessageDto>> GetMessageHistory()
    {
        if (!state.RecordExists)
        {
            logger.LogWarning("[CHANNELGRAIN]:Attempted to get message history for non-existant channel: {ChannelId}", this.GetPrimaryKeyString());
            return Task.FromResult(new List<MessageDto>());
        }

        var cache = state.State.MessageCache ?? new List<MessageDto>();

        logger.LogDebug("[CHANNELGRAIN]:Returned message history for channel: {ChannelId}.", this.GetPrimaryKeyString());

        return Task.FromResult(cache);

    }



    public async Task<Response> DeleteChannel()
    {
        if (!state.RecordExists)
        {
            return Response.Ok("Channel already doesn't exist.");
        }
        try
        {
            await state.ClearStateAsync();

            DeactivateOnIdle();

            logger.LogInformation("Channel {ChannelId} deleted.", this.GetPrimaryKeyString());
            return Response.Ok("Channel deleted successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting channel {ChannelId}", this.GetPrimaryKeyString());
            return Response.Fail("System not available.", "SYSTEM_ERROR");
        }
    }
}