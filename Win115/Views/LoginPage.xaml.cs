using LiteDB;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json;
using Org.BouncyCastle.Security;
using RestSharp;
using System;
using System.Threading.Tasks;
using Tanovo.ExtensionMethods;
using Win115.Dtos;
using Win115.Entities;
using Win115.Helpers;
using Win115.Models;
using Win115.Properties;
using Win115.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Win115.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LoginPage : Page
    {
        private LoginViewModel? viewModel = null;
        public object? ViewModel => viewModel;

        public LoginPage(LoginViewModel viewModel)
        {
            this.viewModel = viewModel;
            InitializeComponent();

            _ = RefreshQRCode();
        }

        private async Task RefreshQRCode()
        {
            var hash = DigestUtilities.CalculateDigest("sha256", App.CodeVerifier.GetBytes()).GetBase64String().Replace("+", "-").Replace("/", "_");
            var req = new RestRequest(ApiResource.OpenAuthDeviceCode);
            req.AddOrUpdateParameter("client_id", "100195125");
            req.AddOrUpdateParameter("code_challenge", hash);
            req.AddOrUpdateParameter("code_challenge_method", "sha256");
            req.AlwaysMultipartFormData = true;

            var res = await App.LoginClient.PostAsync<ResponseDTO<OpenAuthDeviceCodeDTO>>(req);
            if (res is null || res.Data?.QrCode is null)
            {
                return;
            }
            Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread().TryEnqueue(async () =>
            {
                img_qrcode.Source = await QRCodeHelper.CreateQrCodeForUrl(res.Data.QrCode);
            });
            var uid = res.Data.Uid;
            var time = res.Data.Time;
            var sign = res.Data.Sign;
            if (this.Tag is not ContentDialog dialog)
            {
                return;
            }
            await Task.Delay(TimeSpan.FromMilliseconds(100));
            var reqCheck = new RestRequest(ApiResource.GetQrCodeStatus);
            reqCheck.AddQueryParameter("uid", uid);
            reqCheck.AddQueryParameter("time", time);
            reqCheck.AddQueryParameter("sign", sign);
            while (true)
            {
                if (dialog.Visibility != Microsoft.UI.Xaml.Visibility.Visible)
                {
                    return;
                }
                var resCheck = await App.QrCodeClient.GetAsync<ResponseDTO<GetQeCodeStatusDTO>>(reqCheck);
                if (resCheck is null || resCheck.Data is null)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(300));
                    continue;
                }
                // 二维码无效，结束轮询
                if (resCheck.State == 0)
                {
                    Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread().TryEnqueue(() =>
                    {
                        viewModel?.ScanInfoVisibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                        viewModel?.ScanFailedVisibility = Microsoft.UI.Xaml.Visibility.Visible;
                    });
                    return;
                }
                // 用户已扫码，显示提醒，并继续请求判断是否完成登录
                if (resCheck.Data.Status == 1)
                {
                    Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread().TryEnqueue(() =>
                    {
                        viewModel?.ScanInfoVisibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                        viewModel?.ScanSuccessfulVisibility = Microsoft.UI.Xaml.Visibility.Visible;
                    });
                }
                else if (resCheck.Data.Status == 2)
                {
                    // 登录成功，跳出循环
                    break;
                }
            }
            string token = string.Empty;
            string refreshToken = string.Empty;
            var reqDoLogin = new RestRequest(ApiResource.OpenDeviceCodeToToken);
            reqDoLogin.AddOrUpdateParameter("uid", uid);
            reqDoLogin.AddOrUpdateParameter("code_verifier", App.CodeVerifier);
            reqDoLogin.AlwaysMultipartFormData = true;

            var resDoLogin = await App.LoginClient.PostAsync(reqDoLogin);
            var tokenInfo = JsonConvert.DeserializeObject<ResponseDTO<OpenDeviceCodeToTokenDTO>>(resDoLogin.Content!);
            if (tokenInfo is null || tokenInfo.Data is null 
                || tokenInfo.Data.AccessToken is null
                || tokenInfo.Data.ExpiresIn == 0
                || tokenInfo.Data.RefreshToken is null)
            {
                return;
            }
            _ = SaveTokenToDbAsync(tokenInfo.Data);
            token = tokenInfo.Data.AccessToken;
            refreshToken = tokenInfo.Data.RefreshToken;
            var expiresIn = tokenInfo.Data.ExpiresIn;
            var refreshExpiresIn = tokenInfo.Data.ExpiresIn;
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(refreshToken))
            {
                return;
            }
            var um = App.Resolve<UserInfoModel>();
            um.AccessToken = token;
            um.RefreshToken = refreshToken;
            um.ExpiresIn = DateTime.Now.AddSeconds(expiresIn);
            var reqUserInfo = new RestRequest(ApiResource.OpenUserInfo);
            try
            {
                var resUserInfo = await App.ProApiClient.GetAsync(reqUserInfo);
                if (resUserInfo is null || resUserInfo.Content is null)
                {
                    return;
                }
                await LogHelper.Trace(resUserInfo.Content);
                var userInfo = JsonConvert.DeserializeObject<ProResponseDTO<OpenUserInfoDTO>>(resUserInfo.Content!);
                if (userInfo is null || userInfo.Data is null)
                {
                    return;
                }
                um.IsLogin = true;
                um.UserId = userInfo.Data.UserId!;
                um.UserName = userInfo.Data.UserName!;
                um.FaceS = userInfo.Data.UserFaceS!;
                um.FaceM = userInfo.Data.UserFaceM!;
                um.FaceL = userInfo.Data.UserFaceL!;
                um.AllTotalSize = userInfo.Data.RtSpaceInfo?.AllTotal?.Size ?? 0;
                um.AllTotalFormat = userInfo.Data.RtSpaceInfo?.AllTotal?.SizeFormat ?? string.Empty;
                um.AllRemainSize = userInfo.Data.RtSpaceInfo?.AllRemain?.Size ?? 0;
                um.AllRemainFormat = userInfo.Data.RtSpaceInfo?.AllRemain?.SizeFormat ?? string.Empty;
                um.AllUseSize = userInfo.Data.RtSpaceInfo?.AllUse?.Size ?? 0;
                um.AllUseFormat = userInfo.Data.RtSpaceInfo?.AllUse?.SizeFormat ?? string.Empty;
                um.VipLevelName = userInfo.Data.VipInfo?.LevelName ?? string.Empty;
                um.VipExpire = userInfo.Data.VipInfo?.Expire > 0 ? DateTimeOffset.FromUnixTimeSeconds(userInfo.Data.VipInfo.Expire.Value).AddHours(8).DateTime : null;

                _ = App.ShowMessageBar($"用户【{um.UserName}】登录成功，欢迎您使用！", "信息", severity: InfoBarSeverity.Success, autoClose: TimeSpan.FromSeconds(3));
                _ = App.SetFace(um.FaceS);
                var vm = App.Resolve<MyFilesViewModel>();
                if (vm is not null)
                {
                    await vm.RefreshFilesCommand.ExecuteAsync(null);
                }
                dialog.Hide();
            }
            catch (Exception ex)
            {
                await LogHelper.Error(ex);
            }
        }

        private async Task SaveTokenToDbAsync(OpenDeviceCodeToTokenDTO data)
        {
            if (data is null)
            {
                return;
            }
            var db = App.Resolve<LiteDatabase>();
            var col = db.GetCollection<TokenEntity>(CollectionResource.Tokens);
            col.DeleteAll();
            col.Insert(new TokenEntity 
            {
                AccessToken = data.AccessToken,
                AccessExpiresIn = DateTime.Now.AddSeconds(data.ExpiresIn),
                RefreshToken = data.RefreshToken,
                RefreshExpiresIn = DateTime.Now.AddYears(1)
            });
        }

        private void btn_refreshQrcode_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            _ = RefreshQRCode();
        }
    }
}
