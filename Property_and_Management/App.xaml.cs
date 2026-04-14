using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using H.NotifyIcon;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.AppNotifications;
using Property_and_Management.Src.DataTransferObjects;
using Property_and_Management.Src.Interface;
using Property_and_Management.Src.Mapper;
using Property_and_Management.Src.Model;
using Property_and_Management.Src.Repository;
using Property_and_Management.Src;
using Property_and_Management.Src.Service;
using Property_and_Management.Src.Service.Listeners;
using Property_and_Management.Src.Viewmodels;
using Property_and_Management.Src.Views;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace Property_and_Management
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private const int DefaultUserIdentifier = 1;
        private const int UserIdentifierArgumentIndex = 1;
        private const int KeyPartIndex = 0;
        private const int ValuePartIndex = 1;
        private const int SplitKeyValuePartsCount = 2;
        private const int DevModePrimaryUserIdentifier = 1;
        private const int DevModeSecondaryUserIdentifier = 2;
        private const int NoRunningProcessCount = 0;
        private const int SuccessExitCode = 0;

        private const string TwoWindowsEnvironmentKey = "TWO_WINDOWS";
        private const string EnabledEnvironmentValue = "true";
        private const string NotificationNavigationArgumentKey = "navigate";

        public static IServiceProvider Services { get; private set; } = default!;

        // Public application state
        public static Window MainWindow { get; set; }
        public Frame RootFrame { get; set; }
        public string AppUserModelId { get; }
        public int CurrentUserIdentifier { get; }
        public NotificationsViewModel NotificationsViewModel { get; private set; }

        // Tray icon
        private TaskbarIcon trayIcon;

        // Handles for processes spawned by two-window dev mode. Retained so the
        // ProcessExit cleanup handler can kill them; otherwise closing user 1's
        // window leaves the NotificationServer and user 2 client as orphans.
        private static Process? notificationServerProcess;
        private static Process? secondClientProcess;

        // Private dependencies and state
        private Window? mainWindow;
        private INotificationRepository notificationRepository;
        private INotificationService notificationService;
        private IGameRepository gameRepository;
        private IGameService gameService;
        private readonly NotificationManager notificationManager;

        /// <summary>
        /// Initializes the singleton application object.
        /// </summary>
        public App()
        {
            CurrentUserIdentifier = GetUserIdFromArgs();

            // Ensure the database, tables, and demo data exist before anything
            // else (DI resolution, ViewModel constructors) touches the DB.
            DatabaseInitializer.EnsureDatabaseInitialized();

            // Two-window dev mode: user 1 spawns the notification server + a second client
            if (CurrentUserIdentifier == DevModePrimaryUserIdentifier && IsTwoWindowsEnabled())
            {
                StartNotificationServer();
                LaunchSecondClient();
            }

            AppUserModelId = $"BoardRent -- user-{CurrentUserIdentifier}";

            // Create manager and wire its generic handlers (handlers may reference fields initialized later)
            notificationManager = new NotificationManager();

            SetupNotificationManager();

            EnsureSingleInstance(AppUserModelId);

            ConfigureServices();
            InitializeServices(CurrentUserIdentifier);

            InitializeComponent();
        }

        private void ConfigureServices()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddSingleton<IMapper<User, UserDataTransferObject>, UserMapper>();
            serviceCollection.AddSingleton<IMapper<Game, GameDataTransferObject>, GameMapper>();
            serviceCollection.AddSingleton<IMapper<Notification, NotificationDataTransferObject>, NotificationMapper>();
            serviceCollection.AddSingleton<IMapper<Rental, RentalDataTransferObject>, RentalMapper>();
            serviceCollection.AddSingleton<IMapper<Request, RequestDataTransferObject>, RequestMapper>();

            // Infrastructure
            serviceCollection.AddSingleton<ICurrentUserContext>(new CurrentUserContext(CurrentUserIdentifier));
            serviceCollection.AddSingleton<IToastNotificationService, ToastNotificationService>();
            serviceCollection.AddSingleton<IServerClient, NotificationClient>();

            // Repositories
            serviceCollection.AddSingleton<IUserRepository, UserRepository>();
            serviceCollection.AddSingleton<IGameRepository, GameRepository>();
            serviceCollection.AddSingleton<IRequestRepository, RequestRepository>();
            serviceCollection.AddSingleton<IRentalRepository, RentalRepository>();
            serviceCollection.AddSingleton<INotificationRepository, NotificationRepository>();

            // Services
            serviceCollection.AddSingleton<IUserService, UserService>();
            serviceCollection.AddSingleton<IGameService, GameService>();
            serviceCollection.AddSingleton<IRentalService, RentalService>();
            serviceCollection.AddSingleton<INotificationService, NotificationService>();
            serviceCollection.AddSingleton<IRequestService, RequestService>();

            // ViewModels
            serviceCollection.AddSingleton<NotificationsViewModel>();
            serviceCollection.AddSingleton<MenuBarViewModel>();
            serviceCollection.AddTransient(serviceProvider => new ListingsViewModel(
                serviceProvider.GetRequiredService<IGameService>(),
                serviceProvider.GetRequiredService<ICurrentUserContext>().CurrentUserIdentifier));
            serviceCollection.AddTransient<CreateGameViewModel>();
            serviceCollection.AddTransient<EditGameViewModel>();
            serviceCollection.AddTransient<CreateRequestViewModel>();
            serviceCollection.AddTransient<CreateRentalViewModel>();
            serviceCollection.AddTransient<RequestsFromOthersViewModel>();
            serviceCollection.AddTransient<RequestsToOthersViewModel>();
            serviceCollection.AddTransient<RentalsFromOthersViewModel>();
            serviceCollection.AddTransient<RentalsToOthersViewModel>();

            Services = serviceCollection.BuildServiceProvider();
        }

        private int GetUserIdFromArgs()
        {
            string[] commandLineArgs = Environment.GetCommandLineArgs(); // first arg is the executable path
            if (commandLineArgs.Length > UserIdentifierArgumentIndex && int.TryParse(commandLineArgs[UserIdentifierArgumentIndex], out int parsedUserIdentifier))
            {
                return parsedUserIdentifier;
            }

            return DefaultUserIdentifier;
        }

        #region Two-window dev mode

        private static string? FindRepoRoot()
        {
            var currentDirectory = new DirectoryInfo(AppContext.BaseDirectory);
            while (currentDirectory != null)
            {
                if (Directory.Exists(System.IO.Path.Combine(currentDirectory.FullName, ".git")))
                {
                    return currentDirectory.FullName;
                }

                currentDirectory = currentDirectory.Parent;
            }
            return null;
        }

        private static bool IsTwoWindowsEnabled()
        {
            try
            {
                var repoRoot = FindRepoRoot();
                if (repoRoot == null)
                {
                    return false;
                }

                var envPath = System.IO.Path.Combine(repoRoot, ".env");
                if (!File.Exists(envPath))
                {
                    return false;
                }

                foreach (var line in File.ReadAllLines(envPath))
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith('#') || !trimmed.Contains('='))
                    {
                        continue;
                    }

                    var parts = trimmed.Split('=', SplitKeyValuePartsCount);
                    if (parts[KeyPartIndex].Trim() == TwoWindowsEnvironmentKey)
                    {
                        return parts[ValuePartIndex].Trim().Equals(EnabledEnvironmentValue, StringComparison.OrdinalIgnoreCase);
                    }
                }
            }
            catch
            {
            }
            return false;
        }

        private static void StartNotificationServer()
        {
            try
            {
                if (Process.GetProcessesByName("NotificationServer").Length > NoRunningProcessCount)
                {
                    return;
                }

                var repoRoot = FindRepoRoot();
                if (repoRoot == null)
                {
                    return;
                }

                var serverBinDir = System.IO.Path.Combine(repoRoot, "NotificationServer", "bin");
                if (!Directory.Exists(serverBinDir))
                {
                    return;
                }

                var serverExe = Directory.GetFiles(serverBinDir, "NotificationServer.exe", SearchOption.AllDirectories)
                    .FirstOrDefault();
                if (serverExe == null)
                {
                    return;
                }

                notificationServerProcess = Process.Start(new ProcessStartInfo
                {
                    FileName = serverExe,
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Minimized
                });
            }
            catch
            {
            }
        }

        private static void LaunchSecondClient()
        {
            try
            {
                var currentExe = Environment.ProcessPath;
                if (currentExe == null)
                {
                    return;
                }

                secondClientProcess = Process.Start(new ProcessStartInfo
                {
                    FileName = currentExe,
                    Arguments = DevModeSecondaryUserIdentifier.ToString(),
                    UseShellExecute = true,
                    WorkingDirectory = System.IO.Path.GetDirectoryName(currentExe)
                });
            }
            catch
            {
            }
        }

        private static void KillSpawnedChildProcesses()
        {
            try
            {
                if (secondClientProcess != null && !secondClientProcess.HasExited)
                {
                    secondClientProcess.Kill(entireProcessTree: true);
                }
            }
            catch
            {
            }

            try
            {
                if (notificationServerProcess != null && !notificationServerProcess.HasExited)
                {
                    notificationServerProcess.Kill(entireProcessTree: true);
                }
            }
            catch
            {
            }
        }

        #endregion

        private void SetupNotificationManager()
        {
            // Ensure the manager is unregistered when the process exits
            AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) =>
            {
                notificationManager.Unregister();
                (notificationService as IDisposable)?.Dispose();
                KillSpawnedChildProcesses();
            };

            // When a notification is clicked, bring the window to foreground and optionally navigate
            notificationManager.NotificationClicked += (sender, eventArguments) =>
            {
                mainWindow?.DispatcherQueue.TryEnqueue(() =>
                {
                    mainWindow?.Activate();

                    if (eventArguments.Arguments.ContainsKey(NotificationNavigationArgumentKey) &&
                        eventArguments.Arguments[NotificationNavigationArgumentKey] == nameof(NotificationsPage))
                    {
                        ActivateWindow();
                        NavigateToNotificationsWithinShell();
                    }
                });
            };

            notificationManager.Init();
        }

        private void NavigateToNotificationsWithinShell()
        {
            if (RootFrame?.Content is MenuBarView currentShell)
            {
                currentShell.NavigateToNotifications();
                return;
            }

            void OnShellLoaded(object sender, NavigationEventArgs navigationEventArgs)
            {
                if (navigationEventArgs.Content is MenuBarView loadedShell)
                {
                    RootFrame.Navigated -= OnShellLoaded;
                    loadedShell.NavigateToNotifications();
                }
            }

            RootFrame.Navigated += OnShellLoaded;
            RootFrame.Navigate(typeof(MenuBarView), gameService);
        }

        private void EnsureSingleInstance(string appUserModelId)
        {
            var appInstance = Microsoft.Windows.AppLifecycle.AppInstance.FindOrRegisterForKey(appUserModelId);
            if (!appInstance.IsCurrent)
            {
                appInstance.RedirectActivationToAsync(Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().GetActivatedEventArgs()).AsTask().Wait();
                Environment.Exit(SuccessExitCode);
            }

            // Get the current instance and activate the window
            appInstance.Activated += (sender, activationEventArgs) =>
            {
                ActivateWindow();
            };
        }

        private void InitializeServices(int userIdentifier)
        {
            // Initialize navigation frame
            RootFrame = new Frame();

            // Resolve repository/service/viewmodel from DI container
            notificationRepository = Services.GetRequiredService<INotificationRepository>();
            notificationService = Services.GetRequiredService<INotificationService>();
            gameRepository = Services.GetRequiredService<IGameRepository>();
            gameService = Services.GetRequiredService<IGameService>();
            NotificationsViewModel = Services.GetRequiredService<NotificationsViewModel>();

            // Start listening and subscribe for the configured user
            notificationService.StartListening();
            notificationService.SubscribeToServer(userIdentifier);
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            CreateAndShowMainWindow();

            // Wrap your Frame in a Grid so the tray icon can be added later
            Grid rootGrid = new Grid();
            rootGrid.Children.Add(RootFrame);  // Your navigation frame
            MainWindow.Content = rootGrid;            // Set Grid as window content

            RootFrame.Navigate(typeof(MenuBarView), gameService);

            CreateTrayIcon();
        }

        private void CreateAndShowMainWindow()
        {
            MainWindow = mainWindow = new MainWindow();
            mainWindow.Content = RootFrame;
            mainWindow.Activate();

            // Display the AppUserModelId in the window title for debugging / identification
            mainWindow.Title = AppUserModelId;
        }

        private void ActivateWindow()
        {
            mainWindow?.DispatcherQueue.TryEnqueue(() =>
            {
                if (mainWindow is MainWindow win)
                {
                    win.AppWindow.Show();
                }
                mainWindow.Activate();
            });
        }

        private void CreateTrayIcon()
        {
            trayIcon = new TaskbarIcon
            {
                ToolTipText = $"{AppUserModelId}",
                IconSource = new BitmapImage(new Uri(Constants.AppTrayIconUri)),
            };

            // 1. Create a Command for Open
            var openCommand = new XamlUICommand();
            openCommand.ExecuteRequested += (commandSender, executeRequestedEventArgs) =>
            {
                ActivateWindow();
            };

            var openItem = new MenuFlyoutItem
            {
                Text = "Open",
                Command = openCommand // Bind to Command instead of Click
            };

            // 2. Create a Command for Exit
            var exitCommand = new XamlUICommand();
            exitCommand.ExecuteRequested += (commandSender, executeRequestedEventArgs) =>
            {
                trayIcon.Dispose();
                Environment.Exit(SuccessExitCode);
            };

            var exitItem = new MenuFlyoutItem
            {
                Text = "Exit",
                Command = exitCommand // Bind to Command instead of Click
            };

            trayIcon.ContextFlyout = new MenuFlyout
            {
                Items =
                {
                    openItem, exitItem
                }
            };

            if (mainWindow.Content is Grid rootGrid)
            {
                rootGrid.Children.Add(trayIcon);
            }
        }
    }
}


