using Beste.Databases.Connector;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NHibernate;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Beste.Rights.Tests
{
    [TestClass]
    public class UnitTests
    {
        readonly Assembly[] Assemblies =
        {
                Assembly.GetAssembly(typeof(BesteRightsDefinition))
        };

        [TestMethod]
        public void InstanciateRightControl()
        {
            ActivateTestSchema();
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
        public void CheckRegisterGivenAuthorizationsDataBaseOnly()
        {
            ActivateTestSchema();
            AddInitialRightsToDatabase();

            RightControl rightControl = new RightControl("CheckRegister");
            string token = rightControl.Register(1);
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
        public void CheckRegisterGivenAuthorizationsWithAdditionalAutorization()
        {
            ActivateTestSchema(false);
            AddInitialRightsToDatabase();

            RightControl rightControl = new RightControl("CheckRegister");
            PureRight pureRight = new PureRight();
            pureRight.Authorized = true;
            pureRight.Operation = "Edit";
            pureRight.RecourceModule = "Authorizations";
            string token = rightControl.Register(1, pureRight);
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
        public void CheckRegisterGivenAuthorizationsWithAdditionalAutorizations()
        {
            ActivateTestSchema(false);
            AddInitialRightsToDatabase();

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

            string token = rightControl.Register(1, pureRights);
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
        private static void AddInitialRightsToDatabase()
        {
            using (ISession s = SessionFactory.GetSession())
            {
                s.Delete("from BesteRightsAuthorization o");
                s.Delete("from BesteRightsDefinition p");
                s.Delete("from BesteRightsNamespace l");
                s.Flush();
            }
            BesteRightsDefinition besteRightsDefinitionDelete;
            BesteRightsDefinition besteRightsDefinitionAdd;
            using (NHibernate.IStatelessSession session = SessionFactory.GetStatelessSession())
            using (ITransaction transaction = session.BeginTransaction())
            {
                BesteRightsNamespace besteRightsNamespace = new BesteRightsNamespace();
                besteRightsNamespace.Name = "CheckRegister";
                session.Insert(besteRightsNamespace);

                List<BesteRightsDefinition> besteRightsDefinitions = new List<BesteRightsDefinition>();
                besteRightsDefinitionDelete = new BesteRightsDefinition
                {
                    BesteRightsNamespace = besteRightsNamespace,
                    Operation = "Delete",
                    RecourceModule = "Authorizations"
                };
                besteRightsDefinitionAdd = new BesteRightsDefinition
                {
                    BesteRightsNamespace = besteRightsNamespace,
                    Operation = "Add",
                    RecourceModule = "Authorizations"
                };

                besteRightsDefinitions.Add(besteRightsDefinitionDelete);
                besteRightsDefinitions.Add(besteRightsDefinitionAdd);
                foreach (BesteRightsDefinition item in besteRightsDefinitions)
                {
                    session.Insert(item);
                }

                List<BesteRightsAuthorization> besteRightsAuthorizations = new List<BesteRightsAuthorization>();
                besteRightsAuthorizations.Add(new BesteRightsAuthorization
                {
                    Authorized = true,
                    LegitimationId = 1,
                    BesteRightsDefinition = besteRightsDefinitionAdd
                });
                besteRightsAuthorizations.Add(new BesteRightsAuthorization
                {
                    Authorized = false,
                    LegitimationId = 1,
                    BesteRightsDefinition = besteRightsDefinitionDelete
                });

                foreach (BesteRightsAuthorization item in besteRightsAuthorizations)
                {
                    session.Insert(item);
                }


                BesteRightsNamespace besteRightsAnotherNamespace = new BesteRightsNamespace();
                besteRightsAnotherNamespace.Name = "CheckRegisterAnotherNamespace";
                session.Insert(besteRightsAnotherNamespace);

                transaction.Commit();
            }
        }

        [TestMethod]
        public void CreateDefaultSettingsByCommitNotExistingPath()
        {
            ActivateTestSchema(false);
            if (File.Exists("nonExistingSettings.xml"))
                File.Delete("nonExistingSettings.xml");
            RightControl rightControl = new RightControl("CheckRegister", "nonExistingSettings.xml");
            if (!File.Exists("nonExistingSettings.xml"))
                Assert.Fail();
        }

        public void ActivateTestSchema(bool regenerateSchema = false)
        {
            SessionFactory.Assemblies = Assemblies;
            SessionFactory.ResetFactory();
            SessionFactory.Assemblies = Assemblies;
            string pathToConfig = "Resources" + Path.DirectorySeparatorChar;
            DbSettings dbSettings = DbSettings.LoadFromFile<DbSettings>(pathToConfig + "DBConnectionSettings.xml");
            dbSettings.DbSchema = "beste_test";
            dbSettings.SaveToFile(pathToConfig + "DBConnectionSettings_test.xml");
            SessionFactory.SettingsPath = pathToConfig + "DBConnectionSettings_test.xml";
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
                    var besteRightsNamespace = session.QueryOver<BesteRightsNamespace>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                // try to generate tables if connection failed
                SessionFactory.GenerateTables();
            }
        }


        [TestMethod]
        public void CheckGrandManually()
        {
            ActivateTestSchema();
            AddInitialRightsToDatabase();

            RightControl rightControl = new RightControl("CheckRegisterAnotherNamespace");
            string token = "SomeToken";
            List<PureRight> pureRights = new List<PureRight>();
            pureRights.Add(new PureRight
            {
                Authorized = true,
                Operation = "AddServerSettings_" + "SomeUser",
                RecourceModule = "ServerSetting"
            });
            string otherToken = rightControl.Register(1337, pureRights, token);

            if (!rightControl.IsGranted(token, "AddServerSettings_" + "SomeUser", "ServerSetting"))
            {
                Assert.Fail();
            }

        }
    }
}
