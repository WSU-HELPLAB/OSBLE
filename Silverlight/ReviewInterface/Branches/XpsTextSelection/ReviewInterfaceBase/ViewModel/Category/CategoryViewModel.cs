﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using System.Xml.Linq;
using ReviewInterfaceBase.Model.Category;
using ReviewInterfaceBase.View.Category;
using ReviewInterfaceBase.View.Tag;
using ReviewInterfaceBase.ViewModel.Tag;

namespace ReviewInterfaceBase.ViewModel.Category
{
    public class CategoryViewModel : INotifyPropertyChanged
    {
        public event SizeChangedEventHandler SizeChanged = delegate { };
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        //Event to let PeerReviewCommentViewModels know that the tags have been loaded
        public event EventHandler LoadComplete = delegate { };

        private FrameworkElement thisView = new CategoryView();
        private CategoryModel thisModel;

        //Static bool, value changes to true if any CategoryViewModel uses ReadXML, which should only happen with issuevoting/viewpeerreview. 
        private static bool readFromXml = false;

        /// <summary>
        /// The name of the Category
        /// </summary>
        public string Name
        {
            get { return thisModel.Name; }
        }

        public int SelectedTagIndex
        {
            get { return thisModel.SelectedTagIndex; }
            set
            {
                thisModel.SelectedTagIndex = value;
                PropertyChanged(this, new PropertyChangedEventArgs("SelectedTagIndex"));
            }
        }

        public List<TagViewModel> TagList
        {
            get { return thisModel.TagViewModelList; }
        }

        //this should ObservableCollection<TagViewModel> but CatergoryView don't like it
        public List<TagView> TagViewList
        {
            get
            {
                List<TagView> tagsView = new List<TagView>();
                foreach (TagViewModel tagVM in thisModel.TagViewModelList)
                {
                    tagsView.Add(tagVM.GetView());
                }
                return tagsView;
            }
        }

        public CategoryViewModel()
        {
        }

        public void LoadTags(string header, int id)
        {
            thisModel = new CategoryModel(id, header);

            //this needs to be set for bindings to work
            thisView.DataContext = this;

            thisModel.LoadCompleted += new EventHandler(thisModel_LoadComplete);
            thisModel.Load();
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("Category");
            writer.WriteAttributeString("ID", thisModel.ID.ToString());
            writer.WriteAttributeString("Name", Name);
            if (thisModel.SelectedTagIndex < 0)
            {
                //"Not Selected" is the string it writes
                writer.WriteAttributeString("SelectedTagText", @"""Not Selected""");
            }
            else
            {
                writer.WriteAttributeString("SelectedTagText", TagList[thisModel.SelectedTagIndex].Text);
            }

            writer.WriteEndElement();
        }

        
        public void ReadXml(XElement Category)
        {
            //Setting readFromXml to true so that certain events won't fire when unneeded.
            readFromXml = true;
            Label lb = new Label();
            string text;
            text = Category.Attribute("Name").Value;
            text += " : " + Category.Attribute("SelectedTagText").Value;
            lb.Content = text;
            thisView = lb;
        }

        private void thisView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SizeChanged(this, e);
        }

        private void thisModel_LoadComplete(object sender, EventArgs e)
        {
            PropertyChanged(this, new PropertyChangedEventArgs("TagViewList"));
            thisView.SizeChanged += new SizeChangedEventHandler(thisView_SizeChanged);

            //Only fire LoadComplete event if readFromXml is false. The assumption is that ReadXml will only be run during issuevoting/viewpeerreview pages (where we don't want the events to run)
            if (readFromXml == false)
            {
                LoadComplete(this, EventArgs.Empty);
            }
        }

        public FrameworkElement GetView()
        {
            return thisView;
        }
    }
}