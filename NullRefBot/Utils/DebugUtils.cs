using System;
using System.Collections.Generic;
using System.Linq;
using DSharpPlus.CommandsNext;

namespace NullRefBot.Utils
{
	public static class DebugUtils
	{
		public static bool IsAllowedInChannel(this CommandContext ctx)
		{
			if (Bot.Instance.Config.DebugMode == false)
				return true;
			if (Bot.Instance.Config.DebugChannels.Contains(ctx.Channel.Id))
				return true;
			return false;
		}
	}
}
