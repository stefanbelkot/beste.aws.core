using Microsoft.VisualStudio.TestTools.UnitTesting;
using Beste.Aws.Databases.Connector;
using System;
using Amazon.DynamoDBv2.Model;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using System.IO;

namespace Beste.Databases.Tests
{
    [TestClass]
    public class IntegrationTest
    {
        public static int TABLE_ID = 1;

        [TestInitialize]
        public async Task TestInitialize()
        {
            await InitializeDatabaseConnection();
        }

        [TestMethod]
        public async Task TestDatabaseConnectionGetById()
        {
            var request = new QueryRequest
            {
                TableName = "user",
                //ScanIndexForward = false,
                KeyConditions = new Dictionary<string, Condition>
                {
                    { "id", new Condition()
                        {
                            ComparisonOperator = ComparisonOperator.EQ,
                            AttributeValueList = new List<AttributeValue>
                            {
                              new AttributeValue { N = TABLE_ID.ToString() }
                            }

                        }
                    }
                },
                Limit = 1,
                AttributesToGet = new List<string> { "user_id" }
            };
            try
            {
                var response2 = await AmazonDynamoDBFactory.Client.QueryAsync(request);
            }
            catch(Exception ex)
            {
                Assert.Fail(ex.ToString());
                Console.WriteLine(ex.ToString());
            }
        }
        [TestMethod]
        public async Task TestDatabaseConnectionGetMaxId()
        {
            var scanRequest = new ScanRequest
            {
                TableName = "user",
                //ProjectionExpression = "Id, Title, #pr",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":q_user_id", new AttributeValue { N = TABLE_ID.ToString() } }
                },
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    { "#user_id", "user_id" }
                },
                FilterExpression = "#user_id >= :q_user_id",
                Limit = 1,
                
            };
            try
            {
                var response2 = await AmazonDynamoDBFactory.Client.ScanAsync(scanRequest);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
                Console.WriteLine(ex.ToString());
            }
        }

        [TestMethod]
        public async Task WriteInTestTable_User()
        {
            User.User user = null;
            user = new User.User
            {
                TableId = TABLE_ID,
                Firstname = "Firstname",
                Lastname = "Lastname",
                Username = "Username",
                Email = "Email",
                MustChangePassword = true,
                Password = "Password",
                SaltValue = 1,
                Uuid = Guid.NewGuid().ToString(),
                WrongPasswordCounter = 1
            };
            await AmazonDynamoDBFactory.ExecuteInTransactionContext(async (client, context) =>
            {
                await context.SaveAsync(user);
            });
            User.User dbUser = await AmazonDynamoDBFactory.ExecuteInTransactionContext(async (client, context) =>
            {
                return await context.LoadAsync(user);
            });
            dbUser = await AmazonDynamoDBFactory.ExecuteInTransactionContext(async (client, context) =>
            {
                dbUser.TableId = TABLE_ID;
                return await context.LoadAsync(dbUser);
            });
            if (!dbUser.Equals(user))
                Assert.Fail();
        }

        [TestMethod]
        public async Task WriteInTestTableFunctionalProgramming_User()
        {
            User.User user = await AmazonDynamoDBFactory.ExecuteInTransactionContext(AddTestUser);

            async Task checkUserExists(IAmazonDynamoDB client, IDynamoDBContext context)
            {
                User.User dbUser = await AmazonDynamoDBFactory.Context.LoadAsync<User.User>(user);
                if (!dbUser.Equals(user))
                    Assert.Fail();
            }
            await AmazonDynamoDBFactory.ExecuteInTransactionContext(checkUserExists);
        }


        public async Task InitializeDatabaseConnection()
        {
            AmazonDynamoDBFactory.ResetFactory();

            string localApplicationDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string subPath = "Beste.Core" + Path.DirectorySeparatorChar + "Tests";
            string configFileName = "configAws.xml";
            string directoryOfTestConfigAws = System.IO.Path.Combine(localApplicationDataPath, subPath);
            string pathToTestConfigAws = System.IO.Path.Combine(localApplicationDataPath, subPath, configFileName);
            if (!Directory.Exists(directoryOfTestConfigAws) ||
                !File.Exists(pathToTestConfigAws))
            {
                AwsConfig awsConfigPattern = new AwsConfig()
                {
                    RegionEndpoint = "REGIONENDPOINT",
                    AccessKey = "YOURACCESSKEY",
                    SecretKey = "YOURSECRETKEY"
                };
                if (!Directory.Exists(directoryOfTestConfigAws))
                {
                    Directory.CreateDirectory(directoryOfTestConfigAws);
                }
                string pathToTestConfigAwsPattern = System.IO.Path.Combine(localApplicationDataPath, subPath, "configAws_pattern.xml");
                awsConfigPattern.SaveToFile(pathToTestConfigAwsPattern);
                Assert.Fail("For AWS tests the to test config file must be found in: '" + pathToTestConfigAws + "'. Please create the file with valid endpoint+key+secret\n" +
                    "A pattern was saved in: '" + pathToTestConfigAwsPattern + "'");
            }
            AwsConfig awsConfig = AwsConfig.LoadFromFile<AwsConfig>(pathToTestConfigAws);


            string pathToConfig = "config" + Path.DirectorySeparatorChar;

            if (!Directory.Exists(pathToConfig))
            {
                Directory.CreateDirectory(pathToConfig);
            }
            if (File.Exists(pathToConfig + configFileName))
            {
                File.Delete(pathToConfig + configFileName);
            }

            awsConfig.SaveToFile(pathToConfig + configFileName);
            await WriteInTestTableFunctionalProgramming_User();
        }

        public async Task<User.User> AddTestUser(IAmazonDynamoDB client, IDynamoDBContext context)
        {
            User.User user = null;
            user = new User.User
            {
                Firstname = "Firstname",
                Lastname = "Lastname",
                Username = "Username",
                Email = "Email",
                MustChangePassword = true,
                Password = "Password",
                SaltValue = 1,
                Uuid = Guid.NewGuid().ToString(),
                WrongPasswordCounter = 1
            };
            await AmazonDynamoDBFactory.Context.SaveAsync(user);
            return user;
        }


    }
}
