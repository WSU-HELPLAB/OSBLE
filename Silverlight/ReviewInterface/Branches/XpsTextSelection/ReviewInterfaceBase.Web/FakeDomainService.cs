namespace ReviewInterfaceBase.Web
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.ServiceModel.DomainServices.Hosting;
    using System.ServiceModel.DomainServices.Server;

    // TODO: Create methods containing your application logic.
    [EnableClientAccess()]
    public class FakeDomainService : DomainService
    {
        string sessionID;

        /// <summary>
        /// I will need to get Categories from the database these should have a string with an ID that can be used to get
        /// their Tags
        /// </summary>
        /// <param name="DocumentID">This will have been gotten from the database first</param>
        /// <returns>This is supposed to be a collection of Categories</returns>
        public IQueryable<Category> GetCategories(int DocumentID)
        {
            return new List<Category>()
                {
                    new Category("Scope", 0),
                    new Category("Severity", 1),
                    new Category("Classification", 2),
                }.ToArray().AsQueryable();
        }

        public IQueryable<Category> GetIssueVotingCategories()
        {
            return new List<Category>()
                {
                    new Category("Helpfulness", 100),
                    new Category("Correctness", 101),
                }.ToArray().AsQueryable();
        }

        /// <summary>
        /// This gets all the Tags with the CategoryID passed in
        /// </summary>
        /// <param name="CategoryID"></param>
        /// <returns></returns>
        public IQueryable<Tag> GetTags(int CategoryID)
        {
            List<Tag> tags = new List<Tag>();

            if (CategoryID == 0)
            {
                tags.Add(new Tag("Limited", 0));
                tags.Add(new Tag("Medium", 1));
                tags.Add(new Tag("Large", 3));
            }
            else if (CategoryID == 1)
            {
                tags.Add(new Tag("Low", 4));
                tags.Add(new Tag("Medium", 5));
                tags.Add(new Tag("High", 6));
            }
            else if (CategoryID == 2)
            {
                tags.Add(new Tag("Meets assignment requirements", 7));
                tags.Add(new Tag("Structure and design", 8));
                tags.Add(new Tag("Documentation, standards & formatting", 9));
                tags.Add(new Tag("Variables & constants", 10));
                tags.Add(new Tag("Arithmetic operations", 11));
                tags.Add(new Tag("Loops & branches", 12));
                tags.Add(new Tag("Defensive programming", 13));
            }
            else if (CategoryID == 100)
            {
                tags.Add(new Tag("0 (Not Helpful)", 100));
                tags.Add(new Tag("1", 101));
                tags.Add(new Tag("2", 102));
                tags.Add(new Tag("3", 103));
                tags.Add(new Tag("4", 104));
                tags.Add(new Tag("5", 105));
                tags.Add(new Tag("6", 106));
                tags.Add(new Tag("7", 107));
                tags.Add(new Tag("8", 108));
                tags.Add(new Tag("9", 109));
                tags.Add(new Tag("10 (Very Helpful)", 110));
            }
            else if (CategoryID == 101)
            {
                tags.Add(new Tag("0 (Not Correct)", 120));
                tags.Add(new Tag("1", 121));
                tags.Add(new Tag("2", 122));
                tags.Add(new Tag("3", 123));
                tags.Add(new Tag("4", 124));
                tags.Add(new Tag("5", 125));
                tags.Add(new Tag("6", 126));
                tags.Add(new Tag("7", 127));
                tags.Add(new Tag("8", 128));
                tags.Add(new Tag("9", 129));
                tags.Add(new Tag("10 (Correct)", 130));
            }
            else
            {
                tags.Add(new Tag("1", 10));
                tags.Add(new Tag("Two", 11));
                tags.Add(new Tag("III", 12));
                tags.Add(new Tag("h", 13));
            }
            return tags.ToArray().AsQueryable();
        }

        /// <summary>
        /// I might need to get the rubric it depends on how Jason's rubric tools works and if I am going to use it
        /// </summary>
        private void GetRubric()
        {
        }

        /// <summary>
        /// This is called from the client side and sets our session ID that will be used in our web services
        /// </summary>
        /// <param name="sessionID"></param>
        public void SetSessionID(string sessionID)
        {
            this.sessionID = sessionID;
        }

        //This needs to get the document locations and return their real location that the client side can open
        public IQueryable<DocumentLocation> GetDocumentLocations()
        {
            //return (new List<DocumentLocation>() { new DocumentLocation("xpsDoc.xps", 6, "bob", AuthorClassification.Student) }).ToArray().AsQueryable();
            return (new List<DocumentLocation>() { new DocumentLocation("csFile.cp", 6, "bob", AuthorClassification.Student) }).ToArray().AsQueryable();
        }

        public IQueryable<DocumentLocation> GetPeerReviewLocations()
        {
            return (new List<DocumentLocation>() { new DocumentLocation("PeerReview2.xml", 100, "Anonymous", AuthorClassification.Student),
                //new DocumentLocation("PeerReview2.xml", 1, "Anonymous", AuthorClassification.Student),
                /*new DocumentLocation("PeerReview2 - Copy.xml", 2, "2", AuthorClassification.Student),
                new DocumentLocation("PeerReview2 - Copy (2).xml", 3, "3", AuthorClassification.Student),
                new DocumentLocation("PeerReview2 - Copy (3).xml", 4, "4", AuthorClassification.Student),
                new DocumentLocation("PeerReview2 - Copy (4).xml", 5, "5", AuthorClassification.Student),
                //new DocumentLocation("PeerReview2 - Copy (5).xml", 6, "theOtherGuy", AuthorClassification.Student),
                new DocumentLocation("PeerReview2 - Copy (6).xml", 7, "7", AuthorClassification.Student),
                new DocumentLocation("PeerReview2 - Copy (7).xml", 8, "8", AuthorClassification.Student),
                new DocumentLocation("PeerReview2 - Copy (8).xml", 9, "9", AuthorClassification.Student),
                new DocumentLocation("PeerReview2 - Copy (9).xml", 10, "10", AuthorClassification.Student),
                new DocumentLocation("PeerReview2 - Copy (10).xml", 11, "11", AuthorClassification.Student),
                new DocumentLocation("PeerReview2 - Copy (11).xml", 12, "12", AuthorClassification.Student),
                new DocumentLocation("PeerReview2 - Copy (12).xml", 13, "13", AuthorClassification.Student),
                new DocumentLocation("PeerReview2 - Copy (13).xml", 14, "14", AuthorClassification.Student),
                new DocumentLocation("PeerReview2 - Copy (14).xml", 15, "15", AuthorClassification.Student),
                new DocumentLocation("PeerReview2 - Copy (15).xml", 16, "16", AuthorClassification.Student),
                new DocumentLocation("PeerReview2 - Copy (16).xml", 17, "17", AuthorClassification.Student)*/ }.AsQueryable());
        }

        public void UploadFile(string str)
        {
            StreamWriter sw = new StreamWriter("C:/Users/sgordon/sgordon/OsbleCMS/Silverlight/ReviewInterface/Branches/XpsTextSelection/ReviewInterfaceBase.Web/ClientBin/PeerReview2.xml");
            sw.Write(str);
            sw.Close();
        }
    }
}