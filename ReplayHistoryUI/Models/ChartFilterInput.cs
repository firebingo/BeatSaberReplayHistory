namespace ReplayHistoryUI.Models
{
	public enum ChartFilterHand
	{
		Average = 0,
		Left = 1,
		Right = 2,
		Both = 3
	}

	public enum ChartYType
	{
		TotalAccuracy = 0,
		Before = 1,
		Accuracy = 2,
		After = 3,
		TimeDependence = 4,
		TotalMisses = 5, //Misses/bad cuts/wall/bomb hits/etc
		Misses = 6,
		BadCuts = 7,
		BombHits = 8,
		WallHits = 9,
		SongsPlayed = 10,
		TimePlayed = 11
	}

	public class ChartFilterInput
	{
		public ChartFilterHand Hand { get; set; }
		public ChartYType Type { get; set; }
		public int DaysOffset { get; set; }
	}
}
