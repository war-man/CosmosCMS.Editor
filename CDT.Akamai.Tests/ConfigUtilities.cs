using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace CDT.Akamai.Tests
{
    internal class ConfigUtilities
    {
        //private const string EditorRoleName = "Editors";
        private static IConfiguration _configuration;

        internal static IConfiguration GetConfig()
        {
            if (_configuration != null) return _configuration;

            // the type specified here is just so the secrets library can 
            // find the UserSecretId we added in the csproj file
            var jsonConfig = Path.Combine(Environment.CurrentDirectory, "appsettings.json");

            var builder = new ConfigurationBuilder()
                .AddJsonFile(jsonConfig, true) // Lowest priority - put here
                .AddUserSecrets(Assembly.GetExecutingAssembly(), true); // User secrects override all - put here

            var config = builder.Build();

            // From either local secrets or app config, get connection info for Azure Vault.
            var clientId = config["ClientId"];
            if (string.IsNullOrEmpty(clientId)) clientId = Environment.GetEnvironmentVariable("AzureVaultClientId");

            var key = config["Key"];
            if (string.IsNullOrEmpty(key)) key = Environment.GetEnvironmentVariable("AzureVaultKey");

            var vaultUrl = config["VaultUrl"];
            if (string.IsNullOrEmpty(vaultUrl)) vaultUrl = Environment.GetEnvironmentVariable("AzureVaultUrl");
            builder.AddAzureKeyVault(vaultUrl, clientId, key);
            _configuration = builder.Build();

            return _configuration;
        }
    }
}