using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;
using System.IO; 
using System.Diagnostics; 
using iTextSharp.text;
using iTextSharp.text.pdf;
using OfficeOpenXml;
using Farxah_Loan_Management_System;


namespace Farxah_Loan_Management_System
{
    public partial class ReportsMain : Form
    {
        private DataTable currentDataTable;

        public ReportsMain()
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

        private void ReportsMain_Load(object sender, EventArgs e)
        {
            cmbReportType.Items.Add("Customer List");
            cmbReportType.Items.Add("Active Loans");
            cmbReportType.Items.Add("Overdue Loans");
            cmbReportType.Items.Add("Payments History");

            //"Customer List" as default
            cmbReportType.SelectedItem = "Customer List";

            // Handle selection change
            cmbReportType.SelectedIndexChanged += cmbReportType_SelectedIndexChanged;

            // Load initial report
            LoadSelectedReport();

            // Set placeholder text for search
            searchTextBox.Text = "Search...";
            searchTextBox.ForeColor = Color.Gray;
            searchTextBox.Enter += searchTextBox_Enter;
            searchTextBox.Leave += searchTextBox_Leave;

            if (GlobalData.CurrentUserRole == "LoanOfficer")
            {
                usersButton.Visible = false;
            }
        }

        private void searchTextBox_Enter(object sender, EventArgs e)
        {
            if (searchTextBox.Text == "Search...")
            {
                searchTextBox.Text = "";
                searchTextBox.ForeColor = Color.Black;
            }
        }

        private void searchTextBox_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(searchTextBox.Text))
            {
                searchTextBox.Text = "Search...";
                searchTextBox.ForeColor = Color.Gray;
            }
        }

        private void cmbReportType_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadSelectedReport();
        }

        private void LoadSelectedReport()
        {
            string selectedReport = cmbReportType.SelectedItem?.ToString();

            switch (selectedReport)
            {
                case "Customer List":
                    LoadCustomerList();
                    break;
                case "Active Loans":
                    LoadActiveLoans();
                    break;
                case "Overdue Loans":
                    LoadOverdueLoans();
                    break;
                case "Payments History":
                    LoadPaymentsHistory();
                    break;
                default:
                    LoadCustomerList(); // Default fallback
                    break;
            }
        }

        private void LoadCustomerList()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(Connection.connectionString))
                {
                    string query = @"SELECT 
                        c.CustomerID,
                        c.FullName,
                        c.Phone,
                        c.Address,
                        CONVERT(VARCHAR(10), c.CreatedDate, 103) AS [Registration Date],

                        ISNULL(loans.TotalLoan, 0) - ISNULL(payments.TotalPaid, 0) AS Balance

                    FROM Customers c
                    LEFT JOIN (
                        SELECT CustomerID, SUM(LoanAmount) AS TotalLoan
                        FROM Loans
                        GROUP BY CustomerID
                    ) loans ON c.CustomerID = loans.CustomerID

                    LEFT JOIN (
                        SELECT l.CustomerID, SUM(p.PaidAmount) AS TotalPaid
                        FROM Payments p
                        INNER JOIN Loans l ON p.LoanID = l.LoanID
                        GROUP BY l.CustomerID
                    ) payments ON c.CustomerID = payments.CustomerID

                    ORDER BY c.FullName;
                    ";

                    SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                    currentDataTable = new DataTable();
                    adapter.Fill(currentDataTable);

                    // Apply search filter if there's search text
                    ApplySearchFilter();

                    // Set column headers
                    if (dgvReports.Columns.Contains("CustomerID"))
                        dgvReports.Columns["CustomerID"].HeaderText = "ID";
                    if (dgvReports.Columns.Contains("FullName"))
                        dgvReports.Columns["FullName"].HeaderText = "Full Name";
                    if (dgvReports.Columns.Contains("Phone"))
                        dgvReports.Columns["Phone"].HeaderText = "Phone Number";
                    if (dgvReports.Columns.Contains("Address"))
                        dgvReports.Columns["Address"].HeaderText = "Address";
                    if (dgvReports.Columns.Contains("Balance"))
                    {
                        dgvReports.Columns["Balance"].HeaderText = "Balance";
                        dgvReports.Columns["Balance"].DefaultCellStyle.Format = "C2";
                        dgvReports.Columns["Balance"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                    }
                    if (dgvReports.Columns.Contains("Registration Date"))
                        dgvReports.Columns["Registration Date"].HeaderText = "Join Date";

                    // Auto-size columns for better display
                    dgvReports.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);

                    // Update title label
                    UpdateReportTitle("Customer List");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading customer list: {ex.Message}",
                    "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadActiveLoans()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(Connection.connectionString))
                {
                    string query = @"SELECT 
                                        l.LoanID,
                                        c.FullName AS 'Customer Name',
                                        l.LoanAmount,
                                        CONVERT(VARCHAR(10), l.LoanDate, 103) AS 'Loan Date',
                                        CONVERT(VARCHAR(10), l.DueDate, 103) AS 'Due Date',
                                        l.Status,
                                        l.LoanAmount - ISNULL(SUM(p.PaidAmount), 0) AS 'Remaining Balance'
                                    FROM Loans l
                                    INNER JOIN Customers c ON l.CustomerID = c.CustomerID
                                    LEFT JOIN Payments p ON l.LoanID = p.LoanID
                                    WHERE l.Status = 'Active'
                                    GROUP BY l.LoanID, c.FullName, l.LoanAmount, l.LoanDate, l.DueDate, l.Status
                                    ORDER BY l.DueDate";

                    SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                    currentDataTable = new DataTable();
                    adapter.Fill(currentDataTable);

                    // Apply search filter if there's search text
                    ApplySearchFilter();

                    // Format currency columns
                    if (dgvReports.Columns.Contains("LoanAmount"))
                    {
                        dgvReports.Columns["LoanAmount"].HeaderText = "Loan Amount";
                        dgvReports.Columns["LoanAmount"].DefaultCellStyle.Format = "C2";
                        dgvReports.Columns["LoanAmount"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                    }

                    if (dgvReports.Columns.Contains("Remaining Balance"))
                    {
                        dgvReports.Columns["Remaining Balance"].HeaderText = "Remaining Balance";
                        dgvReports.Columns["Remaining Balance"].DefaultCellStyle.Format = "C2";
                        dgvReports.Columns["Remaining Balance"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                    }

                    // Auto-size columns
                    dgvReports.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);

                    // Update title label
                    UpdateReportTitle("Active Loans");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading active loans: {ex.Message}",
                    "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadOverdueLoans()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(Connection.connectionString))
                {
                    string query = @"SELECT 
                                        l.LoanID,
                                        c.FullName AS 'Customer Name',
                                        c.Phone,
                                        l.LoanAmount,
                                        CONVERT(VARCHAR(10), l.LoanDate, 103) AS 'Loan Date',
                                        CONVERT(VARCHAR(10), l.DueDate, 103) AS 'Due Date',
                                        l.Status,
                                        DATEDIFF(DAY, l.DueDate, GETDATE()) AS 'Days Overdue'
                                    FROM Loans l
                                    JOIN Customers c ON l.CustomerID = c.CustomerID
                                    WHERE l.DueDate < GETDATE() 
                                      AND l.Status = 'Active'
                                    ORDER BY l.DueDate";

                    SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                    currentDataTable = new DataTable();
                    adapter.Fill(currentDataTable);

                    // Apply search filter if there's search text
                    ApplySearchFilter();

                    // Format currency columns
                    if (dgvReports.Columns.Contains("LoanAmount"))
                    {
                        dgvReports.Columns["LoanAmount"].HeaderText = "Loan Amount";
                        dgvReports.Columns["LoanAmount"].DefaultCellStyle.Format = "C2";
                        dgvReports.Columns["LoanAmount"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                    }

                    // Auto-size columns
                    dgvReports.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);

                    // Update title label
                    UpdateReportTitle("Overdue Loans");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading overdue loans: {ex.Message}",
                    "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadPaymentsHistory()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(Connection.connectionString))
                {
                    string query = @"SELECT 
                                        p.PaymentID,
                                        p.LoanID,
                                        c.FullName AS 'Customer',
                                        p.PaidAmount,
                                        CONVERT(VARCHAR(10), p.PaymentDate, 103) AS 'Payment Date',
                                        u.Username AS 'Recorded By'
                                    FROM Payments p
                                    INNER JOIN Loans l ON p.LoanID = l.LoanID
                                    INNER JOIN Customers c ON l.CustomerID = c.CustomerID
                                    INNER JOIN Users u ON p.RecordedBy = u.UserID
                                    ORDER BY p.PaymentDate DESC";

                    SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                    currentDataTable = new DataTable();
                    adapter.Fill(currentDataTable);

                    // Apply search filter if there's search text
                    ApplySearchFilter();

                    // Format currency columns
                    if (dgvReports.Columns.Contains("PaidAmount"))
                    {
                        dgvReports.Columns["PaidAmount"].HeaderText = "Amount Paid";
                        dgvReports.Columns["PaidAmount"].DefaultCellStyle.Format = "C2";
                        dgvReports.Columns["PaidAmount"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                    }

                    // Auto-size columns
                    dgvReports.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);

                    // Update title label
                    UpdateReportTitle("Payments History");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading payments history: {ex.Message}",
                    "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ApplySearchFilter()
        {
            string searchText = searchTextBox.Text.Trim();

            // If search text is empty or placeholder, show all data
            if (string.IsNullOrWhiteSpace(searchText) || searchText == "Search...")
            {
                dgvReports.DataSource = currentDataTable;
            }
            else
            {
                // Create a filtered DataTable
                DataTable filteredTable = currentDataTable.Clone();
                string searchLower = searchText.ToLower();

                foreach (DataRow row in currentDataTable.Rows)
                {
                    bool matchFound = false;

                    // Search in all string columns
                    foreach (DataColumn column in currentDataTable.Columns)
                    {
                        if (row[column] != null && row[column] != DBNull.Value)
                        {
                            string cellValue = row[column].ToString().ToLower();
                            if (cellValue.Contains(searchLower))
                            {
                                matchFound = true;
                                break;
                            }
                        }
                    }

                    if (matchFound)
                    {
                        filteredTable.ImportRow(row);
                    }
                }

                dgvReports.DataSource = filteredTable;
            }
        }

        private void UpdateReportTitle(string reportType)
        {
            int totalRecords = currentDataTable.Rows.Count;
            int displayedRecords = dgvReports.Rows.Count;

            if (displayedRecords == totalRecords)
            {
                reportTypeLabel.Text = $"{reportType} - Total: {totalRecords} records";
            }
            else
            {
                reportTypeLabel.Text = $"{reportType} - Showing: {displayedRecords} of {totalRecords} records";
            }
        }

        // Search functionality
        private void searchTextBox_TextChanged(object sender, EventArgs e)
        {
            if (currentDataTable != null)
            {
                ApplySearchFilter();

                // Update title to show filtered count
                string reportType = cmbReportType.SelectedItem?.ToString() ?? "Report";
                UpdateReportTitle(reportType);
            }
        }

        // Refresh button click event
        private void btnRefresh_Click(object sender, EventArgs e)
        {
            LoadSelectedReport();
        }

        // Clear search button
        private void btnClearSearch_Click(object sender, EventArgs e)
        {
            searchTextBox.Text = "Search...";
            searchTextBox.ForeColor = Color.Gray;
            LoadSelectedReport();
        }

        private void btnDownloadPdf_Click(object sender, EventArgs e)
        {
            try
            {
                if (currentDataTable == null || currentDataTable.Rows.Count == 0)
                {
                    MessageBox.Show("No data to export!", "Export Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    string reportName = cmbReportType.SelectedItem?.ToString() ?? "Report";
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string defaultFileName = $"{reportName.Replace(" ", "_")}_{timestamp}.pdf";

                    saveFileDialog.FileName = defaultFileName;
                    saveFileDialog.Filter = "PDF Files (*.pdf)|*.pdf|All Files (*.*)|*.*";
                    saveFileDialog.Title = "Save PDF Report";
                    saveFileDialog.DefaultExt = "pdf";

                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        // Create PDF document
                        Document document = new Document(PageSize.A4.Rotate(), 10f, 10f, 10f, 10f);
                        PdfWriter writer = PdfWriter.GetInstance(document, new FileStream(saveFileDialog.FileName, FileMode.Create));
                        document.Open();

                        // Add title
                        Paragraph title = new Paragraph($"{reportName} - Generated on {DateTime.Now.ToString("dd/MM/yyyy HH:mm")}",
                            FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16));
                        title.Alignment = Element.ALIGN_CENTER;
                        document.Add(title);

                        // Add spacing
                        document.Add(new Paragraph("\n"));

                        // Create PDF table
                        PdfPTable pdfTable = new PdfPTable(dgvReports.Columns.Count);
                        pdfTable.WidthPercentage = 100;

                        // Add column headers
                        foreach (DataGridViewColumn column in dgvReports.Columns)
                        {
                            PdfPCell cell = new PdfPCell(new Phrase(column.HeaderText,
                                FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)));
                            cell.BackgroundColor = new BaseColor(240, 240, 240);
                            cell.Padding = 5;
                            pdfTable.AddCell(cell);
                        }

                        // Add rows
                        foreach (DataGridViewRow row in dgvReports.Rows)
                        {
                            if (row.IsNewRow) continue;

                            foreach (DataGridViewCell cell in row.Cells)
                            {
                                string cellValue = cell.Value?.ToString() ?? "";

                                // Format currency values
                                if (cell.Value is decimal || cell.Value is double)
                                {
                                    if (decimal.TryParse(cellValue, out decimal decimalValue))
                                    {
                                        cellValue = decimalValue.ToString("C2");
                                    }
                                }

                                PdfPCell pdfCell = new PdfPCell(new Phrase(cellValue,
                                    FontFactory.GetFont(FontFactory.HELVETICA, 9)));
                                pdfCell.Padding = 4;
                                pdfTable.AddCell(pdfCell);
                            }
                        }

                        document.Add(pdfTable);

                        // Add footer
                        Paragraph footer = new Paragraph($"Total Records: {dgvReports.Rows.Count - 1}",
                            FontFactory.GetFont(FontFactory.HELVETICA, 10));
                        footer.Alignment = Element.ALIGN_RIGHT;
                        document.Add(footer);

                        document.Close();

                        MessageBox.Show($"PDF report saved successfully!\n\nFile: {saveFileDialog.FileName}",
                            "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting to PDF: {ex.Message}",
                    "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        private void paymentsButton_Click(object sender, EventArgs e)
        {
            PaymentsMain form = new PaymentsMain();
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