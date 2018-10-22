using DSharpPlus;
using DSharpPlus.Entities;

namespace NullRefBot.RPG {
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
}
