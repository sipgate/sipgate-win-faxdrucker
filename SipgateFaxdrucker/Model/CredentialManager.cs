using Newtonsoft.Json;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SipgateFaxdrucker
{

    public class CredentialManager
    {
        private SipgateCredentials _credentials;
        private readonly byte[] _sAdditionalEntropy = { 9, 8, 7, 6, 5 };

        public CredentialManager()
        {
            FaxDruckerUtils.LogInformation("Created CredentialManager");
        }

        private string GetConfigPath()
        {
            try
            {
                var localApplicationDataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string[] configPaths = { localApplicationDataDirectory, $@"{FaxDruckerUtils.appName}" };
                string configPath = Path.Combine(configPaths);

                if (!Directory.Exists(configPath))
                {
                    Directory.CreateDirectory(configPath);
                }

                string[] configFilePaths = { configPath, @"config.dat" };
                return Path.Combine(configFilePaths);
            }
            catch (Exception e)
            {
                FaxDruckerUtils.LogCritical($"Error creating config path for credentials ({e.Message})");
                return null;
            }
        }

        private byte[] Protect(byte[] data)
        {
            try
            {
                // Encrypt the data using DataProtectionScope.CurrentUser. The result can be decrypted
                // only by the same current user.
                return ProtectedData.Protect(data, _sAdditionalEntropy, DataProtectionScope.CurrentUser);
            }
            catch (CryptographicException e)
            {
                Console.WriteLine("Data was not encrypted. An error occurred.");
                Console.WriteLine(e.ToString());
                return null;
            }
        }

        private byte[] Unprotect(byte[] data)
        {
            try
            {
                //Decrypt the data using DataProtectionScope.CurrentUser.
                return ProtectedData.Unprotect(data, _sAdditionalEntropy, DataProtectionScope.CurrentUser);
            }
            catch (CryptographicException e)
            {
                Console.WriteLine("Data was not decrypted. An error occurred.");
                Console.WriteLine(e.ToString());
                return null;
            }
        }

        private SipgateCredentials LoadCredentials()
        {
            try
            {
                string directory = GetConfigPath();
                if (!File.Exists(directory))
                {
                    return null;
                }

                byte[] protectedContent = File.ReadAllBytes(directory);

                string jsonString = Encoding.Unicode.GetString(Unprotect(protectedContent));

                return JsonConvert.DeserializeObject<SipgateCredentials>(jsonString);
            }
            catch (Exception e)
            {
                FaxDruckerUtils.LogCritical($"error loading credentials: {e.Message}");
                return null;
            }
        }

        public void RemoveCredentials()
        {
            string directory = GetConfigPath();
            if (File.Exists(directory))
            {
                File.Delete(directory);
            }
            _credentials = null;
        }

        public SipgateCredentials GetCredentials()
        {
            return _credentials ?? (_credentials = LoadCredentials());
        }

        private void SaveCredentials()
        {
            try
            {
                string directory = GetConfigPath();

                string jsonString = JsonConvert.SerializeObject(_credentials);

                File.WriteAllBytes(directory, Protect(Encoding.Unicode.GetBytes(jsonString)));
            }
            catch (Exception e)
            {
                FaxDruckerUtils.LogCritical($"error saving credentials: {e.Message}");
            }
        }

        public void SaveCredentials(SipgateCredentials newCredentials)
        {
            _credentials = newCredentials;
            SaveCredentials();
        }

        public bool IsLoggedIn()
        {
            return GetCredentials() != null && GetCredentials().AccessToken != "";
        }
    }
}
