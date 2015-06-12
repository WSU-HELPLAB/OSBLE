// Created 5-13-13 by Evan Olds for the OSBLE project at WSU

// References:
// - http://msdn.microsoft.com/en-us/library/system.security.cryptography.rijndaelmanaged.aspx
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace OSBLEExcelPlugin
{
    internal partial class LoginForm : Form
    {
        /// <summary>
        /// Name of the file that contains the encrypted password (full path and name)
        /// </summary>
        private string m_credPFileName;
        
        /// <summary>
        /// Name of the file that contains the encrypted user name (full path and name)
        /// </summary>
        private string m_credUFileName;
        
        private OSBLEState m_state = null;

        private static byte[] s_key = new byte[]{
	        185,204,123,80,94,243,206,55,111,36,53,197,43,58,87,197,244,6,181,81,235,
            249,34,106,119,93,36,29,195,106,237,113
        };

        private static byte[] s_iv = new byte[]{
	        240,218,75,92,242,171,14,143,97,50,227,197,9,242,206,202
        };
        
        public LoginForm()
        {
            InitializeComponent();

            m_credUFileName = Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData);
            m_credUFileName = Path.Combine(
                m_credUFileName, "OSBLE_Excel_u.dat");
            m_credPFileName = Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData);
            m_credPFileName = Path.Combine(
                m_credPFileName, "OSBLE_Excel_p.dat");
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            // Ignore the click if we have an empty user name or password
            if (0 == tbUserName.TextLength)
            {
                tbUserName.Focus();
                return;
            }
            if (0 == tbPassword.TextLength)
            {
                tbPassword.Focus();
                return;
            }
            
            // Disable the buttons while we're processing
            btnOK.Enabled = false;
            btnCancel.Enabled = false;
            // Also show the progress bar
            progressBar1.Visible = true;

            // Save user name and password if needed
            if (cbRemember.Checked)
            {
                byte[] encUser = EncryptStringToBytes(tbUserName.Text, s_key, s_iv);
                byte[] encPass = EncryptStringToBytes(tbPassword.Text, s_key, s_iv);
                try
                {
                    File.WriteAllBytes(m_credUFileName, encUser);
                    File.WriteAllBytes(m_credPFileName, encPass);
                }
                catch (Exception) { }
            }
            else // Otherwise delete save files
            {
                if (File.Exists(m_credUFileName)) { File.Delete(m_credUFileName); }
                if (File.Exists(m_credPFileName)) { File.Delete(m_credPFileName); }
            }

            m_state = new OSBLEState(tbUserName.Text, tbPassword.Text);
            m_state.RefreshAsync(this.LoginAttemptCompleted_CT);
        }

        public OSBLEState DoPrompt()
        {
            if (this.ShowDialog() != DialogResult.OK)
            {
                return null;
            }

            return m_state;
        }

        private static string LoadEncrypted(string fileName)
        {
            // Load encrypted file
            try
            {
                byte[] fileBytes = File.ReadAllBytes(fileName);
                if (null == fileBytes || fileBytes.Length < 6)
                {
                    File.Delete(fileName);
                    return null;
                }

                return DecryptStringFromBytes(fileBytes, s_key, s_iv);
            }
            catch (Exception ex) { return null; }
        }

        private void LoginAttemptCompleted(object sender, EventArgs e)
        {
            progressBar1.Visible = false;
            btnOK.Enabled = true;
            btnCancel.Enabled = true;
            
            OSBLEStateEventArgs oe = e as OSBLEStateEventArgs;
            if (!oe.Success)
            {
                MessageBox.Show(this, oe.Message, "OSBLE Login");
                return;
            }

            this.DialogResult = DialogResult.OK;
        }

        // CT = cross-thread
        private void LoginAttemptCompleted_CT(object sender, EventArgs e)
        {
            this.Invoke(new EventHandler(LoginAttemptCompleted), sender, e);
        }

        private void LoginForm_Load(object sender, EventArgs e)
        {
            if (!File.Exists(m_credUFileName))
            {
                // No credentials file means that they didn't save their user 
                // name and password last time.
                cbRemember.Checked = false;
                return;
            }

            cbRemember.Checked = true;

            string userName = LoadEncrypted(m_credUFileName);
            if (string.IsNullOrEmpty(userName))
            {
                return;
            }
            tbUserName.Text = userName;

            // Now the password
            if (File.Exists(m_credPFileName))
            {
                string password = LoadEncrypted(m_credPFileName);
                if (string.IsNullOrEmpty(password))
                {
                    return;
                }
                tbPassword.Text = password;
            }
            else
            {
                tbPassword.Text = string.Empty;
            }
        }

        #region Cryptography methods from http://msdn.microsoft.com/en-us/library/system.security.cryptography.rijndaelmanaged.aspx
        static byte[] EncryptStringToBytes(string plainText, byte[] Key, byte[] IV)
        {
            // Check arguments. 
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("Key");
            byte[] encrypted;
            // Create an RijndaelManaged object 
            // with the specified key and IV. 
            using (RijndaelManaged rijAlg = new RijndaelManaged())
            {
                rijAlg.Key = Key;
                rijAlg.IV = IV;

                // Create a decrytor to perform the stream transform.
                ICryptoTransform encryptor = rijAlg.CreateEncryptor(rijAlg.Key, rijAlg.IV);

                // Create the streams used for encryption. 
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {

                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }


            // Return the encrypted bytes from the memory stream. 
            return encrypted;

        }

        static string DecryptStringFromBytes(byte[] cipherText, byte[] Key, byte[] IV)
        {
            // Check arguments. 
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("Key");

            // Declare the string used to hold 
            // the decrypted text. 
            string plaintext = null;

            // Create an RijndaelManaged object 
            // with the specified key and IV. 
            using (RijndaelManaged rijAlg = new RijndaelManaged())
            {
                rijAlg.Key = Key;
                rijAlg.IV = IV;

                // Create a decrytor to perform the stream transform.
                ICryptoTransform decryptor = rijAlg.CreateDecryptor(rijAlg.Key, rijAlg.IV);

                // Create the streams used for decryption. 
                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {

                            // Read the decrypted bytes from the decrypting stream 
                            // and place them in a string.
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }

            }

            return plaintext;

        }
#endregion
    }
}
