using BeatSaberReplayHistory;
using BsorParse.Model;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using ReplayHistoryUI.Messages;
using ReplayHistoryUI.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ReplayHistoryUI.Services
{
	public class BSORService
	{
		private readonly ConfigService _configService;
		private readonly LogService _logService;
		private readonly WeakReferenceMessenger _messenger;
		private ImmutableList<StatsReplay> _replays;

		public float LoadingProgress { get; private set; }
		private readonly SemaphoreSlim _progressLock;

		public BSORService()
		{
			_replays = [];
			_configService = Ioc.Default.GetRequiredService<ConfigService>();
			_logService = Ioc.Default.GetRequiredService<LogService>();
			_messenger = Ioc.Default.GetRequiredService<WeakReferenceMessenger>();
			_progressLock = new SemaphoreSlim(1, 1);
		}

		public async Task LoadReplays()
		{
			var config = await _configService.GetConfig();
			if (string.IsNullOrWhiteSpace(config.ReplayDirectory))
			{
				await _logService.WriteLog("BSORService:LoadReplays Called while ReplayDirectory is empty");
				return;
			}
			var files = Directory.GetFiles(config.ReplayDirectory).Where(x => !x.Contains("-practice-") && string.Equals(Path.GetExtension(x), ".bsor", StringComparison.OrdinalIgnoreCase)).ToList();
			if (files.Count == 0)
			{
				await _logService.WriteLog($"BSORService:LoadReplays No bsor files in directory ({config.ReplayDirectory})");
				return;
			}
			//Free old memory before loading everything again since this takes up a decent amount.
			_replays = [];
			GC.Collect();
			ConcurrentBag<StatsReplay> replays = [];
			ConcurrentBag<Exception> errors = [];
			ParallelOptions parallelOptions = new()
			{
				MaxDegreeOfParallelism = 8
			};
			var p = 0;
			await Parallel.ForEachAsync(files, parallelOptions, async (file, cancelToken) =>
			{
				try
				{
					//I have found when running in parallel reading the whole file into memory first is faster 
					// than using file streams where the disk has to constantly jump around to read a few bytes.
					using var ms = new MemoryStream(await File.ReadAllBytesAsync(file, cancelToken));
					replays.Add(new StatsReplay(BSORDecode.DecodeBSORV1(ms)));
					await _progressLock.WaitAsync(cancelToken);
					Interlocked.Increment(ref p);
					LoadingProgress = (float)p / files.Count;
					if (p % 5 == 0 || p == files.Count)
						_messenger.Send(new LoadingProgressMessage(LoadingProgress));

					_progressLock.Release();
				}
				catch (Exception ex)
				{
					errors.Add(ex);
				}
			});

			if (!errors.IsEmpty)
			{
				foreach (var error in errors)
				{
					await _logService.WriteLog($"BSORService:LoadReplays Error on reading file\n{error}");
				}
			}

			_replays = [.. replays];
		}

		private static ChartPoint CreateChartPoint<T>(ChartFilterInput input, ChartFilterHand hand, IGrouping<T, StatsReplay> x, TimeSpan time)
		{
			return new ChartPoint
			{
				XValue = x.First().Info.Time!.Value.Ticks,
				YValue = input.Type switch
				{
					ChartYType.Before => hand switch
					{
						ChartFilterHand.Left => Math.Round(x.Average(y => y.LeftBeforeCut), 2),
						ChartFilterHand.Right => Math.Round(x.Average(y => y.RightBeforeCut), 2),
						_ or ChartFilterHand.Average => Math.Round((x.Average(y => y.LeftBeforeCut) + x.Average(y => y.RightBeforeCut)) / 2.0, 2)
					},
					ChartYType.Accuracy => hand switch
					{
						ChartFilterHand.Left => Math.Round(x.Average(y => y.LeftAccuracy), 2),
						ChartFilterHand.Right => Math.Round(x.Average(y => y.RightAccuracy), 2),
						_ or ChartFilterHand.Average => Math.Round((x.Average(y => y.LeftAccuracy) + x.Average(y => y.RightAccuracy)) / 2.0, 2)
					},
					ChartYType.After => hand switch
					{
						ChartFilterHand.Left => Math.Round(x.Average(y => y.LeftAfterCut), 2),
						ChartFilterHand.Right => Math.Round(x.Average(y => y.RightAfterCut), 2),
						_ or ChartFilterHand.Average => Math.Round((x.Average(y => y.LeftAfterCut) + x.Average(y => y.RightAfterCut)) / 2.0, 2)
					},
					ChartYType.TimeDependence => hand switch
					{
						ChartFilterHand.Left => Math.Round(x.Average(y => y.LeftTimeDependence), 2),
						ChartFilterHand.Right => Math.Round(x.Average(y => y.RightTimeDependence), 2),
						_ or ChartFilterHand.Average => Math.Round((x.Average(y => y.LeftTimeDependence) + x.Average(y => y.RightTimeDependence)) / 2.0, 2)
					},
					ChartYType.TotalMisses => hand switch
					{
						ChartFilterHand.Left => x.Sum(y => y.LeftMisses + y.LeftBadCuts + y.LeftBombCuts),
						ChartFilterHand.Right => x.Sum(y => y.RightMisses + y.RightBadCuts + y.RightBombCuts),
						_ or ChartFilterHand.Average => x.Sum(y => y.TotalMisses)
					},
					ChartYType.Misses => hand switch
					{
						ChartFilterHand.Left => x.Sum(y => y.LeftMisses),
						ChartFilterHand.Right => x.Sum(y => y.RightMisses),
						_ or ChartFilterHand.Average => x.Sum(y => y.Misses),
					},
					ChartYType.BadCuts => hand switch
					{
						ChartFilterHand.Left => x.Sum(y => y.LeftBadCuts),
						ChartFilterHand.Right => x.Sum(y => y.RightBadCuts),
						_ or ChartFilterHand.Average => x.Sum(y => y.BadCuts)
					},
					ChartYType.BombHits => hand switch
					{
						ChartFilterHand.Left => x.Sum(y => y.LeftBombCuts),
						ChartFilterHand.Right => x.Sum(y => y.RightBombCuts),
						_ or ChartFilterHand.Average => x.Sum(y => y.BombCuts)
					},
					ChartYType.WallHits => hand switch
					{
						_ => x.Sum(y => y.WallHits)
					},
					ChartYType.SongsPlayed => hand switch
					{
						_ => x.Count()
					},
					ChartYType.TimePlayed => hand switch
					{
						_ => time.TotalMinutes
					},
					_ or ChartYType.TotalAccuracy => hand switch
					{
						ChartFilterHand.Left => Math.Round((x.Average(y => y.LeftTotalAccuracy) / 115.0) * 100.0f, 2),
						ChartFilterHand.Right => Math.Round((x.Average(y => y.RightTotalAccuracy) / 115.0) * 100.0f, 2),
						_ or ChartFilterHand.Average => Math.Round(x.Average(y => y.Accuracy) * 100.0f, 2)
					}
				},
				SecondaryValues = new Dictionary<string, string>()
				{
					{ "Songs Played", x.Count().ToString() },
					{ "Time Played", time.TotalMinutes > 60 ? time.ToString(@"hh\:mm\:ss") : time.ToString(@"mm\:ss") }
				}
			};
		}

		public static ChartPoint FilterSelect<T>(ChartFilterInput input, ChartFilterHand hand, IGrouping<T, StatsReplay> x)
		{
			TimeSpan time = TimeSpan.Zero;
			if (x.Count() == 1)
			{
				time = new TimeSpan(0, 0, 0, 0, (int)(x.First().LengthSeconds * 1000));
			}
			else
			{
				var order = x.OrderBy(y => y.Info.Time);
				var lastTime = ((DateTimeOffset)order.Last().Info.Time!).AddSeconds(order.Last().LengthSeconds);
				time = (TimeSpan)(lastTime - order.First().Info.Time)!;
			}
			return CreateChartPoint(input, hand, x, time);
		}

		public List<List<ChartPoint>> GetChartInfo(ChartFilterInput input)
		{
			var retval = new List<List<ChartPoint>>();

			DateTimeOffset? minDate = input.DaysOffset == -1 ? null : DateTimeOffset.Now.AddDays(-input.DaysOffset);

			var a = _replays.Where(x => !string.IsNullOrWhiteSpace(x.Info?.Modifiers)).OrderByDescending(x => x.Info.Timestamp);
			var dateGrouping = _replays.Where(x => x.Info.FailTime <= 0)
				.Where(x => x.Info.Time.HasValue && (!minDate.HasValue || x.Info.Time > minDate.Value))
				.OrderBy(x => x.Info.Time)
				.GroupBy(x => new { x.Info.Time!.Value.DayOfYear, x.Info.Time.Value.Year });

			if (input.Hand == ChartFilterHand.Both)
			{
				//Left
				var addRet = new List<ChartPoint>();
				IEnumerable<ChartPoint> filtered = [];
				filtered = dateGrouping.Select(x => FilterSelect(input, ChartFilterHand.Left, x));
				addRet.AddRange(filtered);
				retval.Add(addRet);

				//Right
				addRet = [];
				filtered = [];
				filtered = dateGrouping.Select(x => FilterSelect(input, ChartFilterHand.Right, x));
				addRet.AddRange(filtered);
				retval.Add(addRet);
			}
			else
			{
				var addRet = new List<ChartPoint>();
				IEnumerable<ChartPoint> filtered = [];
				filtered = dateGrouping.Select(x => FilterSelect(input, input.Hand, x));
				addRet.AddRange(filtered);
				retval.Add(addRet);
			}

			return retval;
		}
	}
}
