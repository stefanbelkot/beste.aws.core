using Microsoft.VisualStudio.TestTools.UnitTesting;
using Beste.Module;
using Beste.Databases.User;
using Newtonsoft.Json;
using System.Reflection;
using Beste.Databases.Connector;
using System.IO;
using System;
using Beste.Core.Models;
using Amazon.DynamoDBv2.Model;
using Amazon.DynamoDBv2;
using System.Collections.Generic;
using Beste.Aws.Databases.Connector;
using System.Threading.Tasks;
using Beste.Aws.Module;

namespace Beste.Module.Tests
{
    [TestClass]
    public class UnitTests
    {
        public static int TABLE_ID = 1;

        [TestInitialize]
        public async Task TestInitialize()
        {
            await ResetTables();
        }

        [TestMethod]
        public async Task CreateUserWrongPasswordGuidelines()
        {
            BesteUser besteUser = new BesteUser();
            User user = new User();
            user.Username = "UsernamePasswordGuidelines";
            user.Lastname = "Lastname";
            user.Firstname = "Firstname";
            user.Email = "Email";
            user.Password = "passwort";
            ModifyUserResponse response = await besteUser.CreateUser(JsonConvert.SerializeObject(user, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            ValiateResponse(response, ModifyUserResult.PASSWORD_GUIDELINES_ERROR);

        }

        [TestMethod]
        public async Task CreateUserMissingParams()
        {
            BesteUser besteUser = new BesteUser();
            User user = new User();
            user.Username = "Username";
            user.Password = "Passwort1$";
            ModifyUserResponse response = await besteUser.CreateUser(JsonConvert.SerializeObject(user, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            ValiateResponse(response, ModifyUserResult.MISSING_USER_PARAMS);
            
        }

        [TestMethod]
        public async Task CreateUserAndLogin()
        {
            BesteUser besteUser = new BesteUser();

            User user = new User
            {
                Username = "UsernameLogin",
                Lastname = "Lastname",
                Firstname = "Firstname",
                Email = "Email",
                Password = "Passwort1$"
            };

            ModifyUserResponse response = await besteUser.CreateUser(JsonConvert.SerializeObject(user, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            ValiateResponse(response, ModifyUserResult.SUCCESS);

            User loginUser = new User
            {
                Username = user.Username,
                Password = user.Password
            };
            BesteUserAuthentificationResponse authResponse = await besteUser.Authenticate(JsonConvert.SerializeObject(loginUser, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            ValiateResponse(authResponse, BesteUserAuthentificationResult.MUST_CHANGE_PASSWORT);

            response = await besteUser.ChangePasswordByUser(JsonConvert.SerializeObject(loginUser, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            ValiateResponse(response, ModifyUserResult.SUCCESS);

            authResponse = await besteUser.Authenticate(JsonConvert.SerializeObject(loginUser, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            ValiateResponse(authResponse, BesteUserAuthentificationResult.SUCCESS);

        }

        [TestMethod]
        public async Task CreateUserAndEdit()
        {
            BesteUser besteUser = new BesteUser();
            User user = new User
            {
                Username = "UsernameToEdit",
                Lastname = "Lastname",
                Firstname = "Firstname",
                Email = "Email",
                Password = "Passwort1$"
            };
            ModifyUserResponse response = await besteUser.CreateUser(JsonConvert.SerializeObject(user, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            ValiateResponse(response, ModifyUserResult.SUCCESS);

            User loginUser = new User
            {
                Username = "UsernameToEdit",
                Lastname = "Lastname",
                Firstname = "Firstname",
                Email = "Email",
                Password = "Passwort1$",
                MustChangePassword = false
            };
            response = await besteUser.EditUser(JsonConvert.SerializeObject(loginUser, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            ValiateResponse(response, ModifyUserResult.SUCCESS);

            BesteUserAuthentificationResponse authResponse = await besteUser.Authenticate(JsonConvert.SerializeObject(loginUser, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            ValiateResponse(authResponse, BesteUserAuthentificationResult.SUCCESS);

        }

        [TestMethod]
        public async Task CreateUserAndChangePasswortBreakRules()
        {
            BesteUser besteUser = new BesteUser();
            User user = new User
            {
                Username = "UsernameChangePasswortBreakRules",
                Lastname = "Lastname",
                Firstname = "Firstname",
                Email = "Email",
                Password = "Passwort1$"
            };
            ModifyUserResponse response = await besteUser.CreateUser(JsonConvert.SerializeObject(user, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            ValiateResponse(response, ModifyUserResult.SUCCESS);

            User loginUser = new User
            {
                Username = user.Username,
                Password = "passwort"
            };
            response = await besteUser.ChangePasswordByUser(JsonConvert.SerializeObject(loginUser, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            ValiateResponse(response, ModifyUserResult.PASSWORD_GUIDELINES_ERROR);

        }
        [TestMethod]
        public async Task CreateUserAndDelete()
        {
            BesteUser besteUser = new BesteUser();
            User user = new User
            {
                Username = "UsernameCreateAndDelete",
                Lastname = "Lastname",
                Firstname = "Firstname",
                Email = "Email",
                Password = "Passwort1$"
            };
            ModifyUserResponse response = await besteUser.CreateUser(JsonConvert.SerializeObject(user, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            ValiateResponse(response, ModifyUserResult.SUCCESS);

            User loginUser = new User
            {
                Username = user.Username,
                Password = user.Password
            };
            response = await besteUser.DeleteUser(JsonConvert.SerializeObject(loginUser, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            ValiateResponse(response, ModifyUserResult.SUCCESS);
        }
        [TestMethod]

        public async Task CreateDuplicateUser()
        {
            BesteUser besteUser = new BesteUser();
            User user = new User
            {
                Username = "UsernameDuplicate",
                Lastname = "Lastname",
                Firstname = "Firstname",
                Email = "Email",
                Password = "Passwort1$"
            };
            ModifyUserResponse response = await besteUser.CreateUser(JsonConvert.SerializeObject(user, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            ValiateResponse(response, ModifyUserResult.SUCCESS);

            response = await besteUser.CreateUser(JsonConvert.SerializeObject(user, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            ValiateResponse(response, ModifyUserResult.USER_ALREADY_EXISTS);
        }

        [TestMethod]
        public async Task UnknownUser()
        {
            BesteUser besteUser = new BesteUser();
            User user = new User
            {
                Username = "UnknnownUsernameLogin",
                Password = "Passwort1$"
            };
            BesteUserAuthentificationResponse authResponse = await besteUser.Authenticate(JsonConvert.SerializeObject(user, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            ValiateResponse(authResponse, BesteUserAuthentificationResult.USER_UNKNOWN);
            
            ModifyUserResponse response = await besteUser.EditUser(JsonConvert.SerializeObject(user, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            ValiateResponse(response, ModifyUserResult.USER_UNKNOWN);

            response = await besteUser.ChangePasswordByUser(JsonConvert.SerializeObject(user, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            ValiateResponse(response, ModifyUserResult.USER_UNKNOWN);

            response = await besteUser.DeleteUser(JsonConvert.SerializeObject(user, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            ValiateResponse(response, ModifyUserResult.USER_UNKNOWN);

        }

        [TestMethod]
        public void RightViolation()
        {
            // The checking of rights must be done in the application which uses the Module.User
            // This test checks for the result code existing

            BesteUserAuthentificationResponse authResponse = new BesteUserAuthentificationResponse(BesteUserAuthentificationResult.RIGHT_VIOLATION, null);
            ValiateResponse(authResponse, BesteUserAuthentificationResult.RIGHT_VIOLATION);

            ModifyUserResponse response = new ModifyUserResponse(ModifyUserResult.RIGHT_VIOLATION, null, null, null);
            ValiateResponse(response, ModifyUserResult.RIGHT_VIOLATION);
        }

        [TestMethod]
        public async Task CreateUserAndWrongPasswortCounter()
        {
            BesteUser besteUser = new BesteUser();
            User user = new User
            {
                Username = "UsernameWrongPasswortCounter",
                Lastname = "Lastname",
                Firstname = "Firstname",
                Email = "Email",
                Password = "Passwort1$"
            };
            ModifyUserResponse response = await besteUser.CreateUser(JsonConvert.SerializeObject(user, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            ValiateResponse(response, ModifyUserResult.SUCCESS);

            User loginUser = new User();
            loginUser.Username = user.Username;
            loginUser.Password = user.Password + "1";

            BesteUserAuthentificationResponse authResponse;
            for (int i = 0; i < 13; i++)
            {
                authResponse = await besteUser.Authenticate(JsonConvert.SerializeObject(loginUser, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
                ValiateResponse(authResponse, BesteUserAuthentificationResult.WRONG_PASSWORD);
            }

            loginUser.Password = user.Password;
            authResponse = await besteUser.Authenticate(JsonConvert.SerializeObject(loginUser, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            ValiateResponse(authResponse, BesteUserAuthentificationResult.WRONG_PASSWORD_COUNTER_TOO_HIGH);

        }

        [TestMethod]
        public async Task ForcedJsonSerializationErrors()
        {
            BesteUser besteUser = new BesteUser();
            ModifyUserResponse response = await besteUser.CreateUser("no json]");
            ValiateResponse(response, ModifyUserResult.JSON_ERROR);

            response = await besteUser.ChangePasswordByUser("no json]");
            ValiateResponse(response, ModifyUserResult.JSON_ERROR);

            response = await besteUser.DeleteUser("no json]");
            ValiateResponse(response, ModifyUserResult.JSON_ERROR);

            response = await besteUser.EditUser("no json]");
            ValiateResponse(response, ModifyUserResult.JSON_ERROR);

            BesteUserAuthentificationResponse authResponse = await besteUser.Authenticate("no json]");
            ValiateResponse(authResponse, BesteUserAuthentificationResult.JSON_ERROR);


        }

        [TestMethod]
        public async Task WrongParameters()
        {
            BesteUser besteUser = new BesteUser();
            User user = new User
            {
                Username = "",
                Password = ""
            };
            BesteUserAuthentificationResponse authResponse = await besteUser.Authenticate(JsonConvert.SerializeObject(user, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            ValiateResponse(authResponse, BesteUserAuthentificationResult.WRONG_PARAMETER);
        }
        [TestMethod]
        public async Task CreateUserAndTryLoginWithWrongPepper()
        {
            BesteUser besteUser = new BesteUser();
            User user = new User
            {
                Username = "UsernameLoginWrongPepper",
                Lastname = "Lastname",
                Firstname = "Firstname",
                Email = "Email",
                Password = "Passwort1$"
            };
            ModifyUserResponse response = await besteUser.CreateUser(JsonConvert.SerializeObject(user, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            ValiateResponse(response, ModifyUserResult.SUCCESS);

            User loginUser = new User
            {
                Username = user.Username,
                Password = user.Password
            };
            response = await besteUser.ChangePasswordByUser(JsonConvert.SerializeObject(loginUser, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            ValiateResponse(response, ModifyUserResult.SUCCESS);

            BesteUser besteUserOtherPepper = new BesteUser("otherPepper");
            BesteUserAuthentificationResponse authResponse = await besteUserOtherPepper.Authenticate(JsonConvert.SerializeObject(loginUser, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            ValiateResponse(authResponse, BesteUserAuthentificationResult.WRONG_PASSWORD);
        }
        [TestMethod]
        public async Task GetUsers()
        {
            BesteUser besteUser = new BesteUser();
            User user = new User
            {
                Username = "A_A_User",
                Lastname = "Lastname",
                Firstname = "Firstname",
                Email = "A_C_Email",
                Password = "Passwort1$"
            };
            ModifyUserResponse response = await besteUser.CreateUser(JsonConvert.SerializeObject(user, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            ValiateResponse(response, ModifyUserResult.SUCCESS);

            user.Username = "A_B_User";
            user.Email = "A_B_Email";
            response = await besteUser.CreateUser(JsonConvert.SerializeObject(user, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            ValiateResponse(response, ModifyUserResult.SUCCESS);

            user.Username = "A_C_User";
            user.Email = "A_A_Email";
            response = await besteUser.CreateUser(JsonConvert.SerializeObject(user, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            ValiateResponse(response, ModifyUserResult.SUCCESS);

            GetUsersParams getUsersParams = new GetUsersParams(10, 0, SortUsersBy.USERNAME);
            GetUsersResponse getUserResponse = await besteUser.GetUsers(JsonConvert.SerializeObject(getUsersParams, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            ValiateResponse(getUserResponse, GetUsersResult.SUCCESS);
            if(getUserResponse.Users.Count < 3)
            {
                Assert.Fail("getUserResponse.Users.Count < 3");
            }
            if (getUserResponse.Users[0].Username != "A_A_User")
            {
                Assert.Fail("getUserResponse.Users[0].Username != 'A_A_User'");
            }

            getUsersParams = new GetUsersParams(10, 1, SortUsersBy.USERNAME);
            getUserResponse = await besteUser.GetUsers(JsonConvert.SerializeObject(getUsersParams, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            ValiateResponse(getUserResponse, GetUsersResult.SUCCESS);
            if (getUserResponse.Users.Count < 2)
            {
                Assert.Fail("getUserResponse.Users.Count < 2");
            }
            if (getUserResponse.Users[0].Username != "A_B_User")
            {
                Assert.Fail("getUserResponse.Users[0].Username != 'A_B_User'");
            }

            getUsersParams = new GetUsersParams(1, 1, SortUsersBy.USERNAME);
            getUserResponse = await besteUser.GetUsers(JsonConvert.SerializeObject(getUsersParams, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            ValiateResponse(getUserResponse, GetUsersResult.SUCCESS);
            if (getUserResponse.Users.Count != 1)
            {
                Assert.Fail("getUserResponse.Users.Count != 1");
            }
            if (getUserResponse.Users[0].Username != "A_B_User")
            {
                Assert.Fail("getUserResponse.Users[0].Username != 'A_B_User'");
            }

            getUsersParams = new GetUsersParams(10, 2, SortUsersBy.EMAIL);
            getUserResponse = await besteUser.GetUsers(JsonConvert.SerializeObject(getUsersParams, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            ValiateResponse(getUserResponse, GetUsersResult.SUCCESS);

            if (getUserResponse.Users[0].Email != "A_C_Email")
            {
                Assert.Fail("getUserResponse.Users[0].Email != 'A_C_Email'");
            }
            
            getUserResponse = await besteUser.GetUser(JsonConvert.SerializeObject(user, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            ValiateResponse(getUserResponse, GetUsersResult.SUCCESS);
            if (getUserResponse.Users[0].Email != "A_A_Email")
            {
                Assert.Fail("getUserResponse.Users[0].Email != 'A_A_Email'");
            }
        }

        internal static void ValiateResponse<T, T2>(T2 response, T expectedResult)
            where T2 : IResponse<T>
            where T : IComparable
        {
            if (!response.Result.Equals(expectedResult))
            {
                Console.WriteLine("response.Result = " + response.Result.ToString() + " Expected = " + expectedResult.ToString());
                Assert.Fail("response.Result = " + response.Result.ToString() + " Expected = " + expectedResult.ToString());
            }
        }

        public async Task ResetTables()
        {

            // try to connect (check if table available)
            try
            {
                var request = new QueryRequest
                {
                    TableName = "user",
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
                    //AttributesToGet = new List<string> { "user_id" }
                };
                var response = await AmazonDynamoDBFactory.Client.QueryAsync(request);
                foreach (var item in response.Items)
                {
                    User user = new User()
                    {
                        TableId = TABLE_ID,
                        Username = item["username"].S
                    };
                    await AmazonDynamoDBFactory.Context.DeleteAsync(user);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

        }
    }
}
