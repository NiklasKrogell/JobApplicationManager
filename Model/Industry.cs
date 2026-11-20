using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace JobApplicationManager.Model
{
    public class Industry
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public virtual ICollection<Company> Companies { get; set; } = new ObservableCollection<Company>();
    }
}
