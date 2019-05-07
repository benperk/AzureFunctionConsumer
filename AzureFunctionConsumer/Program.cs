using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

using static System.Console;
using static System.Convert;

using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.ServiceBus;

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;

using Newtonsoft.Json;

namespace AzureFunctionConsumer
{
    class Program
    {
        private static CloudBlobClient blobClient;
        private static CloudQueue storageAccountQueue;
        private static EventHubClient eventHubClient;
        private static HttpClient httpClient;
        private static IQueueClient queueClient;
        private static DocumentClient documentClient;
        private static CloudTableClient tableStorageClient;
        static Program()
        {
            httpClient = new HttpClient();
        }
        static void Main(string[] args)
        {
            bool keepGoing = true;
            try
            {
                do
                {
                    var selection = DisplayMenu();
                    switch (selection)
                    {
                        case 1:
                            WriteLine("You selected Event Hub.");
                            MainEventHubAsync(args).GetAwaiter().GetResult();
                            break;
                        case 2:
                            WriteLine("You selected Storage Queue.");
                            MainStorageQueueAsync(args).GetAwaiter().GetResult();
                            break;
                        case 3:
                            WriteLine("You selected Blob Storage.");
                            MainBlobStorageAsync(args).GetAwaiter().GetResult();
                            break;
                        case 4:
                            WriteLine("You selected Service Bus.");
                            MainServiceBusAsync(args).GetAwaiter().GetResult();
                            break;
                        case 5:
                            WriteLine("You selected Cosomos DB.");
                            MainCosmosDBAsync(args).GetAwaiter().GetResult();
                            break;
                        case 6:
                            WriteLine("You selected HTTP Trigger.");
                            MainHTTPTriggerAsync(args).GetAwaiter().GetResult();
                            break;
                        case 7:
                            WriteLine("You selected Event Grid.");
                            WriteLine("NOT YET IMPLEMENTED.");
                            break;
                        case 8:
                            WriteLine("You selected Table Storage.");
                            MainStorageTableAsync(args).GetAwaiter().GetResult();
                            break;
                        case 9:
                            WriteLine("You selected Microsoft Graph.");
                            WriteLine("You need to run this one from a browser and send your AAD credentials.");
                            break;
                        case 10:
                            WriteLine("You selected SendGrid.");
                            WriteLine("NOT YET IMPLEMENTED.");
                            break;
                        case 11:
                            WriteLine("You selected SignalR.");
                            WriteLine("NOT YET IMPLEMENTED.");
                            break;
                        case 12:
                            WriteLine("You selected Timer.");
                            WriteLine("This one is run completely from the portal.");
                            break;
                        case 13:
                            WriteLine("Bye.");
                            keepGoing = false;
                            break;
                        default:
                            throw new InvalidOperationException("You entered an invalid option.  Bye.");
                    }
                } while (keepGoing);
            }
            catch (Exception ex)
            {
                WriteLine($"Well...something happend that wasn't expected, specifically: {ex.Message}");
            }
        }
        static public int DisplayMenu()
        {
            WriteLine();
            WriteLine("1.  Event Hub");
            WriteLine("2.  Storage Queue");
            WriteLine("3.  Blob Storage");
            WriteLine("4.  Service Bus");
            WriteLine("5.  Cosomos DB");
            WriteLine("6.  HTTP Trigger");
            WriteLine("7.  Event Grid");
            WriteLine("8.  Table Storage");
            WriteLine("9.  Microsoft Graph");
            WriteLine("10. SendGrid");
            WriteLine("11. SignalR");
            WriteLine("12. SignalR");
            WriteLine("13. Exit");
            WriteLine("Which would you like to trigger?  Enter '13' to exit.");
            var result = ReadLine();
            return ToInt32(result);
        }
        #region Blob Storage
        //Use the Event Grid trigger instead of the Blob storage trigger for high consumption blob-only storage accounts
        private static async Task MainBlobStorageAsync(string[] args)
        {
            WriteLine("Enter your Blob Storage connection string:");
            var BlobStorageConnectionString = ReadLine();
            while (BlobStorageConnectionString.Length == 0)
            {
                WriteLine("Try again, this value must have a length > 0");
                WriteLine("Enter your Blob Storage connection string:");
                BlobStorageConnectionString = ReadLine();
            }
            WriteLine("Enter the Blob Container name:");
            var BlobContainerName = ReadLine();
            while (BlobContainerName.Length == 0)
            {
                WriteLine("Try again, this value must have a length > 0");
                WriteLine("Enter the Blob Container name: (lowercase only letters!)");
                BlobContainerName = ReadLine().ToLower();
            }
            WriteLine("Enter number of blobs to add: ");
            int BlobsToSend = 0;
            while (!int.TryParse(ReadLine(), out BlobsToSend))
            {
                WriteLine("Try again, this value must be numeric.");
            }
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(BlobStorageConnectionString);
            blobClient = storageAccount.CreateCloudBlobClient();
            WriteLine("Creating blob container...");
            CloudBlobContainer blobContainer = blobClient.GetContainerReference(BlobContainerName);
            if (await blobContainer.CreateIfNotExistsAsync())
            {
                WriteLine($"Blob container '{blobContainer.Name}' Created.");
            }
            else
            {
                WriteLine($"Blob container '{blobContainer.Name}' Exists.");
            }

            await SendBlobsToBlobStorage(BlobsToSend, blobContainer);

            WriteLine("Press Y to delete the blobs.");

            if (ReadLine() == "Y")
            {
                await DeleteBlobs(BlobsToSend, blobContainer);
            }
            else
            {
                WriteLine("No blobs deleted.");
            }
        }
        private static async Task SendBlobsToBlobStorage(int numBlobsToSend, CloudBlobContainer container)
        {
            for (var i = 0; i < numBlobsToSend; i++)
            {
                try
                {
                    var blob = $"Blob {i}";
                    WriteLine($"Sending blob: {blob} named helloworld{i}.txt to {container.Name}");
                    CloudBlockBlob blockBlob = container.GetBlockBlobReference($"helloworld{i}.txt");
                    blockBlob.UploadTextAsync($"Hello, World! {i}").Wait();
                }
                catch (StorageException se)
                {
                    WriteLine($"StorageException: {se.Message}");
                }
                catch (Exception ex)
                {
                    WriteLine($"{DateTime.Now} > Exception: {ex.Message}");
                }

                await Task.Delay(10);
            }

            WriteLine($"{numBlobsToSend} blobs sent.");
        }
        private static async Task DeleteBlobs(int numBlobsToDelete, CloudBlobContainer container)
        {
            for (var i = 0; i < numBlobsToDelete; i++)
            {
                try
                {
                    var blob = $"Blob {i}";
                    WriteLine($"Deleting blob: {blob} named helloworld{i}.txt from {container.Name}");
                    CloudBlockBlob blockBlob = container.GetBlockBlobReference($"helloworld{i}.txt");
                    await blockBlob.DeleteIfExistsAsync();
                }
                catch (StorageException se)
                {
                    WriteLine($"StorageException: {se.Message}");
                }
                catch (Exception ex)
                {
                    WriteLine($"{DateTime.Now} > Exception: {ex.Message}");
                }

                await Task.Delay(10);
            }

            WriteLine($"{numBlobsToDelete} blobs deleted.");
        }
        #endregion
        #region Event Hubs
        private static async Task MainEventHubAsync(string[] args)
        {
            WriteLine("Enter your Event Hub connection string:");
            var EventHubConnectionString = ReadLine();
            while (EventHubConnectionString.Length == 0)
            {
                WriteLine("Try again, this value must have a length > 0");
                WriteLine("Enter your Event Hub connection string:");
                EventHubConnectionString = ReadLine();
            }
            WriteLine("Enter your Event Hub name:");
            var EventHubName = ReadLine();
            while (EventHubName.Length == 0)
            {
                WriteLine("Try again, this value must have a length > 0");
                WriteLine("Enter your Event Hub name:");
                EventHubName = ReadLine();
            }
            WriteLine("Enter number of events to add: ");
            int EventHubEventsToSend = 0;
            while (!int.TryParse(ReadLine(), out EventHubEventsToSend))
            {
                WriteLine("Try again, this value must be numeric.");
            }

            var connectionStringBuilder = new EventHubsConnectionStringBuilder(EventHubConnectionString)
            {
                EntityPath = EventHubName
            };
            eventHubClient = EventHubClient.CreateFromConnectionString(connectionStringBuilder.ToString());
            await SendMessagesToEventHub(EventHubEventsToSend);
            await eventHubClient.CloseAsync();
        }
        private static async Task SendMessagesToEventHub(int numMessagesToSend)
        {
            for (var i = 0; i < numMessagesToSend; i++)
            {
                try
                {
                    var message = $"Message-{i}";

                    var json = "{\"message\":\"" + message + "\"}";
                    WriteLine($"Sending message: {json}");
                    await eventHubClient.SendAsync(new EventData(Encoding.UTF8.GetBytes(json)));
                }
                catch (EventHubsCommunicationException ehce)
                {
                    WriteLine($"EventHubsCommunicationException: {ehce.Message}");
                }
                catch (EventHubsException ehe)
                {
                    WriteLine($"EventHubsException: {ehe.Message}");
                }
                catch (Exception ex)
                {
                    WriteLine($"{DateTime.Now} > Exception: {ex.Message}");
                }

                await Task.Delay(10);
            }

            WriteLine($"{numMessagesToSend} messages sent.");
        }
        #endregion
        #region Azure Queue
        private static async Task MainStorageQueueAsync(string[] args)
        {
            WriteLine("Enter your Storage Queue connection string:");
            var StorageQueueConnectionString = ReadLine();
            while (StorageQueueConnectionString.Length == 0)
            {
                WriteLine("Try again, this value must have a length > 0");
                WriteLine("Enter your Storage Queue connection string:");
                StorageQueueConnectionString = ReadLine();
            }
            WriteLine("Enter your Storage Queue name:");
            var StorageQueueName = ReadLine();
            while (StorageQueueName.Length == 0)
            {
                WriteLine("Try again, this value must have a length > 0");
                WriteLine("Enter your Storage Queue name:");
                StorageQueueName = ReadLine();
            }
            WriteLine("Enter number of messages to add: ");
            int StorageMessagesToSend = 0;
            while (!int.TryParse(ReadLine(), out StorageMessagesToSend))
            {
                WriteLine("Try again, this value must be numeric.");
            }
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(StorageQueueConnectionString);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            storageAccountQueue = queueClient.GetQueueReference(StorageQueueName);
            if (await storageAccountQueue.CreateIfNotExistsAsync())
            {
                WriteLine($"Queue '{storageAccountQueue.Name}' Created.");
            }
            else
            {
                WriteLine($"Queue '{storageAccountQueue.Name}' Exists.");
            }
            await SendMessagesToStorageAccount(StorageMessagesToSend);
        }
        private static async Task SendMessagesToStorageAccount(int numMessagesToSend)
        {
            for (var i = 0; i < numMessagesToSend; i++)
            {
                try
                {
                    var message = $"Message {i} - {Guid.NewGuid().ToString()}";
                    WriteLine($"Sending message: {message}");
                    await storageAccountQueue.AddMessageAsync(new CloudQueueMessage(message));
                }
                catch (StorageException se)
                {
                    WriteLine($"StorageException: {se.Message}");
                }
                catch (Exception ex)
                {
                    WriteLine($"{DateTime.Now} > Exception: {ex.Message}");
                }

                await Task.Delay(10);
            }

            WriteLine($"{numMessagesToSend} messages sent.");
        }
        #endregion
        #region HTTP Trigger
        private static async Task MainHTTPTriggerAsync(string[] args)
        {
            WriteLine("Enter your HTTP Trigger URL:");
            var HTTPTriggerUrl = ReadLine();
            while (HTTPTriggerUrl.Length == 0)
            {
                WriteLine("Try again, this value must have a length > 0");
                WriteLine("Enter your HTTP Trigger URL:");
                HTTPTriggerUrl = ReadLine();
            }
            WriteLine("Enter HTTP Trigger function key:");
            var HTTPTriggerFunctionKey = ReadLine();
            while (HTTPTriggerFunctionKey.Length == 0)
            {
                WriteLine("Try again, this value must have a length > 0");
                WriteLine("Enter HTTP Trigger function key:");
                HTTPTriggerFunctionKey = ReadLine();
            }
            WriteLine("Enter your name or any name:");
            var Name = ReadLine();
            while (Name.Length == 0)
            {
                WriteLine("Try again, this value must have a length > 0");
                WriteLine("Enter any value for Query String usage:");
                Name = ReadLine();
            }
            WriteLine("Enter number of requests to send: ");
            int HTTPRequestsToSend = 0;
            while (!int.TryParse(ReadLine(), out HTTPRequestsToSend))
            {
                WriteLine("Try again, this value must be numeric.");
            }
            await SendHTTPRequests(HTTPRequestsToSend, HTTPTriggerUrl, HTTPTriggerFunctionKey, Name);
        }
        private static async Task SendHTTPRequests(int numHTTPRequestsToSend, string Url, string FunctionKey, string Name)
        {
            for (var i = 0; i < numHTTPRequestsToSend; i++)
            {
                try
                {
                    var httpRequest = $"HTTP request {i}";
                    WriteLine($"Sending request: {httpRequest}");
                    var json = "{\"name\":\"" + Name + "\"}";
                    var encodedContent = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync(Url + "?code=" + FunctionKey, encodedContent).ConfigureAwait(false);
                    WriteLine($"The response code is: {response.StatusCode}");
                    response.EnsureSuccessStatusCode();
                    var resultContent = await response.Content.ReadAsStringAsync();
                    WriteLine(resultContent);
                }
                catch (HttpRequestException hre)
                {
                    WriteLine($"HttpRequestException: {hre.Message}");
                }
                catch (Exception ex)
                {
                    WriteLine($"{DateTime.Now} > Exception: {ex.Message}");
                }

                await Task.Delay(10);
            }

            WriteLine($"{numHTTPRequestsToSend} requests sent.");
        }
        #endregion
        #region Service Bus
        private static async Task MainServiceBusAsync(string[] args)
        {
            WriteLine("Enter your Service Bus connection string:");
            var ServiceBusConnectionString = ReadLine();
            while (ServiceBusConnectionString.Length == 0)
            {
                WriteLine("Try again, this value must have a length > 0");
                WriteLine("Enter your Service Bus connection string:");
                ServiceBusConnectionString = ReadLine();
            }
            WriteLine("Enter your Queue name:");
            var QueueName = ReadLine();
            while (QueueName.Length == 0)
            {
                WriteLine("Try again, this value must have a length > 0");
                WriteLine("Enter yourQueue name:");
                QueueName = ReadLine();
            }
            WriteLine("Enter number of messages to add: ");
            int ServiceBusMessagesToSend = 0;
            while (!int.TryParse(ReadLine(), out ServiceBusMessagesToSend))
            {
                WriteLine("Try again, this value must be numeric.");
            }
            queueClient = new QueueClient(ServiceBusConnectionString, QueueName);
            await SendMessagesToServicebus(ServiceBusMessagesToSend);
            await queueClient.CloseAsync();
        }
        private static async Task SendMessagesToServicebus(int numMessagesToSend)
        {
            for (var i = 0; i < numMessagesToSend; i++)
            {
                try
                {
                    var messageBody = $"Message-{i}-{Guid.NewGuid().ToString("N")}-{DateTime.Now.Minute}";
                    WriteLine($"Sending message: {messageBody}");
                    var message = new Message(Encoding.UTF8.GetBytes(messageBody));
                    await queueClient.SendAsync(message);
                }
                catch (ServiceBusTimeoutException sbte)
                {
                    WriteLine($"ServiceBusTimeoutException: {sbte.Message}");
                }
                catch (ServiceBusException sbe)
                {
                    WriteLine($"ServiceBusException: {sbe.Message}");
                }
                catch (Exception ex)
                {
                    WriteLine($"{DateTime.Now} > Exception: {ex.Message}");
                }

                await Task.Delay(10);
            }

            WriteLine($"{numMessagesToSend} messages sent.");
        }
        #endregion
        #region CosmosDB
        private static async Task MainCosmosDBAsync(string[] args)
        {
            WriteLine("Enter your Cosmos database name:");
            var CosmosDatabaseName = ReadLine();
            while (CosmosDatabaseName.Length == 0)
            {
                WriteLine("Try again, this value must have a length > 0");
                WriteLine("Enter your Cosmos database name:");
                CosmosDatabaseName = ReadLine();
            }
            WriteLine("Enter your Cosmos collection name:");
            var CosmosCollectionName = ReadLine();
            while (CosmosCollectionName.Length == 0)
            {
                WriteLine("Try again, this value must have a length > 0");
                WriteLine("Enter your Cosmos collection name:");
                CosmosCollectionName = ReadLine();
            }
            WriteLine("Enter your Cosmos endpoint url name:");
            var CosmosEndpointUrl = ReadLine();
            while (CosmosEndpointUrl.Length == 0)
            {
                WriteLine("Try again, this value must have a length > 0");
                WriteLine("Enter your Cosmos endpoint url name:");
                CosmosEndpointUrl = ReadLine();
            }
            WriteLine("Enter your Cosmos account key:");
            var CosmosAccountKey = ReadLine();
            while (CosmosAccountKey.Length == 0)
            {
                WriteLine("Try again, this value must have a length > 0");
                WriteLine("Enter your Cosmos account key:");
                CosmosAccountKey = ReadLine();
            }
            WriteLine("Enter number of documents to add: ");
            int CosmosDocumentsToSend = 0;
            while (!int.TryParse(ReadLine(), out CosmosDocumentsToSend))
            {
                WriteLine("Try again, this value must be numeric.");
            }

            try
            {
                using (documentClient = new DocumentClient(new Uri(CosmosEndpointUrl), CosmosAccountKey))
                {
                    Uri collectionUri = UriFactory.CreateDocumentCollectionUri(CosmosDatabaseName, CosmosCollectionName);
                    await SendDocumentsToCosmos(CosmosDocumentsToSend, collectionUri);
                    WriteLine("Press Y to delete these document.");
                    if (ReadLine() == "Y")
                    {
                        await DeleteCosmosDocuments(CosmosDocumentsToSend, CosmosDatabaseName, CosmosCollectionName);
                    }
                    else
                    {
                        WriteLine("No documents deleted.");
                    }
                }
            }
            catch (DocumentClientException dce)
            {
                WriteLine($"{dce.StatusCode} error occurred: {dce.Message}");
            }
            catch (Exception ex)
            {
                WriteLine($"Error occurred: {ex.Message}");
            }
        }
        private static async Task SendDocumentsToCosmos(int numDocumentsToSend, Uri collectionUri)
        {
            for (var i = 0; i < numDocumentsToSend; i++)
            {
                try
                {
                    var message = $"Document {i}";
                    CosmosDocument cosmosDocument = CreateCosmosDocument(i.ToString());
                    WriteLine($"Sending document: {message}");
                    await documentClient.CreateDocumentAsync(collectionUri, cosmosDocument);
                }
                catch (DocumentClientException dce)
                {
                    WriteLine($"{dce.StatusCode} error occurred: {dce.Message}");
                }
                catch (Exception ex)
                {
                    WriteLine($"Error occurred: {ex.Message}");
                }

                await Task.Delay(10);
            }

            WriteLine($"{numDocumentsToSend} documents sent.");
        }
        private static async Task DeleteCosmosDocuments(int numDocumentsToDelete, string cosmosDatabaseName, string cosmosCollectionName)
        {
            for (var i = 0; i < numDocumentsToDelete; i++)
            {
                try
                {
                    var message = $"Document {i}";
                    WriteLine($"Deleting document: {message}");
                    ResourceResponse<Document> response = await documentClient.DeleteDocumentAsync(
                            UriFactory.CreateDocumentUri(cosmosDatabaseName, cosmosCollectionName, i.ToString()));
                }
                catch (DocumentClientException dce)
                {
                    WriteLine($"{dce.StatusCode} error occurred: {dce.Message}");
                }
                catch (Exception ex)
                {
                    WriteLine($"Error occurred: {ex.Message}");
                }

                await Task.Delay(10);
            }

            WriteLine($"{numDocumentsToDelete} documents deleted.");
        }
        private static CosmosDocument CreateCosmosDocument(string documentId)
        {
            CosmosDocument cosmosDocument = new CosmosDocument()
            {
                Id = documentId,
                CreateDate = DateTime.Now,
                AccountNumber = $"Account{documentId}",
                Freight = 472.3108m,
                TotalDue = 985.018m,
                Items = new CosmosDocumentDetail[]
                {
                    new CosmosDocumentDetail
                    {
                        OrderQty = ToInt32(documentId),
                        ProductId = ToInt32(documentId) + ToInt32(documentId),
                        UnitPrice = 419.4589m
                    }
                },
            };
            return cosmosDocument;
        }
        public class CosmosDocument
        {
            [JsonProperty(PropertyName = "id")]
            public string Id { get; set; }
            public DateTime CreateDate { get; set; }
            public string AccountNumber { get; set; }
            public decimal Freight { get; set; }
            public decimal TotalDue { get; set; }
            public CosmosDocumentDetail[] Items { get; set; }
        }
        public class CosmosDocumentDetail
        {
            public int OrderQty { get; set; }
            public int ProductId { get; set; }
            public decimal UnitPrice { get; set; }
        }
        #endregion
        #region Azure Table
        private static async Task MainStorageTableAsync(string[] args)
        {
            WriteLine("Enter your Table Storage connection string:");
            var TableStorageConnectionString = ReadLine();
            while (TableStorageConnectionString.Length == 0)
            {
                WriteLine("Try again, this value must have a length > 0");
                WriteLine("Enter your Table Storage connection string:");
                TableStorageConnectionString = ReadLine();
            }
            WriteLine("Enter your Table name:");
            var TableName = ReadLine();
            while (TableName.Length == 0)
            {
                WriteLine("Try again, this value must have a length > 0");
                WriteLine("Enter your Table name:");
                TableName = ReadLine();
            }
            WriteLine("Enter number of rows to add: ");
            int StorageRowsToSend = 0;
            while (!int.TryParse(ReadLine(), out StorageRowsToSend))
            {
                WriteLine("Try again, this value must be numeric.");
            }
            CloudStorageAccount TableStorageAccount = CloudStorageAccount.Parse(TableStorageConnectionString);
            tableStorageClient = TableStorageAccount.CreateCloudTableClient();
            CloudTable table = tableStorageClient.GetTableReference(TableName);
            if (await table.CreateIfNotExistsAsync())
            {
                WriteLine($"Queue '{table.Name}' Created.");
            }
            else
            {
                WriteLine($"Queue '{table.Name}' Exists.");
            }
            await SendMessagesToTableStorageAccount(StorageRowsToSend, table);

            WriteLine($"Press Y to delete table {TableName}.");

            if (ReadLine() == "Y")
            {
                await DeleteTableRows(table);
            }
            else
            {
                WriteLine("No rows deleted.");
            }
        }
        private static async Task SendMessagesToTableStorageAccount(int numRowToInsert, CloudTable table)
        {
            var pKey = Guid.NewGuid().ToString();

            for (var i = 0; i < numRowToInsert; i++)
            {
                try
                {
                    var row = $"Row #{i} - {Guid.NewGuid().ToString()}";

                    TableStorageRowEntity tsre = new TableStorageRowEntity(pKey, Guid.NewGuid().ToString())
                        { message = row , dateTime = DateTime.Now.ToString() };                                       
                    
                    TableOperation insertOperation = TableOperation.InsertOrMerge(tsre);
                    TableResult result = table.ExecuteAsync(insertOperation).Result;
                    WriteLine($"Inserted: {row}");
                }
                catch (StorageException se)
                {
                    WriteLine($"StorageException: {se.Message}");
                }
                catch (Exception ex)
                {
                    WriteLine($"{DateTime.Now} > Exception: {ex.Message}");
                }

                await Task.Delay(10);
            }

            WriteLine($"{numRowToInsert} rows inserted.");
        }
        private static async Task DeleteTableRows(CloudTable table)
        {
            int counter = 0;
            try
            {                
                TableContinuationToken token = null;
                TableQuery<TableStorageRowEntity> query = new TableQuery<TableStorageRowEntity>()
                    .Select(new List<string> { "PartitionKey" });
                var rows = await table.ExecuteQuerySegmentedAsync<TableStorageRowEntity>(query, token);

                foreach (var item in rows)
                {
                    WriteLine($"Deleting row: {item.RowKey} ");
                    TableOperation deleteOperation = TableOperation.Delete(item);
                    TableResult result = await table.ExecuteAsync(deleteOperation);                    
                    counter++;
                }
            }
            catch (StorageException se)
            {
                WriteLine($"StorageException: {se.Message}");
            }
            catch (Exception ex)
            {
                WriteLine($"{DateTime.Now} > Exception: {ex.Message}");
            }

            await Task.Delay(10);

            WriteLine($"{counter.ToString()} rows deleted.");
        }
        #endregion
    }

    public class TableStorageRowEntity : TableEntity
    {
        public string message { get; set; }
        public string dateTime { get; set; }

        public TableStorageRowEntity() { }

        public TableStorageRowEntity(string pKey, string rKey)
        {
            PartitionKey = pKey;
            RowKey = rKey;
        }
    }
}
