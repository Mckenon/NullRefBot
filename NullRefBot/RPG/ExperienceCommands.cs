using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace NullRefBot.RPG {

	public class ExperienceCommands : BaseCommandModule {
		[Command("xp")]
		public async Task DisplayExp( CommandContext c, DiscordUser user = null ) {
			await c.TriggerTypingAsync();

			if( user == null ) user = c.Member;

			var exp = await ExperienceManager.GetExpAsync( user );

			await c.RespondAsync( string.Format( "**{0}** has {1} experience.", user.Username, exp ) );
		}
	}
}
