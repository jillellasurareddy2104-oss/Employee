using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Data;
using System;
using System.Data.SqlClient;

namespace RestaurantAppWPF
{
    /// <summary>
    /// Interaction logic for EmployeeForm.xaml
    /// </summary>
    public partial class EmployeeForm : UserControl
    {
        private ObservableCollection<Employee> _employees = new ObservableCollection<Employee>();
        private Employee? _selectedEmployee;
        // Connection string provided by user
        private readonly string _connectionString = "Data Source=localhost;Database=Testing;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True";

        public EmployeeForm()
        {
            InitializeComponent();

            EmployeesDataGrid.ItemsSource = _employees;

            // load from database
            try
            {
                LoadEmployeesFromDatabase();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load employees: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {

            if (!int.TryParse(EmpIdTextBox.Text.Trim(), out int id))
            {
                MessageBox.Show("Please enter a valid EmpId (integer).", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string name = EmpNameTextBox.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Please enter employee name.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(SalaryTextBox.Text.Trim(), out decimal salary))
            {
                MessageBox.Show("Please enter a valid salary (numeric).", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_employees.Any(x => x.EmpId == id))
            {
                MessageBox.Show("An employee with the same EmpId already exists.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var emp = new Employee { EmpId = id, EmpName = name, Salary = (double)salary };
            try
            {
                InsertEmployeeToDatabase(emp);
                _employees.Add(emp);
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to insert employee: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedEmployee == null)
            {
                MessageBox.Show("Select an employee from the list to update.", "Update", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!int.TryParse(EmpIdTextBox.Text.Trim(), out int id))
            {
                MessageBox.Show("Please enter a valid EmpId (integer).", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string name = EmpNameTextBox.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Please enter employee name.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(SalaryTextBox.Text.Trim(), out decimal salary))
            {
                MessageBox.Show("Please enter a valid salary (numeric).", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Prevent changing to an EmpId that collides with another employee
            if (_selectedEmployee.EmpId != id && _employees.Any(x => x.EmpId == id))
            {
                MessageBox.Show("Another employee with the same EmpId already exists.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            long oldId = _selectedEmployee.EmpId;

            try
            {
                var updated = new Employee { EmpId = id, EmpName = name, Salary = (double)salary };
                UpdateEmployeeInDatabase(oldId, updated);

                // apply changes in-memory
                _selectedEmployee.EmpId = id;
                _selectedEmployee.EmpName = name;
                _selectedEmployee.Salary = (double)salary;
                EmployeesDataGrid.Items.Refresh();
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to update employee: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private void ClearForm()
        {
            EmpIdTextBox.Text = string.Empty;
            EmpNameTextBox.Text = string.Empty;
            SalaryTextBox.Text = string.Empty;
            EmployeesDataGrid.SelectedItem = null;
            _selectedEmployee = null;
        }

        private void EmployeesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EmployeesDataGrid.SelectedItem is Employee emp)
            {
                _selectedEmployee = emp;
                EmpIdTextBox.Text = emp.EmpId.ToString();
                EmpNameTextBox.Text = emp.EmpName;
                SalaryTextBox.Text = emp.Salary.ToString();
            }
            else
            {
                _selectedEmployee = null;
            }
        }

        // Database operations
        private void LoadEmployeesFromDatabase()
        {
            _employees.Clear();
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT EmpId, EmpName, Salary FROM Employee";
                conn.Open();
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        var emp = new Employee
                        {
                            EmpId = rdr.GetInt64(0),
                            EmpName = rdr.IsDBNull(1) ? string.Empty : rdr.GetString(1),
                            Salary = rdr.IsDBNull(2) ? 0 : rdr.GetDouble(2)
                        };
                        _employees.Add(emp);
                    }
                }
            }
        }

        private void InsertEmployeeToDatabase(Employee emp)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "INSERT INTO Employee (EmpId, EmpName, Salary) VALUES (@id, @name, @salary)";
                cmd.Parameters.AddWithValue("@id", emp.EmpId);
                cmd.Parameters.AddWithValue("@name", emp.EmpName ?? string.Empty);
                cmd.Parameters.AddWithValue("@salary", emp.Salary);
                conn.Open();
                int rows = cmd.ExecuteNonQuery();
                if (rows != 1)
                    throw new DataException("Insert did not affect expected number of rows.");
            }
        }

        private void UpdateEmployeeInDatabase(long oldId, Employee updated)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "UPDATE Employee SET EmpId = @newId, EmpName = @name, Salary = @salary WHERE EmpId = @oldId";
                cmd.Parameters.AddWithValue("@newId", updated.EmpId);
                cmd.Parameters.AddWithValue("@name", updated.EmpName ?? string.Empty);
                cmd.Parameters.AddWithValue("@salary", updated.Salary);
                cmd.Parameters.AddWithValue("@oldId", oldId);
                conn.Open();
                int rows = cmd.ExecuteNonQuery();
                if (rows != 1)
                    throw new DataException("Update did not affect expected number of rows.");
            }
        }
    }
}
