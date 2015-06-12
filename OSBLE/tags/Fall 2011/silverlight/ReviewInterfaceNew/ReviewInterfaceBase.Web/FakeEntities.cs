using System.ComponentModel.DataAnnotations;

namespace ReviewInterfaceBase.Web
{
    public enum AuthorClassification
    {
        Student,
        Moderator,
        Anonymous
    }

    public class Category
    {
        string name;
        int id;

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        [KeyAttribute]
        public int ID
        {
            get { return id; }
            set { id = value; }
        }

        public Category()
        {
            name = "DEFAULT";
            id = 0;
        }

        public Category(string name, int id)
        {
            this.name = name;
            this.id = id;
        }
    }

    public class Tag
    {
        string name;
        int id;

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        [KeyAttribute]
        public int ID
        {
            get { return id; }
            set { id = value; }
        }

        public Tag()
        {
            name = "DEFAULT";
            id = 0;
        }

        public Tag(string name, int id)
        {
            this.name = name;
            this.id = id;
        }
    }

    public class DocumentLocation
    {
        string location;
        int id;
        string author;
        AuthorClassification role;

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

        public DocumentLocation()
        {
            location = "DEFAULT";
            id = 0;
        }

        public DocumentLocation(string location, int id, string author, AuthorClassification role)
        {
            this.location = location;
            this.id = id;
            this.author = author;
            this.role = role;
        }
    }
}