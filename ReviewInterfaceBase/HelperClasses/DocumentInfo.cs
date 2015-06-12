using System.IO;

namespace ReviewInterfaceBase.HelperClasses
{
    public enum Classification
    {
        Student,
        Moderator,
        Anonymous
    }

    /// <summary>
    /// This class is used to hold necessary document information so it can be opened by an documentHolder
    /// </summary>
    public class DocumentInfo
    {
        private Stream stream;
        private string fileName;
        private string author;

        private Classification role;

        public Classification Role
        {
            get { return role; }
        }

        public string Author
        {
            get { return author; }
        }

        /// <summary>
        /// This gets the document stream
        /// </summary>
        public Stream Stream
        {
            get { return stream; }
            set { stream = value; }
        }

        /// <summary>
        /// This gets the file name of the document
        /// </summary>
        public string FileName
        {
            get { return fileName; }
        }

        /// <summary>
        /// This gets the extension of the file
        /// </summary>
        public string FileExtension
        {
            get
            {
                //from the fileName get the string after the last dot(.)
                string[] array = fileName.Split('.');

                //add the dot(.) back in
                return "." + array[array.Length - 1];
            }
        }

        private int id;

        /// <summary>
        /// This gets the id of the document
        /// </summary>
        public int Id
        {
            get { return id; }
        }

        /// <summary>
        /// This is the constructor
        /// </summary>
        /// <param name="id">The id of the document</param>
        /// <param name="fileName">the fileName of the document</param>
        public DocumentInfo(int id, string fileName, string author, Classification role)
        {
            this.author = author;
            this.role = role;
            this.id = id;
            this.fileName = fileName;
        }
    }
}