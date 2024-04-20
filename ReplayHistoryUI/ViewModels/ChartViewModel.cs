using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using ReplayHistoryUI.Messages;
using ReplayHistoryUI.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ReplayHistoryUI.ViewModels;

public partial class ChartViewModel : ViewModelBase, IDisposable
{
	private bool _isDisposed;
	private readonly SynchronizationContext _syncContext;
	private readonly WeakReferenceMessenger _messenger;
	private readonly LogService _logService;
	private readonly ConfigService _configService;
	private readonly BSORService _bsorService;

	[ObservableProperty]
	public bool _loading = true;
	[ObservableProperty]
	public bool _needDirectory = false;
	[ObservableProperty]
	public int _loadingProgress = 0;

	public ChartViewModel()
	{
		_syncContext = SynchronizationContext.Current!;
		_messenger = Ioc.Default.GetRequiredService<WeakReferenceMessenger>();
		_logService = Ioc.Default.GetRequiredService<LogService>();
		_configService = Ioc.Default.GetRequiredService<ConfigService>();
		_bsorService = Ioc.Default.GetRequiredService<BSORService>();
		_messenger.Register<DirectoryChangedMessage>(this, (r, changed) =>
		{
			if(changed.Value)
				_ = Task.Run(OnDirectoryChanged);
		});
		_messenger.Register<LoadingProgressMessage>(this, (r, value) =>
		{
			_syncContext.Post((state) =>
			{
				LoadingProgress = (int)(value.Value * 100f);
			}, null);
		});
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
					Loading = false;
					NeedDirectory = true;
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
			NeedDirectory = false;
			Loading = true;
		}, null);
		
		await _bsorService.LoadReplays();

		_syncContext.Post((state) =>
		{
			Loading = false;
		}, null);
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
}

