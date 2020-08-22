using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Data;
using System.Numerics;

namespace bi_testproj.Utils
{
    public class DbConnectioHelper
    {
        private string connetionString;

        public DbConnectioHelper(string _connetionString)
        {
            connetionString = _connetionString;
        }

        public async Task<object[][]> GetDbResultAsync(string query, Dictionary<string, object> parameters)
        {
            using (SqlConnection dbConnection = new SqlConnection(connetionString))
            {
                dbConnection.Open();
                var command = new SqlCommand(query, dbConnection);

                foreach(var parameter in parameters)
                {
                    if(parameter.Value is string)
                        command.Parameters.Add(parameter.Key, SqlDbType.NVarChar).Value = parameter.Value;
                    else if(parameter.Value is DateTime)
                        command.Parameters.Add(parameter.Key, SqlDbType.DateTime).Value = parameter.Value;
                    else if(parameter.Value is BigInteger)
                        command.Parameters.Add(parameter.Key, SqlDbType.BigInt).Value = parameter.Value;
                    else if (parameter.Value is int)
                        command.Parameters.Add(parameter.Key, SqlDbType.Int).Value = parameter.Value;
                }

                SqlDataReader reader;
                var  rows = new List<object[]>();
                try
                {
                    reader = await command.ExecuteReaderAsync();

                    while(await reader.ReadAsync())
                    {
                        var nCols = reader.FieldCount;
                        var colValues = new object[nCols];
                        for (int i = 0; i < nCols; i++)
                        {
                            colValues[i] = reader.GetValue(i);
                        }
                        rows.Add(colValues);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    dbConnection.Close();
                    return null;
                }

                dbConnection.Close();

                return rows.ToArray();
            }
        }
    }
}
