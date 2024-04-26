namespace ReplayHistoryUI.Models
{
	public enum ChartFilterHand
	{
		Both = 0,
		Left = 1,
		Right = 2
	}

	public enum ChartYType
	{
		TotalAccuracy = 0,
	}

	public class ChartFilterInput
	{
		public ChartFilterHand Hand { get; set; }
		public ChartYType Type { get; set; }
		public int DaysOffset { get; set; }
	}
}
