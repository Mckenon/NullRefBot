using DSharpPlus.Entities;

namespace NullRefBot.RPG {
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
}
