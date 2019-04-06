using Microsoft.VisualStudio.TestTools.UnitTesting;
using Beste.Module;
using Beste.Databases.User;
using Newtonsoft.Json;
using System.Reflection;
using Beste.Databases.Connector;
using System.IO;
using System;
using NHibernate;
using Beste.Core.Models;

namespace Beste.Module.Tests
{
    [TestClass]
    public class UnitTests
    {
        readonly Assembly[] Assemblies =
        {
            Assembly.GetAssembly(typeof(UserMap))
        };

        [TestInitialize]
        public void TestInitialize()
        {
            ActivateTestSchema();
            ResetTables();
        }

        [TestMethod]
        public void CreateUserWrongPasswordGuidelines()
        {
            BesteUser besteUser = new BesteUser();
            User user = new User();
            user.Username = "UsernamePasswordGuidelines";
            user.Lastname = "Lastname";
            user.Firstname = "Firstname";
            user.Email = "Email";
            user.Password = "passwort";
            ModifyUserResponse response = besteUser.CreateUser(JsonConvert.SerializeObject(user, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            ValiateResponse(response, ModifyUserResult.PASSWORD_GUIDELINES_ERROR);

        }

        [TestMethod]
        public void CreateUserMissingParams()
        {
            BesteUser besteUser = new BesteUser();
            User user = new User();
            user.Username = "Username";
            user.Password = "Passwort1$";
            ModifyUserResponse response = besteUser.CreateUser(JsonConvert.SerializeObject(user, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            ValiateResponse(response, ModifyUserResult.MISSING_USER_PARAMS);
            
        }

        [TestMethod]
        public void CreateUserAndLogin()
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

            ModifyUserResponse response = besteUser.CreateUser(JsonConvert.SerializeObject(user, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            ValiateResponse(response, ModifyUserResult.SUCCESS);

            User loginUser = new User
            {
                Username = user.Username,
                Password = user.Password
            };
            BesteUserAuthentificationResponse authResponse = besteUser.Authenticate(JsonConvert.SerializeObject(loginUser, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            ValiateResponse(authResponse, BesteUserAuthentificationResult.MUST_CHANGE_PASSWORT);

            response = besteUser.ChangePasswordByUser(JsonConvert.SerializeObject(loginUser, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            ValiateResponse(response, ModifyUserResult.SUCCESS);

            authResponse = besteUser.Authenticate(JsonConvert.SerializeObject(loginUser, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            ValiateResponse(authResponse, BesteUserAuthentificationResult.SUCCESS);

        }

        [TestMethod]
        public void CreateUserAndEdit()
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
            ModifyUserResponse response = besteUser.CreateUser(JsonConvert.SerializeObject(user, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
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
            response = besteUser.EditUser(JsonConvert.SerializeObject(loginUser, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            ValiateResponse(response, ModifyUserResult.SUCCESS);

            BesteUserAuthentificationResponse authResponse = besteUser.Authenticate(JsonConvert.SerializeObject(loginUser, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            ValiateResponse(authResponse, BesteUserAuthentificationResult.SUCCESS);

        }

        [TestMethod]
        public void CreateUserAndChangePasswortBreakRules()
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
            ModifyUserResponse response = besteUser.CreateUser(JsonConvert.SerializeObject(user, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            ValiateResponse(response, ModifyUserResult.SUCCESS);

            User loginUser = new User
            {
                Username = user.Username,
                Password = "passwort"
            };
            response = besteUser.ChangePasswordByUser(JsonConvert.SerializeObject(loginUser, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            ValiateResponse(response, ModifyUserResult.PASSWORD_GUIDELINES_ERROR);

        }
        [TestMethod]
        public void CreateUserAndDelete()
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
            ModifyUserResponse response = besteUser.CreateUser(JsonConvert.SerializeObject(user, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            ValiateResponse(response, ModifyUserResult.SUCCESS);

            User loginUser = new User
            {
                Username = user.Username,
                Password = user.Password
            };
            response = besteUser.DeleteUser(JsonConvert.SerializeObject(loginUser, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            ValiateResponse(response, ModifyUserResult.SUCCESS);
        }
        [TestMethod]

        public void CreateDuplicateUser()
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
            ModifyUserResponse response = besteUser.CreateUser(JsonConvert.SerializeObject(user, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            ValiateResponse(response, ModifyUserResult.SUCCESS);

            response = besteUser.CreateUser(JsonConvert.SerializeObject(user, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            ValiateResponse(response, ModifyUserResult.USER_ALREADY_EXISTS);
        }

        [TestMethod]
        public void UnknownUser()
        {
            BesteUser besteUser = new BesteUser();
            User user = new User
            {
                Username = "UnknnownUsernameLogin",
                Password = "Passwort1$"
            };
            BesteUserAuthentificationResponse authResponse = besteUser.Authenticate(JsonConvert.SerializeObject(user, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            ValiateResponse(authResponse, BesteUserAuthentificationResult.USER_UNKNOWN);
            
            ModifyUserResponse response = besteUser.EditUser(JsonConvert.SerializeObject(user, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            ValiateResponse(response, ModifyUserResult.USER_UNKNOWN);

            response = besteUser.ChangePasswordByUser(JsonConvert.SerializeObject(user, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            ValiateResponse(response, ModifyUserResult.USER_UNKNOWN);

            response = besteUser.DeleteUser(JsonConvert.SerializeObject(user, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
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
        public void CreateUserAndWrongPasswortCounter()
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
            ModifyUserResponse response = besteUser.CreateUser(JsonConvert.SerializeObject(user, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            ValiateResponse(response, ModifyUserResult.SUCCESS);

            User loginUser = new User();
            loginUser.Username = user.Username;
            loginUser.Password = user.Password + "1";

            BesteUserAuthentificationResponse authResponse;
            for (int i = 0; i < 13; i++)
            {
                authResponse = besteUser.Authenticate(JsonConvert.SerializeObject(loginUser, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
                ValiateResponse(authResponse, BesteUserAuthentificationResult.WRONG_PASSWORD);
            }

            loginUser.Password = user.Password;
            authResponse = besteUser.Authenticate(JsonConvert.SerializeObject(loginUser, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            ValiateResponse(authResponse, BesteUserAuthentificationResult.WRONG_PASSWORD_COUNTER_TOO_HIGH);

        }

        [TestMethod]
        public void ForcedJsonSerializationErrors()
        {
            BesteUser besteUser = new BesteUser();
            ModifyUserResponse response = besteUser.CreateUser("no json]");
            ValiateResponse(response, ModifyUserResult.JSON_ERROR);

            response = besteUser.ChangePasswordByUser("no json]");
            ValiateResponse(response, ModifyUserResult.JSON_ERROR);

            response = besteUser.DeleteUser("no json]");
            ValiateResponse(response, ModifyUserResult.JSON_ERROR);

            response = besteUser.EditUser("no json]");
            ValiateResponse(response, ModifyUserResult.JSON_ERROR);

            BesteUserAuthentificationResponse authResponse = besteUser.Authenticate("no json]");
            ValiateResponse(authResponse, BesteUserAuthentificationResult.JSON_ERROR);


        }

        [TestMethod]
        public void WrongParameters()
        {
            BesteUser besteUser = new BesteUser();
            User user = new User
            {
                Username = "",
                Password = ""
            };
            BesteUserAuthentificationResponse authResponse = besteUser.Authenticate(JsonConvert.SerializeObject(user, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            ValiateResponse(authResponse, BesteUserAuthentificationResult.WRONG_PARAMETER);
        }
        [TestMethod]
        public void CreateUserAndTryLoginWithWrongPepper()
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
            ModifyUserResponse response = besteUser.CreateUser(JsonConvert.SerializeObject(user, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            ValiateResponse(response, ModifyUserResult.SUCCESS);

            User loginUser = new User
            {
                Username = user.Username,
                Password = user.Password
            };
            response = besteUser.ChangePasswordByUser(JsonConvert.SerializeObject(loginUser, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            ValiateResponse(response, ModifyUserResult.SUCCESS);

            BesteUser besteUserOtherPepper = new BesteUser("otherPepper");
            BesteUserAuthentificationResponse authResponse = besteUserOtherPepper.Authenticate(JsonConvert.SerializeObject(loginUser, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            ValiateResponse(authResponse, BesteUserAuthentificationResult.WRONG_PASSWORD);
        }
        [TestMethod]
        public void GetUsers()
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
            ModifyUserResponse response = besteUser.CreateUser(JsonConvert.SerializeObject(user, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            ValiateResponse(response, ModifyUserResult.SUCCESS);

            user.Username = "A_B_User";
            user.Email = "A_B_Email";
            response = besteUser.CreateUser(JsonConvert.SerializeObject(user, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            ValiateResponse(response, ModifyUserResult.SUCCESS);

            user.Username = "A_C_User";
            user.Email = "A_A_Email";
            response = besteUser.CreateUser(JsonConvert.SerializeObject(user, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            ValiateResponse(response, ModifyUserResult.SUCCESS);

            GetUsersParams getUsersParams = new GetUsersParams(10, 0, SortUsersBy.USERNAME);
            GetUsersResponse getUserResponse = besteUser.GetUsers(JsonConvert.SerializeObject(getUsersParams, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
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
            getUserResponse = besteUser.GetUsers(JsonConvert.SerializeObject(getUsersParams, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
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
            getUserResponse = besteUser.GetUsers(JsonConvert.SerializeObject(getUsersParams, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
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
            getUserResponse = besteUser.GetUsers(JsonConvert.SerializeObject(getUsersParams, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            ValiateResponse(getUserResponse, GetUsersResult.SUCCESS);

            if (getUserResponse.Users[0].Email != "A_C_Email")
            {
                Assert.Fail("getUserResponse.Users[0].Email != 'A_C_Email'");
            }
            
            getUserResponse = besteUser.GetUser(JsonConvert.SerializeObject(user, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
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

        public void ActivateTestSchema(bool regenerateSchema = false)
        {
            SessionFactory.Assemblies = Assemblies;
            SessionFactory.ResetFactory();
            string pathToConfig = "TestData" + Path.DirectorySeparatorChar;
            SessionFactory.SettingsPath = pathToConfig + "DBConnectionSettings_test.xml";
            SessionFactory.ResetFactory();
            SessionFactory.Assemblies = Assemblies;
            if (regenerateSchema)
            {
                SessionFactory.GenerateTables();
            }

            // try to connect (check if table available)
            try
            {
                using (NHibernate.ISession session = SessionFactory.GetSession())
                using (ITransaction transaction = session.BeginTransaction())
                {
                    var result = session.QueryOver<User>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                // try to generate tables if connection failed
                SessionFactory.GenerateTables();
            }
        }
        public void ResetTables()
        {

            // try to connect (check if table available)
            try
            {
                using (ISession s = SessionFactory.GetSession())
                {
                    s.Delete("from User o");
                    s.Flush();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                // try to generate tables if connection failed
                SessionFactory.GenerateTables();
            }

        }
    }
}
