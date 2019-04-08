using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Beste.Aws.Databases.Connector
{
    public static class AmazonDynamoDBFactory
    {
        private static IAmazonDynamoDB client = null;
        private static DynamoDBContext context = null;
        private static RegionEndpoint RegionEndpoint = null;


        /// <summary>
        /// Returns and if needed generates the AmazonDynamoDB client
        /// </summary>
        /// <returns>the AmazonDynamoDB client</returns>
        public static IAmazonDynamoDB Client
        {
            get
            {
                if (client == null)
                {
                    AmazonDynamoDBConfig clientConfig = new AmazonDynamoDBConfig();
                    if (RegionEndpoint == null)
                        RegionEndpoint = GetRegionEndpointFromSettings();
                    clientConfig.RegionEndpoint = RegionEndpoint;
                    client = new AmazonDynamoDBClient(clientConfig);
                }
                return client;
            }
            private set => client = value;
        }

        private static RegionEndpoint GetRegionEndpointFromSettings()
        {
            AwsConfig awsConfig = new AwsConfig();
            string pathToConfig = "config" + Path.DirectorySeparatorChar;
            string configFileName = "configAws.xml";
            if (Directory.Exists(pathToConfig) && File.Exists(pathToConfig + configFileName))
            {
                try
                {
                    awsConfig = Beste.Xml.Xml.LoadFromFile<AwsConfig>(pathToConfig + configFileName);
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
            return awsConfig.GetRegionEndpoint();
        }

        /// <summary>
        /// Returns and if needed generates the DynamoDBContext
        /// </summary>
        /// <returns>the DynamoDBContext</returns>
        public static IDynamoDBContext Context
        {
            get
            {
                if (context == null)
                {
                    context = new DynamoDBContext(Client);
                }
                return context;
            }
            private set => context = (DynamoDBContext)value;
        }


        /// <summary>
        /// Returns a stateless session of the already configured FluentNhibernate SessionFactory
        /// </summary>
        /// <returns>a session</returns>
        public static IDynamoDBContext GetStatelessSession()
        {
            DynamoDBContext context = new DynamoDBContext(client);
            return context;
        }

        /// <summary>
        /// Resets the factory in case other connection etc. needed
        /// </summary>
        public static void ResetFactory()
        {
            client = null;
            context = null;
        }

        /// <summary>
        /// Executes the given Function in a transaction context with return Type of T
        /// </summary>
        /// <typeparam name="T">the return type</typeparam>
        /// <param name="body">the function</param>
        /// <returns>the return type</returns>
        public static async Task<T> ExecuteInTransactionContext<T>(Func<IAmazonDynamoDB, IDynamoDBContext, Task<T>> body)
        {
            try
            {
                return await body(Client, Context);
            }
            catch (ObjectDisposedException)
            {
                client = null;
                context = null;
                return await body(Client, Context);
            }
        }

        /// <summary>
        /// Executes the given Function in a transaction context
        /// </summary>
        /// <param name="body">the function</param>
        public static async Task ExecuteInTransactionContext(Func<IAmazonDynamoDB, IDynamoDBContext, Task> body)
        {
            try
            {
                await body(Client, Context);
            }
            catch (ObjectDisposedException)
            {
                client = null;
                context = null;
                await body(Client, Context);
            }
        }

    }
}
