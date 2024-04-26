using CommunityToolkit.Mvvm.DependencyInjection;
using ReplayHistoryUI.Models;
using ReplayHistoryUI.Util;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ReplayHistoryUI.Services
{
	public class ConfigService
	{
		private readonly string _configFile = "Config.json";
		private readonly string _appPath = string.Empty;
		private Config? _config;
		private readonly SemaphoreSlim _lock;

		private readonly LogService _logService;

		public ConfigService()
		{
			_appPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!, "AppData");
			_logService = Ioc.Default.GetRequiredService<LogService>();
			_lock = new SemaphoreSlim(1, 1);
		}

		private async Task<Config> LoadConfig()
		{
			Config config;
			var path = Path.Combine(_appPath, _configFile);
			if (File.Exists(path))
			{
				using var s = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
				var c = await JsonSerializer.DeserializeAsync<Config>(s, DefaultJsonOptions.DefaultOptions);
				if (c == null)
					await _logService.WriteLog("ConfigService:LoadConfig Failed to deserialize config");
				config = c ?? new Config();
			}
			else
			{
				config = new Config();
				if (!Directory.Exists(_appPath))
					Directory.CreateDirectory(_appPath);
				using var s = File.Create(path);
				await JsonSerializer.SerializeAsync(s, config, DefaultJsonOptions.WriteIndentedCamelCase);
			}

			return config;
		}

		public async Task<Config> GetConfig()
		{
			await _lock.WaitAsync();
			try
			{
				_config ??= await LoadConfig();
				if (_config == null)
					await _logService.WriteLog("ConfigService:GetConfig LoadConfig returned null");
				return _config ?? new Config();
			}
			catch
			{
				throw;
			}
			finally
			{
				_lock.Release();
			}
		}

		public async Task SaveConfig(Config config)
		{
			await _lock.WaitAsync();
			try
			{
				if (!Directory.Exists(_appPath))
					Directory.CreateDirectory(_appPath);
				var path = Path.Combine(_appPath, _configFile);
				using var s = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
				await JsonSerializer.SerializeAsync(s, config, DefaultJsonOptions.WriteIndentedCamelCase);
				_config = config;
			}
			catch
			{
				throw;
			}
			finally
			{
				_lock.Release();
			}
		}
	}
}
