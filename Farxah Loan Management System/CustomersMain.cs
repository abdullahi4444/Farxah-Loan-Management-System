using System;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Farxah_Loan_Management_System;

namespace Farxah_Loan_Management_System
{
    public partial class CustomersMain : Form
    {
        int selectedCustomerID = 0;
        private Timer searchTimer; // Declare timer at class level

        public CustomersMain()
        {
            InitializeComponent();
            InitializeSearchTimer(); // Initialize timer in constructor
        }

        // ================= FORM LOAD =================
        private void CustomersMain_Load(object sender, EventArgs e)
        {
            LoadCustomers();
            dgvCustomers.DefaultCellStyle.SelectionBackColor = Color.FromArgb(220, 230, 250);
            dgvCustomers.DefaultCellStyle.SelectionForeColor = Color.Black;
            dgvCustomers.ThemeStyle.RowsStyle.SelectionBackColor = Color.FromArgb(220, 230, 250);
            dgvCustomers.ThemeStyle.RowsStyle.SelectionForeColor = Color.Black;

            // Clear search placeholder on focus
            searchTextBox.GotFocus += SearchTextBox_GotFocus;
            searchTextBox.LostFocus += SearchTextBox_LostFocus;
            if (GlobalData.CurrentUserRole == "LoanOfficer")
            {
                usersButton.Visible = false;
            }
        }

        private void InitializeSearchTimer()
        {
            searchTimer = new Timer();
            searchTimer.Interval = 500; // 0.5 seconds delay
            searchTimer.Tick += SearchTimer_Tick;
        }

        private void SearchTextBox_GotFocus(object sender, EventArgs e)
        {
            if (searchTextBox.Text == "Search customers ...")
            {
                searchTextBox.Text = "";
                searchTextBox.ForeColor = SystemColors.ControlText;
            }
        }

        private void SearchTextBox_LostFocus(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(searchTextBox.Text))
            {
                searchTextBox.Text = "Search customers ...";
                searchTextBox.ForeColor = Color.Gray;
            }
        }

        // ================= LOAD CUSTOMERS =================
        private void LoadCustomers(string searchTerm = "")
        {
            dgvCustomers.Rows.Clear();

            using (SqlConnection con = new SqlConnection(Connection.connectionString))
            {
                string query = @"
                SELECT 
                    c.CustomerID,
                    c.FullName,
                    c.Phone,
                    c.Address
                FROM Customers c
                LEFT JOIN Loans l ON c.CustomerID = l.CustomerID";

                // Add WHERE clause if search term exists
                if (!string.IsNullOrWhiteSpace(searchTerm) && searchTerm != "Search customers ...")
                {
                    query += @" WHERE (c.FullName LIKE @SearchTerm 
                               OR c.Phone LIKE @SearchTerm 
                               OR c.Address LIKE @SearchTerm)";
                }

                query += @" GROUP BY 
                    c.CustomerID,
                    c.FullName,
                    c.Phone,
                    c.Address
                ORDER BY c.CustomerID";

                SqlCommand cmd = new SqlCommand(query, con);

                // Add parameter if search term exists
                if (!string.IsNullOrWhiteSpace(searchTerm) && searchTerm != "Search customers ...")
                {
                    cmd.Parameters.AddWithValue("@SearchTerm", "%" + searchTerm + "%");
                }

                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    dgvCustomers.Rows.Add(
                        reader["CustomerID"],
                        reader["FullName"],
                        reader["Phone"],
                        reader["Address"]
                    );
                }
            }

            dgvCustomers.ClearSelection();

            // Update status
            UpdateSearchStatus(searchTerm);
        }

        private void UpdateSearchStatus(string searchTerm)
        {
            if (!string.IsNullOrWhiteSpace(searchTerm) && searchTerm != "Search customers ...")
            {
                int rowCount = dgvCustomers.Rows.Count;
                guna2HtmlLabel1.Text = $"Search Results: {rowCount} customer(s) found for '{searchTerm}'";
            }
            else
            {
                guna2HtmlLabel1.Text = "Customer Data";
            }
        }

        // ================= SEARCH FUNCTIONALITY =================
        private void SearchTimer_Tick(object sender, EventArgs e)
        {
            searchTimer.Stop();

            string searchText = searchTextBox.Text.Trim();

            // Don't search if it's the placeholder text
            if (searchText == "Search customers ...")
            {
                LoadCustomers("");
                return;
            }

            if (searchText.Length >= 2) // Only search if at least 2 characters
            {
                LoadCustomers(searchText);
            }
            else if (string.IsNullOrEmpty(searchText))
            {
                LoadCustomers("");
            }
        }

        private void searchTextBox_TextChanged(object sender, EventArgs e)
        {
            string searchText = searchTextBox.Text.Trim();

            // Don't search if it's the placeholder text
            if (searchText == "Search customers ...")
            {
                LoadCustomers(""); // Load all customers
                return;
            }

            // Start search timer (debouncing to avoid too many searches)
            searchTimer.Stop();
            searchTimer.Start();
        }

        // Clear search button (optional - you can add a button or use IconRightClick)
        private void ClearSearch()
        {
            searchTextBox.Text = "Search customers ...";
            searchTextBox.ForeColor = Color.Gray;
            LoadCustomers("");
        }

        // ================= VALIDATIONS =================
        private bool ValidateFullName()
        {
            fullNameErrLabel.Text = "";
            string name = FullNameTextBox.Text.Trim();

            if (string.IsNullOrEmpty(name))
            {
                fullNameErrLabel.Text = "❗Full name is required";
                FullNameTextBox.Text = "";
                FullNameTextBox.Focus();
                return false;
            }

            if (name.Split(' ').Length < 2)
            {
                fullNameErrLabel.Text = "❗Enter first and last name";
                FullNameTextBox.Text = "";
                FullNameTextBox.Focus();
                return false;
            }

            if (!name.All(c => char.IsLetter(c) || char.IsWhiteSpace(c)))
            {
                fullNameErrLabel.Text = "❗Name must contain letters only";
                FullNameTextBox.Text = "";
                FullNameTextBox.Focus();
                return false;
            }

            return true;
        }

        private bool ValidatePhone()
        {
            phoneErrTextBox.Text = "";
            string phone = phoneTextBox.Text.Trim();

            if (string.IsNullOrEmpty(phone))
            {
                phoneErrTextBox.Text = "❗Phone is required";
                phoneTextBox.Text = "";
                phoneTextBox.Focus();
                return false;
            }

            if (!phone.All(char.IsDigit))
            {
                phoneErrTextBox.Text = "❗Digits only allowed";
                phoneTextBox.Text = "";
                phoneTextBox.Focus();
                return false;
            }

            if (!(phone.StartsWith("61") || phone.StartsWith("68")))
            {
                phoneErrTextBox.Text = "❗Must start with 61 or 68";
                phoneTextBox.Text = "";
                phoneTextBox.Focus();
                return false;
            }

            if (phone.Length != 9)
            {
                phoneErrTextBox.Text = "❗Phone must be 9 digits";
                phoneTextBox.Text = "";
                phoneTextBox.Focus();
                return false;
            }

            return true;
        }

        private bool ValidateAddress()
        {
            addressErrTextBox.Text = "";

            if (string.IsNullOrWhiteSpace(addressTextBox.Text))
            {
                addressErrTextBox.Text = "❗Address is required";
                addressTextBox.Text = "";
                addressTextBox.Focus();
                return false;
            }

            return true;
        }

        private bool ValidateAll()
        {
            return ValidateFullName() & ValidatePhone() & ValidateAddress();
        }

        // method to check for existing customer
        private bool CustomerExists(string fullName, string phone)
        {
            using (SqlConnection con = new SqlConnection(Connection.connectionString))
            {
                string query = "SELECT COUNT(*) FROM Customers WHERE FullName = @FullName AND Phone = @Phone";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@FullName", fullName);
                cmd.Parameters.AddWithValue("@Phone", phone);

                con.Open();
                int count = Convert.ToInt32(cmd.ExecuteScalar());
                return count > 0;
            }
        }

        // ================= INSERT =================
        private void btnInsert_Click(object sender, EventArgs e)
        {
            if (!ValidateAll()) return;

            string fullName = FullNameTextBox.Text.Trim();
            string phone = phoneTextBox.Text.Trim();

            // Check if customer already exists
            if (CustomerExists(fullName, phone))
            {
                MessageBox.Show("Customer with this name and phone already exists!",
                               "Duplicate Customer",
                               MessageBoxButtons.OK,
                               MessageBoxIcon.Warning);
                return;
            }

            using (SqlConnection con = new SqlConnection(Connection.connectionString))
            {
                string query =
                    "INSERT INTO Customers (FullName, Phone, Address) VALUES (@FullName,@Phone,@Address)";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@FullName", fullName);
                cmd.Parameters.AddWithValue("@Phone", phone);
                cmd.Parameters.AddWithValue("@Address", addressTextBox.Text.Trim());

                con.Open();
                cmd.ExecuteNonQuery();
            }

            // Get current search term
            string searchText = searchTextBox.Text.Trim();
            if (searchText == "Search loans, customers ...")
                searchText = "";

            LoadCustomers(searchText);
            ClearFields();
            MessageBox.Show("Customer inserted successfully");
        }

        // ================= ROW SELECTION =================
        private void dgvCustomers_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvCustomers.SelectedRows.Count > 0)
            {
                DataGridViewRow selectedRow = dgvCustomers.SelectedRows[0];

                selectedCustomerID = Convert.ToInt32(selectedRow.Cells["colID"].Value);
                FullNameTextBox.Text = selectedRow.Cells["colName"].Value.ToString();
                phoneTextBox.Text = selectedRow.Cells["colPhone"].Value.ToString();
                addressTextBox.Text = selectedRow.Cells["colAddress"].Value.ToString();
            }
        }

        private void dgvCustomers_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            selectedCustomerID = Convert.ToInt32(
                dgvCustomers.Rows[e.RowIndex].Cells["colID"].Value);

            FullNameTextBox.Text =
                dgvCustomers.Rows[e.RowIndex].Cells["colName"].Value.ToString();

            phoneTextBox.Text =
                dgvCustomers.Rows[e.RowIndex].Cells["colPhone"].Value.ToString();

            addressTextBox.Text =
                dgvCustomers.Rows[e.RowIndex].Cells["colAddress"].Value.ToString();
        }

        // ================= UPDATE =================
        private void btnUpdate_Click(object sender, EventArgs e)
        {
            if (selectedCustomerID == 0)
            {
                MessageBox.Show("Please select a record first");
                return;
            }

            if (!ValidateAll()) return;

            using (SqlConnection con = new SqlConnection(Connection.connectionString))
            {
                string query = @"UPDATE Customers 
                                 SET FullName=@FullName, Phone=@Phone, Address=@Address
                                 WHERE CustomerID=@CustomerID";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@FullName", FullNameTextBox.Text.Trim());
                cmd.Parameters.AddWithValue("@Phone", phoneTextBox.Text.Trim());
                cmd.Parameters.AddWithValue("@Address", addressTextBox.Text.Trim());
                cmd.Parameters.AddWithValue("@CustomerID", selectedCustomerID);

                con.Open();
                cmd.ExecuteNonQuery();
            }

            // Get current search term
            string searchText = searchTextBox.Text.Trim();
            if (searchText == "Search customers ...")
                searchText = "";

            LoadCustomers(searchText);
            ClearFields();
            MessageBox.Show("Customer updated successfully");
        }

        private bool CustomerHasLoans(int customerId)
        {
            using (SqlConnection con = new SqlConnection(Connection.connectionString))
            {
                string query = "SELECT COUNT(*) FROM Loans WHERE CustomerID = @CustomerID";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@CustomerID", customerId);

                con.Open();
                int count = (int)cmd.ExecuteScalar();
                return count > 0;
            }
        }


        // ================= DELETE =================
        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (selectedCustomerID == 0)
            {
                MessageBox.Show("Please select a record first");
                return;
            }

            if (CustomerHasLoans(selectedCustomerID))
            {
                MessageBox.Show(
                    "❌ Cannot delete this customer.\nThis customer has loan records.",
                    "Delete Blocked",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            if (MessageBox.Show("Delete this customer?",
                "Confirm", MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) == DialogResult.No)
                return;

            using (SqlConnection con = new SqlConnection(Connection.connectionString))
            {
                string query = "DELETE FROM Customers WHERE CustomerID=@CustomerID";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@CustomerID", selectedCustomerID);

                con.Open();
                cmd.ExecuteNonQuery();
            }

            string searchText = searchTextBox.Text.Trim();
            if (searchText == "Search customers ...")
                searchText = "";

            LoadCustomers(searchText);
            ClearFields();

            MessageBox.Show("Customer deleted successfully");
        }


        // ================= CLEAR =================
        private void ClearFields()
        {
            FullNameTextBox.Clear();
            phoneTextBox.Clear();
            addressTextBox.Clear();

            fullNameErrLabel.Text = "";
            phoneErrTextBox.Text = "";
            addressErrTextBox.Text = "";

            selectedCustomerID = 0;
        }

        // ================= LOGOUT / EXIT =================
        private void logoutBtn_Click(object sender, EventArgs e)
        {
            Login login = new Login();
            this.Hide();
            login.Show();
        }

        private void userPicture_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void dashboardBtn_Click(object sender, EventArgs e)
        {
            DashboardMain form = new DashboardMain();
            form.StartPosition = FormStartPosition.Manual;
            form.Location = this.Location;
            form.Show();
            this.Hide();
        }

        private void loanButton_Click(object sender, EventArgs e)
        {
            LoanMain form = new LoanMain();
            form.StartPosition = FormStartPosition.Manual;
            form.Location = this.Location;
            form.Show();
            this.Hide();
        }

        private void paymentsButton_Click(object sender, EventArgs e)
        {
            PaymentsMain form = new PaymentsMain();
            form.StartPosition = FormStartPosition.Manual;
            form.Location = this.Location;
            form.Show();
            this.Hide();
        }

        private void repoetsButton_Click(object sender, EventArgs e)
        {
            ReportsMain form = new ReportsMain();
            form.StartPosition = FormStartPosition.Manual;
            form.Location = this.Location;
            form.Show();
            this.Hide();
        }

        private void usersButton_Click(object sender, EventArgs e)
        {
            UsersMain form = new UsersMain();
            form.StartPosition = FormStartPosition.Manual;
            form.Location = this.Location;
            form.Show();
            this.Hide();
        }

        private void profileButton_Click(object sender, EventArgs e)
        {
            // Check if ProfileMain is already open
            foreach (Form openForm in Application.OpenForms)
            {
                if (openForm is ProfileMain)
                {
                    openForm.WindowState = FormWindowState.Normal;  // restore if minimized
                    openForm.StartPosition = FormStartPosition.CenterScreen;
                    openForm.BringToFront();
                    openForm.Activate();
                    return;
                }
            }

            // Create new ProfileMain
            ProfileMain profileForm = new ProfileMain();

            // Force it to normal state and center on screen
            profileForm.StartPosition = FormStartPosition.CenterScreen;
            profileForm.WindowState = FormWindowState.Normal;
            profileForm.ShowInTaskbar = true;
            profileForm.TopMost = true;

            profileForm.Show();
            profileForm.BringToFront();
            profileForm.Activate();
        }

        private void btnReport_Click(object sender, EventArgs e)
        {
            // Check if ProfileMain is already open
            foreach (Form openForm in Application.OpenForms)
            {
                if (openForm is ProfileMain)
                {
                    openForm.WindowState = FormWindowState.Normal;  // restore if minimized
                    openForm.StartPosition = FormStartPosition.CenterScreen;
                    openForm.BringToFront();
                    openForm.Activate();
                    return;
                }
            }

            // Create new ProfileMain
            ReportFrm reportForm = new ReportFrm();

            // Force it to normal state and center on screen
            reportForm.StartPosition = FormStartPosition.CenterScreen;
            reportForm.WindowState = FormWindowState.Normal;
            reportForm.ShowInTaskbar = true;
            reportForm.TopMost = true;

            reportForm.Show();
            reportForm.BringToFront();
            reportForm.Activate();
        }
    }
}