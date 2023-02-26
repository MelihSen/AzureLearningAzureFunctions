using System;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Storage.Blobs;

namespace OrderItemsReserverNew
{
    public class OrderRequestFromServiceBus
    {
        [FunctionName("OrderRequestFromServiceBus")]
        public static void Run([ServiceBusTrigger("OrderedItemsQueue", Connection = "ServiceBusConnectionString")]string myQueueItem, ILogger log)
        {
            try
            {
                MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(myQueueItem));
                string Connection = Environment.GetEnvironmentVariable("AzureWebJobsStorage"); 
                //string Connection = Environment.GetEnvironmentVariable("AzureWebJobsStorageFake");  //Fake connection string to test Logic App
                string containerName = Environment.GetEnvironmentVariable("ContainerName");
                
                var blobClient = new BlobContainerClient(Connection, containerName);

                string fileName = "Order_" + DateTime.Now.ToString("yyyy-MM-dd_hh-mm-ss-FF") + "#" + Guid.NewGuid().ToString() + ".json";
                var blob = blobClient.GetBlobClient(fileName);
                blob.Upload(stream, true);
                log.LogInformation($"C# ServiceBus queue trigger function processed message: {myQueueItem}");
            }
            catch (Exception ex)
            {
                try
                {
                    var errorData = new ErrorData()
                    {
                        Exception = ex,
                        OrderData = myQueueItem
                    };                    
                    
                    //Call Logic App to send email
                    var jsonData = JsonConvert.SerializeObject(errorData);

                    ///Logic App Call
                    string funcUrl = Environment.GetEnvironmentVariable("OrderItemsReserverFuncUrl");
                    HttpClient client = new HttpClient();
                    StringContent stringContent = new StringContent(jsonData, Encoding.UTF8, "application/json");
                    var result = client.PostAsync(funcUrl, stringContent).Result;

                    log.LogError(JsonConvert.SerializeObject(ex));
                }
                catch (Exception exInner)
                {
                    log.LogError(JsonConvert.SerializeObject(exInner));
                }
            }
        }

        [Serializable]
        public class ErrorData
        {
            public string OrderData { get; set; }
            public Exception Exception { get; set; }
        }

    }
}
