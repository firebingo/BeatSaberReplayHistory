using Avalonia.Controls;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using ReplayHistoryUI.Messages;
using System;

namespace ReplayHistoryUI.Views;

public partial class MainView : UserControl, IDisposable
{
	private bool _isDisposed;
	private readonly WeakReferenceMessenger _messenger;

	public MainView()
	{
		InitializeComponent();
		_messenger = Ioc.Default.GetRequiredService<WeakReferenceMessenger>();
		_messenger.Register<OpenFolderDialogMessage>(this, async (r, options) =>
		{
			var items = await TopLevel.GetTopLevel(this)!.StorageProvider.OpenFolderPickerAsync(options.Value);
			_messenger.Send(new PickedFolderDialogMessage(items));
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
			_messenger.Unregister<OpenFolderDialogMessage>(this);
		}

		_isDisposed = true;
	}

	~MainView()
	{
		Dispose(false);
	}
}
