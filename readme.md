# Orleans as a Service
Following discussions with more than one customer interested in bringing their Virtual Actor models from Service Fabric Reliable Actors in to Azure Functions, this is my attempt at taking [Orleans](http://dotnet.github.io/orleans) and making it run on Azure's serverless platform offerings.

For reference the discussions which spurred this experiment can be found in [Durable Functions](https://github.com/Azure/azure-functions-durable-extension/issues/22) and [Orleans](https://github.com/dotnet/orleans/issues/4131) issue areas.

This sample was born out of [AzureWorkerRoleSample](https://github.com/dotnet/orleans/tree/master/Samples/2.0/AzureWorkerRoleSample).

## Setup
1. Setup a Cloud Service instance in Azure
2. Get the connection string for the Azure Storage used by the Cloud Service, add this to the `local.settings.json` of the Azure Functions project with key `ClusterStorageConnectionString` like so:
```js
{
  "IsEncrypted": false,
  "Values": {
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
    "APPINSIGHTS_INSTRUMENTATIONKEY": "xxx",
    "ClusterStorageConnectionString": "DefaultEndpointsProtocol=https;AccountName=...;EndpointSuffix=core.windows.net"  // <-here
  }
}
```
3. Add this same key/value pair to [ServiceConfiguration.Cloud.cscfg](SiloDeploy/ServiceConfiguration.Cloud.cscfg)
```xml
    <ConfigurationSettings>
      <Setting name="Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString" value="<storage conn string>" />
      <Setting name="APPINSIGHTS_INSTRUMENTATIONKEY" value="xxx" />
      <Setting name="ClusterStorageConnectionString" value="<storage conn string>" />
```
4. Also add the DNS name of your cloud service to this config file, using key `CloudServiceDnsName` like so:
```xml
      <Setting name="ClusterStorageConnectionString" value="<storage conn string>" />
      <Setting name="CloudServiceDnsName" value="yourservice.cloudapp.net"/>
```

## Deployment
1. Deploy the Orleans Silo by right-clicking the `SiloDeploy` project and choosing `Publish`, pointing it at the Cloud Service instance you created in step 1 of Setup
1. Deploy the Azure Function by right-clicking the `FunctionClient` project and choosing `Publish`.
1. Add `ClusterStorageConnectionString` setting to the Application Settings of the Function in Azure.
1. HTTP POST to your Azure Function with querystring parameters 'from' and 'to' and you should receive an HTTP OK response.

## Interesting bits
If you POST more than once to the endpoint, you'll notice the response comes back with "I last greeted ___." on the end of it. This is to illustrate the state-maintenace you're getting by using Orleans Virtual Actors. Each `from` request is spun up as its own Grain, therefore each "greeter" (`from`) keeps track of those they've "greeted" (`to`) by simply using a private member variable in [its implementation](Grains/GreetGrain.cs).

In theory, augmenting the scaling parameters of our Cloud Service, coupled with the auto-scaling provided by Functions, *should* enable us to field a very large number of these requests concurrently.