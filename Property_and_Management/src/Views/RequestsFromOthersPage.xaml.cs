using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Property_and_Management.src.DTO;
using Property_and_Management.src.Viewmodels;

namespace Property_and_Management.src.Views
{
    public sealed partial class RequestsFromOthersPage : Page
    {
        public RequestsFromOthersPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is RequestsFromOthersViewModel vm)
            {
                DataContext = vm;
                return;
            }

            if (DataContext is not RequestsFromOthersViewModel)
            {
                DataContext = App.Services.GetRequiredService<RequestsFromOthersViewModel>();
            }
        }

        private void RequestItem_Tapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is RequestDTO request && request.Id > 0)
            {
                Frame?.Navigate(typeof(ChatView), request.Id);
            }
        }

        private void RequestItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is RequestDTO request && request.Id > 0)
            {
                Frame?.Navigate(typeof(ChatView), request.Id);
            }
        }

        private void DenyButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;
        }
        private async void DenyButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not int requestId)
                return;

            ContentDialog deleteDialog = new ContentDialog
            {
                Title = "Delete Request?",
                Content = $"Are you sure you want to permanently delete this request and all associated active requests? Existing rentals will not be deleted.",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await deleteDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                var vm = DataContext as RequestsFromOthersViewModel;
                vm?.DenyRequest(requestId, "The owner declined your request");
            }
        }
        private void Image_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            if (sender is not Image img)
            {
                return;
            }

            if (img.Source is BitmapImage current &&
                current.UriSource != null &&
                current.UriSource.AbsoluteUri.EndsWith("/Assets/default-game-placeholder.jpg", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (Resources.TryGetValue("DefaultGameImage", out var localResource) && localResource is BitmapImage localImage)
            {
                img.Source = localImage;
                return;
            }

            if (Application.Current.Resources.TryGetValue("DefaultGameImage", out var appResource) && appResource is BitmapImage appImage)
            {
                img.Source = appImage;
                return;
            }

            img.Source = new BitmapImage(new Uri("ms-appx:///Assets/default-game-placeholder.jpg"));
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as RequestsFromOthersViewModel;
            vm?.NextPage();
        }

        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as RequestsFromOthersViewModel;
            vm?.PrevPage();
        }
    }
}
