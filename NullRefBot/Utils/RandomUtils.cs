using System;

namespace NullRefBot.RPG {
	public class RandomUtils {
		static readonly Random random = new Random();
		public static int Range ( int min, int max ) {
			return random.Next( min, max );
		}

		public static double Range ( double min, double max ) {
			return random.NextDouble() * ( max - min ) + min;
		}
	}
}
