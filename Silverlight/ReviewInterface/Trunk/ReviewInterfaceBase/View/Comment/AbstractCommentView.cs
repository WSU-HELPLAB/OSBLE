using System.Windows.Controls;

namespace ReviewInterfaceBase.View.Comment
{
    /// <summary>
    /// The idea is that this should be an AbstractClass but because it inherits from UserControl and used in .xmal is cannot be abstract
    /// do NOT create an instance of this class (except through xmal)
    /// </summary>
    public class AbstractCommentView : UserControl
    {
    }
}