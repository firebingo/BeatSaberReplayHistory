using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ReplayHistoryUI.Services
{
	public class LogService
	{
		private readonly string _configFile = "{0}_log.txt";
		private readonly string _appPath = string.Empty;
		private readonly SemaphoreSlim _lock;

		public LogService()
		{
			_appPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!, "AppData");
			_lock = new SemaphoreSlim(1, 1);
		}

		public async Task WriteLog(string message)
		{
			await _lock.WaitAsync();
			try
			{
				var now = DateTime.Now;
				var path = Path.Combine(_appPath, string.Format(_configFile, now.ToString("yyyyMMdd")));
				if (!Directory.Exists(_appPath))
					Directory.CreateDirectory(_appPath);
				using var s = File.Open(path, FileMode.Append, FileAccess.Write, FileShare.None);
				using var sw = new StreamWriter(s);
				await sw.WriteLineAsync($"[{now:HH:mm:ss:fff}] {message}");
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}
			_lock.Release();
		}
	}
}
