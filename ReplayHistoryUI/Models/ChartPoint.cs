using System.Collections.Generic;

namespace ReplayHistoryUI.Models
{
	public class ChartPoint
	{
		public long XValue { get; set; } = default;
		public double YValue { get; set; } = default;
		public Dictionary<string, string> SecondaryValues { get; set; } = [];
	}
}
