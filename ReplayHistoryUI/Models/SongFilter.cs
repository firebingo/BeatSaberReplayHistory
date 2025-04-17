namespace ReplayHistoryUI.Models
{
	public class SongFilterEntry
	{
		public string Hash { get; set; } = string.Empty;
		public string SongName { get; set; } = string.Empty;
		public string Author { get; set; } = string.Empty;
		public string ListName
		{
			get => $"{SongName} ({Author})";
		}
	}
}
