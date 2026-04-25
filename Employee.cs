using System.ComponentModel;

namespace RestaurantAppWPF
{
    public class Employee : INotifyPropertyChanged
    {
        private long _empId;
        private string _empName;
        private double _salary;

        public long EmpId
        {
            get => _empId;
            set { if (_empId != value) { _empId = value; OnPropertyChanged(nameof(EmpId)); } }
        }

        public string EmpName
        {
            get => _empName;
            set { if (_empName != value) { _empName = value; OnPropertyChanged(nameof(EmpName)); } }
        }

        public double Salary
        {
            get => _salary;
            set { if (_salary != value) { _salary = value; OnPropertyChanged(nameof(Salary)); } }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
