using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host.Config;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;

namespace OrleansClientExtension
{
    public class OrleansClientConfiguration : IExtensionConfigProvider
    {
        public void Initialize(ExtensionConfigContext context)
        {
            context.AddBindingRule<OrleansClientAttribute>()
                .BindToInput(new ClusterClientAsyncBindingConverter(context));
        }

        class ClusterClientAsyncBindingConverter : IAsyncConverter<OrleansClientAttribute, IClusterClient>
        {
            private readonly ExtensionConfigContext _context;

            public ClusterClientAsyncBindingConverter(ExtensionConfigContext context)
            {
                _context = context;
            }

            public async Task<IClusterClient> ConvertAsync(OrleansClientAttribute attrib, CancellationToken cancellationToken)
            {
                Trace.WriteLine(@"OrleansClient binding - Creating connection to Orleans silo...");

                var builder = new ClientBuilder()
                    .Configure<ClusterOptions>(options =>
                    {
                        options.ClusterId = @"ForFunctions";
                        options.ServiceId = @"AzureFunctionsSample";
                    })
                    .UseAzureStorageClustering(opt => opt.ConnectionString = Environment.GetEnvironmentVariable(attrib.ClusterStorageConnectionStringSetting));

                var instance = builder.Build();
                Trace.WriteLine(@"OrleansClient binding - Client successfully built with Azure Storage clustering...");

                cancellationToken.ThrowIfCancellationRequested();

                const int maxRetries = 5;
                int retryCount = 0;
                await instance.Connect(async ex =>
                {
                    Trace.TraceError($@"Error connecting to Orleans silo: {ex}");

                    if (++retryCount < maxRetries)
                    {
                        if (cancellationToken.IsCancellationRequested) return false;
                        await Task.Delay(TimeSpan.FromSeconds(3));
                        if (cancellationToken.IsCancellationRequested) return false;

                        return true;
                    }
                    else
                    {
                        return false;
                    }
                });

                Trace.WriteLine(@"OrleansClient binding - Connected.");

                return instance;
            }
        }
    }
}
