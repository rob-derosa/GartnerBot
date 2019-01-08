using GartnerBot.CommandHandling;
using Microsoft.Bot;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GartnerBot
{
    public class Bot : IBot
    {
		async public Task OnTurnAsync(ITurnContext context, CancellationToken cancellationToken = default(CancellationToken))
		{
			// switch(context.Activity.Type)
			// {
			// 	case ActivityTypes.ConversationUpdate :
			// 		var msg = "Hi! I'm Gartner Bot. You can ask me questions about vendor briefings or the magic quadrant process. I can also find reports for you if you ask me 'get me the top 5 reports about mobile development based on IDE'.";
			// 		await context.SendActivityAsync(msg);
			// 		break;
			// }
        }
    }
}
