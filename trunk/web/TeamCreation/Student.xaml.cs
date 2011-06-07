using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Media;

namespace TeamCreation
{
    public partial class Student : UserControl, INotifyPropertyChanged
    {
        public int id { get; set; }

        public string firstName { get; set; }

        public string lastName { get; set; }

        private bool _assignmentSubmitted;

        public bool assignmentSubmitted
        {
            get
            {
                return _assignmentSubmitted;
            }

            set
            {
                _assignmentSubmitted = value;
                OnPropertyChanged("color1");
                OnPropertyChanged("color2");
                OnPropertyChanged("textColor");
            }
        }

        public Color color1
        {
            get
            {
                if (assignmentSubmitted)
                {
                    return blue1;
                }
                else
                {
                    return white1;
                }
            }
        }

        public Color color2
        {
            get
            {
                if (assignmentSubmitted)
                {
                    return blue2;
                }
                else
                {
                    return white2;
                }
            }
        }

        public string textColor
        {
            get
            {
                if (assignmentSubmitted)
                {
                    return _textColor1.ToString();
                }
                else
                {
                    return _textColor2.ToString();
                }
            }
        }

        private Color blue1, blue2, white1, white2, _textColor1, _textColor2;

        private LinearGradientBrush blueGradient;
        private LinearGradientBrush whiteGradient;

        public LinearGradientBrush background
        {
            get
            {
                if (assignmentSubmitted)
                {
                    return blueGradient;
                }
                else
                {
                    return whiteGradient;
                }
            }
        }

        public Student()
        {
            blue1 = Color.FromArgb(0xFF, 0x4A, 0x59, 0x88);
            blue2 = Color.FromArgb(0xFF, 0x24, 0x27, 0x33);

            white1 = Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);
            white2 = Color.FromArgb(0xFF, 0x99, 0x99, 0x99);

            _textColor1 = Color.FromArgb(0xFF, 0xF5, 0xF4, 0xE9);
            _textColor2 = Color.FromArgb(0xFF, 0x22, 0x22, 0x22);

            // Required to initialize variables
            InitializeComponent();

            id = 0;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #region Methods

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion Methods
    }

    public class StudentCollection : List<Student>
    {
        public StudentCollection() { }
    }
}