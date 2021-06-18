using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Threading;

namespace dev_assessment
{
    public static class z
    {
        // The code in this function must return successfully by default when package csharpguitar-elx.zip is created
        // This code is to be included in the csharpguitar-elx-http.zip (lab 4) but not in csharpguitar-elx.zip (lab 1)
        // This code is the same for (lab 4 and lab 5), no dependencies.  
        [FunctionName("z")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "csharpguitar")] HttpRequest req,
            ILogger log, CancellationToken cancellationToken)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            int length = 40;            
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            string cancel = data?["cancel"];

            if (cancel == "yes")
            {
                for (int i = 0; i < length; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        if (cancellationToken.CanBeCanceled)
                        {
                            log.LogInformation($"Function invocation was successfully cancelled at: {DateTime.Now} using the '{req.Method}' method.");
                            log.LogInformation($"cancellationToken.CanBeCanceled had a value of: {cancellationToken.CanBeCanceled}");
                            log.LogInformation($"The unique identifier: {Guid.NewGuid()}");
                            break;
                        }
                        else
                        {
                            log.LogInformation($"Function invocation cancellation was requested at: {DateTime.Now} using the '{req.Method}' method.");
                            log.LogInformation($"cancellationToken.CanBeCanceled had a value of: {cancellationToken.CanBeCanceled} and therefore could not be cancelled.");
                            log.LogInformation($"The unique identifier: {Guid.NewGuid()}");
                        }
                    }                    
                    log.LogInformation($"This Function Invocation will loop {length} times.  Current iteration is: {i}");
                    Thread.Sleep(5000);
                }
            }

            string responseMessage = string.IsNullOrEmpty(cancel)
                ? $"HTTP triggered function executed successfully using the '{req.Method}' method. {Guid.NewGuid()}"
                : $"A value of {cancel} was recevied by this HTTP triggered function using the '{req.Method}' method.";

            return new OkObjectResult(responseMessage);
        }
    }
}
