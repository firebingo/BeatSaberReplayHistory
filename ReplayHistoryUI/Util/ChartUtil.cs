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
			ChartYType.Before => "Before cut",
			ChartYType.Accuracy => "Accuracy",
			ChartYType.After => "After",
			ChartYType.TimeDependence => "Time Dependence",
			ChartYType.TotalMisses => "Mistakes",
			ChartYType.Misses => "Misses",
			ChartYType.BadCuts => "Bad Cuts",
			ChartYType.BombHits => "Bomb Hits",
			ChartYType.WallHits => "Wall Hits",
			ChartYType.SongsPlayed => "Songs Played",
			ChartYType.TimePlayed => "Time Played",
			ChartYType.TotalAccuracy or _ => "Rank",
		};

		public static string ChartYTypeToLabeler(double value, ChartYType type) =>
			type switch
			{
				ChartYType.TotalAccuracy => $"{Math.Round(value, 2)}%",
				ChartYType.TimePlayed => value > 60 ? new TimeSpan(0, (int)value, 0).ToString(@"hh\:mm\:ss") : new TimeSpan(0, (int)value, 0).ToString(@"mm\:ss"),
				_ => $"{Math.Round(value, 2)}"
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
				ChartFilterHand.Average or _ => theme.Equals("light", StringComparison.OrdinalIgnoreCase) ? BeatSaberWhiteLight : BeatSaberWhiteDark,
			};
	}
}
