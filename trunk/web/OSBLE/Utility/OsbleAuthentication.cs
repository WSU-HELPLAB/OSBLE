using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Models.Users;
using System.Web.Security;
using System.IO;
using OSBLE.Models;
using System.Security.Cryptography;
using System.Configuration;
using System.Text;

namespace OSBLE.Utility
{
    public class OsbleAuthentication
    {
        public static string ProfileCookieKey
        {
            get
            {
                return "osble_profile";
            }
        }
        private const string userNameKey = "UserName";
        private const string authKey = "AuthKey";

        private OSBLEContext db = new OSBLEContext();

        /// <summary>
        /// Generates an key for use with annotate
        /// </summary>
        /// <param name="phpFunction">The function that we're calling</param>
        /// <param name="annotateUser">The user that is responsible for the call.  For example, if we want to log in as bob@smith.com, we'd send "bob@smith.com".</param>
        /// <returns></returns>
        public static string GenerateAnnotateKey(string phpFunction, string annotateUser, long unixEpoch)
        {
            if (annotateUser == null || annotateUser.Length == 0)
            {
                annotateUser = ConfigurationManager.AppSettings["AnnotateUserName"];
            }
            string apiUser = ConfigurationManager.AppSettings["AnnotateUserName"];

            //build our string, convert into bytes for sha1
            string rawString = string.Format("{0}\n{1}\n{2}\n{3}", phpFunction, apiUser, unixEpoch, annotateUser);
            byte[] rawBytes = Encoding.UTF8.GetBytes(rawString);

            //build our hasher
            byte[] seed = Encoding.UTF8.GetBytes(ConfigurationManager.AppSettings["AnnotateApiKey"]);
            HMACSHA1 sha1 = new HMACSHA1(seed);

            //hash our bytes
            byte[] hashBytes = sha1.ComputeHash(rawBytes);

            string hashString = Convert.ToBase64String(hashBytes).Replace("\n", "");
            return hashString;
        }

        public static string Encrypt(string content)
        {
            string hash = ConfigurationManager.AppSettings["EncryptionHash"];
            byte[] Results;
            System.Text.UTF8Encoding UTF8 = new System.Text.UTF8Encoding();

            // Step 1. We hash the passphrase using MD5
            // We use the MD5 hash generator as the result is a 128 bit byte array
            // which is a valid length for the TripleDES encoder we use below

            MD5CryptoServiceProvider HashProvider = new MD5CryptoServiceProvider();
            byte[] TDESKey = HashProvider.ComputeHash(UTF8.GetBytes(hash));

            // Step 2. Create a new TripleDESCryptoServiceProvider object
            TripleDESCryptoServiceProvider TDESAlgorithm = new TripleDESCryptoServiceProvider();

            // Step 3. Setup the encoder
            TDESAlgorithm.Key = TDESKey;
            TDESAlgorithm.Mode = CipherMode.ECB;
            TDESAlgorithm.Padding = PaddingMode.PKCS7;

            // Step 4. Convert the input string to a byte[]
            byte[] DataToEncrypt = UTF8.GetBytes(content);

            // Step 5. Attempt to encrypt the string
            try
            {
                ICryptoTransform Encryptor = TDESAlgorithm.CreateEncryptor();
                Results = Encryptor.TransformFinalBlock(DataToEncrypt, 0, DataToEncrypt.Length);
            }
            finally
            {
                // Clear the TripleDes and Hashprovider services of any sensitive information
                TDESAlgorithm.Clear();
                HashProvider.Clear();
            }

            // Step 6. Return the encrypted string as a base64 encoded string
            return Convert.ToBase64String(Results);
        }

        public static string Decrypt(string content)
        {
            byte[] Results;
            string hash = ConfigurationManager.AppSettings["EncryptionHash"];
            System.Text.UTF8Encoding UTF8 = new System.Text.UTF8Encoding();

            // Step 1. We hash the passphrase using MD5
            // We use the MD5 hash generator as the result is a 128 bit byte array
            // which is a valid length for the TripleDES encoder we use below

            MD5CryptoServiceProvider HashProvider = new MD5CryptoServiceProvider();
            byte[] TDESKey = HashProvider.ComputeHash(UTF8.GetBytes(hash));

            // Step 2. Create a new TripleDESCryptoServiceProvider object
            TripleDESCryptoServiceProvider TDESAlgorithm = new TripleDESCryptoServiceProvider();

            // Step 3. Setup the decoder
            TDESAlgorithm.Key = TDESKey;
            TDESAlgorithm.Mode = CipherMode.ECB;
            TDESAlgorithm.Padding = PaddingMode.PKCS7;

            // Step 4. Convert the input string to a byte[]
            byte[] DataToDecrypt = Convert.FromBase64String(content);

            // Step 5. Attempt to decrypt the string
            try
            {
                ICryptoTransform Decryptor = TDESAlgorithm.CreateDecryptor();
                Results = Decryptor.TransformFinalBlock(DataToDecrypt, 0, DataToDecrypt.Length);
            }
            finally
            {
                // Clear the TripleDes and Hashprovider services of any sensitive information
                TDESAlgorithm.Clear();
                HashProvider.Clear();
            }

            // Step 6. Return the decrypted string in UTF8 format
            return UTF8.GetString(Results);
        }

        /// <summary>
        /// Logs the supplied user into OSBLE
        /// </summary>
        /// <param name="profile"></param>
        public static void LogIn(UserProfile profile)
        {
            HttpCookie cookie = new HttpCookie(ProfileCookieKey);

            //save the profile's user name
            cookie.Values[userNameKey] = Encrypt(profile.UserName);

            //set a really long expiration date
            cookie.Expires = DateTime.UtcNow.AddDays(300);

            //and then store it in the next response
            if (HttpContext.Current != null)
            {
                HttpContext.Current.Response.Cookies.Set(cookie);
            }
        }

        /// <summary>
        /// Returns the currently logged in user
        /// </summary>
        public static UserProfile CurrentUser
        {
            get
            {
                UserProfile profile = null;
                if (HttpContext.Current != null)
                {
                    using (OSBLEContext db = new OSBLEContext())
                    {
                        try
                        {
                            HttpCookie cookie = HttpContext.Current.Request.Cookies.Get(ProfileCookieKey);
                            string userName = Decrypt(cookie.Values[userNameKey]);
                            return db.UserProfiles.Where(u => u.UserName == userName).FirstOrDefault();
                        }
                        catch (Exception ex)
                        {
                            string message = string.Format("Error parsing current user for IP {0}: {1}", HttpContext.Current.Request.UserHostAddress, ex.Message);
                            ActivityLog log = new ActivityLog()
                            {
                                Sender = typeof(OsbleAuthentication).ToString(),
                                Message = message
                            };
                            //AC: turned off to save space / improve performance
                            /*
                                    db.ActivityLogs.Add(log);
                                    db.SaveChanges();
                             * */
                        }
                    }
                }
                return profile;
            }
        }

        /// <summary>
        /// Logs the current user out of the system
        /// </summary>
        public static void LogOut()
        {
            if (HttpContext.Current != null)
            {
                //cookie might not exist
                try
                {
                    HttpCookie cookie = HttpContext.Current.Request.Cookies.Get(ProfileCookieKey);
                    cookie.Expires = DateTime.UtcNow.AddDays(-1d);
                    HttpContext.Current.Response.Cookies.Set(cookie);
                }
                catch (Exception ex)
                {
                    //removes annoying VS warning message
                    string foo = ex.Message;
                }
            }
        }
    }
}