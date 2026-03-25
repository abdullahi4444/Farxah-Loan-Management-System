using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using OfficeOpenXml;


namespace Farxah_Loan_Management_System
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            //Application.Run(new DashboardMain());
            Application.Run(new Login());
            //Application.Run(new CustomersMain());
            //Application.Run(new LoanMain());
            //Application.Run(new PaymentsMain());
            //Application.Run(new ReportsMain());
            //Application.Run(new UsersMain());
            //Application.Run(new ProfileMain());
            //Application.Run(new ReportFrm());
        }
    }
}
