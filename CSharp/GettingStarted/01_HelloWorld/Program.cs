//Copyright (c) Microsoft Corporation

namespace Microsoft.Azure.Batch.Samples.HelloWorld
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Auth;
    using Microsoft.Azure.Batch.Common;
    using Common;
    using Microsoft.Extensions.Configuration;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;

    /// <summary>
    /// The main program of the HelloWorld sample
    /// </summary>
    public static class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                AccountSettings accountSettings = SampleHelpers.LoadAccountSettings();

                Settings helloWorldSettings = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("settings.json")
                    .Build()
                    .Get<Settings>();

                HelloWorldAsync(accountSettings, helloWorldSettings).Wait();
            }
            catch (AggregateException aggregateException)
            {
                // Go through all exceptions and dump useful information
                foreach (Exception exception in aggregateException.InnerExceptions)
                {
                    Console.WriteLine(exception.ToString());
                    Console.WriteLine();
                }
                Console.ReadLine();
                throw;
            }

            Console.WriteLine("Press return to exit...");
            Console.ReadLine();
        }

        /// <summary>
        /// Submits a job to the Azure Batch service, and waits for it to complete
        /// </summary>
        private static async Task HelloWorldAsync(AccountSettings accountSettings, Settings helloWorldConfigurationSettings)
        {
            Console.WriteLine("Running with the following settings: ");
            Console.WriteLine("-------------------------------------");
            Console.WriteLine(helloWorldConfigurationSettings.ToString());
            Console.WriteLine(accountSettings.ToString());

            /*
            // Set up the Batch Service credentials used to authenticate with the Batch Service.
            string AuthorityUri = "https://login.microsoftonline.com/e2f9c1cc-4ae4-484c-a7fc-5731b73e8c46";
            string BatchResourceUri = "https://batch.core.windows.net/";
            string BatchAccountUrl = "https://jerevenuebatchdev.eastus.batch.azure.com";

            string ClientId = "86fffbbd-d840-4319-88bd-22bfb8fce883";
            string ClientKey = "fT.akf2.-rUwR_Hl23Fu5.81N-BJ7PQH_V";
            */


            string AuthorityUri = "https://login.microsoftonline.com/f5f38075-2a8d-4f98-9a80-f086a61c78a4";
            string BatchResourceUri = "https://batch.core.windows.net/";
            string BatchAccountUrl = "https://batchtestjrp.eastus2.batch.azure.com";

            string ClientId = "9f179100-46f4-41b7-92ec-d94ba40e535f";
            string ClientKey = "Cvu3D.EN010.k.1B9waF5A7.eP.i21DI.g";


            //TO-DO: refactor
            AuthenticationContext authContext = new AuthenticationContext(AuthorityUri);
            AuthenticationResult authResult = authContext.AcquireTokenAsync(BatchResourceUri, new ClientCredential(ClientId, ClientKey)).GetAwaiter().GetResult();



            Console.WriteLine(authResult.AccessToken);


            var credentials =  new BatchTokenCredentials(BatchAccountUrl, authResult.AccessToken);
            //var credentials =
            //          new Microsoft.Azure.Batch.Auth.BatchSharedKeyCredentials(
            //              accountName: "batchtestjrp",
            //              keyValue: "CUSMLo7EhYv+JiZ8kLNWSpfCQN/brlyPKbARuyTf2gDDDHye3pvgg/XusWCyvgxyC8WPi+PRSaJjpJ8vCKjLdg==",
            //              baseUrl: "https://batchtestjrp.eastus2.batch.azure.com"
            //          );

            using (BatchClient batchClient = BatchClient.Open(credentials))
            {
                // add a retry policy. The built-in policies are No Retry (default), Linear Retry, and Exponential Retry
            //    batchClient.CustomBehaviors.Add(RetryPolicyProvider.ExponentialRetryProvider(TimeSpan.FromSeconds(5), 3));
              await ArticleHelpers.CreatePoolIfNotExistAsync(batchClient, helloWorldConfigurationSettings.PoolId, helloWorldConfigurationSettings.PoolNodeVirtualMachineSize, 1, 1);

                string jobId = GettingStartedCommon.CreateJobId("HelloWorl111");

                try
                {
                    // Submit the job
                    //await SubmitJobAsync(batchClient, helloWorldConfigurationSettings, jobId);

                    //// Wait for the job to complete
                    //await WaitForJobAndPrintOutputAsync(batchClient, jobId);
                }
                finally
                {
                    // Delete the job to ensure the tasks are cleaned up
                    if (!string.IsNullOrEmpty(jobId) && helloWorldConfigurationSettings.ShouldDeleteJob)
                    {
                        Console.WriteLine("Deleting job: {0}", jobId);
                        await batchClient.JobOperations.DeleteJobAsync(jobId);
                    }
                }
            }
        }

        /// <summary>
        /// Creates a job and adds a task to it.
        /// </summary>
        /// <param name="batchClient">The BatchClient to use when interacting with the Batch service.</param>
        /// <param name="configurationSettings">The configuration settings</param>
        /// <param name="jobId">The ID of the job.</param>
        /// <returns>An asynchronous <see cref="Task"/> representing the operation.</returns>
        private static async Task SubmitJobAsync(BatchClient batchClient, Settings configurationSettings, string jobId)
        {
            // create an empty unbound Job
            CloudJob unboundJob = batchClient.JobOperations.CreateJob();
            unboundJob.Id = jobId;

            // For this job, ask the Batch service to automatically create a pool of VMs when the job is submitted.
            unboundJob.PoolInformation = new PoolInformation()
            {
                AutoPoolSpecification = new AutoPoolSpecification()
                {
                    AutoPoolIdPrefix = "JPhillips",
                    PoolSpecification = new PoolSpecification()
                    {
                        TargetDedicatedComputeNodes = configurationSettings.PoolTargetNodeCount,
                        CloudServiceConfiguration = new CloudServiceConfiguration(configurationSettings.PoolOSFamily),
                        VirtualMachineSize = configurationSettings.PoolNodeVirtualMachineSize,
                        NetworkConfiguration = new NetworkConfiguration { PublicIPAddressConfiguration = new PublicIPAddressConfiguration(IPAddressProvisioningType.NoPublicIPAddresses), SubnetId = "/subscriptions/a362b710-5701-4fbc-8d74-c2d4e344427f/resourceGroups/rg-jeastusbatchetest/providers/Microsoft.Network/virtualNetworks/jrp-vnet-test/subnets/batchnetwork"}
                        },
                    KeepAlive = false,
                    PoolLifetimeOption = PoolLifetimeOption.Job
                  
                    
                }
            };

           // unboundJob.PoolInformation.AutoPoolSpecification.PoolSpecification.NetworkConfiguration = new NetworkConfiguration() { SubnetId = "/subscriptions/a362b710-5701-4fbc-8d74-c2d4e344427f/resourceGroups/rg-jeastusbatchetest/providers/Microsoft.Network/virtualNetworks/jrp-vnet-test/subnets/default" };



            unboundJob.Commit();


        // Commit Job to create it in the service

        
            
           await batchClient.JobOperations.AddTaskAsync(jobId, new CloudTask("task1", "cmd /c echo Hello world from the Batch Hello world sample!"));
        }

        /// <summary>
        /// Waits for all tasks under the specified job to complete and then prints each task's output to the console.
        /// </summary>
        /// <param name="batchClient">The BatchClient to use when interacting with the Batch service.</param>
        /// <param name="jobId">The ID of the job.</param>
        /// <returns>An asynchronous <see cref="Task"/> representing the operation.</returns>
        private static async Task WaitForJobAndPrintOutputAsync(BatchClient batchClient, string jobId)
        {
            Console.WriteLine("Waiting for all tasks to complete on job: {0} ...", jobId);

            // We use the task state monitor to monitor the state of our tasks -- in this case we will wait for them all to complete.
            TaskStateMonitor taskStateMonitor = batchClient.Utilities.CreateTaskStateMonitor();

            List<CloudTask> ourTasks = await batchClient.JobOperations.ListTasks(jobId).ToListAsync();

            // Wait for all tasks to reach the completed state.
            // If the pool is being resized then enough time is needed for the nodes to reach the idle state in order
            // for tasks to run on them.
            await taskStateMonitor.WhenAll(ourTasks, TaskState.Completed, TimeSpan.FromMinutes(10));

            // dump task output
            foreach (CloudTask t in ourTasks)
            {
                Console.WriteLine("Task {0}", t.Id);

                //Read the standard out of the task
                NodeFile standardOutFile = await t.GetNodeFileAsync(Constants.StandardOutFileName);
                string standardOutText = await standardOutFile.ReadAsStringAsync();
                Console.WriteLine("Standard out:");
                Console.WriteLine(standardOutText);

                Console.WriteLine();
            }
        }
    }
}
