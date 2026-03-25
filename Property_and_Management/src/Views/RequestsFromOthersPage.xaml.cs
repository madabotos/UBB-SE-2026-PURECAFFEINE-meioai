using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Property_and_Management.src.DTO;
using Property_and_Management.src.Repository;
using Property_and_Management.src.Service;
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
                // if (ItemsListView != null) ItemsListView.ItemsSource = vm.PagedRequests;
                return;
            }

            if (DataContext is not RequestsFromOthersViewModel)
            {
                var requestService = new RequestService();
                requestService.SetRequestRepository(new RequestRepository());

                DataContext = new RequestsFromOthersViewModel(requestService);
            }
        }

        private void RequestItem_Tapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is RequestDTO request && request.Id > 0)
            {
                Frame?.Navigate(typeof(ChatView), request.Id);  // [UI-ORQ-04]
            }
        }

        private void RequestItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is RequestDTO request && request.Id > 0)
            {
                Frame?.Navigate(typeof(ChatView), request.Id);  // [UI-ORQ-04]
            }
        }

        private void Image_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            if (sender is not Image img)
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

            img.Source = new BitmapImage(new Uri("ms-appx:///Assets/default-game-placeholder.png"));
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
