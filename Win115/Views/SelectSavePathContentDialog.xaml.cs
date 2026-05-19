using CommunityToolkit.WinUI.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Tanovo.ExtensionMethods;
using Win115.Dtos;
using Win115.Models;
using Win115.Properties;
using Win115.ViewModels;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Win115.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SelectSavePathContentDialog : ContentDialog
    {
        private SelectSavePathViewModel? viewModel;
        private UserInfoModel? _user;
        private SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public object? ViewModel => viewModel;

        public SelectSavePathContentDialog(SelectSavePathViewModel _viewModel)
        {
            InitializeComponent();
            viewModel = _viewModel;
            _user = App.Resolve<UserInfoModel>();

            var root = new TreeNodeModel
            {
                Name = "根目录",
                HasChildren = true
            };

            root.Children.Add(new TreeNodeModel
            {
                Name = "Loading..."
            });

            tv.RootNodes.Add(CreateNode(root));
        }

        private async void TreeView_Expanding(TreeView sender, TreeViewExpandingEventArgs args)
        {
            await _semaphore.WaitAsync();
            try
            {
                if (args.Node.Content is not TreeNodeModel model)
                {
                    return;
                }
                if (model.IsLoaded)
                {
                    if (model.Children.Count <= 0)
                    {
                        args.Node.HasUnrealizedChildren = false;
                    }
                    return;
                }
                // 清除占位节点
                model.Children.Clear();
                args.Node.Children.Clear();
                var req = new RestRequest(ApiResource.OpenUfileFiles);
                if (model.Id is null || model.Id <= 0)
                {
                    req.AddQueryParameter("cid", 0);
                }
                else
                {
                    req.AddQueryParameter("cid", $"{model.Id}");
                }
                req.AddQueryParameter("limit", 1150);
                req.AddQueryParameter("offset", 0);
                req.AddQueryParameter("asc", 0);
                req.AddQueryParameter("o", "user_utime");
                req.AddQueryParameter("custom_order", "1");
                req.AddQueryParameter("cur", 1);
                req.AddQueryParameter("show_dir", 1);

                var res = await App.ProApiClient.GetAsync(req);
                if (!res.IsSuccessful || res.Content.IsBlank())
                {
                    return;
                }
                var dto = JsonConvert.DeserializeObject<OpenUfileFilesDTO>(res.Content);
                if (dto is null)
                {
                    return;
                }
                if (dto.State != true)
                {
                    return;
                }
                if (dto.Data is null)
                {
                    return;
                }

                foreach (var d in dto.Data)
                {
                    if (d.FC != "0")
                    {
                        continue;
                    }
                    var child = new TreeNodeModel
                    {
                        Name = d.FN ?? "",
                        Id = d.FId.ToLong(),
                        HasChildren = true,
                    };
                    model.Children.Add(child);
                    args.Node.Children.Add(CreateNode(child));
                }
                model.IsLoaded = true;
                args.Node.IsExpanded = true;
            }
            finally
            {
                _semaphore.Release();
            }
        }
        private TreeViewNode CreateNode(TreeNodeModel model)
        {
            var node = new TreeViewNode
            {
                Content = model,
                HasUnrealizedChildren = true
            };

            foreach (var child in model.Children)
            {
                node.Children.Add(CreateNode(child));
            }

            return node;
        }

        private void tv_SelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs args)
        {
            if (viewModel is null || args.AddedItems.Count <= 0)
            {
                return;
            }
            if (args.AddedItems.First() is not TreeViewNode _node || _node.Content is not TreeNodeModel node)
            {
                return;
            }
            viewModel.SavePathId = node.Id;
            var names = new List<string>();
            while (true)
            {
                if (_node is null || _node.Content is not TreeNodeModel)
                {
                    break;
                }
                node = (TreeNodeModel)_node.Content;
                names.Insert(0, node.Name);
                _node = _node.Parent;
            }
            viewModel.SavePath = string.Join(" > ", names);
        }
    }
}
