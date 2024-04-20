using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using ReplayHistoryUI.Services;
using ReplayHistoryUI.ViewModels;
using ReplayHistoryUI.Views;

namespace ReplayHistoryUI;

public partial class App : Application
{
	public override void Initialize()
	{
		AvaloniaXamlLoader.Load(this);
	}

	public override void OnFrameworkInitializationCompleted()
	{
		// Line below is needed to remove Avalonia data validation.
		// Without this line you will get duplicate validations from both Avalonia and CT
		BindingPlugins.DataValidators.RemoveAt(0);

		var collection = new ServiceCollection();
		var services = ConfigureServices(collection);
		Ioc.Default.ConfigureServices(services);

		var mainViewModel = services.GetRequiredService<MainViewModel>();
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			desktop.MainWindow = new MainWindow
			{
				DataContext = mainViewModel
			};
			desktop.MainWindow.Closing += OnClosing;
		}
		else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
		{
			singleViewPlatform.MainView = new MainView
			{
				DataContext = mainViewModel
			};
			singleViewPlatform.MainView.Unloaded += OnUnloaded;
		}

		base.OnFrameworkInitializationCompleted();
	}

	private void OnUnloaded(object? sender, RoutedEventArgs e)
	{
		OnExit();
	}

	private void OnClosing(object? sender, WindowClosingEventArgs e)
	{
		OnExit();
	}

	private void OnExit()
	{
		//Dispose services here if needed
	}

	private static ServiceProvider ConfigureServices(ServiceCollection services)
	{
		services.AddSingleton<BSORService>();
		services.AddSingleton<ConfigService>();
		services.AddSingleton<LogService>();
		services.AddSingleton<WeakReferenceMessenger>();

		services.AddSingleton<MainViewModel>();
		services.AddTransient<ChartViewModel>();


		return services.BuildServiceProvider();
	}
}
