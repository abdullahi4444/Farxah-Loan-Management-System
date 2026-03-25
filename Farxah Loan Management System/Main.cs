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
    public partial class DashboardMain : Form
    {
        public DashboardMain()
        {
            InitializeComponent();
        }

        private void logoutBtn_Click(object sender, EventArgs e)
        {
            Login login = new Login();
            this.Hide();
            login.Show();
        }

        private void dashboardBtn_Click(object sender, EventArgs e)
        {
           
        }

        private void userPicture_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void panel4_Paint(object sender, PaintEventArgs e)
        {

        }

        private void LoadDashboardData()
        {
            using (SqlConnection con = new SqlConnection(Connection.connectionString))
            {
                con.Open();

                // ===============================
                // TOTAL CUSTOMERS
                // ===============================
                SqlCommand cmdCustomers = new SqlCommand(
                    "SELECT COUNT(*) FROM Customers", con);
                numberCustomersLabel.Text = cmdCustomers.ExecuteScalar().ToString();

                // ===============================
                // ACTIVE LOANS
                // ===============================
                SqlCommand cmdActiveLoans = new SqlCommand(
                    "SELECT COUNT(*) FROM Loans WHERE Status = 'Active'", con);
                activeLoansLabel.Text = cmdActiveLoans.ExecuteScalar().ToString();

                // ===============================
                // OVERDUE LOANS
                // ===============================
                SqlCommand cmdOverdueLoans = new SqlCommand(
                @"SELECT COUNT(*) 
                FROM Loans
                WHERE DueDate < CAST(GETDATE() AS DATE)
                AND Status = 'Active'
                AND LoanAmount > 0", con);
                overdueLoansLabel.Text = cmdOverdueLoans.ExecuteScalar().ToString();

                // ===============================
                // TOTAL PAYMENTS
                // ===============================
                SqlCommand cmdTotalPayments = new SqlCommand(
                    "SELECT ISNULL(SUM(PaidAmount), 0) FROM Payments", con);

                decimal totalPayments = Convert.ToDecimal(cmdTotalPayments.ExecuteScalar());
                totalPaymentsLabel.Text = "$" + totalPayments.ToString("N2");
            }
        }

        private void DashboardMain_Load(object sender, EventArgs e)
        {
            LoadDashboardData();
            if (GlobalData.CurrentUserRole == "LoanOfficer")
            {
                usersButton.Visible = false;
            }
        }

        private void overdueLoansLabel_Click(object sender, EventArgs e)
        {

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
