using System;
using System.Collections.Generic;
using System.ServiceModel.DomainServices.Client;
using ReviewInterfaceBase.ViewModel.Category;
using ReviewInterfaceBase.Web;

namespace ReviewInterfaceBase.Model.CatergoryHolder
{
    public class CategoryHolderModel : IModel
    {
        public event EventHandler LoadCompleted;
        private static int allowedCategories = 6;
        private int documentID;

        public int DocumentID
        {
            get { return documentID; }
            set
            {
                documentID = value;
            }
        }

        public int AllowedCategories
        {
            get { return allowedCategories; }
        }

        private List<CategoryViewModel> categories = new List<CategoryViewModel>();

        public List<CategoryViewModel> Categories
        {
            get { return categories; }
            set { categories = value; }
        }

        public CategoryHolderModel Clone()
        {
            CategoryHolderModel chm = new CategoryHolderModel();
            chm.DocumentID = this.DocumentID;
            foreach (CategoryViewModel cvm in this.Categories)
            {
                CategoryViewModel newCvm = cvm.Clone();
            }
            return chm;
        }

        public CategoryHolderModel(int documentID)
        {
            this.documentID = documentID;
        }

        public CategoryHolderModel()
        {
            this.documentID = -1;
        }

        private void loadCategoriesOperation_Completed(object sender, EventArgs e)
        {
            categories.Clear();
            LoadOperation<Web.Category> loadOperation = sender as LoadOperation<Web.Category>;
            foreach (Web.Category category in loadOperation.Entities)
            {
                CategoryViewModel cvm = new CategoryViewModel();
                cvm.LoadTags(category.Name, category.ID);
                categories.Add(cvm);
            }

            //Let anyone else know (aka our ViewModel) that we are done loading
            LoadCompleted(this, EventArgs.Empty);
        }

        public void Load()
        {
            if (documentID == -1)
            {
                throw new Exception("DocumentID was -1 which indicates it was not set using the correct constructor, please pass in documentID when calling the constructor if you then want to use Load");
            }
            FakeDomainContext fakeDomainContext = new FakeDomainContext();
            var entityQuerey = fakeDomainContext.GetCategoriesQuery(documentID);
            var loadCategoriesOperation = fakeDomainContext.Load<ReviewInterfaceBase.Web.Category>(entityQuerey);
            loadCategoriesOperation.Completed += new EventHandler(loadCategoriesOperation_Completed);
        }

        public void LoadIssueVotingCategories()
        {
            FakeDomainContext fakeDomainContext = new FakeDomainContext();
            var entityQuerey = fakeDomainContext.GetIssueVotingCategoriesQuery();
            var loadCategoriesOperation = fakeDomainContext.Load<ReviewInterfaceBase.Web.Category>(entityQuerey);
            loadCategoriesOperation.Completed += new EventHandler(loadCategoriesOperation_Completed);
        }
    }
}