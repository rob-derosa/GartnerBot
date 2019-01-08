using GartnerBot.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.BotFramework;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.Bot.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;

namespace GartnerBot
{
    public class Startup
    {
        public IConfiguration Configuration
        {
            get;
        }

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <param name="services"></param>
        public void ConfigureServices(IServiceCollection services)
        {
			var secretKey = Configuration["BotFileSecret"];
			var botFilePath = Configuration["BotFilePath"];
			var botConfig = BotConfiguration.Load(botFilePath, secretKey);
			
			services.AddMvc().AddControllersAsServices();
			services.AddSingleton(sp => botConfig ?? throw new InvalidOperationException($"The .bot configuration file could not be loaded. botFilePath: {botFilePath}"));

			services.AddSingleton(_ => Configuration);

            services.AddBot<GartnerBot.Bot>(options =>
            {
                options.CredentialProvider = new ConfigurationCredentialProvider(Configuration);

				options.OnTurnError = async (turnContext, exception) =>
				{
					Console.WriteLine(exception.ToString());
					var activity = MessageFactory.Text(exception.GetBaseException().Message);
					activity.ApplyConversationReference(turnContext.Activity.GetConversationReference());
					await turnContext.Adapter.SendActivitiesAsync(turnContext, new[] { activity }, default(CancellationToken));
				};

				// The Memory Storage used here is for local bot debugging only. When the bot
				// is restarted, anything stored in memory will be gone. 
				IStorage dataStore = new MemoryStorage();
				// var conversationState = new ConversationState(dataStore);
				// options.State.Add(conversationState);

				// Add middleware
				options.Middleware.Add(new TryDispatchMiddleware(Configuration, botConfig));
				options.Middleware.Add(new HandoffMiddleware(Configuration));

            });

            services.AddMvc(); // Required Razor pages
        }

        /// <summary>
        /// This method gets called by the runtime.
        /// Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseDefaultFiles()
                .UseStaticFiles()
                .UseMvc()
                .UseBotFramework();
        }
    }
}
