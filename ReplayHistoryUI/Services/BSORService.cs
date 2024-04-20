using BeatSaberReplayHistory;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using ReplayHistoryUI.Messages;
using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
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
		private ImmutableList<BSORReplay> _replays;

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
			var files = Directory.GetFiles(config.ReplayDirectory).Where(x => string.Equals(Path.GetExtension(x), ".bsor", StringComparison.OrdinalIgnoreCase));
			if (!files.Any())
			{
				await _logService.WriteLog($"BSORService:LoadReplays No bsor files in directory ({config.ReplayDirectory})");
				return;
			}
			//Free old memory before loading everything again since this takes up a decent amount.
			_replays = [];
			GC.Collect();
			ConcurrentBag<BSORReplay> replays = [];
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
					replays.Add(BSORDecode.DecodeBSORV1(ms));
					await _progressLock.WaitAsync(cancelToken);
					p++;
					LoadingProgress = (float)p / files.Count();
					if(p % 5 == 0 || p == files.Count())
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
				foreach(var error in errors)
				{
					await _logService.WriteLog($"BSORService:LoadReplays Error on reading file\n{error}");
				}
			}

			_replays = [.. replays];
		}
	}
}
