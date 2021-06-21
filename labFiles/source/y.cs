using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace dev_assessment
{
    public static class y
    {
        // This code is the same for (lab 4 and lab 5), no dependencies.
        // This code does not exist in csharpguitar-elx.zip (lab 1), no problem to add it, but not necessary
        //      if added it would require retesting the labs
        // This code is included in csharpguitar-elx-http.zip, as per lab instructions
        public static string _globalString = "*** Begin process ***";
        public static object thisLock = new object();

        [Disable("TIMER_FUNCTION_GO")]
        [FunctionName("y")]
        public static async void Run([TimerTrigger("%TIMER_FUNCTION_SCHEDULE%")]TimerInfo myTimer, ILogger log, CancellationToken cancellationToken)
        {            
            Random r = new Random();
            var number = r.Next(1, 100);
            if (number % 4 == 0)
            {
                log.LogInformation($"C# Timer trigger function started execution at: {DateTime.Now} for scenario 'alpha'");
                //runs long (72 seconds), helpful when you want to trigger a cancellation token
                int length = 24;
                for (int i = 0; i < length; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        if (cancellationToken.CanBeCanceled)
                        {
                            log.LogInformation($"Function invocation was successfully cancelled at: {DateTime.Now}.");
                            log.LogInformation($"cancellationToken.CanBeCanceled had a value of: {cancellationToken.CanBeCanceled}");
                            log.LogInformation($"The unique identifier: {Guid.NewGuid()}");
                            log.LogInformation($"***NOTE*** this is where you can do code clean up, just before being shutdown!");
                            log.LogInformation($"***NOTE*** the invocation did not complete, it was cancelled while executing, the code was on iteration {i} of {length}.");
                            break;
                        }
                        else
                        {
                            log.LogInformation($"Function invocation cancellation was requested at: {DateTime.Now}.");
                            log.LogInformation($"cancellationToken.CanBeCanceled had a value of: {cancellationToken.CanBeCanceled} and therefore could not be cancelled.");
                            log.LogInformation($"The unique identifier: {Guid.NewGuid()}");
                            log.LogInformation($"***NOTE*** although the code received a cancellation request, the invocation could not be cancelled.");
                        }
                    }
                    log.LogInformation($"This Function Invocation will loop {length} times.  Current iteration is: {i}");
                    Thread.Sleep(5000);
                }
                log.LogInformation($"C# Timer trigger function completed execution at: {DateTime.Now} for scenario 'alpha'");
            }
            else
            {
                number = r.Next(1, 100);
                if (number % 4 == 0)
                {
                    log.LogInformation($"C# Timer trigger function started execution at: {DateTime.Now} for scenario 'beta'");
                    //tip: the implementation of the async/await pattern is wrong and causes big problems
                    Stopwatch timer = new Stopwatch();
                    timer.Start();
                    /* Incorrect */
                    log.LogInformation($"Calling InsertAsync()");
                    await InsertAsync();
                    log.LogInformation($"Calling UpdateAsync()");
                    await UpdateAsync();
                    /* --------- */
                    /* Correct */
                    //Task<string> globalInsertStatus = InsertAsync();
                    //var globalUpdateStatus = UpdateAsync();
                    /* ------- */
                    timer.Stop();
                    log.LogInformation($"Calling InsertAsync() and UpdateAsync() methods took {timer.Elapsed}");
                    log.LogInformation("****************************************************************************");
                    log.LogInformation($"The time interval for this timer function is: {Environment.GetEnvironmentVariable("TIMER_FUNCTION_SCHEDULE")}");
                    log.LogInformation($"TimerInfo PastDue: {myTimer.IsPastDue}.");
                    log.LogInformation($"ScheduleStatus Last: {myTimer.ScheduleStatus.Last}");
                    log.LogInformation($"ScheduleStatus LastUpdated: {myTimer.ScheduleStatus.LastUpdated}");
                    log.LogInformation($"ScheduleStatus Next: {myTimer.ScheduleStatus.Next}");
                    log.LogInformation("****************************************************************************");
                    log.LogInformation($"Timer execution interval is: {Environment.GetEnvironmentVariable("TIMER_FUNCTION_SCHEDULE")} but took {timer.Elapsed} to complete.");
                    log.LogInformation("****************************************************************************");
                    log.LogInformation($"C# Timer trigger function completed execution at: {DateTime.Now} for scenario 'beta'");
                    /* Correct */
                    //log.LogInformation($"The InsertAsync() value of _globalString is {await globalInsertStatus} for scenario 'beta'");
                    //log.LogInformation($"The UpdateAsync() value of _globalString was {await globalUpdateStatus} for scenario 'beta'");
                    /* ------- */
                }
                number = r.Next(1, 100);
                if (number % 2 == 0)
                {
                    log.LogInformation($"C# Timer trigger function started execution at: {DateTime.Now} for scenario 'gamma'");
                    Thread.Sleep(2000);
                    log.LogInformation($"C# Timer trigger function completed execution at: {DateTime.Now} for scenario 'gamma'");
                }
                else
                {
                    number = r.Next(1, 100);
                    if (number % 2 == 0)
                    {
                        log.LogInformation($"C# Timer trigger function started execution at: {DateTime.Now} for scenario 'delta'");
                        try
                        {
                            Thread.Sleep(5000);
                            throw new FunctionInvocationException("Explain what can cause a stack overflow exception");
                        }
                        catch (FunctionInvocationException fie)
                        {
                            log.LogInformation($"A {fie.GetType()} was thrown.  Was this a hanlded or unhandled exception?");
                            log.LogInformation($"C# Timer trigger function completed execution at: {DateTime.Now} for scenario 'delta'");
                        }
                    }
                    else
                    {
                        log.LogInformation($"C# Timer trigger function started execution at: {DateTime.Now} for scenario 'epsilon'");
                        Thread.Sleep(10000);
                        throw new FunctionInvocationException("Ouch!  Was this a handled or unhandled exception?");
                        //code execution will cease before logging this, why?
                        log.LogInformation($"C# Timer trigger function completed execution at: {DateTime.Now} for scenario 'epsilon'");
                    }
                }
            }
        }
        public static async Task<string> InsertAsync()
        {
            lock (_globalString)
            {
                _globalString = $"'InsertAsync() begin {DateTime.Now}'";
                //The Sleep() is a simulated Insert into a very busy data repository
                System.Threading.Thread.Sleep(41000);
                _globalString = $"'InsertAsync() end {DateTime.Now}'";
            }

            await Task.Delay(1000);
            return _globalString;
        }
        public static async Task<string> UpdateAsync()
        {
            lock (_globalString)
            {
                _globalString = $"'UpdateAsync() begin {DateTime.Now}'";
                //The Sleep() is a simulated Update on a very busy data repository
                System.Threading.Thread.Sleep(49000);
                _globalString = $"'UpdateAsync() end {DateTime.Now}'";
            }

            await Task.Delay(1000);
            return _globalString;
        }
    }
}
