using Beste.Databases.Connector.Properties;
using FluentNHibernate.Automapping;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.AdoNet;
using NHibernate.Cfg;
using NHibernate.Tool.hbm2ddl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Xml;

namespace Beste.Databases.Connector
{
    public static class SessionFactory
    {
        private static ISessionFactory factory = null;
        
        /// <summary>
        /// Array of Assemblies Nhibernate will use to look up mappings
        /// </summary>
        public static Assembly[] Assemblies { private get; set; }

        /// <summary>
        /// The relative or absolute SettingsPath
        /// </summary>
        public static string SettingsPath { get; set; } = "Resources" + Path.DirectorySeparatorChar + "DBConnectionSettings.xml";

        private static ISessionFactory Factory
        {
            get
            {
                if (factory == null)
                {
                    string connectionString = GetSettings();
                    factory = CreateFluentConfiguration()
                        .ExposeConfiguration(c => c.DataBaseIntegration(prop =>
                        {
                            prop.BatchSize = 100;
                            prop.Batcher<MySqlClientBatchingBatcherFactory>();
                        }))
                        .BuildSessionFactory();
                }
                return factory;
            }
            set => factory = value;
        }

        private static string GetSettings()
        {
            Console.WriteLine("Beste.Databases.Connector.SessionFactory.GetSettings()");
            string directoryPath = new FileInfo(SettingsPath).Directory.FullName;
            if (!Directory.Exists(directoryPath) || !File.Exists(SettingsPath))
            {
                CreateDefaultSettings(SettingsPath);
            }
            DbSettings dbSettings = DbSettings.LoadFromFile< DbSettings>(SettingsPath);
            string connectionString = @"Server=" + dbSettings.Ip + ";" + "Port=3306;" +
                "Database=" + dbSettings.DbSchema + ";User ID=" + dbSettings.DbUser +
                ";Password=" + dbSettings.DbPassword + ";Pooling=false;" + "SslMode=none";
            return connectionString;
        }

        private static void CreateDefaultSettings(string settingsPath)
        {
            string directoryPath = new FileInfo(settingsPath).Directory.FullName;
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            if (!File.Exists(settingsPath))
            {

                var assembly = typeof(SessionFactory).GetTypeInfo().Assembly;
                Stream resourceStream = assembly.GetManifestResourceStream("Beste.Databases.Connector.Resources.DBConnectionSettings.xml");
                resourceStream.WriteAllToFile(settingsPath);
                resourceStream.Dispose();
            }
        }

        private static FluentConfiguration CreateFluentConfiguration()
        {
            if (Assemblies.Length == 0)
                throw new ArgumentNullException("Assemblies", "Assemblies must be set before first session!");

            string connectionString = GetSettings();
            FluentConfiguration fluentConfiguration =
                            Fluently.Configure().Database(MySQLConfiguration.Standard.ConnectionString(connectionString));
            foreach (Assembly assembly in Assemblies)
            {
                fluentConfiguration.Mappings(m => m.FluentMappings.AddFromAssembly(assembly));
            }
            return fluentConfiguration;
        }

        /// <summary>
        /// Returns a session of the already configured FluentNhibernate SessionFactory
        /// </summary>
        /// <returns>a session</returns>
        public static ISession GetSession()
        {
            return Factory.OpenSession();
        }


        /// <summary>
        /// Returns a stateless session of the already configured FluentNhibernate SessionFactory
        /// </summary>
        /// <returns>a session</returns>
        public static IStatelessSession GetStatelessSession()
        {
            return Factory.OpenStatelessSession();
        }

        /// <summary>
        /// Generates the tables of the current configuration
        /// </summary>
        public static void GenerateTables()
        {

            CreateFluentConfiguration().ExposeConfiguration(cfg => new SchemaExport(cfg).Create(true, true))
                        .BuildSessionFactory();
            //CreateFluentConfiguration().ExposeConfiguration(cfg => new NHibernate.Tool.hbm2ddl.SchemaExport(cfg).Execute(true, true, true))
            //    .BuildSessionFactory();
        }

        /// <summary>
        /// Creates the schema of the current configuration
        /// </summary>
        public static void CreateSchema()
        {
            CreateFluentConfiguration()
                .ExposeConfiguration(cfg => new SchemaUpdate(cfg).Execute(true, true))
                .BuildSessionFactory();
        }

        /// <summary>
        /// Resets the factory in case other connection, other mappings, etc. needed
        /// </summary>
        public static void ResetFactory()
        {
            factory = null;
        }

        /// <summary>
        /// Executes the given Function in a transaction context with return Type of T
        /// </summary>
        /// <typeparam name="T">the return type</typeparam>
        /// <param name="body">the function</param>
        /// <returns>the return type</returns>
        public static T ExecuteInTransactionContext<T>(Func<ISession, ITransaction, T> body)
        {
            using (NHibernate.ISession session = SessionFactory.GetSession())
            using (ITransaction transaction = session.BeginTransaction())
            {
                return body(session, transaction);
            }
        }

        /// <summary>
        /// Executes the given Function in a transaction context
        /// </summary>
        /// <param name="body">the function</param>
        public static void ExecuteInTransactionContext(Action<ISession, ITransaction> body)
        {
            using (NHibernate.ISession session = SessionFactory.GetSession())
            using (ITransaction transaction = session.BeginTransaction())
            {
                body(session, transaction);
            }
        }

    }
}
