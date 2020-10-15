using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using bi_testproj.Models;
using bi_testproj.Utils;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Net.Sockets;

namespace bi_testproj.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private DbConnectioHelper connectionHelper;

        public HomeController(ILogger<HomeController> logger)
        {
            var connectionString = "Server=tcp:testprojsqlserver.database.windows.net,1433;Initial Catalog=KidCalc_DW;Persist Security Info=False;User ID=adminsql;Password=bitest_123;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

            connectionHelper = new DbConnectioHelper(connectionString);

            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var startDateTime = new DateTime(2020, 06, 01);
            var endDateTime = new DateTime(2020, 06, 30);

            var sqlQuery = "select taskcd.[Date], taskst.[Task Status], tasks.JobId, tasks.[User Group Name], taskst.[Task Message], taskin.[ISIN], taskin.[Instrument Id], tasks.TaskId "
                            + "from [dbo].[Fact_Task] as tasks "
                            + "inner join [dbo].[Dim_TaskCreationDate] as taskcd "
                            + "on tasks.[Creation Date Key] = taskcd.[Date Key] "
                            + "inner join [dbo].[Dim_TaskStatus] as taskst "
                            + "on taskst.[Task Status Key] = tasks.[Task Status Key] "
                            + "inner join [dbo].[Dim_TaskInfo] as taskin "
                            + "on taskin.[Task Info Key] = tasks.[Task Info Key] "
                            + "where taskcd.[Date] between @startDate and @endDate";

            var parameters = new Dictionary<string, object>();
            parameters.Add("@startDate", startDateTime);
            parameters.Add("@endDate", endDateTime);

            var queryResults = await connectionHelper.GetDbResultAsync(sqlQuery, parameters);


            // Adhoc and Batch Charts
            var succeededAdhoc = new SortedList<DateTime, int>();
            var failedAdhoc = new SortedList<DateTime, int>();

            foreach (var row in queryResults.Where(x => String.IsNullOrEmpty((string)x[2])))
            {
                var date = (DateTime)row[0];
                if (!succeededAdhoc.ContainsKey(date))
                    succeededAdhoc.Add(date, 0);
                if (!failedAdhoc.ContainsKey(date))
                    failedAdhoc.Add(date, 0);


                if ((string)row[1] == "Succeeded")
                    succeededAdhoc[date]++;
                else
                    failedAdhoc[date]++;
            }

            var succeededBatch = new SortedList<DateTime, int>();
            var failedBatch = new SortedList<DateTime, int>();

            foreach (var row in queryResults.Where(x => !String.IsNullOrEmpty((string)x[2])))
            {
                var date = (DateTime)row[0];

                if (!succeededBatch.ContainsKey(date))
                    succeededBatch.Add(date, 0);
                if (!failedBatch.ContainsKey(date))
                    failedBatch.Add(date, 0);

                if ((string)row[1] == "Succeeded")
                    succeededBatch[date]++;
                else
                    failedBatch[date]++;
            }

            ViewBag.LabelsAdhoc = succeededAdhoc.Keys.Select(d => d.ToString("yyyy-MM-dd")).ToArray();
            ViewBag.DataSucceededAdhoc = succeededAdhoc.Values.ToArray();
            ViewBag.DataFailedAdhoc = failedAdhoc.Values.ToArray();
            ViewBag.MaxYTickAdhoc = GetMaxYTick(succeededAdhoc.Values.ToArray(), failedAdhoc.Values.ToArray());

            ViewBag.LabelsBatch = succeededBatch.Keys.Select(d => d.ToString("yyyy-MM-dd")).ToArray();
            ViewBag.DataSucceededBatch = succeededBatch.Values.ToArray();
            ViewBag.DataFailedBatch = failedBatch.Values.ToArray();
            ViewBag.MaxYTickBatch = GetMaxYTick(succeededBatch.Values.ToArray(), failedBatch.Values.ToArray());


            //Erros Message Chart
            var messages = new Dictionary<string, int>();
            var messageBackgroundColors = new List<string>();
            var rg = new Random();
            foreach (var row in queryResults.Where(x => !String.IsNullOrEmpty((string)x[4])))
            {
                var message = (string)row[4];

                if (!messages.ContainsKey(message))
                    messages.Add(message, 1);
                else
                    messages[message]++;

                
                messageBackgroundColors.Add($"rgba({rg.Next(0,255)},{rg.Next(0, 255)},{rg.Next(0, 255)},1)");
            }

            ViewBag.ErrorMessages = messages.Keys.ToArray();
            ViewBag.nErrorMessages = messages.Values.ToArray();
            ViewBag.MessageBackgroundColors = messageBackgroundColors.ToArray();

            //Summary table
            var headers = new[] { "TaskId", "Task Status", "JobId", "User Group Name", "Task Message", "ISIN", "Instrument Id" };
            var rows = new List<string[]>();

            foreach (var row in queryResults)
            {
                var newRow = new string [] { (string)row[7], (string)row[1], (string)row[2], (string)row[3], (string)row[4], (string)row[5], (string)row[6] };
                rows.Add(newRow);
            }

            ViewBag.TableHeaders = headers;
            ViewBag.TableRows = rows;

            return View();
        }

        private int GetMaxYTick(int[] succeeded, int[] failed)
        {
            int max = 0;
            for(int i = 0; i < succeeded.Length; i++)
            {
                var sum = succeeded[i] + failed[i];
                if (sum > max)
                    max = sum;
            }
            return max;
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
