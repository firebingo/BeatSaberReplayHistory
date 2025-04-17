using Avalonia.Controls;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using ReplayHistoryUI.Messages;
using ReplayHistoryUI.ViewModels;
using System;

namespace ReplayHistoryUI.Views;

public partial class MainWindow : Window, IDisposable
{
	private bool _isDisposed;
	private readonly WeakReferenceMessenger _messenger;

	public MainWindow()
	{
		InitializeComponent();
		_messenger = Ioc.Default.GetRequiredService<WeakReferenceMessenger>();
		_messenger.Register<OpenSongFilterDialogMessage>(this, async (r, value) =>
		{
			var dialog = new SongFilterWindow
			{
				DataContext = new SongFilterDialogViewModel()
			};
			await dialog.ShowDialog(this);
		});
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
			_messenger.Unregister<OpenSongFilterDialogMessage>(this);
		}

		_isDisposed = true;
	}

	~MainWindow()
	{
		Dispose(false);
	}
}
