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

namespace bi_testproj.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private DbConnectioHelper connectionHelper;

        public HomeController(ILogger<HomeController> logger)
        {
            var connectionString = "Data Source=localhost;Initial Catalog=KidCalc_DW;Integrated Security=True";
            connectionHelper = new DbConnectioHelper(connectionString);

            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var sqlQuery = "select taskcd.[Date], taskst.[Task Status], tasks.JobId, tasks.[User Group Name], taskst.[Task Message] from [dbo].[Fact_Task] as tasks "
                            + "inner join[dbo].[Dim_TaskCreationDate] as taskcd "
                            + "on tasks.[Creation Date Key] = taskcd.[Date Key] "
                            + "inner join[dbo].[Dim_TaskStatus] as taskst "
                            + "on taskst.[Task Status Key] = tasks.[Task Status Key] "
                            + "where taskcd.[Date] between @startDate and @endDate";

            var parameters = new Dictionary<string, object>();
            parameters.Add("@startDate", DateTime.Now.AddDays(-12));
            parameters.Add("@endDate", DateTime.Now);

            var queryResults = await connectionHelper.GetDbResultAsync(sqlQuery, parameters);

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

            var messages = new Dictionary<string, int>();
            foreach (var row in queryResults.Where(x => !String.IsNullOrEmpty((string)x[4])))
            {
                var message = (string)row[4];

                if (!messages.ContainsKey(message))
                    messages.Add(message, 0);
                else
                    messages[message]++;
            }

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
