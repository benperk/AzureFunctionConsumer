using System;
using System.Net.Http;
using System.Text;
using System.Threading;
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
using System.Linq;

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
                            WriteLine("This one is triggered with using a CRON schedule.");
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
            WriteLine("5.  Cosmos DB");
            WriteLine("6.  HTTP Trigger");
            WriteLine("7.  Event Grid");
            WriteLine("8.  Table Storage");
            WriteLine("9.  Microsoft Graph");
            WriteLine("10. SendGrid");
            WriteLine("11. SignalR");
            WriteLine("12. Timer");
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
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(BlobStorageConnectionString);
            blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer blobContainer = blobClient.GetContainerReference(BlobContainerName);

            WriteLine("Enter A to add blobs or S to search the blob container:");
            var AddOrSearch = ReadLine();
            while (AddOrSearch != "A" && AddOrSearch != "S")
            {
                WriteLine("Try again, this value must be either A or S");
                WriteLine("Enter A to add blobs or S to search the blob container:");
                AddOrSearch = ReadLine().ToUpper();
            }
            if (AddOrSearch == "S")
            {
                await SearchBlobsAsync(blobContainer);
            }
            else
            {
                WriteLine("Enter number of blobs to add: ");
                int BlobsToSend = 0;
                while (!int.TryParse(ReadLine(), out BlobsToSend))
                {
                    WriteLine("Try again, this value must be numeric.");
                }
                WriteLine("Creating blob container...");
                //CloudBlobContainer blobContainer = blobClient.GetContainerReference(BlobContainerName);
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
        }
        private static async Task SendBlobsToBlobStorage(int numBlobsToSend, CloudBlobContainer container)
        {
            Random r = new Random();
            var number = r.Next(1, 10);
            string blobContent = string.Empty;

            for (var i = 0; i < numBlobsToSend; i++)
            {
                try
                {
                    var blob = $"Blob {i}";
                    WriteLine($"Sending blob: {blob} named helloworld{i}.txt to {container.Name}");
                    CloudBlockBlob blockBlob = container.GetBlockBlobReference($"helloworld{i}.txt");

                    for (int n = 0; n < number; n++)
                    {
                        blobContent = blobContent + Guid.NewGuid().ToString("N");
                    }

                    await blockBlob.UploadTextAsync(blobContent);
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
        private static async Task SearchBlobsAsync(CloudBlobContainer container)
        {
            if (await container.ExistsAsync())
            {
                WriteLine($"Enter the path to search, leave blank to search the entire '{container.Name}' container:");
                WriteLineWithGreenColor(true);
                WriteLine($"Ex: blobreceipts/hostId/namespace.functionName.Run");
                WriteLineWithGreenColor(false);
                var prefix = @ReadLine();

                WriteLine($"Enter the start date by counting the number of days from today on which the search should start.");
                WriteLineWithGreenColor(true);
                WriteLine($"Ex: 1 is {DateTime.Now.AddDays(-1)} (yesterday) and 5 is {DateTime.Now.AddDays(-5)} (5 days ago)");
                WriteLineWithGreenColor(false);
                int startDate = (ToInt32(ReadLine()) * -1);

                WriteLine($"Enter the end date by counting the number of days from today on which the search should end.");
                WriteLineWithGreenColor(true);
                WriteLine($"Enter 0 for now {DateTime.Now}");
                WriteLineWithGreenColor(false);
                int endDate = (ToInt32(ReadLine()) * -1);
                if (endDate == 0) endDate = 1; //Seems this logic didn't work when endDate was 0, but nothing can already exist which is added tomorrow...

                if (startDate > endDate)
                {
                    WriteLine($"Start date {DateTime.Now.AddDays(startDate)} " +
                        $"cannot come before end date {DateTime.Now.AddDays(endDate)}, start over.");
                    return;
                }
                WriteLineWithGreenColor(true);
                WriteLine($"Searching '{container.Name} -> {prefix}' from {DateTime.Now.AddDays(startDate)} " +
                    $"to {DateTime.Now.AddDays(endDate)}");
                WriteLineWithGreenColor(false);

                try
                {
                    int maxResults = 500;
                    BlobContinuationToken continuationToken = null;
                    CloudBlob blob;
                    IEnumerable<IListBlobItem> blobList;

                    do
                    {
                        BlobResultSegment resultSegment = await container.ListBlobsSegmentedAsync(prefix,
                            true, BlobListingDetails.Metadata, maxResults, continuationToken, null, null);

                        blobList = resultSegment.Results.OfType<CloudBlob>()
                            .Where(b => b.Properties.Created >= DateTime.Today.AddDays(startDate) &&
                            b.Properties.Created <= DateTime.Today.AddDays(endDate))
                            .Select(b => b);

                        foreach (var blobItem in blobList)
                        {
                            blob = (CloudBlob)blobItem;
                            await blob.FetchAttributesAsync();
                            WriteLine($"Blob name: {blob.Name} - last modified on {blob.Properties.LastModified}");
                        }
                        continuationToken = resultSegment.ContinuationToken;

                    } while (continuationToken != null);

                    if (blobList.Count() > 0)
                    {
                        WriteLine("Would you like to remove/reprocess a blob? Y/N ");
                        var delete = ReadLine();
                        while (delete == "Y")
                        {
                            //should repopulate blobList and check there are blobs to delete
                            WriteLine("Enter the path and blob name you would like to remove/reprocess: ");
                            WriteLineWithGreenColor(true);
                            WriteLine($"Ex: {((CloudBlob)blobList.First()).Name}");
                            WriteLineWithGreenColor(false);
                            var path = ReadLine();

                            CloudBlockBlob blockBlob = container.GetBlockBlobReference(path);
                            await blockBlob.DeleteIfExistsAsync();
                            WriteLine($"Deleted {path} from {container.Name}");

                            WriteLine("Would you like to remove/reprocess another blob? Y/N ");
                            delete = ReadLine();
                        }
                    }
                }
                catch (StorageException e)
                {
                    WriteLine(e.Message);
                    ReadLine();
                }

                if (container.Name.ToLower() != "azure-webjobs-hosts")
                {
                    WriteLineWithYellowColor(true);
                    WriteLine($"NOTE: you searched '{container.Name} -> {prefix}'.  You need to search in the " +
                        "azure-webjobs-hosts container if you want to reprocess a blob.");
                    WriteLineWithYellowColor(false);
                }                
            }
            else
            {
                WriteLine($"Blob container '{container.Name}' doesn't exist.  Please start over.");
            }
        }
        public static void WriteLineWithGreenColor(bool enable)
        {
            if (enable)
            {
                Console.BackgroundColor = ConsoleColor.DarkGreen;
                Console.ForegroundColor = ConsoleColor.Black;
            }
            else
            {
                Console.ResetColor();
            }
        }
        public static void WriteLineWithYellowColor(bool enable)
        {
            if (enable)
            {
                Console.BackgroundColor = ConsoleColor.DarkYellow;
                Console.ForegroundColor = ConsoleColor.Black;
            }
            else
            {
                Console.ResetColor();
            }
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
            WriteLine("Enter HTTP Trigger function key or enter: anonymous");
            var HTTPTriggerFunctionKey = ReadLine();
            while (HTTPTriggerFunctionKey.Length == 0)
            {
                WriteLine("Try again, this value must have a length > 0");
                WriteLine("Enter HTTP Trigger function key:");
                HTTPTriggerFunctionKey = ReadLine();
            }
            WriteLine("Enter POST or GET:");
            var Method = ReadLine();
            while (Method.Length == 0)
            {
                WriteLine("Try again, this value must have a length > 0");
                WriteLine("Enter POST or GET:");
                HTTPTriggerFunctionKey = ReadLine();
            }
            string Name = String.Empty;
            if (Method == "POST")
            {
                WriteLine("Enter your name or any name:");
                Name = ReadLine();
                while (Name.Length == 0)
                {
                    WriteLine("Try again, this value must have a length > 0");
                    WriteLine("Enter any value for Query String usage:");
                    Name = ReadLine();
                }
            }
            WriteLine("Enter number of requests to send: ");
            int HTTPRequestsToSend = 0;
            while (!int.TryParse(ReadLine(), out HTTPRequestsToSend))
            {
                WriteLine("Try again, this value must be numeric.");
            }
            await SendHTTPRequests(HTTPRequestsToSend, HTTPTriggerUrl, HTTPTriggerFunctionKey, Method, Name);
        }
        private static async Task SendHTTPRequests(int numHTTPRequestsToSend, string Url, string FunctionKey, string Method, string Name)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            for (var i = 0; i < numHTTPRequestsToSend; i++)
            {
                try
                {
                    var httpRequest = $"HTTP request {i}";
                    WriteLine($"Sending request: {httpRequest}");

                    if (Method == "POST")
                    {
                        var json = "{\"name\":\"" + Name + "\"}";
                        var encodedContent = new StringContent(json, Encoding.UTF8, "application/json");
                        if (FunctionKey == "anonymous")
                        {
                            response = await httpClient.PostAsync(Url, encodedContent).ConfigureAwait(false);
                        }
                        else
                        {
                            response = await httpClient.PostAsync(Url + "?code=" + FunctionKey, encodedContent).ConfigureAwait(false);
                        }
                    }
                    else if (Method == "GET")
                    {
                        WriteLine("If you execute an anonymous GET, then the number of sent requests will burst 10 concurrent requests that many times.");
                        if (FunctionKey == "anonymous")
                        {
                            response = await httpClient.GetAsync(Url);
                            for (int t = 0; t < 10; t++)
                            {
                                Thread rThread = new Thread(() => httpClient.GetAsync(Url));
                                rThread.Start();
                                //This is a burst mode code segment, be careful if you do this, costs, ddos and latency
                                //if (t % 10 == 0)
                                //{
                                //    System.Threading.Thread.Sleep(2000);
                                //    WriteLine($"Sleeping...");
                                //}
                                WriteLine($"Thread {t} is sending an anonymous GET to {Url} now: {DateTime.Now}");
                            }
                        }
                        else
                        {
                            response = await httpClient.GetAsync(Url + "?code=" + FunctionKey);
                        }
                    }
                    else
                    {
                        throw new Exception("Method must be POST or GET");
                    }

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
                    WriteLine($"Press Y to delete **ALL** documents in the '{CosmosCollectionName}' container.");
                    if (ReadLine() == "Y")
                    {
                        await DeleteCosmosDocuments(CosmosDatabaseName, CosmosCollectionName, collectionUri);
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
            Random r = new Random();

            for (var i = 0; i < numDocumentsToSend; i++)
            {
                try
                {
                    var Id = r.Next(1, 2147483647).ToString();
                    CosmosDocument cosmosDocument = CreateCosmosDocument(Id);
                    WriteLine($"Sending document with Id = : {Id}");
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
        private static async Task DeleteCosmosDocuments(string cosmosDatabaseName, string cosmosCollectionName, Uri collectionUri)
        {
            FeedOptions fe = new FeedOptions();
            var documents =
                from d in documentClient.CreateDocumentQuery(collectionUri, fe).ToList()
                select d;

            int i = 0;
            foreach (var item in documents)
            {
                try
                {
                    ResourceResponse<Document> response = await documentClient.DeleteDocumentAsync(
                            UriFactory.CreateDocumentUri(cosmosDatabaseName, cosmosCollectionName, item.Id),
                            new RequestOptions() { PartitionKey = new PartitionKey(item.Id) }); //Undefined.Value

                    WriteLine($"Deleted document with Id: {item.Id}");
                }
                catch (DocumentClientException dce)
                {
                    if (dce.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        WriteLine($"The Partition Key for the container must be '/id' for the delete to work.");
                    }
                    else
                    {
                        WriteLine($"{dce.StatusCode} error occurred: {dce.Message}");
                    }
                }
                catch (Exception ex)
                {
                    WriteLine($"Error occurred: {ex.Message}");
                }
                await Task.Delay(10);
                i++;
            }
            WriteLine($"{i.ToString()} documents deleted.");
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
                    { message = row, dateTime = DateTime.Now.ToString() };

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
