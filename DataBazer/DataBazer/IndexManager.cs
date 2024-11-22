using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataBazer
{
    internal class IndexManager
    {
        private readonly SqlConnection _sqlConnection;

        public IndexManager(SqlConnection sqlConnection)
        {
            _sqlConnection = sqlConnection;
        }

        public async Task<SqlConnection?> HandleIndexManagement()
        {
            throw new NotImplementedException();
        }
    }
}
