using Microsoft.VisualStudio.TestTools.UnitTesting;
using Beste.Aws.Databases.Connector;
using System;
using Amazon.DynamoDBv2.Model;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;

namespace Beste.Databases.Tests
{
    [TestClass]
    public class IntegrationTest
    {
        public static int TABLE_ID = 1;

        [TestMethod]
        public async Task TestDatabaseConnectionGetById()
        {
            ActivateTestSchema();

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
            ActivateTestSchema();
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
            ActivateTestSchema();
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
            await AmazonDynamoDBFactory.Context.SaveAsync(user);
            User.User dbUser = await AmazonDynamoDBFactory.Context.LoadAsync(user);
            if (!dbUser.Equals(user))
                Assert.Fail();
        }

        [TestMethod]
        public async Task WriteInTestTableFunctionalProgramming_User()
        {
            ActivateTestSchema();

            User.User user = await AmazonDynamoDBFactory.ExecuteInTransactionContext(AddTestUser);

            async Task checkUserExists(IAmazonDynamoDB client, IDynamoDBContext context)
            {
                User.User dbUser = await AmazonDynamoDBFactory.Context.LoadAsync<User.User>(user);
                if (!dbUser.Equals(user))
                    Assert.Fail();
            }
            await AmazonDynamoDBFactory.ExecuteInTransactionContext(checkUserExists);
        }

        [TestMethod]
        public void WriteDefaultDBSettings()
        {
            Assert.Inconclusive("WriteDefaultDBSettings invalid! Because we won't add our AWS key+secred to the project!");
        }

        public void ActivateTestSchema()
        {
            //todo access tables with e.g. _test post or pre fix
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
