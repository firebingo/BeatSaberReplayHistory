using CommunityToolkit.Mvvm.Messaging.Messages;

namespace ReplayHistoryUI.Messages
{
	public class DirectoryChangedMessage(bool changed) : ValueChangedMessage<bool>(changed) { }
}
