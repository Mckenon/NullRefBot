using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace NullRefBot.Commands
{
	public class Utilities : BaseCommandModule
	{
		[Command("lmgtfy")]
		[Description("Did someone fail to google something? Help 'em out.")]
		public async Task Lmgtfy(CommandContext ctx, [RemainingText]string thingToGoogle)
		{
			await ctx.TriggerTypingAsync();

			var embed = new DiscordEmbedBuilder
			{
				Title = "**Let me google that for you.**",
				Url = "http://lmgtfy.com/?q=" + Uri.EscapeDataString(thingToGoogle),
				Description = "Because you probably should've done it yourself.",
				ThumbnailUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/5/53/Google_%22G%22_Logo.svg/2000px-Google_%22G%22_Logo.svg.png",
				Author = new DiscordEmbedBuilder.EmbedAuthor()
				{
					IconUrl = ctx.Message.Author.AvatarUrl,
					Name = ctx.Message.Author.Username,
				},
			};

			await ctx.RespondAsync(embed: embed);
		}

		[Command("testlogroles"), RequirePermissions(Permissions.Administrator), Hidden]
		public async Task TestLogRoles(CommandContext ctx)
		{
			await ctx.TriggerTypingAsync();

			string s = "```\n";
			foreach (DiscordRole role in ctx.Guild.Roles)
				s += role.Name + "\t" + role.Id + "\n";
			s += "```";

			await ctx.RespondAsync(s);
		}
	}
}