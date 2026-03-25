using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;
using Farxah_Loan_Management_System;

namespace Farxah_Loan_Management_System
{
    public partial class UsersMain : Form
    {
        int selectedUserId = 0;

        public UsersMain()
        {
            InitializeComponent();
            dgvUsers.CellClick += dgvUsers_CellClick;
            searchTextBox.TextChanged += searchTextBox_TextChanged;
        }

        /* ===================== FORM LOAD ===================== */
        private void UsersMain_Load(object sender, EventArgs e)
        {
            passwordTextBox.UseSystemPasswordChar = true;

            // Add roles manually
            cmbUserRole.Items.Clear();
            cmbUserRole.Items.Add("Admin");
            cmbUserRole.Items.Add("LoanOfficer");

            // DataGridView settings
            dgvUsers.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvUsers.MultiSelect = false;
            dgvUsers.ReadOnly = true;
            dgvUsers.AllowUserToAddRows = false;

            LoadUsersList();
        }

        /* ===================== INSERT ===================== */
        private void btnInsert_Click(object sender, EventArgs e)
        {
            ClearErrors();
            if (!ValidateInputs()) return;

            try
            {
                using (SqlConnection con = new SqlConnection(Connection.connectionString))
                {
                    string query = @"INSERT INTO Users (Username, PasswordHash, Role)
                                     VALUES (@Username, @Password, @Role)";

                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@Username", userNameTextBox.Text.Trim());
                    cmd.Parameters.AddWithValue("@Password", passwordTextBox.Text.Trim()); // plain password
                    cmd.Parameters.AddWithValue("@Role", cmbUserRole.SelectedItem.ToString());

                    con.Open();
                    cmd.ExecuteNonQuery();
                }

                MessageBox.Show("User added successfully");
                ClearInputs();
                LoadUsersList();
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2627)
                    userNameErrLabel.Text = "Username already exists";
                else
                    MessageBox.Show(ex.Message);
            }
        }

        /* ===================== UPDATE ===================== */
        private void btnUpdate_Click(object sender, EventArgs e)
        {
            if (selectedUserId == 0)
            {
                MessageBox.Show("Please select a user to update");
                return;
            }

            ClearErrors();
            if (!ValidateUpdate()) return;

            try
            {
                using (SqlConnection con = new SqlConnection(Connection.connectionString))
                {
                    string query;

                    if (string.IsNullOrWhiteSpace(passwordTextBox.Text))
                    {
                        // Update without changing password
                        query = @"UPDATE Users
                                  SET Username=@Username, Role=@Role
                                  WHERE UserID=@UserID";
                    }
                    else
                    {
                        query = @"UPDATE Users
                                  SET Username=@Username, PasswordHash=@Password, Role=@Role
                                  WHERE UserID=@UserID";
                    }

                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@Username", userNameTextBox.Text.Trim());
                    cmd.Parameters.AddWithValue("@Role", cmbUserRole.SelectedItem.ToString());
                    cmd.Parameters.AddWithValue("@UserID", selectedUserId);

                    if (!string.IsNullOrWhiteSpace(passwordTextBox.Text))
                        cmd.Parameters.AddWithValue("@Password", passwordTextBox.Text.Trim());

                    con.Open();
                    cmd.ExecuteNonQuery();
                }

                MessageBox.Show("User updated successfully");
                ClearInputs();
                LoadUsersList();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /* ===================== DELETE ===================== */
        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (selectedUserId == 0)
            {
                MessageBox.Show("Please select a user to delete");
                return;
            }

            if (MessageBox.Show("Are you sure you want to delete this user?", "Confirm",
                MessageBoxButtons.YesNo) != DialogResult.Yes) return;

            using (SqlConnection con = new SqlConnection(Connection.connectionString))
            {
                string query = "DELETE FROM Users WHERE UserID=@UserID";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@UserID", selectedUserId);

                con.Open();
                cmd.ExecuteNonQuery();
            }

            MessageBox.Show("User deleted");
            ClearInputs();
            LoadUsersList();
        }

        /* ===================== LOAD USERS ===================== */
        private void LoadUsersList(string search = "")
        {
            using (SqlConnection con = new SqlConnection(Connection.connectionString))
            {
                string query = @"SELECT 
                                    UserID,
                                    Username,
                                    Role,
                                    PasswordHash AS Password,
                                    CONVERT(VARCHAR(10), CreatedAt, 103) AS CreatedDate
                                 FROM Users
                                 WHERE Username LIKE @search OR Role LIKE @search
                                 ORDER BY Username";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@search", "%" + search + "%");

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                dgvUsers.DataSource = dt;

                // Hide the password column in grid (optional)
                if (dgvUsers.Columns.Contains("Password"))
                    dgvUsers.Columns["Password"].Visible = false;
            }

            dgvUsers.ClearSelection();
        }

        /* ===================== SEARCH ===================== */
        private void searchTextBox_TextChanged(object sender, EventArgs e)
        {
            LoadUsersList(searchTextBox.Text.Trim());
        }

        /* ===================== GRID CLICK ===================== */
        private void dgvUsers_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            DataGridViewRow row = dgvUsers.Rows[e.RowIndex];

            selectedUserId = Convert.ToInt32(row.Cells["UserID"].Value);
            userNameTextBox.Text = row.Cells["Username"].Value.ToString();
            cmbUserRole.SelectedItem = row.Cells["Role"].Value.ToString();

            // Password is always cleared for security
            passwordTextBox.Clear();

            ClearErrors();
        }

        /* ===================== VALIDATION ===================== */
        private bool ValidateInputs()
        {
            bool valid = true;

            if (string.IsNullOrWhiteSpace(userNameTextBox.Text))
            {
                userNameErrLabel.Text = "Username is required";
                valid = false;
            }

            if (string.IsNullOrWhiteSpace(passwordTextBox.Text) || passwordTextBox.Text.Length < 6)
            {
                passwordErrTextBox.Text = "Password must be at least 6 characters";
                valid = false;
            }

            if (cmbUserRole.SelectedIndex == -1)
            {
                roleErrLabel.Text = "Please select a role";
                valid = false;
            }

            return valid;
        }

        private bool ValidateUpdate()
        {
            bool valid = true;

            if (string.IsNullOrWhiteSpace(userNameTextBox.Text))
            {
                userNameErrLabel.Text = "Username is required";
                valid = false;
            }

            if (!string.IsNullOrWhiteSpace(passwordTextBox.Text) && passwordTextBox.Text.Length < 6)
            {
                passwordErrTextBox.Text = "Password must be at least 6 characters";
                valid = false;
            }

            if (cmbUserRole.SelectedIndex == -1)
            {
                roleErrLabel.Text = "Please select a role";
                valid = false;
            }

            return valid;
        }

        private void ClearErrors()
        {
            userNameErrLabel.Text = "";
            passwordErrTextBox.Text = "";
            roleErrLabel.Text = "";
        }

        private void ClearInputs()
        {
            userNameTextBox.Clear();
            passwordTextBox.Clear();
            cmbUserRole.SelectedIndex = -1;
            selectedUserId = 0;
            ClearErrors();
        }

        /* ===================== EXTRA ===================== */
        private void userPicture_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void logoutBtn_Click(object sender, EventArgs e)
        {
            Login login = new Login();
            this.Hide();
            login.Show();
        }

        private void clearBtn_Click(object sender, EventArgs e)
        {
            ClearInputs();
        }

        private void guna2PictureBox2_Click(object sender, EventArgs e)
        {
            ClearInputs();
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
