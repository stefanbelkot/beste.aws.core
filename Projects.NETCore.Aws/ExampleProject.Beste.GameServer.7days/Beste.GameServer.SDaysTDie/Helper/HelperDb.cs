using Beste.Databases.Connector;
using Beste.Databases.User;
using Beste.GameServer.SDaysTDie.Modules;
using Beste.Rights;
using NHibernate;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Beste.GameServer.SDaysTDie.Helper
{
    public static class HelperDb
    {
        static readonly Assembly[] Assemblies =
{
                Assembly.GetAssembly(typeof(User)),
                Assembly.GetAssembly(typeof(BesteRightsDefinition)),
                Assembly.GetAssembly(typeof(ServerSetting))
        };

        internal static void ActivateSessionFactory()
        {
            SessionFactory.Assemblies = Assemblies;
            SessionFactory.ResetFactory();
            SessionFactory.Assemblies = Assemblies;
        }
        internal static void ActivateTestSchema(bool regenerateSchema = false)
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
                    var users = session.QueryOver<User>();
                    var rights = session.QueryOver<BesteRightsAuthorization>();
                    var serverSettings = session.QueryOver<ServerSetting>();
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
