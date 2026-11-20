using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace JobApplicationManager.Model
{
    public class Industry : ObservableObject
    {
        private string _name = string.Empty;

        public int Id { get; set; }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public virtual ICollection<Company> Companies { get; set; } = new ObservableCollection<Company>();

        public override string ToString() => Name;
    }
}
