using DSharpPlus.Entities;
using static NullRefBot.Bot;

namespace NullRefBot.RPG {
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
}
