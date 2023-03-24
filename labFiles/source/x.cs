using System;
using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Collections.Generic;

using Azure.Storage.Blobs;

namespace dev_assessment
{
    // The HTTP trigger must return successfully by default when package csharpguitar-elx.zip is created
    // The 'not default' (as coded now) code in the HTTP trigger (Function 'z') is used to create csharpguitar-elx-http.zip
    // What this means is that if you need to create a new csharpguitar-elx.zip, modify Function 'z' so it always returns a 200 
    public static class x
    {
        ("BLOB_FUNCTION_GO")
        [FunctionName("x")]
        public static void Run([BlobTrigger("elx/{name}", Connection = "BLOB_CONNECTION")]Stream myBlob, string name, ILogger log, Uri uri, IDictionary<string, string> metadata)
        {
            log.LogInformation($"C# Blob trigger function processed blob named: {name} with a size of: {myBlob.Length} bytes");

            var connectionString = Environment.GetEnvironmentVariable("BLOB_CONNECTION");
            var container = new BlobContainerClient(connectionString, "elx");
            var blob = container.GetBlobClient(name);

            log.LogInformation($"************************* blob properties for: {name} *************************");
            log.LogInformation($"Blob: {name} has an ETAG of {blob.GetProperties().Value.ETag}");
            log.LogInformation($"Blob: {name} has a creation time of {blob.GetProperties().Value.CreatedOn}");
            log.LogInformation($"Blob: {name} has a last modified value of {blob.GetProperties().Value.LastModified}");
            log.LogInformation($"*******************************************************************************");

            Type uriType = typeof(Uri);
            PropertyInfo[] properties = uriType.GetProperties();
            foreach (PropertyInfo uriProp in properties)
            {
                log.LogInformation($"File Property Name: {uriProp.Name} Value: {uriProp.GetValue(uri, null)}");
            }

            foreach (KeyValuePair<string, string> data in metadata)
            {
                log.LogInformation($"User-Defined Metadata Key  = { data.Key  }");
                log.LogInformation($"User-Defined Metadata Value  = { data.Value  }");
            }
            if (metadata.Count == 0)
            {
                log.LogInformation("No user-defined metadata was found.");
            }
        }
    }
}
