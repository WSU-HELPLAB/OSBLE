using System;
using System.Collections.Generic;
using ReviewInterfaceBase.ViewModel.Tag;

namespace ReviewInterfaceBase.Model.Category
{
    public class CategoryModel : IModel
    {
        public event EventHandler LoadCompleted = delegate { };

        int id = 0;

        private string name = "";

        private int selectedTagIndex = -1;

        public string Name
        {
            get { return name; }
        }

        public int SelectedTagIndex
        {
            get { return selectedTagIndex; }
            set { selectedTagIndex = value; }
        }

        public int ID
        {
            get { return id; }
        }

        private List<TagViewModel> tagViewModelList = new List<TagViewModel>();

        public List<TagViewModel> TagViewModelList
        {
            get { return tagViewModelList; }
            set { tagViewModelList = value; }
        }

        public CategoryModel(int id, string header)
        {
            this.id = id;
            this.name = header;
        }

        public void Load()
        {
            /*
            FakeDomainContext fakeDomainContext = new FakeDomainContext();
            var entityQuerey = fakeDomainContext.GetTagsQuery(id);
            var loadTagOperation = fakeDomainContext.Load<ReviewInterfaceBase.Web.Tag>(entityQuerey);
            loadTagOperation.Completed += new EventHandler(loadTagOperation_Completed);
             */

            //This was added while Categories dont work with OSBLE needs to be removed when they do
            LoadCompleted(this, EventArgs.Empty);
        }

        private void loadTagOperation_Completed(object sender, EventArgs e)
        {
            /*
            LoadOperation<Web.Tag> loadOperation = sender as LoadOperation<Web.Tag>;
            tagViewModelList = new List<TagViewModel>();
            foreach (Web.Tag tag in loadOperation.Entities)
            {
                tagViewModelList.Add(new TagViewModel(tag.Name));
            }

            //Let anyone else know (aka our ViewModel) that we are done loading
            LoadCompleted(this, EventArgs.Empty);
             */
        }
    }
}