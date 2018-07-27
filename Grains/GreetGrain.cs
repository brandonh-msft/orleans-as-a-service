using System.Diagnostics;
using System.Threading.Tasks;
using AzureWorker.Interfaces;
using Orleans;

namespace AzureWorker.Grains
{
    public class GreetGrain : Grain, IGreetGrain
    {
        private string _lastGreeted;
        public Task<string> Greet(string name)
        {
            Trace.WriteLine($@"Greet grain '{this.GetPrimaryKeyString()}' called: [{nameof(name)}] {name}");

            var retVal = $"Hello from {this.GetPrimaryKeyString()} to {name}";
            if (!string.IsNullOrWhiteSpace(_lastGreeted))
            {
                retVal += $@". I last greeted {_lastGreeted}.";
            }

            _lastGreeted = name;

            return Task.FromResult(retVal);
        }
    }
}
