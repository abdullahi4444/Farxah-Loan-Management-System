using System;
using System.Diagnostics;
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
    public partial class Login : Form
    {
        //string connectionString = @"server = DESKTOP-R2LIBPL\SQLEXPRESS; Database = FarxahLMS; Integrated Security=True;";

        Stopwatch sw = new Stopwatch();
        public Login()
        {
            InitializeComponent();

            this.AcceptButton = logInButton;
        }

        //private void clear()
        //{
        //    userNameTextBox.Clear();
        //    passwordTextBox.Clear();
        //}

        private void Login_Load(object sender, EventArgs e)
        {
            //sw.Start();

            //// Your existing loading code here

            //sw.Stop();
            //MessageBox.Show("Screen load time: " + sw.ElapsedMilliseconds + " ms");
        }

        private void passwordTextBox_TextChanged(object sender, EventArgs e)
        {

        }

        

        private void guna2HtmlLabel5_Click(object sender, EventArgs e)
        {

        }

        private void userPicture_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void logInButton_Click(object sender, EventArgs e)
        {
            // Clear previous errors
            usernameErrLabel.Text = "";
            passwordErrLabel.Text = "";

            string username = userNameTextBox.Text.Trim();
            string password = passwordTextBox.Text.Trim();

            // ===============================
            // BASIC VALIDATIONS
            // ===============================
            if (string.IsNullOrEmpty(username))
            {
                usernameErrLabel.Text = "❗Username is required";
                userNameTextBox.Focus();
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                passwordErrLabel.Text = "❗Password is required";
                passwordTextBox.Focus();
                return;
            }

            // ===============================
            // DATABASE LOGIN CHECK
            // ===============================
            using (SqlConnection con = new SqlConnection(Connection.connectionString))
            {
                string query = @"SELECT PasswordHash, Role 
                         FROM Users 
                         WHERE Username = @Username";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Username", username);

                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                // ===============================
                // USERNAME NOT FOUND
                // ===============================
                if (!reader.HasRows)
                {
                    usernameErrLabel.Text = "❗Username does not exist";
                    userNameTextBox.Clear();
                    userNameTextBox.Focus();
                    return;
                }

                reader.Read();
                string dbPassword = reader["PasswordHash"].ToString();
                string role = reader["Role"].ToString();

                // ===============================
                // PASSWORD INCORRECT
                // ===============================
                if (dbPassword != password)
                {
                    passwordErrLabel.Text = "❗Incorrect password";
                    passwordTextBox.Clear();
                    passwordTextBox.Focus();
                    return;
                }

                // ===============================
                // SAVE ROLE TO GLOBAL VARIABLE (ADD THIS LINE)
                // ===============================
                GlobalData.CurrentUsername = username;
                GlobalData.CurrentUserRole = role;


                // ===============================
                // LOGIN SUCCESS → ROLE BASED ACCESS
                // ===============================
                MessageBox.Show("Login successful", "Success",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);

                DashboardMain dashboard = new DashboardMain();
                dashboard.Show();
                Hide();
            }
        }
    }
}
