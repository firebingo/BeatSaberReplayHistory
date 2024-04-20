using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ReplayHistoryUI.Messages;
using ReplayHistoryUI.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ReplayHistoryUI.ViewModels;

public partial class MainViewModel : ViewModelBase, IDisposable
{
	private bool _isDisposed;
	private readonly SynchronizationContext _syncContext;
	private readonly WeakReferenceMessenger _messenger;
	private readonly ConfigService _configService;
	private readonly LogService _logService;

	public MainViewModel()
	{
		_syncContext = SynchronizationContext.Current!;
		_logService = Ioc.Default.GetRequiredService<LogService>();
		_configService = Ioc.Default.GetRequiredService<ConfigService>();
		_messenger = Ioc.Default.GetRequiredService<WeakReferenceMessenger>();
		_messenger.Register<PickedFolderDialogMessage>(this, (r, result) =>
		{
			if (result.Value.Count == 0)
				return;
			var value = result.Value[0];
			_ = Task.Run(async () => await UpdateDirectory(value.TryGetLocalPath()));
		});
	}

	[RelayCommand]
	public void SetDirectory()
	{
		_messenger.Send(new OpenFolderDialogMessage(new FolderPickerOpenOptions()
		{
			AllowMultiple = false,
			Title = "Select BeatLeader replay directory"
		}));
	}

	public async Task UpdateDirectory(string? path)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(path))
				throw new Exception("Empty path passed");

			var config = await _configService.GetConfig();
			config.ReplayDirectory = path;
			await _configService.SaveConfig(config);
			_syncContext.Post((state) =>
			{
				_messenger.Send(new DirectoryChangedMessage(true));
			}, null);
		}
		catch (Exception ex)
		{
			await _logService.WriteLog($"MainViewModel:UpdateDirectory\n{ex}");
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
			_messenger.Unregister<PickedFolderDialogMessage>(this);
		}

		_isDisposed = true;
	}

	~MainViewModel()
	{
		Dispose(false);
	}
}
