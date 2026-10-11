using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using OrleansChat.Shared;

namespace OrleansChat.Grains;

public class PrivateChatGrain(
    [PersistentState("privateChatState", "PostgreSQLStore")] IPersistentState<PrivateChatState> state,
    ILogger<PrivateChatGrain> logger
    ) : Grain, IPrivateChatGrain
{
    public async Task<Response> SendPrivateMessage(string senderId, string receiverId, string text)
    {
        var senderGrain = GrainFactory.GetGrain<IUserGrain>(senderId);
        var receiverGrain = GrainFactory.GetGrain<IUserGrain>(receiverId);

        if (!await senderGrain.Exists() || !await receiverGrain.Exists())
        {
            return Response.Fail("One of the participants doesn't exist.", "PARTICIPANT_NOT_FOUND");
        }

        if (!state.RecordExists)
        {
            var users = this.GetPrimaryKeyString().Split('_');
            state.State = new PrivateChatState
            {
                Messages = new List<MessageDto>(),
                User1 = users[0],
                User2 = users[1]
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
            state.State.Messages.Add(msg);

            if (state.State.Messages.Count > 50)
            {
                state.State.Messages.RemoveAt(0);
            }

            await state.WriteStateAsync();

            logger.LogInformation("[DM CHAT]: {senderId} -> {receiverId}: {text}", senderId, receiverId, msg.Text);

            var userGrain = GrainFactory.GetGrain<IUserGrain>(receiverId);
            await userGrain.PrivateMessageReceived(this.GetPrimaryKeyString(), senderId, text);

            return Response.Ok("Message sent successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending DM in chat: {ChatId} by: {SenderId}", this.GetPrimaryKeyString(), senderId);
            return Response.Fail("System not available.", "SYSTEM_ERROR");
        }
    }

    public Task<List<MessageDto>> GetMessageHistory()
    {
        if (!state.RecordExists)
        {
            return Task.FromResult(new List<MessageDto>());
        }

        return Task.FromResult(state.State.Messages ?? new List<MessageDto>());
    }
}