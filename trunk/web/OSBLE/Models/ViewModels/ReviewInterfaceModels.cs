using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.ViewModels.ReviewInterface
{
    public enum AuthorClassification
    {
        Student,
        Moderator,
        Anonymous,
        Instructor
    }

    public class DocumentLocation
    {
        string location;
        int id;
        string author;
        AuthorClassification role;
        string fileName;

        public string Location
        {
            get { return location; }
        }

        public string Author
        {
            get { return author; }
        }

        public AuthorClassification Role
        {
            get { return role; }
        }

        [KeyAttribute]
        public int ID
        {
            get { return id; }
        }

        public string FileName
        {
            get { return fileName; }
        }

        public DocumentLocation()
        {
            location = "DEFAULT";
            id = 0;
        }

        public DocumentLocation(string location, int id, string author, AuthorClassification role, string fileName)
        {
            this.location = location;
            this.id = id;
            this.author = author;
            this.role = role;
            this.fileName = fileName;
        }
    }
}