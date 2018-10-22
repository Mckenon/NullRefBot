using DSharpPlus.Entities;

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
}
