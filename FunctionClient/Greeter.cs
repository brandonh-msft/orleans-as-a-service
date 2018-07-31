using System;
using System.Threading.Tasks;
using AzureWorker.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Orleans;
using OrleansClientExtension;

namespace FunctionClient
{
    public static class Greeter
    {
        [FunctionName("Greet")]
        public static async Task<IActionResult> GreetAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post")]HttpRequest req,
            [OrleansClient(@"ClusterStorageConnectionString", @"ClusterId", OrleansGrainInterfaceTypes = new Type[] { typeof(IGreetGrain) })]IGrainFactory orleansClient,
            TraceWriter log)
        {
            string to = null, from = null;
            var queryParams = req.GetQueryParameterDictionary();
            if (queryParams?.TryGetValue(nameof(from), out from) == false
                || string.IsNullOrWhiteSpace(from))
            {
                return new BadRequestObjectResult($@"Must provide '{nameof(from)}' as a querystring parameter");
            }

            if (queryParams?.TryGetValue(nameof(to), out to) == false
                || string.IsNullOrWhiteSpace(to))
            {
                return new BadRequestObjectResult($@"Must provide '{nameof(to)}' as a querystring parameter");
            }

            var greeterGrain = orleansClient.GetGrain<IGreetGrain>(from);
            string greeting = await greeterGrain.Greet(to);

            log.Info(greeting);

            return new OkObjectResult(greeting);
        }
    }
}
