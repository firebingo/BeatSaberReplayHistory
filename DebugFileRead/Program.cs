using BeatSaberReplayHistory;
using System.Collections.Concurrent;

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
			using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
			var decoder = new BSORDecode();
			var replay = await decoder.DecodeBSORV1(fs);
			Console.WriteLine("Read replay successfully");
		}

		static async Task ParseDirectory()
		{
			var directory = "";
			var files = Directory.GetFiles(directory);
			var decoder = new BSORDecode();
			ConcurrentBag<BSORReplay> replays = [];
			ConcurrentBag<Exception> errors = [];
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
					replays.Add(await decoder.DecodeBSORV1(ms));
				}
				catch (Exception ex)
				{
					errors.Add(ex);
				}
			});
			var t = "";
		}
	}
}
