using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Win115.Models
{
    public partial class TreeNodeModel : ObservableObject
    {
        [ObservableProperty]
        public partial string Name { get; set; } = "";

        [ObservableProperty]
        public partial long? Id { get; set; } = null;

        public List<TreeNodeModel> Children { get; set; } = new();

        [ObservableProperty]
        public partial bool IsLoaded { get; set; }

        [ObservableProperty]
        public partial bool HasChildren { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
