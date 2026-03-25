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

namespace Farxah_Loan_Management_System
{
    internal class Connection
    {
        public static string connectionString =
            @"server=DESKTOP-R2LIBPL\SQLEXPRESS;Database=FarxahLMS;Integrated Security=True;";
    }
}
