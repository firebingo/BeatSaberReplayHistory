using BeatSaberReplayHistory;
using BsorParse.Model;
using System.Collections.Concurrent;
using System.Runtime;

namespace DebugFileRead
{
	internal class Program
	{
		static async Task Main()
		{
			var start = DateTime.Now;
			try
			{
				await ParseDirectory();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}
			var end = DateTime.Now;
			Console.WriteLine($"Runtime: {(end - start).TotalMilliseconds:0.##}ms");
			Console.WriteLine("Press any key to exit");
			Console.ReadLine();
		}

		static async Task ParseSingle()
		{
			var filePath = "";
			using var ms = new MemoryStream(await File.ReadAllBytesAsync(filePath));
			var replay = BSORDecode.DecodeBSORV1(ms);
			Console.WriteLine("Read replay successfully");
			await Task.Delay(0);
		}

		static async Task ParseDirectory()
		{
			var directory = "";
			var files = Directory.GetFiles(directory).Where(x => !x.Contains("-practice-")).ToList();
			//files = [.. files[0..128]];
			ConcurrentBag<StatsReplay> replays = [];
			ConcurrentBag<(string, Exception)> errors = [];
			ParallelOptions parallelOptions = new()
			{
				MaxDegreeOfParallelism = 8
			};
			await Parallel.ForEachAsync(files, parallelOptions, async (file, cancelToken) =>
			{
				try
				{
					//I have found when running in parallel reading the whole file into memory first is faster 
					// than using file streams where the disk has to constantly jump around to read a few bytes.
					using var ms = new MemoryStream(await File.ReadAllBytesAsync(file, cancelToken));
					var replay = BSORDecode.DecodeBSORV1(ms);
					var sReplay = new StatsReplay(replay);
					replays.Add(sReplay);
				}
				catch (Exception ex)
				{
					errors.Add((file, ex));
				}
			});
			GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
			GC.Collect();
			GC.WaitForPendingFinalizers();
			long x = GC.GetTotalMemory(false);
			var t = "";
		}
	}
}
