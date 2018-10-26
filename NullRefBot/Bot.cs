using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Newtonsoft.Json;
using NullRefBot.Commands;
using NullRefBot.RPG;
using NullRefBot.Utils;
using RestSharp;

namespace NullRefBot
{
	public class Bot
	{
		public DiscordClient Client;
		public CommandsNextExtension Commands;
		public ConfigJson Config;
		public ConfigRolesJson RolesConfig;
		public RestClient RestClient;

		public static DebugLogger Logger => Instance.Client.DebugLogger;
		public static Bot Instance => instance ?? (instance = new Bot());
		private static Bot instance;

		public static readonly Task Done = Task.CompletedTask;

		private static Dictionary<DiscordMessage,ReactionTrigger> reactionTriggers = new Dictionary<DiscordMessage, ReactionTrigger>();

		public async Task RunAsync()
		{

			RestClient = new RestClient();

			string jsonString = "";
			using (var fs = File.OpenRead("config.json"))
			using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
				jsonString = await sr.ReadToEndAsync();

			Config = JsonConvert.DeserializeObject<ConfigJson>(jsonString);

			using (var fs = File.OpenRead("roles.json"))
			using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
				jsonString = await sr.ReadToEndAsync();
			RolesConfig = JsonConvert.DeserializeObject<ConfigRolesJson>(jsonString);


			var discordConfig = new DiscordConfiguration
			{
				Token = Config.Token,
				TokenType = TokenType.Bot,
				AutoReconnect = true,
				LogLevel = LogLevel.Info,
				UseInternalLogHandler = true
			};

			Client = new DiscordClient(discordConfig);
			Client.Ready += Client_Ready;
			Client.GuildAvailable += Client_GuildAvailable;
			Client.ClientErrored += Client_ClientError;
			Client.MessageCreated += Client_MessageCreated;
		    Client.GuildMemberAdded+= Client_GuildMemberAdded;
			Client.MessageReactionAdded += Client_MessageReactionAdded;
			Client.MessageReactionsCleared += Client_MessageReactionsCleared;

            var ccfg = new CommandsNextConfiguration()
			{
				StringPrefixes = new[] {Config.CommandPrefix},
				EnableDms = true,
				EnableMentionPrefix = true
			};

			Commands = Client.UseCommandsNext(ccfg);

			Commands.CommandExecuted += Commands_CommandExecuted;
			Commands.CommandErrored += Commands_CommandErrored;

			// TODO - Find something more elegant than grabbing a specific type

			Commands.RegisterCommands(Assembly.GetAssembly(typeof(TestCommands)));

			// TODO - Setup Help Formatter
			// this.Commands.SetHelpFormatter<HelpFormatter>();

			await Client.ConnectAsync();

			EncounterImporter.ImportFromXML( "testencounter.xml" );

			await Task.Delay(-1);
		}

		private Task Client_Ready(ReadyEventArgs e)
		{
			// let's log the fact that this event occured
			e.Client.DebugLogger.LogMessage(LogLevel.Info, "ExampleBot", "Client is ready to process events.", DateTime.Now);

			Bot.Instance.Client.UpdateStatusAsync(new DiscordActivity("with Unity", ActivityType.Playing), UserStatus.Online);

			// since this method is not async, let's return
			// a completed task, so that no additional work
			// is done
			return Done;
		}

		private Task Client_GuildMemberAdded(GuildMemberAddEventArgs e)
		{
		    const ulong WELCOME_CHANNEL_ID = 503022367442862094; //TODO: temp

			e.Client.DebugLogger.LogMessage(LogLevel.Info, "NRB", $"{e.Member.Username} joined", DateTime.Now);
		    e.Guild.GetChannel(WELCOME_CHANNEL_ID).SendMessageAsync($"A wild {e.Member.Mention} has appeared!");

            return Done;
		}

		private Task Client_MessageCreated(MessageCreateEventArgs e)
		{
			UpdateListener.CheckMessage(e);
			if ( e.Author.IsBot ) return Done; // explicitly ignore bot messages

			var isKarmaMessage = ExperienceManager.IsExpMessage( e.Message );

			var gaveKarma = false;
			if( isKarmaMessage ) {
				gaveKarma = ExperienceManager.UserToUserGiveExpAndNotify( e.Channel, e.Author, e.MentionedUsers );
			}

			return Done;
		}

		public ReactionTrigger AddReactionTrigger ( DiscordMessage message, DiscordUser user, ReactionTrigger.OnTriggeredEvent onTrigger = null ) {
			if( reactionTriggers.ContainsKey( message ) ) {
				Logger.LogMessage( LogLevel.Warning, "ReactionTrigger", "Can't add more than one trigger to a message at a time.", DateTime.Now );
				return null;
			}

			var trigger = new ReactionTrigger();
			trigger.message = message;
			trigger.userWhitelist = new[] { user };
			if( onTrigger != null ) trigger.onTriggered += onTrigger;

			reactionTriggers.Add( message, trigger );

			return trigger;
		}

		public ReactionTrigger AddReactionTrigger ( DiscordMessage message, params DiscordUser[] userWhitelist ) {
			if( reactionTriggers.ContainsKey( message ) ) {
				Logger.LogMessage( LogLevel.Warning, "ReactionTrigger", "Can't add more than one trigger to a message at a time.", DateTime.Now );
				return null;
			}

			var trigger = new ReactionTrigger();
			trigger.message = message;
			trigger.userWhitelist = userWhitelist;

			reactionTriggers.Add( message, trigger );

			return trigger;
		}

		internal void RemoveReactionTrigger ( DiscordMessage message ) {
			reactionTriggers.Remove( message );
		}

		private Task Client_MessageReactionAdded ( MessageReactionAddEventArgs e ) {
			if( e.User.IsBot ) return Done; // explicitly ignore bot messages

			if( reactionTriggers.TryGetValue( e.Message, out var trigger ) ) {
				if( trigger.TryTrigger( e.User, e.Emoji ) ) {
					if( trigger.oneShot ) {
						reactionTriggers.Remove( e.Message );
					}
				}
			}

			return Done;
		}

		private Task Client_MessageReactionsCleared ( MessageReactionsClearEventArgs e ) {
			if( reactionTriggers.Remove( e.Message ) ) {
				Logger.LogMessage( LogLevel.Info, "ReactionTrigger", "Removed extraneous reaction trigger as reactions were removed.", DateTime.Now );
			}

			return Done;
		}

		private Task Client_GuildAvailable(GuildCreateEventArgs e)
		{
			// let's log the name of the guild that was just
			// sent to our client
			e.Client.DebugLogger.LogMessage(LogLevel.Info, "ExampleBot", $"Guild available: {e.Guild.Name}", DateTime.Now);

			// since this method is not async, let's return
			// a completed task, so that no additional work
			// is done
			return Done;
		}

		private Task Client_ClientError(ClientErrorEventArgs e)
		{
			// let's log the details of the error that just 
			// occured in our client
			e.Client.DebugLogger.LogMessage(LogLevel.Error, "ExampleBot", $"Exception occured: {e.Exception.GetType()}: {e.Exception.Message}", DateTime.Now);

			// since this method is not async, let's return
			// a completed task, so that no additional work
			// is done
			return Done;
		}

		private Task Commands_CommandExecuted(CommandExecutionEventArgs e)
		{
			// let's log the name of the command and user
			e.Context.Client.DebugLogger.LogMessage(LogLevel.Info, "ExampleBot", $"{e.Context.User.Username} successfully executed '{e.Command.QualifiedName}'", DateTime.Now);

			// since this method is not async, let's return
			// a completed task, so that no additional work
			// is done
			return Done;
		}

		private async Task Commands_CommandErrored(CommandErrorEventArgs e)
		{
			// let's log the error details
			e.Context.Client.DebugLogger.LogMessage(LogLevel.Error, "ExampleBot", $"{e.Context.User.Username} tried executing '{e.Command?.QualifiedName ?? "<unknown command>"}' but it errored: {e.Exception.GetType()}: {e.Exception.Message ?? "<no message>"}", DateTime.Now);

			// let's check if the error is a result of lack
			// of required permissions
			if (e.Exception is ChecksFailedException ex)
			{
				// yes, the user lacks required permissions, 
				// let them know

				var emoji = DiscordEmoji.FromName(e.Context.Client, ":no_entry:");

				// let's wrap the response into an embed
				var embed = new DiscordEmbedBuilder
				{
					Title = "Access denied",
					Description = $"{emoji} You do not have the permissions required to execute this command.",
					Color = new DiscordColor(0xFF0000) // red
					// there are also some pre-defined colors available
					// as static members of the DiscordColor struct
				};
				await e.Context.RespondAsync("", embed: embed);
			}
		}
	}
}
