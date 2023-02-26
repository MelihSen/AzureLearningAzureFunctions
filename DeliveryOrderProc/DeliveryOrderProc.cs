using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Configuration;
using Microsoft.WindowsAzure.Storage.Queue.Protocol;
using System.Reflection.Metadata;

//https://markheath.net/post/azure-functions-rest-csharp-bindings

namespace DeliveryOrderProc
{
    public static class DeliveryOrderProc
    {
        [FunctionName("DeliveryOrderProc")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            [CosmosDB(
                databaseName: "CosmosdDBeShopDbFinal",
                containerName: "OrdersContainer",
                Connection = "CosmosDBConnection")] IAsyncCollector<dynamic> documentsOut,
            ILogger log)
        {

            string response;
            
            try
            {
                string requestBody = new StreamReader(req.Body).ReadToEndAsync().Result;
                var document = JsonConvert.DeserializeObject(requestBody);

                if (string.IsNullOrEmpty(requestBody) == false)
                {
                    document = JsonConvert.DeserializeObject(requestBody);
                }
                else
                {
                    document = new { Description = "Test Request", id = Guid.NewGuid() };
                    //document = new { Description = "Test Request", id = 3 };
                }

                await documentsOut.AddAsync(document);
                response = "C# HTTP trigger function processed a request";
                log.LogInformation(response);
            }
            catch (Exception ex)
            {
                response = ex.Message;
                log.LogError(response);
                return new BadRequestObjectResult(response);
            }
            return new OkObjectResult(response);
        }
    }
}
