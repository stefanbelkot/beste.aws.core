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

        public static IAmazonDynamoDB Client
        {
            get
            {
                if (client == null)
                {
                    //todo: being able to use other than default hardcoded settings use GetSettings
                    //string connectionString = GetSettings();
                    AmazonDynamoDBConfig clientConfig = new AmazonDynamoDBConfig();
                    // This client will access the US East 1 region.
                    clientConfig.RegionEndpoint = RegionEndpoint.USEast2;
                    client = new AmazonDynamoDBClient(clientConfig);
                }
                try
                {
                    var anyProp = client.Config;
                }
                catch
                {
                    //todo: being able to use other than default hardcoded settings use GetSettings
                    //string connectionString = GetSettings();
                    AmazonDynamoDBConfig clientConfig = new AmazonDynamoDBConfig();
                    // This client will access the US East 1 region.
                    clientConfig.RegionEndpoint = RegionEndpoint.USEast2;
                    client = new AmazonDynamoDBClient(clientConfig);
                }
                return client;
            }
            set => client = value;
        }
        public static bool ClientIsDisposed(IAmazonDynamoDB client)
        {
            BindingFlags bfIsDisposed = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetProperty;
            // Retrieve a FieldInfo instance corresponding to the field
            PropertyInfo field = client.GetType().GetProperty("_disposed", bfIsDisposed);

            // Retrieve the value of the field, and cast as necessary
            return (bool)field.GetValue(client, null);
        }
        private static string GetSettings()
        {
            throw new NotImplementedException();
        }

        private static void CreateDefaultSettings(string settingsPath)
        {
            throw new NotImplementedException();
        }



        /// <summary>
        /// Returns a session of the already configured FluentNhibernate SessionFactory
        /// </summary>
        /// <returns>a session</returns>
        public static IDynamoDBContext Context
        {
            get
            {
                //if (context == null)
                //{
                    context = new DynamoDBContext(Client);
                //}
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
        /// Resets the factory in case other connection, other mappings, etc. needed
        /// </summary>
        public static void ResetFactory()
        {
            client = null;
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
