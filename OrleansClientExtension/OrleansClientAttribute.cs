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

        [AppSetting(Default = @"ClusterStorageConnectionString")]
        public string ClusterStorageConnectionStringSetting { get; private set; }

        /// <summary>
        /// Gets or sets the orleans grain interface types.
        /// eg: new [] { typeof(MyGrain1), typeof(MyGrain2) }
        /// </summary>
        public Type[] OrleansGrainInterfaceTypes { get; set; } = new Type[0];
    }
}
