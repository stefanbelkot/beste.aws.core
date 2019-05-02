using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Runtime;
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
        private static AwsConfig awsConfig = null;


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
                    if (awsConfig == null)
                    {
                        awsConfig = LoadAwsConfig();
                        RegionEndpoint = awsConfig.GetRegionEndpoint();
                    }
                    clientConfig.RegionEndpoint = RegionEndpoint;
                    if(!string.IsNullOrEmpty(awsConfig.AccessKey) &&
                       !string.IsNullOrEmpty(awsConfig.SecretKey))
                    {
                        Console.WriteLine("Using Credentials from configAws.xml, RegionEndpoint=" + RegionEndpoint.ToString());
                        BasicAWSCredentials aWSCredentials = new BasicAWSCredentials(awsConfig.AccessKey, awsConfig.SecretKey);
                        client = new AmazonDynamoDBClient(aWSCredentials, clientConfig);
                    }
                    else
                    {
                        Console.WriteLine("Using Default Credentials for AWS, RegionEndpoint=" + RegionEndpoint.ToString());
                        client = new AmazonDynamoDBClient(clientConfig);
                    }
                }
                return client;
            }
            private set => client = value;
        }

        private static AwsConfig LoadAwsConfig()
        {
            //todo the path should be moved to a system folder and not provided with the executable
            // e.g. 
            //string subPath = "Beste.Core" + Path.DirectorySeparatorChar + "<theProvidedApplicationName>";
            //string configFileName = "configAws.xml";
            //string directoryOfTestConfigAws = System.IO.Path.Combine(localApplicationDataPath, subPath);
            //string pathToConfigAws = System.IO.Path.Combine(localApplicationDataPath, subPath, configFileName);

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
            return awsConfig;
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
            awsConfig = null;
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
