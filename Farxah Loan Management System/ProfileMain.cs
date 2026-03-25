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
    public partial class ProfileMain : Form
    {
        public ProfileMain()
        {
            InitializeComponent();
        }

        private void ProfileMain_Load(object sender, EventArgs e)
        {
            LoadUserProfile();
        }

        void LoadUserProfile()
        {
            using (SqlConnection con = new SqlConnection(Connection.connectionString))
            {
                string query = @"SELECT Username, Role, CreatedAt 
                         FROM Users 
                         WHERE Username = @username";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@username", GlobalData.CurrentUsername);

                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    txtUsername.Text = dr["Username"].ToString();
                    txtRole.Text = dr["Role"].ToString();          // READONLY ✅
                    txtCreatedAt.Text = Convert.ToDateTime(dr["CreatedAt"])
                                        .ToString("yyyy-MM-dd HH:mm");
                }

                con.Close();
            }
        }

        private void userPicture_Click(object sender, EventArgs e)
        {

        }

        private void userPicture_Click_1(object sender, EventArgs e)
        {
            this.Close();
        }

        private void logInButton_Click(object sender, EventArgs e)
        {
            if (txtPassword.Text == "" || txtConfirmPassword.Text == "")
            {
                MessageBox.Show("Please fill all password fields ❌");
                return;
            }

            if (txtPassword.Text != txtConfirmPassword.Text)
            {
                MessageBox.Show("Passwords do not match ❌");
                return;
            }

            using (SqlConnection con = new SqlConnection(Connection.connectionString))
            {
                string query = @"UPDATE Users 
                                 SET PasswordHash = @password
                                 WHERE Username = @username";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@password", txtPassword.Text);
                cmd.Parameters.AddWithValue("@username", txtUsername.Text);

                con.Open();
                cmd.ExecuteNonQuery();
                con.Close();

                MessageBox.Show("Password updated successfully ✅");
                txtPassword.Clear();
                txtConfirmPassword.Clear();
            }
        }
    }
}
