using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;
using Farxah_Loan_Management_System;

namespace Farxah_Loan_Management_System
{
    public partial class LoanMain : Form
    {
        int selectedLoanID = 0;

        public LoanMain()
        {
            InitializeComponent();
        }

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

        private void LoanMain_Load(object sender, EventArgs e)
        {
            LoadCustomersToComboBox();
            LoadLoansData();

            // Set default dates
            LoanDateTimePicker.Value = DateTime.Now;
            loanDueDateTimePicker.Value = DateTime.Now.AddMonths(1);

            // Clear all error labels
            ClearErrorLabels();

            dgvLoans.DefaultCellStyle.SelectionBackColor = Color.FromArgb(220, 230, 250);
            dgvLoans.DefaultCellStyle.SelectionForeColor = Color.Black;
            dgvLoans.ThemeStyle.RowsStyle.SelectionBackColor = Color.FromArgb(220, 230, 250);
            dgvLoans.ThemeStyle.RowsStyle.SelectionForeColor = Color.Black;

            dgvLoans.ClearSelection();
            if (GlobalData.CurrentUserRole == "LoanOfficer")
            {
                usersButton.Visible = false;
            }

            // Restrict Loan Date for non-admin users
            if (GlobalData.CurrentUserRole != "Admin")
            {
                LoanDateTimePicker.MinDate = DateTime.Now.Date;
            }
            else
            {
                LoanDateTimePicker.MinDate = DateTimePicker.MinimumDateTime;
            }
        }

        private void ClearErrorLabels()
        {
            CustomerNameErrLabel.Text = "";
            LoanAmountErrLabel.Text = "";
            LoanDateErrLabel.Text = "";
            loanDueDateErrLabel.Text = "";
        }

        private void LoadCustomersToComboBox()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(Connection.connectionString))
                {
                    string query = "SELECT CustomerID, FullName FROM Customers ORDER BY FullName";
                    SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    // Add a blank/default option
                    DataRow blankRow = dt.NewRow();
                    blankRow["FullName"] = "-- Select Customer --";
                    blankRow["CustomerID"] = 0;
                    dt.Rows.InsertAt(blankRow, 0);

                    // Set up the ComboBox
                    cmbCustomerName.DisplayMember = "FullName";
                    cmbCustomerName.ValueMember = "CustomerID";
                    cmbCustomerName.DataSource = dt;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading customers: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadLoansData()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(Connection.connectionString))
                {
                    string query = @"
                    SELECT 
                        l.LoanID,
                        c.FullName AS CustomerName,
                        l.LoanAmount,
                        l.Status,
                        ISNULL(l.LoanDate, GETDATE()) AS LoanDate,
                        ISNULL(l.DueDate, DATEADD(MONTH, 1, GETDATE())) AS DueDate
                    FROM Loans l
                    INNER JOIN Customers c ON l.CustomerID = c.CustomerID
                    ORDER BY l.LoanID DESC";

                    SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    // Clear existing rows
                    dgvLoans.Rows.Clear();

                    // Add data to DataGridView
                    foreach (DataRow row in dataTable.Rows)
                    {
                        dgvLoans.Rows.Add(
                            row["LoanID"],
                            row["CustomerName"] != DBNull.Value ? row["CustomerName"].ToString() : "",
                            row["LoanAmount"] != DBNull.Value ? row["LoanAmount"] : 0,
                            row["Status"] != DBNull.Value ? row["Status"].ToString() : "Unknown",
                            row["LoanDate"] != DBNull.Value ? Convert.ToDateTime(row["LoanDate"]).ToString("yyyy-MM-dd") : "",
                            row["DueDate"] != DBNull.Value ? Convert.ToDateTime(row["DueDate"]).ToString("yyyy-MM-dd") : ""
                        );
                    }

                    // Format currency column
                    if (dgvLoans.Columns["colAmount"] != null)
                    {
                        dgvLoans.Columns["colAmount"].DefaultCellStyle.Format = "C2";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading loans data: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void refreshBtn_Click(object sender, EventArgs e)
        {
            LoadLoansData();
            ClearForm();
            ClearErrorLabels();
        }

        private void ClearForm()
        {
            selectedLoanID = 0;
            cmbCustomerName.SelectedIndex = 0;
            LoanAmountNumericUpDown.Value = 0;
            LoanDateTimePicker.Value = DateTime.Now;
            loanDueDateTimePicker.Value = DateTime.Now.AddMonths(1);
        }

        private void dgvLoans_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            try
            {
                // Clear error labels when selecting a row
                ClearErrorLabels();

                // Check if the row has data
                DataGridViewRow row = dgvLoans.Rows[e.RowIndex];

                // Get LoanID from the LoanID column
                if (row.Cells["colLoanID"]?.Value == null || string.IsNullOrEmpty(row.Cells["colLoanID"].Value.ToString()))
                {
                    MessageBox.Show("Loan ID is missing.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                selectedLoanID = Convert.ToInt32(row.Cells["colLoanID"].Value);

                // Get customer name from Customer column
                if (row.Cells["colCustomer"]?.Value == null || string.IsNullOrEmpty(row.Cells["colCustomer"].Value.ToString()))
                {
                    MessageBox.Show("Customer name is missing.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cmbCustomerName.SelectedIndex = 0;
                }
                else
                {
                    string customerName = row.Cells["colCustomer"].Value.ToString();
                    cmbCustomerName.Text = customerName;
                }

                // Set loan amount from Amount column
                if (row.Cells["colAmount"]?.Value != null && !string.IsNullOrEmpty(row.Cells["colAmount"].Value.ToString()))
                {
                    string amountValue = row.Cells["colAmount"].Value.ToString();
                    if (decimal.TryParse(amountValue.Replace("$", "").Replace(",", ""), out decimal loanAmount))
                    {
                        LoanAmountNumericUpDown.Value = loanAmount;
                    }
                    else
                    {
                        LoanAmountNumericUpDown.Value = 0;
                    }
                }
                else
                {
                    LoanAmountNumericUpDown.Value = 0;
                }

                // Set loan date from LoanDate column
                if (row.Cells["colLoanDate"]?.Value != null && !string.IsNullOrEmpty(row.Cells["colLoanDate"].Value.ToString()))
                {
                    if (DateTime.TryParse(row.Cells["colLoanDate"].Value.ToString(), out DateTime loanDate))
                    {
                        try
                        {
                            LoanDateTimePicker.Value = loanDate;
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            ClearForm();
                            dgvLoans.ClearSelection();
                            MessageBox.Show(
                                "This loan date is outside the allowed range for your role.",
                                "Date Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning
                            );
                            return;
                        }
                    }
                    else
                    {
                        LoanDateTimePicker.Value = DateTime.Now;
                    }
                }
                else
                {
                    LoanDateTimePicker.Value = DateTime.Now;
                }

                // Set due date from DueDate column
                if (row.Cells["colDueDate"]?.Value != null && !string.IsNullOrEmpty(row.Cells["colDueDate"].Value.ToString()))
                {
                    if (DateTime.TryParse(row.Cells["colDueDate"].Value.ToString(), out DateTime dueDate))
                    {
                        try
                        {
                            loanDueDateTimePicker.Value = dueDate;
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            ClearForm();
                            dgvLoans.ClearSelection();
                            MessageBox.Show(
                                "This due date is outside the allowed range.",
                                "Date Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning
                            );
                            return;
                        }

                    }
                    else
                    {
                        loanDueDateTimePicker.Value = DateTime.Now.AddMonths(1);
                    }
                }
                else
                {
                    loanDueDateTimePicker.Value = DateTime.Now.AddMonths(1);
                }
            }
            catch (FormatException)
            {
                MessageBox.Show("Error loading loan details: Invalid data format.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading loan details: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool ValidateForm()
        {
            bool isValid = true;
            ClearErrorLabels();

            // Validate Customer Name
            if (cmbCustomerName.SelectedIndex <= 0)
            {
                CustomerNameErrLabel.Text = "Please select a customer.";
                isValid = false;
            }

            // Validate Loan Amount
            if (LoanAmountNumericUpDown.Value <= 0)
            {
                LoanAmountErrLabel.Text = "Please enter a valid loan amount.";
                isValid = false;
            }

            // Validate Loan Date (Role-based)
            DateTime selectedLoanDate = LoanDateTimePicker.Value.Date;
            DateTime today = DateTime.Now.Date;

            // Not allowed for anyone
            if (selectedLoanDate > today)
            {
                LoanDateErrLabel.Text = "Loan date cannot be in the future.";
                isValid = false;
            }
            // Only Admin can use past dates
            else if (selectedLoanDate < today && GlobalData.CurrentUserRole != "Admin")
            {
                LoanDateErrLabel.Text = "Only Admin can record past loan dates.";
                isValid = false;
            }


            // Validate Due Date
            if (loanDueDateTimePicker.Value <= LoanDateTimePicker.Value)
            {
                loanDueDateErrLabel.Text = "Due date must be after loan date.";
                isValid = false;
            }

            return isValid;
        }

        private void btnInsert_Click(object sender, EventArgs e)
        {
            try
            {
                // Validate form first
                if (!ValidateForm())
                {
                    return;
                }

                // Get values from form
                int customerID = Convert.ToInt32(cmbCustomerName.SelectedValue);
                decimal loanAmount = LoanAmountNumericUpDown.Value;
                DateTime loanDate = LoanDateTimePicker.Value;
                DateTime dueDate = loanDueDateTimePicker.Value;
                string status = "Active"; // Default status

                using (SqlConnection connection = new SqlConnection(Connection.connectionString))
                {
                    string query = @"INSERT INTO Loans (CustomerID, LoanAmount, Status, LoanDate, DueDate) 
                                   VALUES (@CustomerID, @LoanAmount, @Status, @LoanDate, @DueDate)";

                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@CustomerID", customerID);
                    command.Parameters.AddWithValue("@LoanAmount", loanAmount);
                    command.Parameters.AddWithValue("@Status", status);
                    command.Parameters.AddWithValue("@LoanDate", loanDate);
                    command.Parameters.AddWithValue("@DueDate", dueDate);

                    connection.Open();
                    int rowsAffected = command.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("Loan added successfully!", "Success",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadLoansData();
                        ClearForm();
                        ClearErrorLabels();
                    }
                    else
                    {
                        MessageBox.Show("Failed to add loan.", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error adding loan: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            try
            {
                if (selectedLoanID == 0)
                {
                    MessageBox.Show("Please select a loan to update.", "Validation Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Validate form first
                if (!ValidateForm())
                {
                    return;
                }

                // Get values from form
                int customerID = Convert.ToInt32(cmbCustomerName.SelectedValue);
                decimal loanAmount = LoanAmountNumericUpDown.Value;
                DateTime loanDate = LoanDateTimePicker.Value;
                DateTime dueDate = loanDueDateTimePicker.Value;

                using (SqlConnection connection = new SqlConnection(Connection.connectionString))
                {
                    string query = @"UPDATE Loans 
                                   SET CustomerID = @CustomerID, 
                                       LoanAmount = @LoanAmount, 
                                       LoanDate = @LoanDate,
                                       DueDate = @DueDate
                                   WHERE LoanID = @LoanID";

                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@CustomerID", customerID);
                    command.Parameters.AddWithValue("@LoanAmount", loanAmount);
                    command.Parameters.AddWithValue("@LoanDate", loanDate);
                    command.Parameters.AddWithValue("@DueDate", dueDate);
                    command.Parameters.AddWithValue("@LoanID", selectedLoanID);

                    connection.Open();
                    int rowsAffected = command.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("Loan updated successfully!", "Success",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadLoansData();
                        ClearForm();
                        ClearErrorLabels();
                    }
                    else
                    {
                        MessageBox.Show("Failed to update loan. Loan not found.", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating loan: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (selectedLoanID == 0)
            {
                MessageBox.Show("Please select a loan to delete.", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (SqlConnection con = new SqlConnection(Connection.connectionString))
            {
                string checkQuery = "SELECT COUNT(*) FROM Payments WHERE LoanID = @LoanID";
                SqlCommand checkCmd = new SqlCommand(checkQuery, con);
                checkCmd.Parameters.AddWithValue("@LoanID", selectedLoanID);

                con.Open();
                int paymentCount = (int)checkCmd.ExecuteScalar();

                if (paymentCount > 0)
                {
                    MessageBox.Show(
                        "This loan has payments and cannot be deleted.\nDelete payments first.",
                        "Delete Not Allowed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                DialogResult result = MessageBox.Show(
                    "Are you sure you want to delete this loan?",
                    "Confirm Delete",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    string deleteQuery = "DELETE FROM Loans WHERE LoanID = @LoanID";
                    SqlCommand deleteCmd = new SqlCommand(deleteQuery, con);
                    deleteCmd.Parameters.AddWithValue("@LoanID", selectedLoanID);

                    deleteCmd.ExecuteNonQuery();

                    MessageBox.Show("Loan deleted successfully!", "Success",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);

                    LoadLoansData();
                    ClearForm();
                    ClearErrorLabels();
                }
            }
        }


        private void searchTextBox_TextChanged(object sender, EventArgs e)
        {
            try
            {
                string searchTerm = searchTextBox.Text.Trim();

                if (string.IsNullOrEmpty(searchTerm))
                {
                    LoadLoansData();
                    return;
                }

                using (SqlConnection connection = new SqlConnection(Connection.connectionString))
                {
                    string query = @"
                    SELECT 
                        l.LoanID,
                        c.FullName AS CustomerName,
                        l.LoanAmount,
                        l.Status,
                        ISNULL(l.LoanDate, GETDATE()) AS LoanDate,
                        ISNULL(l.DueDate, DATEADD(MONTH, 1, GETDATE())) AS DueDate
                    FROM Loans l
                    INNER JOIN Customers c ON l.CustomerID = c.CustomerID
                    WHERE c.FullName LIKE @SearchTerm 
                       OR CONVERT(VARCHAR, l.LoanID) LIKE @SearchTerm
                       OR l.Status LIKE @SearchTerm
                    ORDER BY l.LoanID DESC";

                    SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                    adapter.SelectCommand.Parameters.AddWithValue("@SearchTerm", "%" + searchTerm + "%");

                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    // Clear existing rows
                    dgvLoans.Rows.Clear();

                    // Add filtered data to DataGridView
                    foreach (DataRow row in dataTable.Rows)
                    {
                        dgvLoans.Rows.Add(
                            row["LoanID"],
                            row["CustomerName"] != DBNull.Value ? row["CustomerName"].ToString() : "",
                            row["LoanAmount"] != DBNull.Value ? row["LoanAmount"] : 0,
                            row["Status"] != DBNull.Value ? row["Status"].ToString() : "Unknown",
                            row["LoanDate"] != DBNull.Value ? Convert.ToDateTime(row["LoanDate"]).ToString("yyyy-MM-dd") : "",
                            row["DueDate"] != DBNull.Value ? Convert.ToDateTime(row["DueDate"]).ToString("yyyy-MM-dd") : ""
                        );
                    }

                    // Format currency column
                    if (dgvLoans.Columns["colAmount"] != null)
                    {
                        dgvLoans.Columns["colAmount"].DefaultCellStyle.Format = "C2";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error searching loans: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void dgvLoans_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            // Keep this empty or add specific cell content click logic if needed
        }

        // Event handlers for validation as user types/changes values
        private void cmbCustomerName_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbCustomerName.SelectedIndex > 0)
            {
                CustomerNameErrLabel.Text = "";
            }
        }

        private void LoanAmountNumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            if (LoanAmountNumericUpDown.Value > 0)
            {
                LoanAmountErrLabel.Text = "";
            }
        }

        private void loanDateTimePicker_ValueChanged(object sender, EventArgs e)
        {
            if (LoanDateTimePicker.Value <= DateTime.Now)
            {
                LoanDateErrLabel.Text = "";
            }

            // Also validate due date if loan date changes
            if (loanDueDateTimePicker.Value > LoanDateTimePicker.Value)
            {
                loanDueDateErrLabel.Text = "";
            }
        }

        private void loanDueDateTimePicker_ValueChanged(object sender, EventArgs e)
        {
            if (loanDueDateTimePicker.Value > LoanDateTimePicker.Value)
            {
                loanDueDateErrLabel.Text = "";
            }
        }

        private void guna2PictureBox2_Click(object sender, EventArgs e)
        {
            ClearForm();
        }

        private void dashboardBtn_Click(object sender, EventArgs e)
        {
            DashboardMain form = new DashboardMain();
            form.StartPosition = FormStartPosition.Manual;
            form.Location = this.Location;
            form.Show();
            this.Hide();
        }

        private void customersButton_Click(object sender, EventArgs e)
        {
            CustomersMain form = new CustomersMain();
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