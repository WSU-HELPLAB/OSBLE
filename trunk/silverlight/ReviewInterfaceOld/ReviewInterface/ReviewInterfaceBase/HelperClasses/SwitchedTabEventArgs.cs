using System;
using System.Windows.Controls;

namespace ReviewInterfaceBase.HelperClasses
{
    public delegate void SwitchedTabEventHandler(object sender, SwitchedTabEventArgs e);

    public class SwitchedTabEventArgs : EventArgs
    {
        private TabItem oldTab;

        public TabItem OldTab
        {
            get { return oldTab; }
        }

        private TabItem newTab;

        public TabItem NewTab
        {
            get { return newTab; }
        }

        public SwitchedTabEventArgs(TabItem oldTab, TabItem newTab)
        {
            this.oldTab = oldTab;
            this.newTab = newTab;
        }
    }
}