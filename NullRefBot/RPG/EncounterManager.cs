using System;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using static NullRefBot.Bot;

namespace NullRefBot.RPG {
	public class EncounterManager {

		public static Encounter TrySpawnEncounterForUser ( DiscordMember member ) {
			var subEncounter = new Encounter();
			subEncounter.title = "Suddenly and Without Warning";

			subEncounter.text =
@"As soon as you push the **white** button, the floor silently opens up beneath you! You fall in a rather silly manner, your arms and legs flailing comically. 

After falling for what seems a bit longer than necessary, you land sprawling on a giant stack of pillows. After taking a moment to make sure you're still in one piece, you pick yourself up and dust yourself off, no worse for wear besides your tarnished dignity.

Your new surroundings are what appears to be a tall concrete room. You can see the chute you fell out of above you, and a single door on the wall.

There's more than one doorframe, mind you, but all the others have nothing but concrete behind them. You have the feeling that the other doors were cut for budgetary reasons. You steel yourself and step through the available door.";

			subEncounter.options = new [] {
				new EncounterOption( "Continue", new EncounterResult( "Hmm...", "You find yourself blinking in sudden harsh sunlight. It seems this door led to the exit of the test encounter. You have a feeling that the rest of the encounter was cut for budgetary reasons. 'Bloody beauracrats,' you mutter to yourself." ), EmojiUtils.ArrowRight ),
			};
			subEncounter.listOptions = true;




			var testEncounter = new Encounter();

			testEncounter.title = "A Test Encounter";
			testEncounter.text = 
@"You seem to have stumbled into a test encounter. You're not entirely sure what that means, but you do know that you seem to be in an entirely white room with no doors or windows.

It's well lit, but you're not sure from where. The only other objects in the room are three ornate pedastals, each topped with a large cartoony button.

* The left button is **black**
* The middle button is **white**
* And the right button is **red**.

You get the feeling that your only options involve pressing one of the buttons. Which button do you choose?";

			testEncounter.author = "The Narrator";

			testEncounter.options = new[] {
				new EncounterOption( "Press the black button", new EncounterResult( "Hmm...", "You push the **black** button, and are suddenly and violently teleported out of the test encounter. How anticlimatic." ), ":black_circle:" ),
				new EncounterOption( "Press the white button", new EncounterResult( subEncounter ), ":white_circle:" ),
				new EncounterOption( "Press the red button",  new EncounterResult( new EncounterLoot( 2 ), title: null, text: "As soon as you push the **red** button, a warm sensation wells up inside you. You can feel yourself getting more powerful! A reddish mist faintly wafts off your skin. After a moment, both the mist and sensation dissipate, but you still feel stronger." ), ":red_circle:" ),
			};

			testEncounter.listOptions = false;

			return testEncounter;
		}

		public static Task SpawnEncounter ( DiscordChannel channel, DiscordUser user, Encounter encounter ) {
			return Task.Run( async () => {
				var message = await channel.SendMessageAsync( embed: encounter.ToEmbed( user ) );

				try {
					var trigger = Instance.AddReactionTrigger( message, user, ( u, selection ) => {
						var selectedOption = encounter.GetOptionFromEmoji( selection );
						message.DeleteAllReactionsAsync();
						if( selectedOption != null ) selectedOption.result.Execute( channel, user );
					} );
					trigger.oneShot = true;

					var tasks = new Task[ encounter.options.Length ];
					for( int i = 0; i < encounter.options.Length; i++ ) {
						var option = encounter.options[ i ];

						DiscordEmoji emoji;
						if( option.emojiOverride != null ) emoji = option.emojiOverride;
						else emoji = EmojiUtils.emojiNumbers[ i ];

						await message.CreateReactionAsync( option.emojiOverride );
					}
				} catch( Exception e ) {
					Console.WriteLine( e.ToString() );
				}
			});
		}
	}
}
