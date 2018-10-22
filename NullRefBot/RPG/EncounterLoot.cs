namespace NullRefBot.RPG {
	public class EncounterLoot {
		public int experience;
		public int gold;

		public EncounterLoot ( int experience, int gold = 0 ) {
			this.experience = experience;
			this.gold = gold;
		}
	}
}
