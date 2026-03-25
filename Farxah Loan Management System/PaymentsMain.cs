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
    public partial class PaymentsMain : Form
    {
        int selectedPaymentID = 0; // track selected row

        public PaymentsMain()
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

        private void LoadPayments()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(Connection.connectionString))
                {
                    string query = @"
                    SELECT 
                        p.PaymentID,
                        p.LoanID,
                        c.FullName AS Customer,
                        p.PaidAmount,
                        p.PaymentDate,
                        u.Username AS RecordedBy
                    FROM Payments p
                    INNER JOIN Loans l ON p.LoanID = l.LoanID
                    INNER JOIN Customers c ON l.CustomerID = c.CustomerID
                    INNER JOIN Users u ON p.RecordedBy = u.UserID
                    ORDER BY p.PaymentDate DESC";

                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    dgvPayments.DataSource = dt;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }
        }

        private void PaymentsMain_Load(object sender, EventArgs e)
        {
            LoadPayments();
            LoadLoansIntoCombo();

            dgvPayments.DefaultCellStyle.SelectionBackColor = Color.FromArgb(220, 230, 250);
            dgvPayments.DefaultCellStyle.SelectionForeColor = Color.Black;
            dgvPayments.ThemeStyle.RowsStyle.SelectionBackColor = Color.FromArgb(220, 230, 250);
            dgvPayments.ThemeStyle.RowsStyle.SelectionForeColor = Color.Black;

            dgvPayments.ClearSelection();

            if (GlobalData.CurrentUserRole == "LoanOfficer")
            {
                usersButton.Visible = false;
            }

            // Payment date restriction
            if (GlobalData.CurrentUserRole != "Admin")
            {
                // Allow only last 5 days up to today
                paymentDateTimePicker.MinDate = DateTime.Today.AddDays(-20);
                paymentDateTimePicker.MaxDate = DateTime.Today;
            }
            else
            {
                // Admin can select any date
                paymentDateTimePicker.MinDate = DateTimePicker.MinimumDateTime;
                paymentDateTimePicker.MaxDate = DateTimePicker.MaximumDateTime;
            }

        }

        private void LoadLoansIntoCombo()
        {
            using (SqlConnection con = new SqlConnection(Connection.connectionString))
            {
                string query = @"
                SELECT 
                    l.LoanID,
                    c.FullName,
                    l.LoanAmount - ISNULL(SUM(p.PaidAmount), 0) AS RemainingBalance
                FROM Loans l
                INNER JOIN Customers c ON l.CustomerID = c.CustomerID
                LEFT JOIN Payments p ON l.LoanID = p.LoanID
                GROUP BY l.LoanID, c.FullName, l.LoanAmount
                HAVING l.LoanAmount - ISNULL(SUM(p.PaidAmount), 0) > 0
                ORDER BY c.FullName";

                SqlDataAdapter da = new SqlDataAdapter(query, con);
                DataTable dt = new DataTable();
                da.Fill(dt);

                dt.Columns.Add("DisplayText", typeof(string));

                foreach (DataRow row in dt.Rows)
                {
                    row["DisplayText"] =
                        $"{row["FullName"]} (Loan #{row["LoanID"]}) - Remaining: ${row["RemainingBalance"]}";
                }

                DataRow defaultRow = dt.NewRow();
                defaultRow["LoanID"] = 0;
                defaultRow["DisplayText"] = "--- Select Loan ---";
                dt.Rows.InsertAt(defaultRow, 0);

                cmbLoanID.DataSource = dt;
                cmbLoanID.DisplayMember = "DisplayText";
                cmbLoanID.ValueMember = "LoanID";
                cmbLoanID.SelectedIndex = 0;
            }
        }

        private decimal GetRemainingBalance(int loanId)
        {
            decimal remainingBalance = 0;

            using (SqlConnection con = new SqlConnection(Connection.connectionString))
            {
                string query = @"
                SELECT 
                    l.LoanAmount - ISNULL(SUM(p.PaidAmount), 0) AS RemainingBalance
                FROM Loans l
                LEFT JOIN Payments p ON l.LoanID = p.LoanID
                WHERE l.LoanID = @LoanID
                GROUP BY l.LoanID, l.LoanAmount";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@LoanID", loanId);

                con.Open();
                var result = cmd.ExecuteScalar();

                if (result != null && result != DBNull.Value)
                {
                    remainingBalance = Convert.ToDecimal(result);
                }
                else
                {
                    // If no payments yet, remaining balance is the full loan amount
                    query = "SELECT LoanAmount FROM Loans WHERE LoanID = @LoanID";
                    cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@LoanID", loanId);

                    result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        remainingBalance = Convert.ToDecimal(result);
                    }
                }
            }

            return remainingBalance;
        }

        private bool ValidateInputs()
        {
            bool isValid = true;

            LoanIDErrLabel.Text = "";
            paymentAmountErrLabel.Text = "";
            paymentDateErrLabel.Text = "";

            // Loan validation
            if (cmbLoanID.SelectedValue == null || Convert.ToInt32(cmbLoanID.SelectedValue) == 0)
            {
                LoanIDErrLabel.Text = "Please select a loan";
                isValid = false;
                return isValid; // Return early if no loan selected
            }

            // Amount validation
            if (paymentAmountNumericUpDown.Value <= 0)
            {
                paymentAmountErrLabel.Text = "Amount must be greater than zero";
                isValid = false;
            }

            // Date validation (Role-based)
            DateTime selectedDate = paymentDateTimePicker.Value.Date;
            DateTime today = DateTime.Today;

            // ❌ No one can use future dates
            if (selectedDate > today)
            {
                paymentDateErrLabel.Text = "Payment date cannot be in the future";
                isValid = false;
            }
            // ❌ LoanOfficer limited to last 5 days
            else if (
                GlobalData.CurrentUserRole != "Admin" &&
                selectedDate < today.AddDays(-5)
            )
            {
                paymentDateErrLabel.Text = "You can only record payments within the last 5 days";
                isValid = false;
            }


            // Check for overpayment (only if loan is selected and amount is valid)
            if (isValid && cmbLoanID.SelectedValue != null && Convert.ToInt32(cmbLoanID.SelectedValue) > 0)
            {
                int loanId = Convert.ToInt32(cmbLoanID.SelectedValue);
                decimal paymentAmount = paymentAmountNumericUpDown.Value;

                decimal remainingBalance = GetRemainingBalance(loanId);

                if (paymentAmount > remainingBalance)
                {
                    paymentAmountErrLabel.Text = $"Payment amount (${paymentAmount}) exceeds remaining balance (${remainingBalance})";
                    isValid = false;
                }
            }

            return isValid;
        }

        private void dgvPayments_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            DataGridViewRow row = dgvPayments.Rows[e.RowIndex];

            DateTime paymentDate;

            // Try to read payment date first
            if (!DateTime.TryParse(row.Cells[4].Value.ToString(), out paymentDate))
            {
                MessageBox.Show(
                    "Invalid payment date format.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                dgvPayments.ClearSelection();
                return;
            }

            // Try to assign payment date (this may fail due to MinDate/MaxDate)
            try
            {
                paymentDateTimePicker.Value = paymentDate;
            }
            catch (ArgumentOutOfRangeException)
            {
                ClearInputs();
                dgvPayments.ClearSelection();
                MessageBox.Show(
                    "This payment record cannot be opened.\n" +
                    "The payment date is outside the allowed range for your role.",
                    "Access Denied",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;                  // 🚨 STOP loading data
            }

            // ✅ ONLY load other fields if date is allowed
            selectedPaymentID = Convert.ToInt32(row.Cells[0].Value); // PaymentID
            cmbLoanID.SelectedValue = Convert.ToInt32(row.Cells[1].Value); // LoanID
            paymentAmountNumericUpDown.Value = Convert.ToDecimal(row.Cells[3].Value); // PaidAmount
        }


        private void UpdateLoanStatus(int loanId)
        {
            decimal remainingBalance = GetRemainingBalance(loanId);

            string status = remainingBalance <= 0 ? "Completed" : "Active";

            using (SqlConnection con = new SqlConnection(Connection.connectionString))
            {
                string query = "UPDATE Loans SET Status = @Status WHERE LoanID = @LoanID";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Status", status);
                cmd.Parameters.AddWithValue("@LoanID", loanId);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }


        private void btnInsert_Click(object sender, EventArgs e)
        {
            if (!ValidateInputs()) return;

            // Additional check for overpayment
            int loanId = Convert.ToInt32(cmbLoanID.SelectedValue);
            decimal paymentAmount = paymentAmountNumericUpDown.Value;
            decimal remainingBalance = GetRemainingBalance(loanId);

            if (paymentAmount > remainingBalance)
            {
                MessageBox.Show($"Payment cannot exceed remaining balance of ${remainingBalance}", "Overpayment Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (SqlConnection con = new SqlConnection(Connection.connectionString))
            {
                string query = @"
                INSERT INTO Payments (LoanID, PaidAmount, PaymentDate, RecordedBy)
                VALUES (@LoanID, @Amount, @Date, @UserID)";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@LoanID", loanId);
                cmd.Parameters.AddWithValue("@Amount", paymentAmount);
                cmd.Parameters.AddWithValue("@Date", paymentDateTimePicker.Value.Date);
                cmd.Parameters.AddWithValue("@UserID", 1); 

                con.Open();
                cmd.ExecuteNonQuery();
                UpdateLoanStatus(loanId);

            }

            MessageBox.Show("Payment recorded successfully");
            LoadPayments();
            ClearInputs();
            LoadLoansIntoCombo(); // Refresh combobox to show updated balances
        }

        //private void btnUpdate_Click(object sender, EventArgs e)
        //{
        //    if (selectedPaymentID == 0)
        //    {
        //        MessageBox.Show("Please select a payment to update");
        //        return;
        //    }

        //    if (!ValidateInputs()) return;

        //    // For update, we need to calculate remaining balance excluding the current payment
        //    int loanId = Convert.ToInt32(cmbLoanID.SelectedValue);
        //    decimal newPaymentAmount = paymentAmountNumericUpDown.Value;

        //    // Get current payment amount before update
        //    decimal oldPaymentAmount = 0;
        //    using (SqlConnection con = new SqlConnection(connectionString))
        //    {
        //        string query = "SELECT PaidAmount FROM Payments WHERE PaymentID = @PaymentID";
        //        SqlCommand cmd = new SqlCommand(query, con);
        //        cmd.Parameters.AddWithValue("@PaymentID", selectedPaymentID);

        //        con.Open();
        //        var result = cmd.ExecuteScalar();
        //        if (result != null && result != DBNull.Value)
        //        {
        //            oldPaymentAmount = Convert.ToDecimal(result);
        //        }
        //    }

        //    // Calculate remaining balance with adjustment for the updated payment
        //    decimal remainingBalance = GetRemainingBalance(loanId) + oldPaymentAmount; // Add back old payment

        //    if (newPaymentAmount > remainingBalance)
        //    {
        //        MessageBox.Show($"Updated payment amount (${newPaymentAmount}) exceeds remaining balance (${remainingBalance})", "Overpayment Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //        return;
        //    }

        //    using (SqlConnection con = new SqlConnection(connectionString))
        //    {
        //        string query = @"
        //        UPDATE Payments
        //        SET LoanID=@LoanID, PaidAmount=@Amount, PaymentDate=@Date
        //        WHERE PaymentID=@PaymentID";

        //        SqlCommand cmd = new SqlCommand(query, con);
        //        cmd.Parameters.AddWithValue("@LoanID", loanId);
        //        cmd.Parameters.AddWithValue("@Amount", newPaymentAmount);
        //        cmd.Parameters.AddWithValue("@Date", paymentDateTimePicker.Value.Date);
        //        cmd.Parameters.AddWithValue("@PaymentID", selectedPaymentID);

        //        con.Open();
        //        cmd.ExecuteNonQuery();
        //    }

        //    MessageBox.Show("Payment updated successfully");
        //    LoadPayments();
        //    ClearInputs();
        //    LoadLoansIntoCombo(); // Refresh combobox to show updated balances
        //    UpdateLoanStatus(loanId);
        //}

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (selectedPaymentID == 0)
            {
                MessageBox.Show("Please select a payment to delete");
                return;
            }

            DialogResult dr = MessageBox.Show(
                "Are you sure you want to delete this payment?",
                "Confirm",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (dr == DialogResult.No) return;

            using (SqlConnection con = new SqlConnection(Connection.connectionString))
            {
                string query = "DELETE FROM Payments WHERE PaymentID=@PaymentID";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@PaymentID", selectedPaymentID);

                con.Open();
                cmd.ExecuteNonQuery();
            }

            MessageBox.Show("Payment deleted successfully");
            LoadPayments();
            ClearInputs();
            LoadLoansIntoCombo(); // Refresh combobox to show updated balances
        }

        private void ClearInputs()
        {
            cmbLoanID.SelectedIndex = 0;
            paymentAmountNumericUpDown.Value = 0;
            paymentDateTimePicker.Value = DateTime.Today;

            LoanIDErrLabel.Text = "";
            paymentAmountErrLabel.Text = "";
            paymentDateErrLabel.Text = "";

            selectedPaymentID = 0;
            dgvPayments.ClearSelection();
        }

        private void clearPictureBox_Click(object sender, EventArgs e)
        {
            ClearInputs();
        }

        private void searchTextBox_TextChanged(object sender, EventArgs e)
        {
            using (SqlConnection con = new SqlConnection(Connection.connectionString))
            {
                string query = @"
                SELECT 
                    p.PaymentID,
                    p.LoanID,
                    c.FullName AS Customer,
                    p.PaidAmount,
                    p.PaymentDate,
                    u.Username AS RecordedBy
                FROM Payments p
                INNER JOIN Loans l ON p.LoanID = l.LoanID
                INNER JOIN Customers c ON l.CustomerID = c.CustomerID
                INNER JOIN Users u ON p.RecordedBy = u.UserID
                WHERE c.FullName LIKE @search OR p.LoanID LIKE @search
                ORDER BY p.PaymentDate DESC";

                SqlDataAdapter da = new SqlDataAdapter(query, con);
                da.SelectCommand.Parameters.AddWithValue("@search", "%" + searchTextBox.Text + "%");

                DataTable dt = new DataTable();
                da.Fill(dt);
                dgvPayments.DataSource = dt;
            }
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

        private void loanButton_Click(object sender, EventArgs e)
        {
            LoanMain form = new LoanMain();
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