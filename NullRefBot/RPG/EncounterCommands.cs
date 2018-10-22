using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using static NullRefBot.Bot;
using System.Timers;

namespace NullRefBot.RPG {
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
}
