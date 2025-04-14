using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using ReplayHistoryUI.Messages;
using ReplayHistoryUI.Models;
using ReplayHistoryUI.Services;
using ReplayHistoryUI.Util;
using SkiaSharp;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ReplayHistoryUI.ViewModels;

public enum ChartViewLoadingStatus
{
	NeedDirectory = 0,
	Loading = 1,
	Loaded = 2
}

public enum ChartDateSelectionValue
{
	SevenDays = 0,
	ThirtyDays = 1,
	SixMonths = 2,
	OneYear = 3,
	YearToDate = 4,
	All = 5
}

public class ChartDateSelectionItem
{
	public ChartDateSelectionValue Days { get; set; }
	public string Text { get; set; } = string.Empty;
}

public class ChartComboBoxItem
{
	public int Value { get; set; }
	public string Text { get; set; } = string.Empty;
}

public partial class ChartViewModel : ViewModelBase, IDisposable
{
	private bool _isDisposed;
	private readonly SynchronizationContext _syncContext;
	private readonly WeakReferenceMessenger _messenger;
	private readonly LogService _logService;
	private readonly ConfigService _configService;
	private readonly BSORService _bsorService;

	[ObservableProperty]
	public ChartViewLoadingStatus _loadingStatus = ChartViewLoadingStatus.Loading;

	[ObservableProperty]
	public int _loadingProgress = 0;

	[ObservableProperty]
	public ChartDateSelectionItem[] _chartDateSelection;
	private ChartDateSelectionItem _dateSelectedValue;
	public ChartDateSelectionItem DateSelectedValue
	{
		get => _dateSelectedValue;
		set
		{
			SetProperty(ref _dateSelectedValue, value);
			UpdateDateFilter(value.Days);
		}
	}

	public ChartComboBoxItem[] HandSelection { get; }
	private ChartComboBoxItem _handSelectedValue;
	public ChartComboBoxItem HandSelectedValue
	{
		get => _handSelectedValue;
		set
		{
			SetProperty(ref _handSelectedValue, value);
			UpdateHandFilter(value.Value);
		}
	}

	[ObservableProperty]
	public ObservableCollection<ISeries> _chartSeries;
	[ObservableProperty]
	public ObservableCollection<Axis> _chartXAxes;
	[ObservableProperty]
	public ObservableCollection<Axis> _chartYAxes;

	private ChartFilterInput _currentFilter;

	public ChartViewModel()
	{
		_syncContext = SynchronizationContext.Current!;
		_messenger = Ioc.Default.GetRequiredService<WeakReferenceMessenger>();
		_logService = Ioc.Default.GetRequiredService<LogService>();
		_configService = Ioc.Default.GetRequiredService<ConfigService>();
		_bsorService = Ioc.Default.GetRequiredService<BSORService>();
		_messenger.Register<DirectoryChangedMessage>(this, (r, changed) =>
		{
			if (changed.Value)
				_ = Task.Run(OnDirectoryChanged);
		});
		_messenger.Register<LoadingProgressMessage>(this, (r, value) =>
		{
			_syncContext.Post((state) =>
			{
				LoadingProgress = (int)(value.Value * 100f);
			}, null);
		});
		_chartDateSelection =
		[
			new ChartDateSelectionItem() { Days = ChartDateSelectionValue.SevenDays, Text = "7D" },
			new ChartDateSelectionItem() { Days = ChartDateSelectionValue.ThirtyDays, Text = "30D" },
			new ChartDateSelectionItem() { Days = ChartDateSelectionValue.SixMonths, Text = "6M" },
			new ChartDateSelectionItem() { Days = ChartDateSelectionValue.OneYear, Text = "1Y" },
			new ChartDateSelectionItem() { Days = ChartDateSelectionValue.YearToDate, Text = "YTD" },
			new ChartDateSelectionItem() { Days = ChartDateSelectionValue.All, Text = "ALL" }
		];
		_dateSelectedValue = _chartDateSelection[^1];
		HandSelection =
		[
			new ChartComboBoxItem() { Text = "Both", Value = (int)ChartFilterHand.Both },
			new ChartComboBoxItem() { Text = "Left", Value = (int)ChartFilterHand.Left },
			new ChartComboBoxItem() { Text = "Right", Value = (int)ChartFilterHand.Right },
		];
		_handSelectedValue = HandSelection[0];
		_chartSeries = [];
		_chartXAxes = [new Axis()];
		_chartYAxes = [new Axis()];
		_currentFilter = new ChartFilterInput()
		{
			DaysOffset = ChartUtil.GetDaysValueFromSelection(_dateSelectedValue.Days),
			Hand = ChartFilterHand.Both,
			Type = ChartYType.TotalAccuracy
		};
		_ = Task.Run(CheckDirectory);
	}

	public async Task CheckDirectory()
	{
		try
		{
			var config = await _configService.GetConfig();
			if (string.IsNullOrWhiteSpace(config.ReplayDirectory))
			{
				_syncContext.Post((state) =>
				{
					LoadingStatus = ChartViewLoadingStatus.NeedDirectory;
				}, null);
			}
			else
				_ = Task.Run(LoadReplays);
		}
		catch (Exception ex)
		{
			await _logService.WriteLog($"ChartViewModel:CheckDirectory Exception\n{ex}");
		}
	}

	public async Task OnDirectoryChanged()
	{
		await LoadReplays();
	}

	public async Task LoadReplays()
	{
		_syncContext.Post((state) =>
		{
			LoadingStatus = ChartViewLoadingStatus.Loading;
		}, null);

		await _bsorService.LoadReplays();

		_syncContext.Post((state) =>
		{
			try
			{
				CreateChart();
			}
			catch (Exception ex)
			{
				_ = _logService.WriteLog($"ChartViewModel:LoadReplays post load sync post exception\n{ex}");
			}
			LoadingStatus = ChartViewLoadingStatus.Loaded;
		}, null);
	}

	private void CreateChart()
	{
		var theme = Application.Current!.ActualThemeVariant!;
		Application.Current.TryFindResource("SystemControlForegroundBaseHighBrush", theme, out var fgb);
		var foregroundBrush = (fgb as SolidColorBrush)!;
		var foregroundColor = new SKColor(foregroundBrush.Color.R, foregroundBrush.Color.G, foregroundBrush.Color.B);
		Application.Current.TryFindResource("SystemControlForegroundChromeHighBrush", theme, out var fgmb);
		var foregroundMediumBrush = (fgmb as SolidColorBrush)!;
		var foregroundMediumColor = new SKColor(foregroundMediumBrush.Color.R, foregroundMediumBrush.Color.G, foregroundMediumBrush.Color.B);

		ChartSeries.Clear();
		ChartXAxes.Clear();
		ChartYAxes.Clear();
		var info = _bsorService.GetChartInfo(_currentFilter);
		var vals = info.OrderBy(x => x.XValue);
		var series = new LineSeries<ChartPoint>()
		{
			Fill = new SolidColorPaint(ChartUtil.ChartHandTypeToColor((string)theme.Key, _currentFilter.Hand).WithAlpha(30)),
			GeometryFill = new SolidColorPaint(ChartUtil.ChartHandTypeToColor((string)theme.Key, _currentFilter.Hand)),
			Stroke = new SolidColorPaint(ChartUtil.ChartHandTypeToColor((string)theme.Key, _currentFilter.Hand)) { StrokeThickness = 3 },
			GeometryStroke = null,
			GeometrySize = 16,
			LineSmoothness = 0.30,
			Values = [.. vals],
			Mapping = (val, index) => new(val.XValue, val.YValue),
			XToolTipLabelFormatter = (point) =>
			{
				var s = $"Date: {new DateTime(point.Model!.XValue):yy-MM-dd}";
				foreach (var v in point.Model.SecondaryValues)
				{
					s += $"\r\n{v.Key}: {v.Value}";
				}
				return s;
			}
		};
		ChartXAxes.Add(new DateTimeAxis(TimeSpan.FromDays(1), date => date.ToString("yy-MM-dd"))
		{
			ShowSeparatorLines = true,
			SeparatorsPaint = new SolidColorPaint
			{
				StrokeThickness = 1,
				Color = foregroundMediumColor
			},
			LabelsPaint = new SolidColorPaint
			{
				Color = foregroundColor
			},
		});
		ChartYAxes.Add(new Axis()
		{
			Name = ChartUtil.ChartYTypeToName(_currentFilter.Type),
			ShowSeparatorLines = true,
			NamePaint = new SolidColorPaint
			{
				Color = foregroundColor
			},
			LabelsPaint = new SolidColorPaint
			{
				Color = foregroundColor
			},
			SeparatorsPaint = new SolidColorPaint
			{
				StrokeThickness = 1,
				Color = foregroundColor
			},
			SubseparatorsCount = 1,
			SubseparatorsPaint = new SolidColorPaint
			{
				StrokeThickness = 1,
				Color = foregroundMediumColor,
				PathEffect = new DashEffect([3, 3])
			},
			Labeler = (double val) => $"{val}%"
		});
		ChartSeries.Add(series);
	}

	private void UpdateChart(ChartUpdateChanges update)
	{
		if (update.DaysOffset.HasValue)
			_currentFilter.DaysOffset = update.DaysOffset.Value;
		if (update.Hand.HasValue)
			_currentFilter.Hand = update.Hand.Value;
		if (update.Type.HasValue)
			_currentFilter.Type = update.Type.Value;

		var info = _bsorService.GetChartInfo(_currentFilter);
		var vals = info.OrderBy(x => x.XValue);

		if (update.Type.HasValue)
			ChartSeries[0].Name = ChartUtil.ChartYTypeToName(_currentFilter.Type);
		if (update.Hand.HasValue)
		{
			var theme = Application.Current!.ActualThemeVariant!;
			((LineSeries<ChartPoint>)ChartSeries[0]).Fill = new SolidColorPaint(ChartUtil.ChartHandTypeToColor((string)theme.Key, _currentFilter.Hand).WithAlpha(30));
			((LineSeries<ChartPoint>)ChartSeries[0]).GeometryFill = new SolidColorPaint(ChartUtil.ChartHandTypeToColor((string)theme.Key, _currentFilter.Hand));
			((LineSeries<ChartPoint>)ChartSeries[0]).Stroke = new SolidColorPaint(ChartUtil.ChartHandTypeToColor((string)theme.Key, _currentFilter.Hand)) { StrokeThickness = 3 };
		}
		ChartSeries[0].Values = vals.ToList();
	}

	public void UpdateHandFilter(int value)
	{
		var hand = (ChartFilterHand)value;
		if (_currentFilter.Hand != hand)
		{
			UpdateChart(new ChartUpdateChanges()
			{
				Hand = hand
			});
		}
	}

	public void UpdateDateFilter(ChartDateSelectionValue value)
	{
		var days = ChartUtil.GetDaysValueFromSelection(value);
		if (_currentFilter.DaysOffset != days)
		{
			UpdateChart(new ChartUpdateChanges()
			{
				DaysOffset = days
			});
		}
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (_isDisposed) return;

		if (disposing)
		{
			_messenger.Unregister<DirectoryChangedMessage>(this);
			_messenger.Unregister<LoadingProgressMessage>(this);
		}

		_isDisposed = true;
	}

	~ChartViewModel()
	{
		Dispose(false);
	}

	private class ChartUpdateChanges()
	{
		public ChartFilterHand? Hand { get; set; }
		public ChartYType? Type { get; set; }
		public int? DaysOffset { get; set; }
	}
}

