using System;
using Microsoft.Azure.WebJobs.Description;

namespace OrleansClientExtension
{
    [Binding, AttributeUsage(AttributeTargets.Parameter, AllowMultiple = true)]
    public class OrleansClientAttribute : Attribute
    {
        public OrleansClientAttribute(string clusterStorageConnectionStringSetting)
        {
            this.ClusterStorageConnectionStringSetting = clusterStorageConnectionStringSetting;
        }

        public string ClusterStorageConnectionStringSetting { get; }
    }
}
