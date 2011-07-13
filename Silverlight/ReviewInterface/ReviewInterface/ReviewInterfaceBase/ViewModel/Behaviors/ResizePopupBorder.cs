using System.Windows;
using System.Windows.Controls.Primitives;

using Behaviors;

namespace ReviewInterfaceBase.ViewModel.Behaviors
{
    public class ResizePopupBorder : BorderSizeableBehavior
    {
        private Popup mainObject;

        public ResizePopupBorder()
            : base()
        {
        }

        protected override Point GetObjectLocation()
        {
            GetObjectToMove();
            return new Point(mainObject.HorizontalOffset, mainObject.VerticalOffset);
        }

        protected override FrameworkElement GetObjectToMove()
        {
            if (mainObject == null)
            {
                mainObject = this.AssociatedObject.Parent as Popup;
            }
            return mainObject;
        }

        protected override UIElement GetTopMostContainer()
        {
            GetObjectToMove();
            return (mainObject.Parent as UIElement);
        }

        protected override void SetLeftPosition(double postion)
        {
            GetObjectToMove();
            mainObject.HorizontalOffset = postion;
        }

        protected override void SetTopPosition(double postion)
        {
            GetObjectToMove();
            mainObject.VerticalOffset = postion;
        }
    }
}