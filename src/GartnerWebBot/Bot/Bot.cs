using GartnerBot.CommandHandling;
using Microsoft.Bot;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GartnerBot.Bot
{
    public class Bot : IBot
    {
		async public Task OnTurnAsync(ITurnContext context, CancellationToken cancellationToken = default(CancellationToken))
		{
            Command showOptionsCommand = new Command(Commands.ShowOptions);

            HeroCard heroCard = new HeroCard()
            {
                Title = "Hello!",
                Subtitle = "I am Intermediator Bot",
                Buttons = new List<CardAction>()
                {
                    new CardAction()
                    {
                        Title = "Show options",
                        Value = showOptionsCommand.ToString(),
                        Type = ActionTypes.ImBack
                    }
                }
            };

            Activity replyActivity = context.Activity.CreateReply();
            replyActivity.Attachments = new List<Attachment>() { heroCard.ToAttachment() };
            await context.SendActivityAsync(replyActivity, cancellationToken);
        }
    }
}
