using System;
using System.Windows.Controls;
using System.Windows.Input;

using ReviewInterfaceBase.ViewModel.Document.ChemProVDocument;

namespace ReviewInterfaceBase.View.Document
{
    public partial class ChemProVDocumentView : UserControl, IDocumentView
    {
        private ChemProVDocumentViewModel thisVM;

        public ChemProVDocumentView(ChemProVDocumentViewModel chemProVDocumentViewModel)
        {
            InitializeComponent();

            if (chemProVDocumentViewModel == null)
            {
                throw new ArgumentNullException("chemProVDocumentViewModel");
            }

            thisVM = chemProVDocumentViewModel;
        }

        private void ChemProVLayer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            thisVM.ChemProVLayer_MouseLeftButtonDown(sender, e);
        }

        private void ChemProVLayer_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            thisVM.ChemProVLayer_MouseLeftButtonUp(sender, e);
        }
    }
}