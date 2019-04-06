using FluentNHibernate.Automapping;
using FluentNHibernate.Cfg;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NHibernate;
using System.IO;
using System.Reflection;
using Beste.Databases;
using Beste.Databases.Connector;
using System;

namespace Beste.Databases.Tests
{
    [TestClass]
    public class IntegrationTest
    {
        readonly Assembly[] Assemblies =
            {
                Assembly.GetAssembly(typeof(User.UserMap))
            };

        [TestMethod]
        public void TestDatabaseConnection()
        {
            SessionFactory.Assemblies = Assemblies;
            ActivateTestSchema();
            using (NHibernate.ISession session = SessionFactory.GetSession())
            using (ITransaction transaction = session.BeginTransaction())
            {
                
            }
        }

        [TestMethod]
        public void ReGenerateTestTables()
        {
            ActivateTestSchema();
            SessionFactory.GenerateTables();
        }

        [TestMethod]
        public void WriteInTestTable_User()
        {
            ActivateTestSchema();
            User.User user = null;
            using (NHibernate.ISession session = SessionFactory.GetSession())
            using (ITransaction transaction = session.BeginTransaction())
            {
                user = new User.User
                {
                    Firstname = "Firstname",
                    Lastname = "Lastname",
                    Username = "Username",
                    Email = "Email",
                    MustChangePassword = true,
                    Password = "Password",
                    SaltValue = 1,
                    WrongPasswordCounter = 1
                };
                session.Save(user);
                transaction.Commit();
            }
            using (NHibernate.ISession session = SessionFactory.GetSession())
            using (ITransaction transaction = session.BeginTransaction())
            {
                User.User dbUser = session.Get<User.User>(user.UserId);
                if (!dbUser.Equals(user))
                    Assert.Fail();
            }
        }

        [TestMethod]
        public void WriteInTestTableFunctionalProgramming_User()
        {
            ActivateTestSchema();

            User.User user = SessionFactory.ExecuteInTransactionContext(AddTestUser);

            void checkUserExists(ISession session, ITransaction transaction)
            {
                User.User dbUser = session.Get<User.User>(user.UserId);
                if (!dbUser.Equals(user))
                    Assert.Fail();
            }
            SessionFactory.ExecuteInTransactionContext(checkUserExists);
        }

        [TestMethod]
        public void WriteDefaultDBSettings()
        {
            ActivateTestSchema();
            try
            {
                string settingsPath = "TestData" + Path.DirectorySeparatorChar + "NonExistingSettings.xml";
                if (File.Exists(settingsPath))
                    File.Delete(settingsPath);
                SessionFactory.SettingsPath = settingsPath;
                SessionFactory.ResetFactory();
                SessionFactory.ExecuteInTransactionContext((s,t)=> { });
            }
            catch(FluentConfigurationException ex)
            {
                //connection error is okay because default DB may not exist on test system
                if (!ex.ToString().Contains("Authentication to host 'localhost' for user 'root' using method 'mysql_native_password' failed with message: Unknown database 'beste'"))
                {
                    Console.WriteLine(ex.ToString());
                    Assert.Fail();
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Assert.Fail();
            }
        }

        public void ActivateTestSchema()
        {
            SessionFactory.Assemblies = Assemblies;
            SessionFactory.ResetFactory();
            string pathToConfig = "TestData" + Path.DirectorySeparatorChar;
            DbSettings dbSettings = DbSettings.LoadFromFile<DbSettings>(pathToConfig + "DBConnectionSettings.xml");
            dbSettings.DbSchema = "beste_test";
            dbSettings.SaveToFile(pathToConfig + "DBConnectionSettings_test.xml");
            SessionFactory.SettingsPath = pathToConfig + "DBConnectionSettings_test.xml";
        }

        public User.User AddTestUser(ISession session, ITransaction transaction)
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
                WrongPasswordCounter = 1
            };
            session.Save(user);
            transaction.Commit();
            return user;
        }
    }
}
