using System.Windows;
using System.Windows.Controls;
using Behaviors;

namespace ReviewInterfaceBase.ViewModel.Behaviors
{
    public class ResizeUserControlBorder : BorderSizeableBehavior
    {
        private UserControl mainObject;

        public ResizeUserControlBorder()
            : base()
        {
        }

        protected override Point GetObjectLocation()
        {
            GetObjectToMove();
            return new Point((double)mainObject.GetValue(Canvas.LeftProperty), (double)mainObject.GetValue(Canvas.TopProperty));
        }

        protected override FrameworkElement GetObjectToMove()
        {
            if (mainObject == null)
            {
                mainObject = this.AssociatedObject.Parent as UserControl;
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
            mainObject.SetValue(Canvas.LeftProperty, postion);
        }

        protected override void SetTopPosition(double postion)
        {
            GetObjectToMove();
            mainObject.SetValue(Canvas.TopProperty, postion);
        }
    }
}