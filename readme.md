# Orleans as a Service
Following discussions with more than one customer interested in taking their Service Fabric Reliable Actors server**less** via Azure Functions, this is my attempt at taking [Orleans](http://dotnet.github.io/orleans) and making it run on Azure's serverless platform offerings.

For reference the discussions which spurred this experiment can be found in [Durable Functions](https://github.com/Azure/azure-functions-durable-extension/issues/22) and [Orleans](https://github.com/dotnet/orleans/issues/4131) issue areas.

This sample was born out of [Orleans' AzureWorkerRoleSample](https://github.com/dotnet/orleans/tree/master/Samples/2.0/AzureWorkerRoleSample).

In the interest of "modernization" you can find this same functionality implemented as an [Azure Webjob](tree/webjob) and [Service Fabric Guest Executable](tree/sf-guest-exec) in this repo as well.

## Instructions
1. Create an Azure Storage **Classic** Storage account in your Azure subscription:

![](https://brandonhmsdnblog.blob.core.windows.net/images/2018/07/28/fa707dd9.png)

Grab the primary connection string for it; you'll need this in a minute

![](https://brandonhmsdnblog.blob.core.windows.net/images/2018/07/28/2018-07-27_22-21-47.png)

Also grab the DNS hostname of this instance:

![](https://brandonhmsdnblog.blob.core.windows.net/images/2018/07/28/2018-07-27_22-44-40.png)

2. Create an Application Insights instance in your Azure Subscription:

![](https://brandonhmsdnblog.blob.core.windows.net/images/2018/07/28/ac7964ab.png)

Grab the Instrumentation Key for it; you'll also need this in a minute

![](https://brandonhmsdnblog.blob.core.windows.net/images/2018/07/28/2018-07-27_22-23-12.png)

3. Open [OrleansAsAService.sln](OrleansAsAService.sln) in Visual Studio
1. Put the storage connection string & app insights instrumentation key in the appropriate places of [ServiceConfiguration.Cloud.cscfg](SiloDeploy/ServiceConfiguration.Cloud.cscfg):

![](https://brandonhmsdnblog.blob.core.windows.net/images/2018/07/28/37bd7a7c.png)

and `local.settings.json` of the FunctionsClient project. You'll have to create this from scratch, so here's a template:
```js
{
  "IsEncrypted": false,
  "Values": {
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
    "APPINSIGHTS_INSTRUMENTATIONKEY": "xxx",
    "ClusterStorageConnectionString": "conn_string"
  }
}
```

5. Deploy the `SiloDeploy` project as a Cloud Service to Azure by right-clicking it and choosing `Publish...`:

![](https://brandonhmsdnblog.blob.core.windows.net/images/2018/07/28/d3a695fb.png)

then going through the Wizard:
![](https://brandonhmsdnblog.blob.core.windows.net/images/2018/07/28/2018-07-27_21-37-03.png)

![](https://brandonhmsdnblog.blob.core.windows.net/images/2018/07/28/2018-07-27_21-40-25.png)

![](https://brandonhmsdnblog.blob.core.windows.net/images/2018/07/28/2018-07-27_21-41-51.png)

Go to 'Advanced Settings' and choose the Storage Account you created in Step 1, then click Publish. Deployment takes quite some time, but you can monitor it in the Microsoft Azure Activity Log window w/in VS:

![](https://brandonhmsdnblog.blob.core.windows.net/images/2018/07/28/d9d15d9a.png)

6. Once deployed, grab the DNS hostname of your new Cloud Service, and enter this in to the `CloudServiceDnsName` value in [ServiceConfiguration.Cloud.cscfg](SiloDeploy/ServiceConfiguration.Cloud.cscfg):

![](https://brandonhmsdnblog.blob.core.windows.net/images/2018/07/28/2018-07-27_22-44-40.png)

> **Tip:** If you know the cloud service you want to create will be available, you can do this step ahead of the first Deployment and save yourself Step 7

7. Re-Publish the `SiloDeploy` project

Once completed, you should see the cloud service's VM in a `Running` state in Azure, ready to field requests from our Function.

8. Publish the `FunctionClient` app to an Azure Function endpoint.
1. Update the Application Settings on the Function Client instance to add those from your `local.settings.json` file, namely `APPINSIGHTS_INSTRUMENTATIONKEY` and `ClusterStorageConnectionString`.
1. Issue an HTTP POST to `http://yourfunctionendpoint.azurewebsites.net/api/Greet`
    * The first request will error out, telling you that you must specify `from` in the query string. Add this with a name & try again: `http://yourfunctionendpoint.azurewebsites.net/api/Greet?from=Brandon`
    * This request will error out telling you to add a `to` parameter to the query string. Add this & try again: `http://yourfunctionendpoint.azurewebsites.net/api/Greet?from=Brandon&to=DearUser`
    * This request will take some time to answer back, because it's doing the initial connection from the Function Host to the Orleans Silo in the cloud service; but this is only done on the first Function execution within this Function Host (because it's a `static` member variable). After this, subsequent runs will be much faster.

## Interesting bits
If you POST more than once to the endpoint, you'll notice the response comes back with "I last greeted ___." on the end of it. This is to illustrate the state-maintenace you're getting by using Orleans Virtual Actors. Each `from` request is spun up as its own Grain, therefore each "greeter" (`from`) keeps track of those they've "greeted" (`to`) by simply using a private member variable in [its implementation](Grains/GreetGrain.cs).

In theory, augmenting the scaling parameters of our Cloud Service, coupled with the auto-scaling provided by Functions, *should* enable us to field a very large number of these requests concurrently.