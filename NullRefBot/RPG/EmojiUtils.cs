using DSharpPlus.Entities;
using static NullRefBot.Bot;

namespace NullRefBot.RPG {
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
}
