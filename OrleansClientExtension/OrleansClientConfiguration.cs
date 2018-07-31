using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Extensions.Logging;
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
                .BindToInput(new GrainFactoryAsyncBindingConverter(context))
                .AddValidator((attrib, t) =>
                {
                    if (string.IsNullOrWhiteSpace(attrib.ClusterStorageConnectionStringSetting))
                    {
                        throw new ArgumentNullException($@"Must specify the application setting '{nameof(attrib.ClusterStorageConnectionStringSetting)}' to use the Orleans Client input binding");
                    }

                    if (!attrib.OrleansGrainInterfaceTypes.Any())
                    {
                        throw new ArgumentException(@"At least one grain interface type must be specified so GetGrain<TGrainInterfaceType> will succeed when called.");
                    }

                    if (attrib.OrleansGrainInterfaceTypes.Any(t2 => t2 == null || Type.Missing.Equals(t2)))
                    {
                        throw new ArgumentException($@"Cannot have any null or 'Type.Missing' types in the {nameof(attrib.OrleansGrainInterfaceTypes)} collection");
                    }
                });
        }

        class GrainFactoryAsyncBindingConverter : IAsyncConverter<OrleansClientAttribute, IGrainFactory>
        {
            private static IClusterClient _instance;

            private readonly ILogger _logger;

            public GrainFactoryAsyncBindingConverter(ExtensionConfigContext context)
            {
                _logger = context.Config.LoggerFactory.CreateLogger(this.GetType().Name);
            }

            public async Task<IGrainFactory> ConvertAsync(OrleansClientAttribute attrib, CancellationToken cancellationToken)
            {
                if (_instance == null)
                {
                    _logger.LogTrace(@"OrleansClient binding - Creating connection to Orleans silo...");

                    var builder = new ClientBuilder()
                        .Configure<ClusterOptions>(options =>
                        {
                            options.ClusterId = @"ForFunctions";
                            options.ServiceId = @"AzureFunctionsSample";
                        })
                        .UseAzureStorageClustering(opt => opt.ConnectionString = attrib.ClusterStorageConnectionStringSetting)
                        .ConfigureApplicationParts(parts =>
                        {
                            foreach (var t in attrib.OrleansGrainInterfaceTypes)
                            {
                                parts.AddApplicationPart(t.Assembly);
                            }
                        });

                    _instance = builder.Build();
                    _logger.LogTrace(@"OrleansClient binding - Client successfully built with Azure Storage clustering...");

                    const int maxRetries = 5;
                    int retryCount = 0;
                    await _instance.Connect(async ex =>
                    {
                        _logger.LogError(ex, $@"Error connecting to Orleans silo");

                        if (++retryCount < maxRetries)
                        {
                            await Task.Delay(TimeSpan.FromSeconds(3));

                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    });

                    _logger.LogTrace(@"OrleansClient binding - Connected.");
                }

                return _instance;
            }
        }
    }
}
