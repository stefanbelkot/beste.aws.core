using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using Beste.Databases.User;
using Beste.Databases.Connector;
using NHibernate;
using Beste.Module.Settings;
using System.IO;
using Beste.Module.ExtensionMethods;
using NHibernate.Util;
using NHibernate.Exceptions;

namespace Beste.Module
{
    public class BesteUser : IBesteUser
    {
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

        public BesteUserAuthentificationResponse Authenticate(string param)
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
            using (NHibernate.ISession session = SessionFactory.GetSession())
            using (ITransaction transaction = session.BeginTransaction())
            {
                User dbUser = (User)session.QueryOver<User>()
                                .Where(p => p.Username == user.Username)
                                .SingleOrDefault();// new DatabaseObjects.Salutation();
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
                            session.Save(dbUser);
                            transaction.Commit();
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
                            dbUser.UserId = 0;
                            return new BesteUserAuthentificationResponse(BesteUserAuthentificationResult.WRONG_PASSWORD_COUNTER_TOO_HIGH, dbUser);
                        }
                    }
                    else
                    {
                        dbUser.WrongPasswordCounter++;
                        session.Save(dbUser);
                        transaction.Commit();
                        dbUser.WrongPasswordCounter = null;
                        dbUser.Password = null;
                        dbUser.SaltValue = null;
                        dbUser.Firstname = null;
                        dbUser.Lastname = null;
                        dbUser.SaltValue = null;
                        dbUser.UserId = 0;
                        return new BesteUserAuthentificationResponse(BesteUserAuthentificationResult.WRONG_PASSWORD, dbUser);
                    }
                }
            }
        }

        public ModifyUserResponse ChangePasswordByUser(string param)
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
            using (NHibernate.ISession session = SessionFactory.GetSession())
            using (ITransaction transaction = session.BeginTransaction())
            {
                if (!CheckPasswordRules(user.Password))
                {
                    return new ModifyUserResponse(ModifyUserResult.PASSWORD_GUIDELINES_ERROR, null, besteUserSettings?.PasswordRules, user);
                }
                User dbUser = (User)session.QueryOver<User>()
                    .Where(p => p.Username == user.Username)
                    .SingleOrDefault();
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
                    session.Save(dbUser);
                    transaction.Commit();
                    return new ModifyUserResponse(ModifyUserResult.SUCCESS, null, null, user);
                }
            }
        }

        public ModifyUserResponse CreateUser(string param)
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
            using (NHibernate.ISession session = SessionFactory.GetSession())
            using (ITransaction transaction = session.BeginTransaction())
            {
                if (!CheckMandatoryUserParameters(user))
                {
                    return new ModifyUserResponse(ModifyUserResult.MISSING_USER_PARAMS, besteUserSettings?.MandatoryUserParams, null, user);
                }
                if (!CheckPasswordRules(user.Password))
                {
                    return new ModifyUserResponse(ModifyUserResult.PASSWORD_GUIDELINES_ERROR, null, besteUserSettings?.PasswordRules, user);
                }
                if(session.QueryOver<User>()
                    .Where(p => p.Username == user.Username)
                    .SingleOrDefault() != null)
                {
                    return new ModifyUserResponse(ModifyUserResult.USER_ALREADY_EXISTS, null, null, user);
                }

                user.WrongPasswordCounter = 0;
                user.MustChangePassword = true;
                user.SaltValue = random.Next(0, 1000000);
                user.Password = Sha256WithSaltAndPepper(user.Password, user.SaltValue.ToString());
                session.Save(user);
                transaction.Commit();
                return new ModifyUserResponse(ModifyUserResult.SUCCESS, null, null, user);
            }
        }

        public ModifyUserResponse DeleteUser(string param)
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
                using (NHibernate.ISession session = SessionFactory.GetSession())
                using (ITransaction transaction = session.BeginTransaction())
                {
                    User dbUser = (User)session.QueryOver<User>()
                        .Where(p => p.Username == user.Username)
                        .SingleOrDefault();

                    if (dbUser == null || dbUser.Equals(new User()))
                    {
                        return new ModifyUserResponse(ModifyUserResult.USER_UNKNOWN, null, null, user);
                    }
                    else
                    {

                    }
                    session.Delete(dbUser);
                    transaction.Commit();
                    return new ModifyUserResponse(ModifyUserResult.SUCCESS, null, null, user);
                }
            }
            catch(GenericADOException ex)
            {
                if(ex.ToString().Contains("a foreign key constraint fails"))
                {
                    return new ModifyUserResponse(ModifyUserResult.FOREIGN_KEY_CONSTRAINT_ERROR, null, null, user);
                }
                else
                {
                    return new ModifyUserResponse(ModifyUserResult.EXCEPTION, null, null, user);
                }
            }
        }

        public ModifyUserResponse EditUser(string param)
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
            using (NHibernate.ISession session = SessionFactory.GetSession())
            using (ITransaction transaction = session.BeginTransaction())
            {
                User dbUser = (User)session.QueryOver<User>()
                    .Where(p => p.Username == user.Username)
                    .SingleOrDefault();
                if (dbUser == null || dbUser.Equals(new User()))
                {
                    return new ModifyUserResponse(ModifyUserResult.USER_UNKNOWN, null, null, user);
                }

                dbUser.Firstname = user.Firstname != null && user.Firstname != "" ? user.Firstname : dbUser.Firstname;
                dbUser.Lastname = user.Lastname != null && user.Lastname != "" ? user.Lastname : dbUser.Lastname;
                dbUser.Email = user.Email != null && user.Email != "" ? user.Email : dbUser.Email;
                dbUser.MustChangePassword = user.MustChangePassword != null ? user.MustChangePassword : dbUser.MustChangePassword;
                if(user.Password != null && user.Password != "")
                {
                    if (!CheckPasswordRules(user.Password))
                    {
                        return new ModifyUserResponse(ModifyUserResult.PASSWORD_GUIDELINES_ERROR, null, besteUserSettings?.PasswordRules, user);
                    }
                    dbUser.SaltValue = random.Next(0, 1000000);
                    dbUser.Password = Sha256WithSaltAndPepper(user.Password, dbUser.SaltValue.ToString());
                    dbUser.WrongPasswordCounter = 0;
                }
                session.Save(dbUser);
                transaction.Commit();
                return new ModifyUserResponse(ModifyUserResult.SUCCESS, null, null, user);

            }
        }
        public GetUsersResponse GetUsers(string param)
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
            using (NHibernate.ISession session = SessionFactory.GetSession())
            using (ITransaction transaction = session.BeginTransaction())
            {
                System.Linq.Expressions.Expression<Func<User, object>> orderBy;
                switch (getUsersParams.SortUsersBy)
                {
                    case SortUsersBy.EMAIL:
                        orderBy = (k) => k.Email;
                        break;
                    case SortUsersBy.ID:
                        orderBy = (k) => k.UserId;
                        break;
                    case SortUsersBy.LASTNAME:
                        orderBy = (k) => k.Lastname;
                        break;
                    case SortUsersBy.USERNAME:
                        orderBy = (k) => k.Username;
                        break;
                    default:
                        orderBy = (k) => k.Username;
                        break;
                }
                //System.Linq.Expressions.Expression< Func<User, object>> expression = new System.Linq.Expressions.Expression<Func<User, object>>();
                IList<User> dbUsers = session.QueryOver<User>()
                    .OrderBy(orderBy).Asc
                    .Skip(getUsersParams.Offset)
                    .Take(getUsersParams.Limit)
                    .List<User>();
                List<User> users = new List<User>(dbUsers);
                users.ForEach((user) =>
                {
                    user.Password = null;
                    user.SaltValue = null;
                });
                return new GetUsersResponse(GetUsersResult.SUCCESS, users);
            }
        }

        public GetUsersResponse GetUser(string param)
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
            using (NHibernate.ISession session = SessionFactory.GetSession())
            using (ITransaction transaction = session.BeginTransaction())
            {
                IList<User> dbUsers = session.QueryOver<User>()
                    .Where(p => p.Username == user.Username)
                    .List<User>();
                if(dbUsers.Count == 0)
                {
                    return new GetUsersResponse(GetUsersResult.USER_UNKNOWN, null);
                }
                else if (dbUsers.Count > 1)
                {
                    return new GetUsersResponse(GetUsersResult.TOO_MANY_RESULTS, null);
                }
                List<User> users = new List<User>(dbUsers);
                users.ForEach((item) =>
                {
                    item.Password = null;
                    item.SaltValue = null;
                });
                return new GetUsersResponse(GetUsersResult.SUCCESS, users);
            }
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
