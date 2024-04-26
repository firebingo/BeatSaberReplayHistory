using ReplayHistoryUI.Models;
using ReplayHistoryUI.ViewModels;
using SkiaSharp;
using System;

namespace ReplayHistoryUI.Util
{
	public static class ChartUtil
	{
		public static readonly SKColor BeatSaberRed = new SKColor(204, 51, 51);
		public static readonly SKColor BeatSaberBlue = new SKColor(51, 51, 204);
		public static readonly SKColor BeatSaberWhiteDark = new SKColor(225, 225, 225);
		public static readonly SKColor BeatSaberWhiteLight = new SKColor(35, 35, 35);

		public static string ChartYTypeToName(ChartYType value) =>
		value switch
		{
			ChartYType.TotalAccuracy or _ => "Total Accuracy"
		};

		public static int GetDaysValueFromSelection(ChartDateSelectionValue value) =>
			value switch
			{
				ChartDateSelectionValue.YearToDate => DateTimeOffset.Now.DayOfYear,
				ChartDateSelectionValue.OneYear => (int)Math.Floor((DateTimeOffset.Now - DateTimeOffset.Now.AddYears(-1)).TotalDays),
				ChartDateSelectionValue.SixMonths => (int)Math.Floor((DateTimeOffset.Now - DateTimeOffset.Now.AddMonths(-6)).TotalDays),
				ChartDateSelectionValue.ThirtyDays => 30,
				ChartDateSelectionValue.SevenDays => 7,
				ChartDateSelectionValue.All or _ => -1
			};

		public static SKColor ChartHandTypeToColor(string theme, ChartFilterHand value) =>
			value switch
			{
				ChartFilterHand.Right => BeatSaberBlue,
				ChartFilterHand.Left => BeatSaberRed,
				ChartFilterHand.Both or _ => theme.Equals("light", StringComparison.OrdinalIgnoreCase) ? BeatSaberWhiteLight : BeatSaberWhiteDark,
			};
	}
}
