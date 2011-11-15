﻿using System;
using System.Windows.Controls;

namespace TeamCreation
{
    public partial class AddButton : UserControl
    {
        public event EventHandler AddTeamRequested = delegate { };

        public MainPage parent;

        public AddButton()
        {
            // Required to initialize variables
            InitializeComponent();
        }

        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            AddTeamRequested(this, EventArgs.Empty);
        }
    }
}