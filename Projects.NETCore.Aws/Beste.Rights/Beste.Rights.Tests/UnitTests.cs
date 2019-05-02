using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Beste.Aws.Databases.Connector;
using Beste.Databases.Connector;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Beste.Rights.Tests
{
    [TestClass]
    public class UnitTests
    {
        public static int TABLE_ID = 1;

        [ClassInitialize]
        public static async Task ClassInitialize(TestContext testContext)
        {
            InitializeDatabaseConnection();
            await AddInitialRightsToDatabase();
        }

        [TestMethod]
        public void InstanciateRightControl()
        {
            RightControl rightControl = new RightControl("InstanciateRightControl");
        }

        [TestMethod]
        public void CreateSettingsFile()
        {
            Beste.Rights.Settings xmlObject = new Settings();
            xmlObject.TokenInitialTimeout = new TokenInterval();
            xmlObject.TokenInitialTimeout.Hours = 1;
            xmlObject.TokenInitialTimeout.Minutes = 1;
            xmlObject.TokenInitialTimeout.Seconds = 1;
            xmlObject.TokenRefreshOnUsage = new TokenInterval();
            xmlObject.TokenRefreshOnUsage.Hours = 1;
            xmlObject.TokenRefreshOnUsage.Minutes = 1;
            xmlObject.TokenRefreshOnUsage.Seconds = 1;
            xmlObject.SaveToFile("test.xml");
        }

        [TestMethod]
        public async Task CheckRegisterGivenAuthorizationsDataBaseOnly()
        {

            RightControl rightControl = new RightControl("CheckRegister");
            string token = await rightControl.Register("1");
            if (!rightControl.IsGranted(token, "Add", "Authorizations"))
            {
                Assert.Fail();
            }
            if (rightControl.IsGranted(token, "Delete", "Authorizations"))
            {
                Assert.Fail();
            }
            if (rightControl.IsGranted(token, "Anything", "SometingElse"))
            {
                Assert.Fail();
            }

        }
        [TestMethod]
        public async Task CheckRegisterGivenAuthorizationsWithAdditionalAutorization()
        {

            RightControl rightControl = new RightControl("CheckRegister");
            PureRight pureRight = new PureRight();
            pureRight.Authorized = true;
            pureRight.Operation = "Edit";
            pureRight.RecourceModule = "Authorizations";
            string token = await rightControl.Register("1", pureRight);
            if (!rightControl.IsGranted(token, "Add", "Authorizations"))
            {
                Assert.Fail();
            }
            if (rightControl.IsGranted(token, "Delete", "Authorizations"))
            {
                Assert.Fail();
            }
            if (!rightControl.IsGranted(token, "Edit", "Authorizations"))
            {
                Assert.Fail();
            }
        }

        [TestMethod]
        public async Task CheckRegisterGivenAuthorizationsWithAdditionalAutorizations()
        {

            RightControl rightControl = new RightControl("CheckRegister");

            List<PureRight> pureRights = new List<PureRight>();
            pureRights.Add(new PureRight
            {
                Authorized = false,
                Operation = "Add",
                RecourceModule = "Authorizations"
            });
            pureRights.Add(new PureRight
            {
                Authorized = true,
                Operation = "Modify",
                RecourceModule = "Authorizations"
            });
            pureRights.Add(new PureRight
            {
                Authorized = true,
                Operation = "Any",
                RecourceModule = "SomethingElse"
            });

            string token = await rightControl.Register("1", pureRights);
            if (rightControl.IsGranted(token, "Add", "Authorizations"))
            {
                Assert.Fail();
            }
            if (rightControl.IsGranted(token, "Delete", "Authorizations"))
            {
                Assert.Fail();
            }
            if (!rightControl.IsGranted(token, "Modify", "Authorizations"))
            {
                Assert.Fail();
            }
            if (!rightControl.IsGranted(token, "Any", "SomethingElse"))
            {
                Assert.Fail();
            }
        }
        

        [TestMethod]
        public void CreateDefaultSettingsByCommitNotExistingPath()
        {
            if (File.Exists("nonExistingSettings.xml"))
                File.Delete("nonExistingSettings.xml");
            RightControl rightControl = new RightControl("CheckRegister", "nonExistingSettings.xml");
            if (!File.Exists("nonExistingSettings.xml"))
                Assert.Fail();
        }



        [TestMethod]
        public async Task CheckGrandManually()
        {

            RightControl rightControl = new RightControl("CheckRegisterAnotherNamespace");
            string token = "SomeToken";
            List<PureRight> pureRights = new List<PureRight>();
            pureRights.Add(new PureRight
            {
                Authorized = true,
                Operation = "AddServerSettings_" + "SomeUser",
                RecourceModule = "ServerSetting"
            });
            string otherToken = await rightControl.Register("1337", pureRights, token);

            if (!rightControl.IsGranted(token, "AddServerSettings_" + "SomeUser", "ServerSetting"))
            {
                Assert.Fail();
            }

        }

        public static void InitializeDatabaseConnection()
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
        }

        private static async Task AddInitialRightsToDatabase()
        {
            var request = new QueryRequest
            {
                TableName = BesteRightsAuthorization.TableName,
                KeyConditions = new Dictionary<string, Condition>
                    {
                        { "TableId", new Condition()
                            {
                                ComparisonOperator = ComparisonOperator.EQ,
                                AttributeValueList = new List<AttributeValue>
                                {
                                  new AttributeValue { N = TABLE_ID.ToString() }
                                }

                            }
                        }
                    },
                //AttributesToGet = new List<string> { "user_id" }
            };
            var response = await AmazonDynamoDBFactory.Client.QueryAsync(request);
            foreach (var item in response.Items)
            {
                BesteRightsAuthorization user = new BesteRightsAuthorization()
                {
                    TableId = TABLE_ID,
                    Uuid = item["Uuid"].S
                };
                await AmazonDynamoDBFactory.Context.DeleteAsync(user);
            }


            await AmazonDynamoDBFactory.ExecuteInTransactionContext(async (client, context) =>
            {
                List<BesteRightsAuthorization> besteRightsAuthorizations = new List<BesteRightsAuthorization>();
                besteRightsAuthorizations.Add(new BesteRightsAuthorization
                {
                    TableId = TABLE_ID,
                    Namespace = "CheckRegister",
                    Operation = "Delete",
                    RecourceModule = "Authorizations",
                    Authorized = false,
                    LegitimationUuid = "1",
                    Uuid = Guid.NewGuid().ToString()
                });
                besteRightsAuthorizations.Add(new BesteRightsAuthorization
                {
                    TableId = TABLE_ID,
                    Namespace = "CheckRegister",
                    Operation = "Add",
                    RecourceModule = "Authorizations",
                    Authorized = true,
                    LegitimationUuid = "1",
                    Uuid = Guid.NewGuid().ToString()
                });
                foreach(var item in besteRightsAuthorizations)
                {
                    await AmazonDynamoDBFactory.Context.SaveAsync(item);
                }
            });
        }
    }
}
