using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using System;

namespace Win115.Models
{
    public partial class UserInfoModel : ObservableObject
    {
        public Visibility LoginButtonVisibility => IsLogin == true ? Visibility.Collapsed : Visibility.Visible;

        public Visibility UserInfoButtonVisibility => IsLogin == true ? Visibility.Visible : Visibility.Collapsed;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(UserInfoButtonVisibility))]
        [NotifyPropertyChangedFor(nameof(LoginButtonVisibility))]
        public partial bool IsLogin { get; set; } = false;

        [ObservableProperty]
        public partial string UserId { get; set; } = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(WindowDisplayName))]
        public partial string UserName { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string FaceS { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string FaceM { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string FaceL { get; set; } = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(AllTotalSizeD))]
        public partial long AllTotalSize { get; set; } = 0;

        public double AllTotalSizeD => AllTotalSize;

        [ObservableProperty]
        public partial string AllTotalFormat { get; set; } = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(AllRemainSizeD))]
        public partial long AllRemainSize { get; set; } = 0;

        public double AllRemainSizeD => AllRemainSize;

        [ObservableProperty]
        public partial string AllRemainFormat { get; set; } = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(AllUseSizeD))]
        public partial long AllUseSize { get; set; } = 0;

        public double AllUseSizeD => AllUseSize;

        [ObservableProperty]
        public partial string AllUseFormat { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string VipLevelName { get; set; } = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ExpiresInText))]
        public partial DateTime? VipExpire { get; set; } = null;

        public string? ExpiresInText => VipExpire.HasValue ? VipExpire.Value.ToString("yyyy-MM-dd HH:mm:ss") + "到期" : string.Empty;

        public string RefreshToken { get; set; } = string.Empty;

        public string AccessToken { get; set; } = string.Empty;

        public DateTime? ExpiresIn { get; set; } = null;

        public string WindowDisplayName => $"用户：{UserName}";
    }
}
