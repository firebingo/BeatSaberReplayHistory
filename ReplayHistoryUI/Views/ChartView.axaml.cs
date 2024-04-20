using Avalonia.Controls;
using CommunityToolkit.Mvvm.DependencyInjection;
using ReplayHistoryUI.ViewModels;

namespace ReplayHistoryUI.Views
{
	public partial class ChartView : UserControl
	{
		public ChartView()
		{
			InitializeComponent();
			DataContext = Ioc.Default.GetRequiredService<ChartViewModel>();
		}
	}
}
