using System.Windows.Controls;
using System.Windows.Data;

namespace ReviewInterfaceBase.View.Tag
{
    public class TagView : ComboBoxItem
    {
        public TagView()
        {
            Binding contentBinding = new Binding("Text");
            contentBinding.Mode = BindingMode.TwoWay;
            this.SetBinding(ComboBoxItem.ContentProperty, contentBinding);
        }
    }
}