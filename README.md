# Azure Function Consumer
The Azure Function Consumer is a .NET Core console app to consume Azure Functions. This program let's you send blobs, messages or documents to Azure resource which are bound to an Azure Function.  

There are Labs available [here][LINK1] that will walk you through the **creation** of  Azure Functions bound to numerous Azure products.  When you send data to an Azure product (i.e. Azure Blob Storage, Event Hub, Service Bus, etc.) that is bound to an Azure Function, the Azure Function is triggered and the code within your Azure Function is executed.

Sometimes the code doesn't work as expected, here are some labs which can help you learn how to troubleshoot and resolve badly behaving Azure Functions.

The Azure Function consumer requires the dotnet runtime, which can be downoaded from [here][LINK2] and configured following the provided instructions.

To see which version of the .NET Core runtime you have installed, enter **dotnet --version**

![checking .net code version](images/AFC001.PNG)

Start the program by runnung:  **dotnet AzureFunctionConsumer.dll**

![starting the Azure Function Consumer](images/AFC002.PNG)

The Azure Function Consumer currently supports sending data transmissions to:
+ Event Hub
+ Storage Queue
+ Blob Storage
+ Service Bus Queue
+ Cosmos DB
+ HTTP Trigger

## Download the Azure Function Consumer program ##
Download the AzureFunctionConsumer.dll and its dependencies from [here][LINK3].  Take the most current version.

## Examples ##
The following examples illustrate how to trigger an HTTP, an Event Hub, Storage Queue and an Azure Cosmos DB bound Azure Function

### HTTP Triggered Azure Function ###
![Calling an HTTP triggered Azure Function](images/AFC003.PNG)

### Event Hub Triggered Function ###
![Invoking an Event Hub triggered Azure Function](images/AFC004.PNG)

### Storge Queue Triggered Function ###
![Invoking a Storage Queue triggered Azure Function](images/AFC005.PNG)

### Azure Cosmos DB Triggered Function ###
![Invoking an Azure Cosmos DB triggered Azure Function](images/AFC006.PNG)

## Report an issue or have an improvment idea ##
If you experience an unexpected exceptiion or have an awkward experience, log it [here][LINK4].  If you would like to recommend a new feature log it [here][LINK4] as well.

[LINK1]: https://www.thebestcsharpprogrammerintheworld.com/2020/07/06/azure-functions-labs-information-and-setup-instructions/
[LINK2]: https://github.com/dotnet/core
[LINK3]: https://github.com/benperk/AzureFunctionConsumer/releases
[LINK4]: https://github.com/benperk/AzureFunctionConsumer/issues
