using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using ReplayHistoryUI.Messages;
using ReplayHistoryUI.Models;
using ReplayHistoryUI.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace ReplayHistoryUI.ViewModels;

public partial class SongFilterDialogViewModel : ViewModelBase
{
	private readonly SynchronizationContext _syncContext;
	private readonly WeakReferenceMessenger _messenger;
	private readonly BSORService _bsorService;
	private readonly List<SongFilterEntry> _songs;
	Timer _searchDebounceTimer;

	[ObservableProperty]
	public List<SongFilterEntry> _songsList;
	private ObservableCollection<SongFilterEntry> _selectedSongs;
	public ObservableCollection<SongFilterEntry> SelectedSongs
	{
		get => _selectedSongs;
		set
		{
			SetProperty(ref _selectedSongs, value);
		}
	}

	private string _searchText;
	public string SearchText
	{
		get => _searchText;
		set
		{
			SetProperty(ref _searchText, value);
			UpdateSearchTextDebounce();
		}
	}

	public SongFilterDialogViewModel()
	{
		_syncContext = SynchronizationContext.Current!;
		_messenger = Ioc.Default.GetRequiredService<WeakReferenceMessenger>();
		_bsorService = Ioc.Default.GetRequiredService<BSORService>();
		_songs = _bsorService.GetSongFilterList();
		_songsList = [.. _songs.OrderBy(x => x.SongName)];
		_selectedSongs = [];
		_searchText = string.Empty;
		_searchDebounceTimer = new Timer((x) => UpdateSearchText());
	}

	public void UpdateSearchTextDebounce()
	{
		_searchDebounceTimer.Change(500, Timeout.Infinite);
	}

	public void UpdateSearchText()
	{
		_syncContext.Post((state) =>
		{
			SongsList = [.. _songs.Where(x => x.ListName.Contains(SearchText, System.StringComparison.OrdinalIgnoreCase)).OrderBy(x => x.SongName)];
		}, null);
	}

	public void ConfirmSelection(Window window)
	{
		_messenger.Send(new SongFilterChangedMessage(_selectedSongs.Select(x => x.Hash).ToList()));
		window.Close();
	}
}

