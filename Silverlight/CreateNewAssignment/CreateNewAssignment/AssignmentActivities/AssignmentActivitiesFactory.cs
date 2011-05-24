using System;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CreateNewAssignment.AssignmentActivities
{
    public enum Activities
    {
        [ImageLocation("Icons/submit-01.png")]
        Submission,

        [ImageLocation("Icons/peer-01.png")]
        PeerReview,

        [ImageLocation("Icons/voting-01.png")]
        IssueVoting,

        [ImageLocation("Icons/rebuttal-01.png")]
        AuthorRebuttal,

        [ImageLocation("Icons/stop.png")]
        Stop,

        [ImageLocation(null)]
        Null
    }

    public class ImageLocation : Attribute
    {
        private string location;

        public string Location
        {
            get { return location; }
            set { location = value; }
        }

        public override string ToString()
        {
            return location;
        }

        public ImageLocation(string imageLocation)
        {
            location = imageLocation;
        }
    }

    public static class AssignmentActivitiesFactory
    {
        public static ImageSource GetImageSourceFromActivities(Activities activity)
        {
            if (activity == Activities.Null)
            {
                return null;
            }

            Type type = activity.GetType();

            FieldInfo fi = type.GetField(activity.ToString());

            ImageLocation[] attrs = (fi.GetCustomAttributes(typeof(ImageLocation), false) as ImageLocation[]);

            if (attrs.Length > 0 && attrs[0] is ImageLocation)
            {
                return new BitmapImage(new Uri(attrs[0].Location, UriKind.Relative));
            }
            else
            {
                throw new Exception("Activities must be decorated with an ImageLocation");
            }
        }

        public static Activities GetActivitiesFromImage(Image image)
        {
            string imageSource = (image.Source as BitmapImage).UriSource.OriginalString;

            for (int i = 0; Enum.IsDefined(typeof(Activities), i); i++)
            {
                Activities activity = (Activities)i;

                Type type = activity.GetType();

                FieldInfo fi = type.GetField(activity.ToString());

                ImageLocation[] attrs = (fi.GetCustomAttributes(typeof(ImageLocation), false) as ImageLocation[]);

                if (attrs.Length > 0 && attrs[0] is ImageLocation)
                {
                    if (imageSource == attrs[0].Location)
                    {
                        return activity;
                    }
                }
                else
                {
                    throw new Exception("Activities must be decorated with an ImageLocation");
                }
            }
            return Activities.Null;
        }

        public static Brush GetColor(Activities activity)
        {
            switch (activity)
            {
                case Activities.Submission: return new SolidColorBrush(Color.FromArgb(255, 51, 153, 51));
                case Activities.PeerReview: return new SolidColorBrush(Color.FromArgb(255, 51, 102, 204));
                case Activities.IssueVoting: return new SolidColorBrush(Color.FromArgb(255, 102, 204, 204));
                case Activities.AuthorRebuttal: return new SolidColorBrush(Color.FromArgb(255, 255, 204, 51));
                case Activities.Stop: return new SolidColorBrush(Colors.Transparent);
                default: return new SolidColorBrush(Colors.Black);
            }
        }

        public static Activities[] AllowedPreceedActivities(Activities activity)
        {
            switch (activity)
            {
                case Activities.Submission: return new Activities[] { Activities.Submission, Activities.PeerReview, Activities.IssueVoting, Activities.AuthorRebuttal, Activities.Stop, };
                case Activities.PeerReview: return new Activities[] { Activities.Submission, Activities.PeerReview, Activities.AuthorRebuttal };
                case Activities.IssueVoting: return new Activities[] { Activities.PeerReview };
                case Activities.AuthorRebuttal: return new Activities[] { Activities.PeerReview, Activities.IssueVoting };
                case Activities.Stop: return new Activities[] { Activities.Submission, Activities.PeerReview, Activities.IssueVoting, Activities.AuthorRebuttal };
                default: return new Activities[] { Activities.Submission, Activities.PeerReview, Activities.IssueVoting, Activities.AuthorRebuttal, Activities.Stop, Activities.Null };
            }
        }
    }
}