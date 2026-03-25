using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using System;
using System.Windows.Forms;
using System.IO;
using System.Data.SqlClient;

namespace Farxah_Loan_Management_System
{
    public partial class ReportFrm : Form
    {
        private ReportDocument reportDoc;

        // Report path - UPDATE THIS WITH YOUR PATH
        private string reportPath = @"C:\Users\pc\source\repos\Farxah Loan Management System\Farxah Loan Management System\CrystalReport - Copy.rpt";
        public ReportFrm()
        {
            InitializeComponent();
        }

        private void ReportFrm_Load(object sender, EventArgs e)
        {
            LoadCustomerReport();
        }

        // ==================== MAIN METHODS ====================

        // LOAD CUSTOMER REPORT
        private void LoadCustomerReport()
        {
            try
            {
                Cursor.Current = Cursors.WaitCursor;
                lblTitle.Text = "LOADING REPORT...";

                // Clean previous report
                if (reportDoc != null)
                {
                    reportDoc.Close();
                    reportDoc.Dispose();
                }

                // Check if report file exists
                if (!File.Exists(reportPath))
                {
                    MessageBox.Show($"Report file not found:\n{reportPath}",
                                  "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Load report
                reportDoc = new ReportDocument();
                reportDoc.Load(reportPath);

                // Connect to database
                SetDatabaseConnection(reportDoc);

                // Display in viewer
                crystalReportViewer1.ReportSource = reportDoc;
                crystalReportViewer1.RefreshReport();

                Cursor.Current = Cursors.Default;
                lblTitle.Text = "FARXAH LOAN MANAGEMENT SYSTEM - REPORT LOADED";

                //MessageBox.Show("✅ Report loaded successfully!", "Success",
                //              MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Cursor.Current = Cursors.Default;
                lblTitle.Text = "FARXAH LOAN MANAGEMENT SYSTEM - ERROR";
                MessageBox.Show($"Error: {ex.Message}", "Error",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // SET DATABASE CONNECTION
        // SET DATABASE CONNECTION
        private void SetDatabaseConnection(ReportDocument report)
        {
            try
            {
                ConnectionInfo conn = new ConnectionInfo();

                // CHANGE THIS LINE - Use your SQL Server Express instance
                conn.ServerName = @"DESKTOP-R2LIBPL\SQLEXPRESS";  // Your SQL Server Express

                conn.DatabaseName = "FarxahLMS";
                conn.IntegratedSecurity = true;   // Windows Authentication

                // Alternative if above doesn't work:
                // conn.ServerName = @".";
                // conn.ServerName = "localhost\\SQLEXPRESS";

                foreach (Table table in report.Database.Tables)
                {
                    TableLogOnInfo logon = table.LogOnInfo;
                    logon.ConnectionInfo = conn;
                    table.ApplyLogOnInfo(logon);
                }

                report.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Database Error: {ex.Message}", "Connection Error",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // EXPORT TO PDF
        private void ExportToPDF()
        {
            if (reportDoc == null)
            {
                MessageBox.Show("Please load report first.", "Error",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SaveFileDialog save = new SaveFileDialog();
            save.Filter = "PDF Files|*.pdf";
            save.FileName = $"Farxah_Report_{DateTime.Now:yyyyMMdd}.pdf";

            if (save.ShowDialog() == DialogResult.OK)
            {
                reportDoc.ExportToDisk(ExportFormatType.PortableDocFormat, save.FileName);
                MessageBox.Show($"Exported to:\n{save.FileName}", "Success",
                              MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // PRINT REPORT
        private void PrintReport()
        {
            if (reportDoc == null)
            {
                MessageBox.Show("Please load report first.", "Error",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            reportDoc.PrintToPrinter(1, false, 0, 0);
            MessageBox.Show("Report sent to printer.", "Success",
                          MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // TEST DATABASE CONNECTION
        // TEST DATABASE CONNECTION
        private void TestDatabaseConnection()
        {
            try
            {
                Cursor.Current = Cursors.WaitCursor;

                // Update this connection string
                string connString = @"server=DESKTOP-R2LIBPL\SQLEXPRESS;database=FarxahLMS;Integrated Security=True";

                // Alternative formats:
                // string connString = @"Data Source=DESKTOP-R2LIBPL\SQLEXPRESS;Initial Catalog=FarxahLMS;Integrated Security=True";
                // string connString = @"Server=.\SQLEXPRESS;Database=FarxahLMS;Trusted_Connection=True;";

                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();

                    // Check if view exists
                    string sql = "SELECT COUNT(*) FROM vw_CustomerBalance";
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        int count = (int)cmd.ExecuteScalar();
                        Cursor.Current = Cursors.Default;

                        MessageBox.Show($"✅ Connection Successful!\n\n" +
                                      $"Server: DESKTOP-R2LIBPL\\SQLEXPRESS\n" +
                                      $"Database: FarxahLMS\n" +
                                      $"View: vw_CustomerBalance\n" +
                                      $"Records: {count}",
                                      "Connection Test",
                                      MessageBoxButtons.OK,
                                      MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                Cursor.Current = Cursors.Default;
                MessageBox.Show($"❌ Connection Failed:\n{ex.Message}",
                              "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // SEARCH IN REPORT
        private void SearchInReport()
        {
            string searchText = txtSearch.Text.Trim();
            if (!string.IsNullOrEmpty(searchText) && searchText != "Search...")
            {
                crystalReportViewer1.SearchForText(searchText);
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            LoadCustomerReport();
        }

        private void btnExportPDF_Click(object sender, EventArgs e)
        {
            ExportToPDF();
        }

        private void btnPrint_Click(object sender, EventArgs e)
        {
            PrintReport();
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            SearchInReport();
        }

        private void userPicture_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void ReportFrm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (reportDoc != null)
            {
                reportDoc.Close();
                reportDoc.Dispose();
            }
        }
    }
}
