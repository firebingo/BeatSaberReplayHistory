using System.Text.Json;

namespace ReplayHistoryUI.Util
{
	public static class DefaultJsonOptions
	{
		public static readonly JsonSerializerOptions DefaultOptions = new JsonSerializerOptions()
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			WriteIndented = false
		};

		public static readonly JsonSerializerOptions WriteIndentedCamelCase = new JsonSerializerOptions()
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			WriteIndented = true
		};

		public static readonly JsonSerializerOptions CaseInsensitive = new JsonSerializerOptions()
		{
			PropertyNameCaseInsensitive = true,
			WriteIndented = false
		};
	}
}
