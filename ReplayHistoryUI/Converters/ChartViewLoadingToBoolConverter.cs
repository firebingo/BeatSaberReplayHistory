using Avalonia.Data;
using Avalonia.Data.Converters;
using ReplayHistoryUI.ViewModels;
using System;
using System.Globalization;

namespace ReplayHistoryUI.Converters
{
	public class ChartViewLoadingToBoolConverter : IValueConverter
	{
		public static readonly ChartViewLoadingToBoolConverter Instance = new();

		public object? Convert(object? value, Type targetType, object? parameter,
																CultureInfo culture)
		{
			if (value is ChartViewLoadingStatus sourceValue && parameter is ChartViewLoadingStatus targetValue
				&& targetType.IsAssignableTo(typeof(bool)))
			{
				if (sourceValue == ChartViewLoadingStatus.NeedDirectory && targetValue == ChartViewLoadingStatus.NeedDirectory)
					return true;
				else if (sourceValue == ChartViewLoadingStatus.Loading && targetValue == ChartViewLoadingStatus.Loading)
					return true;
				else if (sourceValue == ChartViewLoadingStatus.Loaded && targetValue == ChartViewLoadingStatus.Loaded)
					return true;
				else
					return false;
			}
			// converter used for the wrong type
			return new BindingNotification(new InvalidCastException(),
													BindingErrorType.Error);
		}

		public object ConvertBack(object? value, Type targetType,
									object? parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}
