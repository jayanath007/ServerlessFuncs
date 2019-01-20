using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage;


namespace GroupChatFuncs
{
    public static class MessageApi
    {
        [FunctionName("MessageApi")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            return name != null
                ? (ActionResult)new OkObjectResult($"Hello, {name}")
                : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
        }


        [FunctionName("CreateMessage")]
        public static async Task<IActionResult> CreateMessage(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "CreateMessage")]HttpRequest req,
            [Table("Messages", Connection = "AzureWebJobsStorage")] IAsyncCollector<MessageTableEntity> messageTable,
            ILogger log)
        {
            log.LogInformation("Creating a new chat messages");
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var input = JsonConvert.DeserializeObject<MessageCreateModel>(requestBody);

            var message = new Message() { Content = input.Content };
            await messageTable.AddAsync(message.ToTableEntity());

            return new OkObjectResult(message);
        
        }



        [FunctionName("GetMessages")]
        public static async Task<IActionResult> GetMessages(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "message")]HttpRequest req,
            [Table("messages", Connection = "AzureWebJobsStorage")] CloudTable messageTable,
            ILogger log)
        {
            log.LogInformation("Getting message list items");
            var query = new TableQuery<MessageTableEntity>();
            var segment = await messageTable.ExecuteQuerySegmentedAsync(query, null);

             // segment.Select(Mappings.ToMessage)
            return new OkObjectResult(segment.Results);
        }

        [FunctionName("GetMessageById")]
        public static IActionResult GetMessageById(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "message/{id}")]HttpRequest req,
            [Table("messages", "MESSAGE", "{id}", Connection = "AzureWebJobsStorage")] MessageTableEntity message,
            ILogger log, string id)
        {
            log.LogInformation("Getting message item by id");
            if (message == null)
            {
                log.LogInformation($"Item {id} not found");
                return new NotFoundResult();
            }
            return new OkObjectResult(message.ToMessage());
        }

        [FunctionName("UpdateMessage")]
        public static async Task<IActionResult> UpdateMessage(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "message/{id}")]HttpRequest req,
            [Table("messages", Connection = "AzureWebJobsStorage")] CloudTable messageTable,
            ILogger log, string id)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var updated = JsonConvert.DeserializeObject<MessageUpdateModel>(requestBody);
            var findOperation = TableOperation.Retrieve<MessageTableEntity>("MESSAGE", id);
            var findResult = await messageTable.ExecuteAsync(findOperation);
            if (findResult.Result == null)
            {
                return new NotFoundResult();
            }
            var existingRow = (MessageTableEntity)findResult.Result;
            // existingRow.IsCompleted = updated.IsCompleted;
            if (!string.IsNullOrEmpty(updated.Content))
            {
                existingRow.Content = updated.Content;
            }

            var replaceOperation = TableOperation.Replace(existingRow);
            await messageTable.ExecuteAsync(replaceOperation);
            return new OkObjectResult(existingRow.ToMessage());
        }

        [FunctionName("DeleteMessage")]
        public static async Task<IActionResult> DeleteMessage(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "message/{id}")]HttpRequest req,
            [Table("messages", Connection = "AzureWebJobsStorage")] CloudTable messageTable,
            ILogger log, string id)
        {
            var deleteOperation = TableOperation.Delete(new TableEntity()
            { PartitionKey = "MESSAGE", RowKey = id, ETag = "*" });
            try
            {
                var deleteResult = await messageTable.ExecuteAsync(deleteOperation);
            }
            catch (StorageException e) when (e.RequestInformation.HttpStatusCode == 404)
            {
                return new NotFoundResult();
            }
            return new OkResult();
        }



    }
}
