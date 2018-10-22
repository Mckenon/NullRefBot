using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using RestSharp;
using NullRefBot.Utils;
using static NullRefBot.Bot;

namespace NullRefBot.RPG {
	public class ExperienceManager {
		static readonly Regex detectThanksRegex = new Regex( @"(?:\s|^)(thanks|thank you)(?:\s|$)", RegexOptions.IgnoreCase );
		static readonly StringBuilder messageBuilder = new StringBuilder();

		public struct InvalidExpReciever {
			public DiscordUser dUser;
			public string reason;
			public bool isSelf;

			public InvalidExpReciever ( DiscordUser user, string reason, bool isSelf ) {
				this.dUser = user;
				this.reason = reason;
				this.isSelf = isSelf;
			}
		}

		static async Task<T> ExecuteAsync<T> ( RestRequest req ) where T : new() {
			var client = new RestClient();
			client.BaseUrl = new Uri( Instance.Config.DatabaseIP );

			var response = await client.ExecuteTaskAsync<T>( req );

			if( response.ErrorException != null ) {
				const string message = "Error retrieving response.  Check inner details for more info.";
				var twilioException = new ApplicationException( message, response.ErrorException );
				throw twilioException;
			}

			return response.Data;
		}

		static Task<User> PostExpAsync ( DiscordUser dUser, int amount ) {
			var req = new RestRequest();
			req.Resource = "/users/{discord_id}/addexperience";
			req.RequestFormat = DataFormat.Json;
			req.Method = Method.PATCH;
			req.Timeout = 5 * 1000;

			req.AddParameter( "discord_id", dUser.Id, ParameterType.UrlSegment );

			req.AddBody( new { experience = amount } );

			Console.WriteLine( req.ToString() );

			return Task.Run( async () => {
				var user = await ExecuteAsync<User>( req );

				if( user == null ) {
					throw new Exception( "User is null" );
				}

				user.dUser = dUser;

				return user;
			} );
		}

		static Task<User> GetUserAsync ( DiscordUser dUser ) {
			var req = new RestRequest();
			req.Resource = "/users/{discord_id}";
			req.AddParameter( "discord_id", dUser.Id, ParameterType.UrlSegment );

			req.Method = Method.GET;
			req.RequestFormat = DataFormat.Json;
			req.Timeout = 5 * 1000;

			return Task.Run( () => ExecuteAsync<User>( req ) );
		}

		public static bool IsExpMessage ( DiscordMessage message ) {
			if( message.MentionedUsers.Count == 0 ) return false;
			return detectThanksRegex.IsMatch( message.Content );
		}

		static readonly DiscordUser[] singleUserArr = new DiscordUser[ 1 ];

		public static Task<int> GetExpAsync ( DiscordUser dUser ) {
			return Task.Run( async () => {
				var user = await GetUserAsync( dUser );

				return user.Experience;
			} );
		}

		public static Task<User> GiveExpAsync ( DiscordChannel channel, DiscordUser recipient, int amount ) {
			return Task.Run<User>( async () => {
				try {
					return await PostExpAsync( recipient, amount );
				} catch( Exception e ) {
					Console.WriteLine( e.ToString() );
					return null;
				}
			} );
		}

		public static Task<User[]> GiveExpAsync ( DiscordChannel channel, IList<DiscordUser> recipients, IList<int> amounts ) {
			return Task.Run<User[]>( async () => {
				var tasks = new Task<User>[ recipients.Count ];
				for( var i = 0; i < recipients.Count; i++ ) {
					var user = recipients[ i ];
					tasks[ i ] = PostExpAsync( user, amounts[ i ] );
				}

				try {
					return await Task.WhenAll( tasks );
				} catch( Exception e ) {
					Console.WriteLine( e.ToString() );
					return null;
				}
			} );
		}

		public static bool UserToUserGiveExpAndNotify ( DiscordChannel channel, DiscordUser author, IEnumerable<DiscordUser> recipients, int amount = 1 ) {
			var invalidUsers = new List<InvalidExpReciever>();
			var validUsers = new List<DiscordUser>();

			Console.WriteLine( author is DiscordUser );

			messageBuilder.Length = 0;

			foreach( var user in recipients ) {
				messageBuilder.Append( user ).Append( "," );

				if( user.IsBot ) {
					if( !invalidUsers.Exists( i => i.dUser == user ) ) invalidUsers.Add( new InvalidExpReciever( user, "bots don't need experience.", false ) );
					continue;
				} else if( user == author ) {
					if( !invalidUsers.Exists( i => i.dUser == user ) ) invalidUsers.Add( new InvalidExpReciever( user, "you cannot give experience to yourself!", true ) );
					continue;
				}
				if( !validUsers.Contains( user ) ) validUsers.Add( user );
			}

			Logger.LogMessage( LogLevel.Info, "Karma", $"{author} tries giving experience to {messageBuilder.ToString()}", DateTime.Now );

			TaskUtils.Run( async () => {
				var success = true;
				User[] retrievedUsers = null;
				if( validUsers.Count > 0 ) {
					var tasks = new Task<User>[ validUsers.Count ];
					for( var i = 0; i < tasks.Length; i++ ) {
						tasks[ i ] = PostExpAsync( validUsers[ i ], amount );
					}

					try {
						retrievedUsers = await Task.WhenAll( tasks );
					} catch( Exception e ) {
						Console.WriteLine( e.ToString() );
						success = false;
					}
				}

				if( success ) {
					await SendExpMessagesAsync( channel, author, retrievedUsers, invalidUsers );
				} else {
					await channel.SendMessageAsync( "Hmm... a goblin seems to have stolen all of my XP! Please try again later." );
				}
			} );

			return validUsers.Count > 0;
		}



		public static Task SendExpMessagesAsync ( DiscordChannel channel, DiscordUser author, IList<User> experienceRecievers, IList<InvalidExpReciever> invalidUsers ) {
			return Task.Run( async () => {
				await channel.TriggerTypingAsync();

				await SendGaveExpMessageAsync( channel, author, experienceRecievers );
				await SendInvalidExpMessageAsync( channel, author, invalidUsers );
			} );
		}

		public static Task SendGetExpMessageAsync ( DiscordChannel channel, IList<User> users ) {
			if( users.Count == 0 ) return Task.CompletedTask;

			return Task.Run( async () => {
				messageBuilder.Length = 0;
				foreach( var user in users ) {
					messageBuilder.AppendFormat( "  **{0}**\n", user.Experience );
				}

				Logger.LogMessage( LogLevel.Info, "Karma", messageBuilder.ToString(), DateTime.Now );

				await channel.SendMessageAsync( messageBuilder.ToString() );
			} );
		}

		public static Task SendGaveExpMessageAsync ( DiscordChannel channel, DiscordUser author, IList<User> experienceRecievers ) {
			if( experienceRecievers == null || experienceRecievers.Count == 0 ) return Task.CompletedTask;

			return Task.Run( async () => {
				messageBuilder.Length = 0;
				messageBuilder.AppendFormat( "{0} gave experience to:\n", author.Username );

				foreach( var user in experienceRecievers ) {
					messageBuilder.AppendFormat( "  **{0}**, {1}\n", user.dUser.Username, user.Experience );
				}

				Logger.LogMessage( LogLevel.Info, "Karma", messageBuilder.ToString(), DateTime.Now );

				await channel.SendMessageAsync( messageBuilder.ToString() );
			} );
		}

		static readonly string[] selfExpErrorMessages = new string[] {
			"What's the fun in giving yourself experience, **{0}**?",
			"Hey! Go earn your own experience, **{0}**!",
			"C'mon **{0}**, you can't possibly think I'd let you pilfer experience like that!",
		};

		public static Task SendInvalidExpMessageAsync ( DiscordChannel channel, DiscordUser author, IList<InvalidExpReciever> invalidUsers ) {
			if( invalidUsers == null || invalidUsers.Count == 0 ) return Task.CompletedTask;

			return Task.Run( async () => {
				messageBuilder.Length = 0;

				if( invalidUsers.Count > 1 ) {
					messageBuilder.Append( "Could not give experience to the following users:\n" );

					foreach( var user in invalidUsers ) {
						messageBuilder.AppendFormat( "  **{0}**, because {1}\n", user.dUser.Username, user.reason );
					}
				} else {
					var user = invalidUsers[ 0 ];
					if( user.isSelf ) messageBuilder.AppendFormat( selfExpErrorMessages[ RandomUtils.Range( 0, selfExpErrorMessages.Length ) ], user.dUser.Username );
					else messageBuilder.AppendFormat( "Could not give experience to **{0}** because {1}\n", user.dUser.Username, user.reason );
				}

				Instance.Client.DebugLogger.LogMessage( LogLevel.Info, "Karma", messageBuilder.ToString(), DateTime.Now );

				await channel.SendMessageAsync( messageBuilder.ToString() );
			} );
		}
	}
}
