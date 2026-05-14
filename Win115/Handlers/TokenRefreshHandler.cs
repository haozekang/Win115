using LiteDB;
using RestSharp;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Tanovo.ExtensionMethods;
using Win115.Dtos;
using Win115.Entities;
using Win115.Models;
using Win115.Properties;
using Win115.ViewModels;

namespace Win115.Handlers
{
    public class TokenRefreshHandler : DelegatingHandler
    {
        private readonly SemaphoreSlim _refreshSemaphore = new(1, 1);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage? response = null;
            // 1. 添加访问令牌到请求头
            var _user = App.Resolve<UserInfoModel>();
            var accessToken = _user.AccessToken;
            if (_user.ExpiresIn <= DateTime.Now)
            {
                var refreshed = await RefreshTokenAsync(_user, cancellationToken);

                if (!refreshed)
                {
                    App.DispatcherQueue?.TryEnqueue(async () =>
                    {
                        var mvm = App.Resolve<MainViewModel>();
                        mvm?.SignOutCommand?.Execute(null);
                        await App.ShowMessageBar("登录失效，请重新扫码登录！", "错误", Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error, autoClose: TimeSpan.FromSeconds(5));
                    }); 
                    return new HttpResponseMessage(HttpStatusCode.Unauthorized);
                }
            }
            if (request.Headers.Any(h => h.Key == "Authorization"))
            {
                request.Headers.Remove("Authorization");
            }
            request.Headers.Add("Authorization", $"Bearer {accessToken}");

            if (request.Headers.Any(h => h.Key == "User-Agent"))
            {
                request.Headers.Remove("User-Agent");
            }
            request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/140.0.0.0 Safari/537.36 Edg/140.0.0.0");

            // 2. 发送请求
            try
            {
                response = await base.SendAsync(request, cancellationToken);
            }
            catch
            {
                response = new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }

            // 3. 检查是否返回401未授权
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                // 4. 移除旧的，尝试刷新token
                var refreshed = await RefreshTokenAsync(_user, cancellationToken);

                if (refreshed)
                {
                    // 5. 刷新成功，重新发送原请求
                    var newAccessToken = _user.AccessToken;
                    if (request.Headers.Any(h => h.Key == "Authorization"))
                    {
                        request.Headers.Remove("Authorization");
                    }
                    request.Headers.Add("Authorization", $"Bearer {newAccessToken}");

                    // 克隆请求并重新发送（原请求可能已被消费）
                    response = await base.SendAsync(request, cancellationToken);
                }
                else
                {
                    App.DispatcherQueue?.TryEnqueue(async() => 
                    {
                        var mvm = App.Resolve<MainViewModel>();
                        mvm?.SignOutCommand?.Execute(null);
                        await App.ShowMessageBar("登录失效，请重新扫码登录！", "错误", Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error, autoClose: TimeSpan.FromSeconds(5));
                    });
                }
            }

            return response;
        }

        private async Task<bool> RefreshTokenAsync(UserInfoModel _user, CancellationToken cancellationToken)
        {
            // 使用信号量确保同一时间只有一个请求在刷新token
            await _refreshSemaphore.WaitAsync(cancellationToken);
            try
            {
                // 双重检查：可能其他线程已经刷新了token
                if (!await IsTokenExpiredAsync(_user))
                {
                    return true;
                }

                var refreshToken = _user.RefreshToken;
                if (string.IsNullOrEmpty(refreshToken))
                {
                    return false;
                }

                // 调用刷新token的API
                return await RefreshTokenAsync(refreshToken);
            }
            finally
            {
                _refreshSemaphore.Release();
            }
        }

        public async Task<bool> IsTokenExpiredAsync(UserInfoModel _user)
        {
            if (_user.ExpiresIn is null)
            {
                return true;
            }
            return DateTime.Now.AddMinutes(10) >= _user.ExpiresIn;
        }

        public async Task<bool> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                var reqRefreshToken = new RestRequest(ApiResource.OpenRefreshToken);
                reqRefreshToken.AddOrUpdateParameter("refresh_token", refreshToken);
                reqRefreshToken.AlwaysMultipartFormData = true;

                var newToken = await App.LoginClient.PostAsync<ResponseDTO<OpenRefreshTokenDTO>>(reqRefreshToken);
                if (newToken is null || newToken.Data is null)
                {
                    return false;
                }
                if (newToken.Data.AccessToken.IsBlank() || newToken.Data.RefreshToken.IsBlank())
                {
                    return false;
                }
                await SaveTokensAsync(newToken.Data.AccessToken, newToken.Data.ExpiresIn, newToken.Data.RefreshToken);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }
            finally
            {
                Debug.WriteLine($"===>RefreshTokenAsync");
            }
        }

        public async Task SaveTokensAsync(string accessToken, long expiresAt, string refreshToken)
        {
            var _db = App.Resolve<LiteDatabase>();
            var col = _db.GetCollection<TokenEntity>(CollectionResource.Tokens);
            var find = col.Query().OrderByDescending(x => x.Id).FirstOrDefault();
            if (find is null)
            {
                find = new TokenEntity 
                {
                    AccessToken = accessToken,
                    AccessExpiresIn = DateTime.Now.AddSeconds(expiresAt),
                    RefreshToken = refreshToken,
                    RefreshExpiresIn = DateTime.Now.AddYears(1),
                };
                col.Insert(find);
            }
            else
            {
                find.AccessToken = accessToken;
                find.AccessExpiresIn = DateTime.Now.AddSeconds(expiresAt);
                find.RefreshToken = refreshToken;
                col.Update(find);
            }
        }
    }
}
