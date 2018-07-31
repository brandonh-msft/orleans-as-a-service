using System;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AzureWorker.Grains;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;

namespace OrleansSiloHost
{
    /// <summary>
    /// Orleans test silo host
    /// </summary>
    public class Program
    {

        public static int Main(string[] args)
        {
            return RunMainAsync().Result;
        }

        private static async Task<int> RunMainAsync()
        {
            try
            {
                var host = await StartSiloAsync();
                Console.WriteLine("Press Enter to terminate...");
                Console.ReadLine();

                await host.StopAsync();

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return 1;
            }
        }

        private static async Task<ISiloHost> StartSiloAsync()
        {
            var siloPort = int.Parse(ConfigurationManager.AppSettings[@"OrleansSiloPort"]);
            Console.WriteLine($@"Silo Port: {siloPort}");

            var gatewayPort = int.Parse(ConfigurationManager.AppSettings[@"OrleansGatewayPort"]);
            Console.WriteLine($@"Gateway Port: {gatewayPort}");

            // First, configure and start a local silo
            var builder = new SiloHostBuilder()
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = ConfigurationManager.AppSettings[@"ClusterId"];
                    options.ServiceId = System.Diagnostics.Process.GetCurrentProcess().Id.ToString();
                })
                .Configure<EndpointOptions>(opt =>
                {
                    // Our advertised IP is the one that our friendly DNS name resolves to
                    var cloudServiceHostname = Dns.GetHostEntry(ConfigurationManager.AppSettings[@"CloudServiceDnsName"]);
                    opt.AdvertisedIPAddress = cloudServiceHostname.AddressList.First(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);

                    Console.WriteLine($@"Advertised IP: {opt.AdvertisedIPAddress}");

                    opt.GatewayListeningEndpoint = new IPEndPoint(IPAddress.Any, gatewayPort);
                    opt.GatewayPort = gatewayPort;
                    opt.SiloListeningEndpoint = new IPEndPoint(IPAddress.Any, siloPort);
                    opt.SiloPort = siloPort;
                })
                .UseAzureStorageClustering(opt => opt.ConnectionString = ConfigurationManager.ConnectionStrings[@"ClusterStorageConnectionString"].ConnectionString)
                .ConfigureApplicationParts(mgr => mgr.AddApplicationPart(typeof(GreetGrain).Assembly).WithReferences())
                .ConfigureLogging(opt => opt.AddConsole());

            var silo = builder.Build();
            await silo.StartAsync();
            return silo;
        }
    }
}
