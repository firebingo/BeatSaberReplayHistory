using CommunityToolkit.Mvvm.Messaging.Messages;
using System.Collections.Generic;

namespace ReplayHistoryUI.Messages
{
	public class OpenSongFilterDialogMessage(bool value) : ValueChangedMessage<bool>(value) { }

	public class SongFilterChangedMessage(List<string> songs) : ValueChangedMessage<List<string>>(songs) { }
}
