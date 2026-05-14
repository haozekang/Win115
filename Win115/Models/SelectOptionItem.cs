using CommunityToolkit.Mvvm.ComponentModel;
using System;
using Tanovo.ExtensionMethods;

namespace Win115.Models
{
    public partial class SelectOptionItem : ObservableObject
    {
        [ObservableProperty]
        public partial long Id { get; set; }

        [ObservableProperty]
        public partial string DisplayName { get; set; }

        [ObservableProperty]
        public partial object? Tag { get; set; } = null;

        public SelectOptionItem(Enum @enum)
        {
            Id = long.Parse(Enum.Format(@enum.GetType(), @enum, "d"));
            DisplayName = @enum.GetName();
        }

        public SelectOptionItem(long id, string name)
        {
            Id = id;
            DisplayName = name;
        }

        public SelectOptionItem(string id, string name)
        {
            Id = long.Parse(id);
            DisplayName = name;
        }

        public SelectOptionItem(long id, string name, object tag)
        {
            Id = id;
            Tag = tag;
            DisplayName = name;
        }

        public SelectOptionItem(string id, string name, object tag)
        {
            Id = long.Parse(id);
            Tag = tag;
            DisplayName = name;
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}
