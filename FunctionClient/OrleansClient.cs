using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;

namespace FunctionClient
{
    static class OrleansClient
    {
        static IClusterClient _instance;
        public static IClusterClient GetInstance(TraceWriter log = null)
        {
            if (_instance == null)
            {
                log?.Info(@"Creating connection to Orleans silo...");
                Trace.WriteLine(@"t: Creating connection to Orleans silo...");
                //// Parse the IP Address out of app settings
                //var siloIpAddressString = Environment.GetEnvironmentVariable(@"SiloEndpointIP");
                //var siloIpAddressBytes = siloIpAddressString.Split('.').Select(i => byte.Parse(i)).ToArray();

                //// parse the port to uint to ensure the value is >=0
                //var siloPort = uint.Parse(Environment.GetEnvironmentVariable(@"SiloEndpointPort"));
                //log?.Info($@" @ {siloIpAddressString}:{siloPort}");
                //Trace.WriteLine($@"t:  @ {siloIpAddressString}:{siloPort}");

                var builder = new ClientBuilder()
                    .Configure<ClusterOptions>(options =>
                    {
                        options.ClusterId = @"ForFunctions";
                        options.ServiceId = @"AzureFunctionsSample";
                    })
                    .UseAzureStorageClustering(opt => opt.ConnectionString = Environment.GetEnvironmentVariable(@"ClusterStorageConnectionString"));

                _instance = builder.Build();
                log?.Info(@"Client successfully built with Azure Storage clustering...");
                Trace.WriteLine(@"t: Client successfully built with Azure Storage clustering...");

                const int maxRetries = 5;
                int retryCount = 0;
                _instance.Connect(async ex =>
                {
                    log.Info(ex.Message);

                    if (++retryCount < maxRetries)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(3));
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }).Wait();

                log?.Info(@"Connected.");
                Trace.WriteLine(@"t: Connected.");
            }

            return _instance;
        }
    }
}
