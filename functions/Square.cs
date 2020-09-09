using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace functions
{
    public static class Square
    {
        [FunctionName("Square")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            int.TryParse(req.Query["number"], out int number);

            log.LogInformation($"Number from query: {number}");

            if (number == 0) {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic data = JsonConvert.DeserializeObject(requestBody);
                log.LogInformation($"POST data: {requestBody}");
                number = data?.number ?? 0;
            }

            var result = number*number;

            log.LogInformation($"Result: {result}");

            return new OkObjectResult(new { result });
        }
    }
}
