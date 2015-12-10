using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Providers.Entities;
using OSBLE.Models;
using OSBLE.Models.Users;
using OSBLEPlus.Logic.Utility.Auth;

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
        public const string FileCacheKey = "FileCacheKey";

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
            UTF8Encoding UTF8 = new UTF8Encoding();

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
            UTF8Encoding UTF8 = new UTF8Encoding();

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
                if (HttpContext.Current != null && HttpContext.Current.Request.Cookies.Get(ProfileCookieKey) != null)
                {
                    try
                    {
                        HttpCookie cookie = HttpContext.Current.Request.Cookies.Get(ProfileCookieKey);
                        string userName = Decrypt(cookie.Values[userNameKey]);
                        return DBHelper.GetUserProfile(userName);
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
                else if (HttpContext.Current != null && HttpContext.Current.Session != null && HttpContext.Current.Session["auth"] != null)
                {
                    return  HttpContext.Current.Session["auth"] as UserProfile;
                }else
                {
                    if (HttpContext.Current != null)
                    {
                        var authToken = HttpContext.Current.Request.QueryString["auth"];

                        var a = HttpContext.Current.Server.MapPath("~").TrimEnd('\\');
                        var path = string.Format(Directory.GetParent(a).FullName);

                        var auth = new Authentication(path);

                        UserProfile user = auth.GetActiveUser(authToken);

                        if (HttpContext.Current.Session != null) HttpContext.Current.Session["auth"] = user;

                        return user;
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
                    var httpCookie = HttpContext.Current.Request.Cookies[ProfileCookieKey];
                    HttpContext.Current.Response.Cookies.Remove(ProfileCookieKey);
                    httpCookie.Expires = DateTime.Now.AddDays(-10);
                    httpCookie.Value = null;
                    HttpContext.Current.Response.SetCookie(httpCookie);
                }
                catch (Exception ex)
                {
                    //removes annoying VS warning message
                    string foo = ex.Message;
                }

                if (HttpContext.Current.Session["auth"] != null)
                    HttpContext.Current.Session["auth"] = null;
            }
        }
    }
}