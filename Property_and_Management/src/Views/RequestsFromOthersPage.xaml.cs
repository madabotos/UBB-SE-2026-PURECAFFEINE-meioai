using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Property_and_Management.src.DTO;
using Property_and_Management.src.Viewmodels;

public sealed partial class RequestsFromOthersPage : Page
{
    public RequestsFromOthersPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        if (e.Parameter is RequestsFromOthersViewModel vm)
        {
            DataContext = vm;
            if (ItemsListView != null) ItemsListView.ItemsSource = vm.pagedRequests;
        }
    }

    private void RequestItem_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is RequestDTO request && request.Id > 0)
        {
            Frame?.Navigate(typeof(ChatPage), request.Id);  // [UI-ORQ-04]
        }
    }

    private void Image_ImageFailed(object sender, ExceptionRoutedEventArgs e)
    {
        if (sender is Image img) img.Source = Application.Current.Resources["DefaultGameImage"] as BitmapImage;
    }
}
