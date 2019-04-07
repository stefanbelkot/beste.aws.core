using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using Beste.Databases.User;
using Beste.Aws.Databases.Connector;
using Beste.Aws.Module.Settings;
using System.IO;
using Beste.Module.ExtensionMethods;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;
using Amazon.DynamoDBv2;
using System.Linq;

namespace Beste.Aws.Module
{
    public class BesteUser : IBesteUser
    {
        public static int TABLE_ID = 1;
        private readonly string PATH_TO_CONFIG = "config" + Path.DirectorySeparatorChar;
        const string CONFIG_FILENAME = "config";
        private readonly string shaPepperValue = "BeSte_Us3R_PeP_Va1U3";
        static Random random = new Random();

        private BesteUserSettings besteUserSettings = null;

        internal BesteUserSettings BesteUserSettings {
            get
            {
                if (besteUserSettings == null)
                {
                    if (File.Exists(PATH_TO_CONFIG + CONFIG_FILENAME))
                    {
                        besteUserSettings = Xml.Xml.LoadFromFile<BesteUserSettings>(PATH_TO_CONFIG + CONFIG_FILENAME);
                    }
                    else
                    {
                        besteUserSettings = new BesteUserSettings();
                    }
                }
                return besteUserSettings;
            }
            set
            {
                besteUserSettings = value;
            }
        }

        public BesteUser()
        {
        }
        public BesteUser(string shaPepperValue)
        {
            this.shaPepperValue = shaPepperValue;
        }

        public async Task<BesteUserAuthentificationResponse> Authenticate(string param)
        {
            User user = null;
            try
            {
                user = JsonConvert.DeserializeObject<User>(param);
            }
            catch (JsonReaderException)
            {
                return new BesteUserAuthentificationResponse(BesteUserAuthentificationResult.JSON_ERROR, null);
            }
            if (user.Username == null ||
                user.Username == "" ||
                user.Password == null ||
                user.Password == "")
            {
                return new BesteUserAuthentificationResponse(BesteUserAuthentificationResult.WRONG_PARAMETER, null);
            }

            return await AmazonDynamoDBFactory.ExecuteInTransactionContext(async (client, context) =>
            {
                user.TableId = TABLE_ID;
                User dbUser = await AmazonDynamoDBFactory.Context.LoadAsync<User>(user);
                if (dbUser == null || dbUser.Equals(new User()))
                {
                    return new BesteUserAuthentificationResponse(BesteUserAuthentificationResult.USER_UNKNOWN, null);
                }
                else
                {
                    string hashedPw = Sha256WithSaltAndPepper(user.Password, dbUser.SaltValue.ToString());
                    if (hashedPw == dbUser.Password)
                    {
                        if (dbUser.WrongPasswordCounter <= 10)
                        {
                            dbUser.WrongPasswordCounter = 0;
                            await AmazonDynamoDBFactory.Context.SaveAsync(dbUser);
                            dbUser.WrongPasswordCounter = null;
                            dbUser.Password = null;
                            dbUser.SaltValue = null;
                            if (dbUser.MustChangePassword == true)
                            {
                                return new BesteUserAuthentificationResponse(BesteUserAuthentificationResult.MUST_CHANGE_PASSWORT, dbUser);
                            }
                            else
                            {
                                return new BesteUserAuthentificationResponse(BesteUserAuthentificationResult.SUCCESS, dbUser);
                            }
                        }
                        else
                        {
                            dbUser.WrongPasswordCounter = null;
                            dbUser.Password = null;
                            dbUser.SaltValue = null;
                            dbUser.Firstname = null;
                            dbUser.Lastname = null;
                            dbUser.SaltValue = null;
                            dbUser.TableId = 0;
                            return new BesteUserAuthentificationResponse(BesteUserAuthentificationResult.WRONG_PASSWORD_COUNTER_TOO_HIGH, dbUser);
                        }
                    }
                    else
                    {
                        dbUser.WrongPasswordCounter++;
                        await AmazonDynamoDBFactory.Context.SaveAsync(dbUser);
                        dbUser.WrongPasswordCounter = null;
                        dbUser.Password = null;
                        dbUser.SaltValue = null;
                        dbUser.Firstname = null;
                        dbUser.Lastname = null;
                        dbUser.SaltValue = null;
                        dbUser.TableId = 0;
                        return new BesteUserAuthentificationResponse(BesteUserAuthentificationResult.WRONG_PASSWORD, dbUser);
                    }
                }
            });
        }

        public async Task<ModifyUserResponse> ChangePasswordByUser(string param)
        {
            User user = null;
            try
            {
                user = JsonConvert.DeserializeObject<User>(param);
            }
            catch (JsonReaderException)
            {
                return new ModifyUserResponse(ModifyUserResult.JSON_ERROR, null, null, null);
            }
            return await AmazonDynamoDBFactory.ExecuteInTransactionContext(async (client, context) =>
            {
                if (!CheckPasswordRules(user.Password))
                {
                    return new ModifyUserResponse(ModifyUserResult.PASSWORD_GUIDELINES_ERROR, null, besteUserSettings?.PasswordRules, user);
                }
                user.TableId = TABLE_ID;
                User dbUser = await AmazonDynamoDBFactory.Context.LoadAsync<User>(user);
                if (dbUser == null || dbUser.Equals(new User()))
                {
                    return new ModifyUserResponse(ModifyUserResult.USER_UNKNOWN, null, null, user);
                }
                else
                {
                    dbUser.MustChangePassword = user.MustChangePassword;
                    dbUser.SaltValue = random.Next(0, 1000000);
                    dbUser.Password = Sha256WithSaltAndPepper(user.Password, dbUser.SaltValue.ToString());
                    dbUser.WrongPasswordCounter = 0;
                    await AmazonDynamoDBFactory.Context.SaveAsync(dbUser);
                    return new ModifyUserResponse(ModifyUserResult.SUCCESS, null, null, user);
                }
            });
        }

        public async Task<ModifyUserResponse> CreateUser(string param)
        {
            User user = null;
            try
            {
                user = JsonConvert.DeserializeObject<User>(param);
            }
            catch (JsonReaderException)
            {
                return new ModifyUserResponse(ModifyUserResult.JSON_ERROR, null, null, null);
            }
            return await AmazonDynamoDBFactory.ExecuteInTransactionContext(async (client, context) =>
            {
                if (!CheckMandatoryUserParameters(user))
                {
                    return new ModifyUserResponse(ModifyUserResult.MISSING_USER_PARAMS, besteUserSettings?.MandatoryUserParams, null, user);
                }
                if (!CheckPasswordRules(user.Password))
                {
                    return new ModifyUserResponse(ModifyUserResult.PASSWORD_GUIDELINES_ERROR, null, besteUserSettings?.PasswordRules, user);
                }

                user.TableId = TABLE_ID;
                User dbUser = await AmazonDynamoDBFactory.Context.LoadAsync<User>(user);
                if (dbUser != null)
                {
                    return new ModifyUserResponse(ModifyUserResult.USER_ALREADY_EXISTS, null, null, user);
                }
                user.Uuid = Guid.NewGuid().ToString();
                user.WrongPasswordCounter = 0;
                user.MustChangePassword = true;
                user.SaltValue = random.Next(0, 1000000);
                user.Password = Sha256WithSaltAndPepper(user.Password, user.SaltValue.ToString());
                await AmazonDynamoDBFactory.Context.SaveAsync(user);
                return new ModifyUserResponse(ModifyUserResult.SUCCESS, null, null, user);
            });
        }

        public async Task<ModifyUserResponse> DeleteUser(string param)
        {
            User user = null;
            try
            {
                user = JsonConvert.DeserializeObject<User>(param);
            }
            catch (JsonReaderException)
            {
                return new ModifyUserResponse(ModifyUserResult.JSON_ERROR, null, null, null);
            }
            try
            {
                return await AmazonDynamoDBFactory.ExecuteInTransactionContext(async (client, context) =>
                {
                    user.TableId = TABLE_ID;
                    User dbUser = await AmazonDynamoDBFactory.Context.LoadAsync<User>(user);

                    if (dbUser == null || dbUser.Equals(new User()))
                    {
                        return new ModifyUserResponse(ModifyUserResult.USER_UNKNOWN, null, null, user);
                    }
                    await AmazonDynamoDBFactory.Context.DeleteAsync(dbUser);
                    return new ModifyUserResponse(ModifyUserResult.SUCCESS, null, null, user);
                });
            }
            catch(Exception)
            {
                return new ModifyUserResponse(ModifyUserResult.EXCEPTION, null, null, user);
            }
        }

        public async Task<ModifyUserResponse> EditUser(string param)
        {
            User user = null;
            try
            {
                user = JsonConvert.DeserializeObject<User>(param);
            }
            catch (JsonReaderException)
            {
                return new ModifyUserResponse(ModifyUserResult.JSON_ERROR, null, null, null);
            }

            return await AmazonDynamoDBFactory.ExecuteInTransactionContext(async (client, context) =>
            {
                user.TableId = TABLE_ID;
                User dbUser = await AmazonDynamoDBFactory.Context.LoadAsync<User>(user);
                if (dbUser == null || dbUser.Equals(new User()))
                {
                    return new ModifyUserResponse(ModifyUserResult.USER_UNKNOWN, null, null, user);
                }

                dbUser.Firstname = user.Firstname != null && user.Firstname != "" ? user.Firstname : dbUser.Firstname;
                dbUser.Lastname = user.Lastname != null && user.Lastname != "" ? user.Lastname : dbUser.Lastname;
                dbUser.Email = user.Email != null && user.Email != "" ? user.Email : dbUser.Email;
                dbUser.MustChangePassword = user.MustChangePassword != null ? user.MustChangePassword : dbUser.MustChangePassword;
                if (user.Password != null && user.Password != "")
                {
                    if (!CheckPasswordRules(user.Password))
                    {
                        return new ModifyUserResponse(ModifyUserResult.PASSWORD_GUIDELINES_ERROR, null, besteUserSettings?.PasswordRules, user);
                    }
                    dbUser.SaltValue = random.Next(0, 1000000);
                    dbUser.Password = Sha256WithSaltAndPepper(user.Password, dbUser.SaltValue.ToString());
                    dbUser.WrongPasswordCounter = 0;
                }
                await AmazonDynamoDBFactory.Context.SaveAsync(dbUser);
                return new ModifyUserResponse(ModifyUserResult.SUCCESS, null, null, user);
            });
        }
        public async Task<GetUsersResponse> GetUsers(string param)
        {
            GetUsersParams getUsersParams;
            try
            {
                getUsersParams = JsonConvert.DeserializeObject<GetUsersParams>(param);
            }
            catch (JsonReaderException)
            {
                return new GetUsersResponse(GetUsersResult.JSON_ERROR, null);
            }
            return await AmazonDynamoDBFactory.ExecuteInTransactionContext(async (client, context) =>
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
                    //AttributesToGet = new List<string> { "user_id" }
                };
                var response = await AmazonDynamoDBFactory.Client.QueryAsync(request);
                List<User> users = new List<User>();
                foreach (var item in response.Items)
                {
                    User user = new User()
                    {
                        TableId = Convert.ToInt32( item["id"].N),
                        Username = item["username"].S,
                        Uuid = item["Uuid"].S,
                        Firstname = item["Firstname"].S,
                        Lastname = item["Lastname"].S,
                        Email = item["Email"].S,
                        Password = item["Password"].S,
                        SaltValue = Convert.ToInt32(item["SaltValue"].N),
                        MustChangePassword = item.ContainsKey("MustChangePassword") ? 
                            item["MustChangePassword"].BOOL : true,
                        WrongPasswordCounter = Convert.ToInt32(item["WrongPasswordCounter"].N)
                    };
                    users.Add(user);
                }

                users.ForEach((user) =>
                {
                    user.Password = null;
                    user.SaltValue = null;
                });
                switch (getUsersParams.SortUsersBy)
                {
                    case SortUsersBy.EMAIL:
                        users = users.OrderBy(u => u.Email).ToList();
                        break;
                    case SortUsersBy.ID:
                        users = users.OrderBy(u => u.TableId).ToList();
                        break;
                    case SortUsersBy.LASTNAME:
                        users = users.OrderBy(u => u.Lastname).ToList();
                        break;
                    case SortUsersBy.USERNAME:
                        users = users.OrderBy(u => u.Username).ToList();
                        break;
                    default:
                        break;
                }
                users = users.Skip(getUsersParams.Offset).Take(getUsersParams.Limit).ToList();
                
                return new GetUsersResponse(GetUsersResult.SUCCESS, users);
            });
        }

        public async Task<GetUsersResponse> GetUser(string param)
        {
            User user = null;
            try
            {
                user = JsonConvert.DeserializeObject<User>(param);
            }
            catch (JsonReaderException)
            {
                return new GetUsersResponse(GetUsersResult.JSON_ERROR, null);
            }
            return await AmazonDynamoDBFactory.ExecuteInTransactionContext(async (client, context) =>
            {
                user.TableId = TABLE_ID;
                User dbUser = await AmazonDynamoDBFactory.Context.LoadAsync<User>(user);
                if (dbUser == null)
                {
                    return new GetUsersResponse(GetUsersResult.USER_UNKNOWN, null);
                }
                List<User> users = new List<User> { user };
                users.ForEach((item) =>
                {
                    item.Password = null;
                    item.SaltValue = null;
                });
                return new GetUsersResponse(GetUsersResult.SUCCESS, users);
            });
        }

        private bool CheckMandatoryUserParameters(User user)
        {
            bool result = true;
            MandatoryUserParams rules = BesteUserSettings?.MandatoryUserParams;
            if (rules == null)
                rules = new MandatoryUserParams();

            result = result && (!rules.Firstname || (user.Firstname != "" && user.Firstname != null));
            result = result && (!rules.Lastname || (user.Lastname != "" && user.Lastname != null));
            result = result && (!rules.EMail || ("TOBEDEFINED" != "" && "TOBEDEFINED" != null));

            return result;
        }
        private bool CheckPasswordRules(string password)
        {
            bool result = true;
            PasswordRules rules = BesteUserSettings?.PasswordRules;
            if (rules == null)
                rules = new PasswordRules();

            result = result && password.Length > rules.MinLength;
            result = result && (!rules.HasDigit || (password.HasDigit()));
            result = result && (!rules.HasLowerCase || (password.HasLowerCase()));
            result = result && (!rules.HasUpperCase || (password.HasUpperCase()));
            result = result && (!rules.HasSpecialChars || (password.HasSpecialChars()));
            
            return result;
        }

        private string Sha256WithSaltAndPepper(string password, string salt)
        {
            return Sha256(password + shaPepperValue + salt);
        }
        static string Sha256(string randomString)
        {
            var crypt = new System.Security.Cryptography.SHA256Managed();
            var hash = new System.Text.StringBuilder();
            byte[] crypto = crypt.ComputeHash(Encoding.UTF8.GetBytes(randomString));
            foreach (byte theByte in crypto)
            {
                hash.Append(theByte.ToString("x2"));
            }
            return hash.ToString();
        }
    }
}
