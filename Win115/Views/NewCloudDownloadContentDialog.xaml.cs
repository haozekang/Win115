using Autofac;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Win115.Models;
using Win115.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Win115.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class NewCloudDownloadContentDialog : ContentDialog
    {
        private NewCloudDownloadViewModel? viewModel;
        private UserInfoModel? _user;

        public object? ViewModel => viewModel;

        public NewCloudDownloadContentDialog(NewCloudDownloadViewModel _viewModel)
        {
            InitializeComponent();
            viewModel = _viewModel;
            _user = App.Resolve<UserInfoModel>();
        }

        private async void btn_show_flyout_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel is null)
            {
                return;
            }
            this.Hide();
            var title = new StackPanel
            {
                Orientation = Orientation.Horizontal,
            };
            title.Children.Add(new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Text = "选择保存目录",
                Margin = new Thickness(10, 0, 0, 0),
            });
            using var scope = App.CreateScope();
            var vm = scope.Resolve<SelectSavePathViewModel>();
            SelectSavePathContentDialog dialog = new SelectSavePathContentDialog(vm);
            dialog.XamlRoot = App.XamlRoot;
            dialog.Title = title;
            dialog.PrimaryButtonText = "确定";
            dialog.SecondaryButtonText = "取消";
            dialog.DefaultButton = ContentDialogButton.Primary;
            dialog.PrimaryButtonCommand = SelectedDirPathCommand;
            dialog.PrimaryButtonCommandParameter = vm;
            await dialog.ShowAsync();

            await this.ShowAsync();
        }

        [RelayCommand]
        private async Task SelectedDirPath(SelectSavePathViewModel vm)
        {
            if (viewModel is null || vm is null)
            {
                return;
            }
            viewModel.SavePath = vm.SavePath;
            viewModel.SavePathId = vm.SavePathId;
        }
    }
}
