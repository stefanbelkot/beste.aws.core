using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace Beste.GameServer.SDaysTDie
{
    public class EndpointConfiguration
    {
        public string Host { get; set; }
        public int? Port { get; set; }
        public string Scheme { get; set; }
        public string StoreName { get; set; }
        public string StoreLocation { get; set; }
        public string FilePath { get; set; }
        public string Password { get; set; }
    }
    public static class KestrelServerOptionsExtensions
    {
        public static void ConfigureEndpoints(this KestrelServerOptions options)
        {
            var environment = options.ApplicationServices.GetRequiredService<IHostingEnvironment>();

            string pathToConfig = "Resources" + Path.DirectorySeparatorChar;
            if (!Directory.Exists(pathToConfig))
            {
                Directory.CreateDirectory(pathToConfig);
            }
            if (!File.Exists(pathToConfig + "Settings.xml"))
            {
                //todo fix the reading of settings if it doesnt exist
                //File.WriteAllBytes(pathToConfig + "Settings.xml", Resources.DBConnectionSettings);
            }
            Settings settings = Settings.LoadFromFile<Settings>(pathToConfig + "Settings.xml");
            if (settings.Endpoints == null)
            {
                Console.WriteLine("[ERROR] No Endpoints found!");
                return;
            }
            foreach (var endpoint in settings.Endpoints)
            {
                var port = endpoint.Port ?? (endpoint.Scheme == "https" ? 443 : 80);

                var ipAddresses = new List<IPAddress>();
                if (endpoint.Host == "localhost")
                {
                    ipAddresses.Add(IPAddress.IPv6Loopback);
                    ipAddresses.Add(IPAddress.Loopback);
                }
                else if (IPAddress.TryParse(endpoint.Host, out var address))
                {
                    ipAddresses.Add(address);
                }
                else
                {
                    ipAddresses.Add(IPAddress.IPv6Any);
                }

                foreach (var address in ipAddresses)
                {
                    try
                    {
                        options.Listen(address, port,
                            listenOptions =>
                            {
                                if (endpoint.Scheme == "https")
                                {
                                    var certificate = LoadCertificate(endpoint, environment);
                                    listenOptions.UseHttps(certificate);
                                }
                            });
                    }
                    catch (InvalidOperationException ex)
                    {
                        Console.WriteLine("[ERROR] " + ex.ToString());
                    }
                }
            }
        }

        private static X509Certificate2 LoadCertificate(SettingsEndpoint endpoint, IHostingEnvironment environment)
        {
            if (endpoint.StoreName != null && endpoint.StoreLocation != null)
            {
                using (var store = new X509Store(endpoint.StoreName, Enum.Parse<StoreLocation>(endpoint.StoreLocation)))
                {
                    store.Open(OpenFlags.ReadOnly);
                    var certificate = store.Certificates.Find(
                        X509FindType.FindBySubjectName,
                        endpoint.Host,
                        validOnly: !environment.IsDevelopment());

                    if (certificate.Count == 0)
                    {
                        throw new InvalidOperationException($"Certificate not found for {endpoint.Host}.");
                    }

                    return certificate[0];
                }
            }

            if (endpoint.FilePath != null && endpoint.Password != null)
            {
                return new X509Certificate2(endpoint.FilePath, endpoint.Password);
            }

            throw new InvalidOperationException("No valid certificate configuration found for the current endpoint.");
        }
    }

}