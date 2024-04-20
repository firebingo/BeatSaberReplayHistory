using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Messaging.Messages;
using System.Collections.Generic;

namespace ReplayHistoryUI.Messages
{
	public class OpenFolderDialogMessage(FolderPickerOpenOptions options) : ValueChangedMessage<FolderPickerOpenOptions>(options) { }

	public class PickedFolderDialogMessage(IReadOnlyList<IStorageFolder> result) : ValueChangedMessage<IReadOnlyList<IStorageFolder>>(result) { }
}
