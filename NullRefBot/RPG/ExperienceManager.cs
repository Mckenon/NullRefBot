using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using RestSharp;
using NullRefBot.Utils;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using static NullRefBot.Bot;
using System.Timers;

namespace NullRefBot.RPG {
	public class ReactionTrigger {
		public delegate void OnTriggeredEvent ( DiscordUser dUser, DiscordEmoji selectedEmoji );

		public DiscordMessage message;
		public DiscordUser[] userWhitelist;
		public bool oneShot;

		public event OnTriggeredEvent onTriggered;

		public void Trigger ( DiscordUser user, DiscordEmoji emoji ) {
			if( onTriggered != null ) onTriggered( user, emoji );
		}

		public bool TryTrigger ( DiscordUser dUser, DiscordEmoji emoji ) {
			if( userWhitelist == null ) {
				if( onTriggered != null ) onTriggered( dUser, emoji );
				return true;
			}
			for( int i = 0; i < userWhitelist.Length; i++ ) {
				if( userWhitelist[ i ] == dUser ) {
					if( onTriggered != null ) onTriggered( dUser, emoji );
					return true;
				}
			}

			return false;
		}

		public override int GetHashCode () {
			return message.GetHashCode();
		}
	}

	public class EncounterLoot {
		public int experience;
		public int gold;

		public EncounterLoot ( int experience, int gold = 0 ) {
			this.experience = experience;
			this.gold = gold;
		}
	}

	public class EncounterResult {
		public Encounter encounter;
		public EncounterLoot loot;
		public string title;
		public string text;

		public bool showTitle;

		public EncounterResult ( Encounter encounter ) {
			this.encounter = encounter;
			this.showTitle = true;
		}

		public EncounterResult ( EncounterLoot loot, string title = null, string text = null, bool showTitle = true ) {
			this.loot = loot;
			this.title = title;
			this.text = text;
			this.showTitle = showTitle;
		}

		public EncounterResult ( string title = null, string text = null, bool showTitle = true ) {
			this.title = title;
			this.text = text;
			this.showTitle = showTitle;
		}

		internal void Execute ( DiscordChannel channel, DiscordUser user ) {
			if( encounter != null ) {
				EncounterManager.SpawnEncounter( channel, user, encounter );
				return;
			}

			var resTitle = title;
			var resText = text;

			if( loot != null ) {
				ExperienceManager.GiveExpAsync( channel, user, loot.experience );

				if( resTitle == null ) resTitle = "Loot Get!";
				resText = string.Format( "{0}\n{1}", resText, $"You gained {loot.experience} experience from this encounter." );
			}

			channel.SendMessageAsync( embed: DiscordEmbedUtils.MakeEmbed( showTitle ? resTitle : null, resText, author: user ) );
		}
	}

	public class EncounterOption {
		public string description;
		public DiscordEmoji emojiOverride;

		public EncounterResult result;

		public EncounterOption ( string description, EncounterResult result, string emojiOverrideName = null ) {
			this.description = description;
			this.result = result;
			if( emojiOverrideName != null ) {
				emojiOverride = DiscordEmoji.FromName( Instance.Client, emojiOverrideName );
			}
		}

		public EncounterOption ( string description, EncounterResult result, DiscordEmoji emojiOverride ) {
			this.description = description;
			this.result = result;
			this.emojiOverride = emojiOverride;
		}
	}

	public class Encounter {
		public string title;
		public string text;
		public string author;

		public bool listOptions;

		public EncounterOption[] options;

		public DiscordEmbed ToEmbed ( DiscordUser owner ) {
			var builder = new DiscordEmbedBuilder();

			builder.Title = title;
			builder.Footer = new DiscordEmbedBuilder.EmbedFooter();
			builder.Author = DiscordEmbedUtils.MakeUserAuther( owner );
			//if( owner != null ) builder.Footer.Text = $"This encounter can only be completed by {owner.Username}";

			if( listOptions ) builder.Description = string.Format( "{0}\n\n{1}", text, CreateOptionsString() );
			else builder.Description = text;

			return builder;
		}

		private string CreateOptionsString () {
			var builder = new StringBuilder();

			for( int i = 0; i < options.Length; i++ ) {
				var option = options[ i ];

				builder.Append( option.emojiOverride.GetDiscordName() ).Append( " **" ).Append( option.description ).Append( "**" );
				builder.Append( "\n" );
			}

			return builder.ToString();
		}

		public EncounterOption GetOptionFromEmoji ( DiscordEmoji emoji ) {
			for( int i = 0; i < options.Length; i++ ) {
				var option = options[ i ];


				if( option.emojiOverride == null ) {
					if( EmojiUtils.emojiNumbers[ i ] == emoji ) {
						return option;
					}
				} else if( option.emojiOverride == emoji ) {
					return option;
				}
			}

			return null;
		}
	}

	public class EncounterCommands : BaseCommandModule {

		[Command( "encounter" )]
		public async Task BeginEncounter ( CommandContext c ) {
			await c.TriggerTypingAsync();

			var encounter = EncounterManager.TrySpawnEncounterForUser( c.Member );

			if( encounter != null ) {
				await EncounterManager.SpawnEncounter( c.Channel, c.User, encounter );
			} else {
				await c.RespondAsync( embed: DiscordEmbedUtils.MakeEmbed( text: $"**{c.Member.DisplayName}** searched but nothing interesting seems to be happening at the moment..." ) );
			}
		}

		[Command( "testlootduel" )]
		public async Task CreateTestLootDuel ( CommandContext c ) {
			var message = await c.RespondAsync( embed: DiscordEmbedUtils.MakeEmbed( ":moneybag: Grab the Loot!", "First to grab the loot gets it! Beware of the decoys, you only get one shot!" ) );

			var userBlacklist = new HashSet<DiscordUser>();

			var timer = new Timer();
			timer.Interval = RandomUtils.Range( 1.0, 5.0 ) * 1000;
			timer.Start();

			var trigger = Instance.AddReactionTrigger( message, null );
			trigger.onTriggered += ( user, emoji ) => {
				if( emoji == EmojiUtils.moneybag && !userBlacklist.Contains( user ) ) {
					Instance.RemoveReactionTrigger( message );
					Task.Run( async () => {
						timer.Stop();
						//await message.DeleteAllReactionsAsync();
						//await message.ModifyAsync( embed: DiscordEmbedUtils.MakeTextEmbed( text: $"**{user.Username}** got the loot!" ) );
						var task = message.DeleteAsync();
						await c.RespondAsync( embed: DiscordEmbedUtils.MakeEmbed( text: $":moneybag: **{user.Username}** got the loot!", author: user ) );
					} );
				} else if( userBlacklist.Add( user ) ) {
					Task.Run( async () => {
						//await message.ModifyAsync( embed: DiscordEmbedUtils.MakeTextEmbed( text: $"**{user.Username}** got the loot!" ) );
						//var task = message.DeleteAsync();
						await c.RespondAsync( embed: DiscordEmbedUtils.MakeEmbed( text: $":skull_crossbones: missed the loot!", author: user ) );
					} );
				}
			};

			var numIcons = EmojiUtils.lootIcons.Length;
			var lootIcons = new DiscordEmoji[ numIcons ];

			for( int i = 0; i < numIcons; i++ ) {
				lootIcons[ i ] = EmojiUtils.lootIcons[ RandomUtils.Range( 0, numIcons ) ];
			}

			int currentIcon = 0;
			timer.Elapsed += ( sender, e ) => {
				if( currentIcon < numIcons ) {
					message.CreateReactionAsync( lootIcons[ currentIcon ] );
					currentIcon++;
				} else {
					timer.Stop();
					Instance.RemoveReactionTrigger( message );
					Task.Run( async () => {
						//await message.ModifyAsync( embed: DiscordEmbedUtils.MakeTextEmbed( text: $"You all missed the loot! Better luck next time." ) );
						//await message.DeleteAllReactionsAsync();
						var task = message.DeleteAsync();
						await c.RespondAsync( embed: DiscordEmbedUtils.MakeEmbed( text: $"You all missed the loot! Better luck next time." ) );
					} );
				}
				timer.Interval = RandomUtils.Range( 1.0, 5.0 ) * 1000;
			};
		}

		[Command( "testloot" )]
		public async Task CreateTestLoot ( CommandContext c ) {
			var message = await c.RespondAsync( embed: DiscordEmbedUtils.MakeEmbed( ":moneybag: Grab the Loot!", "First to grab the loot gets it!" ) );

			var userBlacklist = new HashSet<DiscordUser>();

			var trigger = Instance.AddReactionTrigger( message, null );
			trigger.onTriggered += ( user, emoji ) => {
				Task.Run( async () => {
					await message.DeleteAllReactionsAsync();
					await message.ModifyAsync( embed: DiscordEmbedUtils.MakeEmbed( text: $":moneybag: **{user.Username}** got the loot!", author: user ) );
					//var task = message.DeleteAsync();
					//await c.RespondAsync( embed: DiscordEmbedUtils.MakeEmbed( text: $":moneybag: **{user.Username}** got the loot!", author: user ) );
				} );
			};

			await message.CreateReactionAsync( EmojiUtils.moneybag );
		}
	}

	public class DiscordEmbedUtils {
		public static DiscordEmbed MakeEmbed( string title = null, string text = null, string footer = null, DiscordUser author = null ) {
			var embed = new DiscordEmbedBuilder();

			embed.Title = title;
			embed.Description = text;
			if( author != null ) {
				embed.Author = MakeUserAuther( author );
			}
			if( footer != null ) {
				embed.Footer = new DiscordEmbedBuilder.EmbedFooter();
				embed.Footer.Text = footer;
			}

			return embed;
		}

		internal static DiscordEmbedBuilder.EmbedAuthor MakeUserAuther ( DiscordUser user ) {
			var author = new DiscordEmbedBuilder.EmbedAuthor();
			author.Name = user.Username;
			author.IconUrl = user.GetAvatarUrl( ImageFormat.Png, 16 );
			return author;
		}
	}

	public class EmojiUtils {

		public static readonly DiscordEmoji moneybag = DiscordEmoji.FromName( Instance.Client, ":moneybag:" );

		public static readonly DiscordEmoji[] lootIcons = new[] {
			DiscordEmoji.FromName( Instance.Client, ":tropical_fish:" ),
			DiscordEmoji.FromName( Instance.Client, ":sun_with_face:" ),
			DiscordEmoji.FromName( Instance.Client, ":tangerine:" ),
			DiscordEmoji.FromName( Instance.Client, ":icecream:" ),
			DiscordEmoji.FromName( Instance.Client, ":trophy:" ),
			DiscordEmoji.FromName( Instance.Client, ":dvd:" ),
			moneybag,
		};

		public static readonly DiscordEmoji[] emojiNumbers = new[] {
			DiscordEmoji.FromName( Instance.Client, ":one:" ),
			DiscordEmoji.FromName( Instance.Client, ":two:" ),
			DiscordEmoji.FromName( Instance.Client, ":three:" ),
			DiscordEmoji.FromName( Instance.Client, ":four:" ),
			DiscordEmoji.FromName( Instance.Client, ":five:" ),
			DiscordEmoji.FromName( Instance.Client, ":six:" ),
			DiscordEmoji.FromName( Instance.Client, ":seven:" ),
			DiscordEmoji.FromName( Instance.Client, ":eight:" ),
			DiscordEmoji.FromName( Instance.Client, ":nine:" ),
			DiscordEmoji.FromName( Instance.Client, ":keycap_ten:" )
		};

		public static readonly DiscordEmoji ArrowUp = DiscordEmoji.FromName( Instance.Client, ":arrow_up:" );
		public static readonly DiscordEmoji ArrowRight = DiscordEmoji.FromName( Instance.Client, ":arrow_right:" );
		public static readonly DiscordEmoji ArrowDown = DiscordEmoji.FromName( Instance.Client, ":arrow_down:" );
		public static readonly DiscordEmoji ArrowLeft = DiscordEmoji.FromName( Instance.Client, ":arrow_left:" );
	}

	public class EncounterManager {

		public static Encounter TrySpawnEncounterForUser ( DiscordMember member ) {
			var subEncounter = new Encounter();
			subEncounter.title = "Suddenly and Without Warning";

			subEncounter.text =
@"As soon as you push the **white** button, the floor silently opens up beneath you! You fall in a rather silly manner, your arms and legs flailing comically. 

After falling for what seems a bit longer than necessary, you land sprawling on a giant stack of pillows. After taking a moment to make sure you're still in one piece, you pick yourself up and dust yourself off, no worse for wear besides your tarnished dignity.

Your new surroundings are what appears to be a tall concrete room. You can see the chute you fell out of above you, and a single door on the wall.

There's more than one doorframe, mind you, but all the others have nothing but concrete behind them. You have the feeling that the other doors were cut for budgetary reasons. You steel yourself and step through the available door.";

			subEncounter.options = new [] {
				new EncounterOption( "Continue", new EncounterResult( "Hmm...", "You find yourself blinking in sudden harsh sunlight. It seems this door led to the exit of the test encounter. You have a feeling that the rest of the encounter was cut for budgetary reasons. 'Bloody beauracrats,' you mutter to yourself." ), EmojiUtils.ArrowRight ),
			};
			subEncounter.listOptions = true;




			var testEncounter = new Encounter();

			testEncounter.title = "A Test Encounter";
			testEncounter.text = 
@"You seem to have stumbled into a test encounter. You're not entirely sure what that means, but you do know that you seem to be in an entirely white room with no doors or windows.

It's well lit, but you're not sure from where. The only other objects in the room are three ornate pedastals, each topped with a large cartoony button.

* The left button is **black**
* The middle button is **white**
* And the right button is **red**.

You get the feeling that your only options involve pressing one of the buttons. Which button do you choose?";

			testEncounter.author = "The Narrator";

			testEncounter.options = new[] {
				new EncounterOption( "Press the black button", new EncounterResult( "Hmm...", "You push the **black** button, and are suddenly and violently teleported out of the test encounter. How anticlimatic." ), ":black_circle:" ),
				new EncounterOption( "Press the white button", new EncounterResult( subEncounter ), ":white_circle:" ),
				new EncounterOption( "Press the red button",  new EncounterResult( new EncounterLoot( 2 ), title: null, text: "As soon as you push the **red** button, a warm sensation wells up inside you. You can feel yourself getting more powerful! A reddish mist faintly wafts off your skin. After a moment, both the mist and sensation dissipate, but you still feel stronger." ), ":red_circle:" ),
			};

			testEncounter.listOptions = false;

			return testEncounter;
		}

		public static Task SpawnEncounter ( DiscordChannel channel, DiscordUser user, Encounter encounter ) {
			return Task.Run( async () => {
				var message = await channel.SendMessageAsync( embed: encounter.ToEmbed( user ) );

				try {
					var trigger = Instance.AddReactionTrigger( message, user, ( u, selection ) => {
						var selectedOption = encounter.GetOptionFromEmoji( selection );
						message.DeleteAllReactionsAsync();
						if( selectedOption != null ) selectedOption.result.Execute( channel, user );
					} );
					trigger.oneShot = true;

					var tasks = new Task[ encounter.options.Length ];
					for( int i = 0; i < encounter.options.Length; i++ ) {
						var option = encounter.options[ i ];

						DiscordEmoji emoji;
						if( option.emojiOverride != null ) emoji = option.emojiOverride;
						else emoji = EmojiUtils.emojiNumbers[ i ];

						await message.CreateReactionAsync( option.emojiOverride );
					}
				} catch( Exception e ) {
					Console.WriteLine( e.ToString() );
				}
			});
		}
	}
	
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
