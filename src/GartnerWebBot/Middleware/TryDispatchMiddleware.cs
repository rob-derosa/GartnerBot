
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

public class TryDispatchMiddleware : IMiddleware
{
	string _conenctToAgent = "Would you like to connect to an analyst?";

	Dictionary<string, QnAMaker> _qnaMakerServices;
	Dictionary<string, QnAMaker> QnaMakerServices
	{
		get
		{
			if(_qnaMakerServices == null)
			{
				_qnaMakerServices = new Dictionary<string, QnAMaker>();

				var services = BotConfiguration.Services.Where(s => s.Type == ServiceTypes.QnA).Cast<QnAMakerService>();
				foreach(var svc in services)
				{
					var qnaEndpoint = new QnAMakerEndpoint()
					{
						KnowledgeBaseId = svc.KbId,
						EndpointKey = svc.EndpointKey,
						Host = svc.Hostname,
					};
		
					var service = new QnAMaker(qnaEndpoint);
					_qnaMakerServices.Add(svc.Name, service);
				}
			}

			return _qnaMakerServices;
		}
	}

	Dictionary<string, LuisRecognizer> _luisServices;
	Dictionary<string, LuisRecognizer> LuisServices
	{
		get
		{
			if (_luisServices == null)
			{
				_luisServices = new Dictionary<string, LuisRecognizer>();

				var services = BotConfiguration.Services.Where(s => s.Type == ServiceTypes.Luis).Cast<LuisService>();
				foreach (var svc in services)
				{
					var app = new LuisApplication(svc.AppId, svc.AuthoringKey, svc.GetEndpoint());
					var service = new LuisRecognizer(app);
					_luisServices.Add(svc.Name, service);
				}
			}

			return _luisServices;
		}
	}


	LuisRecognizer _dispatchService;
	LuisRecognizer DispatchService
	{
		get
		{
			if(_dispatchService == null)
			{
				var service = BotConfiguration.Services.FirstOrDefault(s => s.Name == FaqDispatchLuisKey) as LuisService;
				var app = new LuisApplication(service.AppId, service.AuthoringKey, service.GetEndpoint());
				_dispatchService = new LuisRecognizer(app);
			}

			return _dispatchService;
		}
	}
	//Value of the name key in the services section in the .bot file 
	private const string VendorBriefingsFaqQnaKey = "Gartner - Vendor Briefings";
	private const string MagicQuadrantFaqQnaKey = "Gartner - Magic Quadrant";
	private const string InformationDiscoveryLuisKey = "GartnerInformationDiscovery";
	private const string FaqDispatchLuisKey = "GartnerWebBotDispatch";

	public IConfiguration Configuration
	{
		get;
		protected set;
	}

	public BotConfiguration BotConfiguration
	{
		get;
		protected set;
	}

	public TryDispatchMiddleware(IConfiguration configuration, BotConfiguration botConfig)
	{
		Configuration = configuration;
		BotConfiguration = botConfig;
	}

	async public Task OnTurnAsync(ITurnContext context, NextDelegate next, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (context.Activity.Type == ActivityTypes.Message && !context.Responded)
		{
			//Get the intent recognition result
			var recognizerResult = await DispatchService.RecognizeAsync(context, cancellationToken);
			var topIntent = recognizerResult?.GetTopScoringIntent();

			if (topIntent == null)
			{
				await context.SendActivityAsync($"I'm not quite sure what you mean. {_conenctToAgent}");
			}
			else
			{
				await DispatchToTopIntentAsync(context, topIntent, cancellationToken);
			}
		}

		//await next(cancellationToken).ConfigureAwait(false);
	}

	private async Task DispatchToTopIntentAsync(ITurnContext context, (string intent, double score)? topIntent, CancellationToken cancellationToken = default(CancellationToken))
	{
		const string vendorFaqKey = "q_Gartner_-_Vendor_Briefings";
		const string magicQuadrantFaqKey = "q_Gartner_-_Magic_Quadrant";
		const string infoDiscoveryLuis = "l_GartnerInformationDiscovery";
		const string noneDispatchKey = "None";

		switch (topIntent.Value.intent)
		{
			case noneDispatchKey:
			// You can provide logic here to handle the known None intent (none of the above).
			// In this example we fall through to the QnA intent.
			case magicQuadrantFaqKey:
				await DispatchToQnAMakerAsync(context, MagicQuadrantFaqQnaKey);
				break;

			case vendorFaqKey:
				await DispatchToQnAMakerAsync(context, VendorBriefingsFaqQnaKey);
				break;

			case infoDiscoveryLuis:
				await DispatchToLuisAsync(context, InformationDiscoveryLuisKey);
				break;

			default:
				// The intent didn't match any case, so just display the recognition results.
				await context.SendActivityAsync($"I was unable to find an answer across all knowledge bases. {_conenctToAgent}");
				Console.WriteLine($"Dispatch intent: {topIntent.Value.intent} ({topIntent.Value.score}).");
				break;
		}
	}

	private async Task DispatchToLuisAsync(ITurnContext context, string luisApp, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (!string.IsNullOrEmpty(context.Activity.Text))
		{
			var results = await LuisServices[luisApp].RecognizeAsync(context, cancellationToken);
			if (results.Intents.Any())
			{
				var topIntent = results.GetTopScoringIntent();
				switch(topIntent.intent)
				{
					case "SearchForInformation" :

						var count = GetEntity<int>(results, "number");
						var topic = GetEntity<string>(results, "topic");
						var weight = GetEntity<string>(results, "weight");

						var msg = $"Ok, I will email you {count} reports about {topic} based and sorted on {weight}";
						await context.SendActivityAsync(msg, cancellationToken: cancellationToken);

						break;
				}
			}
			else
			{
				await context.SendActivityAsync($"I was unable to determine an intent. {_conenctToAgent}");
			}
		}
	}

	private async Task DispatchToQnAMakerAsync(ITurnContext context, string kbName, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (!string.IsNullOrEmpty(context.Activity.Text))
		{
			var results = await QnaMakerServices[kbName].GetAnswersAsync(context);
			if (results.Any())
			{
				await context.SendActivityAsync(results.First().Answer, cancellationToken: cancellationToken);
			}
			else
			{
				await context.SendActivityAsync($"I was unable to find an answer in the {kbName}. {_conenctToAgent}");
			}
		}
	}

	T GetEntity<T>(RecognizerResult luisResult, string entityKey)
	{
		var data = luisResult.Entities as IDictionary<string, JToken>;
		if (data.TryGetValue(entityKey, out JToken value))
		{
			return value.First.Value<T>();
		}
		return default(T);
	}
}