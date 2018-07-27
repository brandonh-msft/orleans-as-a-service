using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AzureWorker.Grains;
using Microsoft.WindowsAzure.ServiceRuntime;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;

namespace Silo
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);
        private ISiloHost siloHost;

        public override void Run()
        {
            Trace.TraceInformation("Silo is running");

            try
            {
                this.RunAsync(this.cancellationTokenSource.Token).Wait();

                runCompleteEvent.WaitOne();
            }
            finally
            {
                this.runCompleteEvent.Set();
            }
        }

        public override bool OnStart()
        {
            bool result = base.OnStart();

            Trace.TraceInformation("Silo has been started");

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("Silo is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("Silo has stopped");
        }

        private Task RunAsync(CancellationToken cancellationToken)
        {
            var siloEndpoint = RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["OrleansSiloEndpoint"].IPEndpoint;

            var gatewayPort = RoleEnvironment.CurrentRoleInstance.InstanceEndpoints[@"OrleansGatewayEndpoint"].IPEndpoint.Port;

            Trace.TraceInformation($@"Endpoint: {siloEndpoint.Address.ToString()}:{gatewayPort}");

            var builder = new SiloHostBuilder()
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = @"ForFunctions";
                    options.ServiceId = @"AzureFunctionsSample";
                })
                .Configure<EndpointOptions>(opt =>
                {
                    // Our advertised IP is the one that our friendly DNS name resolves to
                    var cloudServiceHostname = Dns.GetHostEntry(RoleEnvironment.GetConfigurationSettingValue(@"CloudServiceDnsName"));
                    opt.AdvertisedIPAddress = cloudServiceHostname.AddressList.First(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);

                    opt.GatewayListeningEndpoint = new IPEndPoint(IPAddress.Any, gatewayPort);
                    opt.GatewayPort = gatewayPort;
                    opt.SiloListeningEndpoint = new IPEndPoint(IPAddress.Any, siloEndpoint.Port);
                    opt.SiloPort = siloEndpoint.Port;
                })
                .UseAzureStorageClustering(opt => opt.ConnectionString = RoleEnvironment.GetConfigurationSettingValue(@"ClusterStorageConnectionString"))
                .ConfigureApplicationParts(mgr =>
                {
                    mgr.AddApplicationPart(typeof(GreetGrain).Assembly).WithReferences();
                });

            siloHost = builder.Build();

            return siloHost.StartAsync(cancellationToken);
        }
    }
}
