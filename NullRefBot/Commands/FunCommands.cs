using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using NullRefBot.RPG;

namespace NullRefBot.Commands
{
	public class FunCommands : BaseCommandModule
	{
		#region Swanson

		private static String[] ronSwansonQuotes = new[] {
			"You had me at \"Meat Tornado\".",
			"There's only one thing I hate more than lying: skim milk. Which is water that is lying about being milk.",
			"I'd wish you the best of luck but I believe luck is a concept created by the weak to explain their failures.",
			"Dear frozen yogurt, you are the celery of desserts. Be ice cream or be nothing. Zero stars.",
			"I'm not interested in caring about people.",
			"Crying: Acceptable at funerals and the Grand Canyon.",
			"There are three acceptable haircuts: high and tight, crew cut, buzz cut.",
			"Normally, if given the choice between doing something and nothing, I’d choose to do nothing. But I will do something if it helps someone else do nothing. I’d work all night, if it meant nothing got done.",
			"Under my tutelage, you will grow from boys to men. From men into gladiators. And from gladiators into Swansons.",
			"It’s pointless for a human to paint scenes of nature when they can go outside and stand in it.",
			"I once worked with a guy for three years and never learned his name. Best friend I ever had. We still never talk sometimes.",
			"Fishing relaxes me. It’s like yoga, except I still get to kill something.",
			"History began on July 4, 1776. Everything that happened before that was a mistake.",
			"Give a man a fish and feed him for a day. Don’t teach a man to fish…and feed yourself. He’s a grown man. And fishing’s not that hard.",
			"Sting like a bee. Do not float like a butterfly. That's ridiculous.",
			"Give 100%. 110% is impossible. Only idiots recommend that.",
			"Capitalism: God's way of determining who is smart and who is poor.",
			"Fishing is for sport only. Fish meat is practically a vegetable.",
			"Any dog under fifty pounds is a cat and cats are useless.",
		};

		[Command("swanson")]
		public async Task Swanson(CommandContext ctx)
		{
			await ctx.TriggerTypingAsync();

			DiscordEmbed embed = new DiscordEmbedBuilder
			{
				Author = new DiscordEmbedBuilder.EmbedAuthor(),
				Description = ronSwansonQuotes[Environment.TickCount % ronSwansonQuotes.Length],
				ThumbnailUrl = "https://uproxx.files.wordpress.com/2016/06/ron-swanson-feature.jpg?quality=95&w=650&h=360",
				Footer = new DiscordEmbedBuilder.EmbedFooter {Text = " - Ron Swanson"}
			};

			await ctx.RespondAsync(embed: embed);
		}
		#endregion

		[Command("popcorn")]
		public async Task Popcorn(CommandContext ctx)
		{
			await ctx.TriggerTypingAsync();

			DiscordEmbed embed = new DiscordEmbedBuilder
			{
				ImageUrl = "https://media1.tenor.com/images/54451401d52c0dd2fe9ee5752857d53c/tenor.gif"
			};

			await ctx.RespondAsync(embed: embed);
		}

		[Command("banned")]
		public async Task Banned(CommandContext ctx)
		{
			await ctx.TriggerTypingAsync();

			DiscordEmbed embed = new DiscordEmbedBuilder
			{
				ImageUrl = "https://media1.tenor.com/images/66b9e27c779a1a314f0a8b31bb5609f7/tenor.gif"
			};

			await ctx.RespondAsync(embed: embed);
		}

		[Command("placekitten")]
		public async Task PlaceKitten(CommandContext ctx)
		{
			await ctx.TriggerTypingAsync();

			var x = RandomUtils.Range( 200, 400 );
			var y = RandomUtils.Range( 200, 400 );

			DiscordEmbed embed = new DiscordEmbedBuilder
			{
				ImageUrl = $"https://placekitten.com/{x}/{y}"
			};

			await ctx.RespondAsync(embed: embed);
		}

		[Command("xkcd")]
		public async Task Xkcd(CommandContext ctx)
		{
			const string BASE_URL = "https://xkcd.com/info.0.json";
			const string COMIC_URL_TEMPLATE = "http://xkcd.com/{0}/info.0.json";
			using (WebClient wc = new WebClient())
			{
				string baseJson = await wc.DownloadStringTaskAsync(new Uri(BASE_URL));
				dynamic baseJsonObject = JsonConvert.DeserializeObject(baseJson);
				int random = RandomUtils.Range(0, (int)baseJsonObject.num);
				string comicUrl = string.Format(COMIC_URL_TEMPLATE, random);
				string comicJson = await wc.DownloadStringTaskAsync(new Uri(comicUrl));
				dynamic comicJsonObject = JsonConvert.DeserializeObject(comicJson);

				DiscordEmbed embed = new DiscordEmbedBuilder()
				{
					ImageUrl = comicJsonObject.img,
					Title = comicJsonObject.title,
					Footer = new DiscordEmbedBuilder.EmbedFooter()
					{
						Text = comicJsonObject.alt
					}
				};
				await ctx.Channel.SendMessageAsync(embed: embed);
			}
		}
	}
}
