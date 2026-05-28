using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanovo.ExtensionMethods;
using Win115.Enums;
using Win115.Models;

namespace Win115.ViewModels
{
    public partial class UserViewModel : ObservableRecipient
    {
        [ObservableProperty]
        public partial UserInfoModel User { get; set; }

        public UserViewModel(UserInfoModel User) 
        {
            this.User = User;

            Messenger.Register<ObservableRecipient, ValueChangedMessage<WeakMessengerTypes>, string>(this, nameof(MainViewModel), (r, msgType) =>
            {
                switch (msgType.Value)
                {
                    case WeakMessengerTypes.SignOut:
                        ClearData();
                        break;
                }
            });
        }

        /// <summary>
        /// 登出后，清理
        /// </summary>
        public void ClearData()
        {
            App.JumpPage(MenuKeys.MyFiles);
        }
    }
}
