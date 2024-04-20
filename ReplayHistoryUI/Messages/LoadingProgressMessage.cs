using CommunityToolkit.Mvvm.Messaging.Messages;

namespace ReplayHistoryUI.Messages
{
	public class LoadingProgressMessage(float value) : ValueChangedMessage<float>(value) { }
}
