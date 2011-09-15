using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using OSBLE.Services;

namespace SilverlightSandbox
{
    public partial class MainPage : UserControl
    {
        public MainPage()
        {
            InitializeComponent();
            Loaded += new RoutedEventHandler(MainPage_Loaded);
        }

        void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            RubricRiaContext context = new RubricRiaContext();
            dummyDataGrid.ItemsSource = context.AbstractCourses;
            context.Load(context.GetCoursesQuery());
        }
    }
}
