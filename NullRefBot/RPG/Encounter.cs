using System.Text;
using DSharpPlus.Entities;

namespace NullRefBot.RPG {
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
}
