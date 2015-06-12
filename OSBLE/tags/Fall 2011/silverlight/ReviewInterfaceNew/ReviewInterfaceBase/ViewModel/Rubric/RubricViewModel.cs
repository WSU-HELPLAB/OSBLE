using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using ReviewInterfaceBase.Model.Rubric;
using ReviewInterfaceBase.View.Rubric;

namespace ReviewInterfaceBase.ViewModel.Rubric
{
    public class RubricViewModel
    {
        public event SizeChangedEventHandler SizeChanged = delegate { };

        private RubricView thisView = new RubricView();

        private RubicModel thisModel = new RubicModel();

        private CustomDataGridViewModel customDataGrid = new CustomDataGridViewModel();

        public int HighestScore
        {
            get { return thisModel.HighestScore; }
            set { thisModel.HighestScore = value; }
        }

        public RubricViewModel()
        {
            customDataGrid.HideBordersForLastRow = false;
            customDataGrid.HideBordersForLastTwoColumns = false;

            customDataGrid.SizeChanged += new SizeChangedEventHandler(customDataGrid_SizeChanged);

            initilizeHeader();

            thisView.LayoutRoot.Children.Add(customDataGrid.GetView());

            thisModel.Criterions.CollectionChanged += new NotifyCollectionChangedEventHandler(criterion_CollectionChanged);

            thisModel.Criterions.Add(new CriterionModel("welcome_screen", 5, new List<string>(new string[] { "Function contains a major implementation error.", "Function is implemented mostly correctly; a minor error exists.", "Function is implemented correctly, and may even exhibit elegance in places.", })));
            thisModel.Criterions.Add(new CriterionModel("initialize_game_board()", 5, new List<string>(new string[] { "Function contains a major implementation error.", "Function is implemented mostly correctly; a minor error exists.", "Function is implemented correctly, and may even exhibit elegance in places.", })));
            thisModel.Criterions.Add(new CriterionModel("select_who_starts_first", 2, new List<string>(new string[] { "Function contains a major implementation error.", "Function is implemented mostly correctly; a minor error exists.", "Function is implemented correctly, and may even exhibit elegance in places.", })));
            thisModel.Criterions.Add(new CriterionModel("randomly_place_ships_on_board()", 15, new List<string>(new string[] { "Function contains a major implementation error.", "Function is implemented mostly correctly; a minor error exists.", "Function is implemented correctly, and may even exhibit elegance in places.", })));
            thisModel.Criterions.Add(new CriterionModel("check_shot", 10, new List<string>(new string[] { "Function contains a major implementation error.", "Function is implemented mostly correctly; a minor error exists.", "Function is implemented correctly, and may even exhibit elegance in places.", })));
            thisModel.Criterions.Add(new CriterionModel("is_winner", 10, new List<string>(new string[] { "Function contains a major implementation error.", "Function is implemented mostly correctly; a minor error exists.", "Function is implemented correctly, and may even exhibit elegance in places.", })));
            thisModel.Criterions.Add(new CriterionModel(" update_board", 10, new List<string>(new string[] { "Function contains a major implementation error.", "Function is implemented mostly correctly; a minor error exists.", "Function is implemented correctly, and may even exhibit elegance in places.", })));
            thisModel.Criterions.Add(new CriterionModel("display_board", 10, new List<string>(new string[] { "Function contains a major implementation error.", "Function is implemented mostly correctly; a minor error exists.", "Function is implemented correctly, and may even exhibit elegance in places.", })));
            thisModel.Criterions.Add(new CriterionModel("output_current_move", 10, new List<string>(new string[] { "Function contains a major implementation error.", "Function is implemented mostly correctly; a minor error exists.", "Function is implemented correctly, and may even exhibit elegance in places.", })));
            thisModel.Criterions.Add(new CriterionModel("check_if_sunk_ship", 5, new List<string>(new string[] { "Function contains a major implementation error.", "Function is implemented mostly correctly; a minor error exists.", "Function is implemented correctly, and may even exhibit elegance in places.", })));
            thisModel.Criterions.Add(new CriterionModel("output_stats", 5, new List<string>(new string[] { "Function contains a major implementation error.", "Function is implemented mostly correctly; a minor error exists.", "Function is implemented correctly, and may even exhibit elegance in places.", })));
            thisModel.Criterions.Add(new CriterionModel("Dcoumentation", 10, new List<string>(new string[] { "Three or more items in “Mastering” list have a minor deficiency,\n or one or more items in “Mastering” list have a major deficiciency.", "One or two items in “Mastering” list have a minor deficiency.", "•	Header block of documentation at top of file\n•	Each function has a header block of documentation\n•   Each logical step is documented\n•	Proper indentation and spacing are used", })));
        }

        private void customDataGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SizeChanged(this, e);
        }

        private void criterion_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                for (int i = 0; i < e.NewItems.Count; i++)
                {
                    addCriterionToView(e.NewStartingIndex + i);
                }
            }
            else
            {
                customDataGrid.ClearAll();

                initilizeHeader();

                for (int i = 0; i < thisModel.Criterions.Count; i++)
                {
                    addCriterionToView(i);
                }
            }
        }

        public RubricView GetView()
        {
            return thisView;
        }

        private void initilizeHeader()
        {
            int column = 0;
            Thickness margin = new Thickness(2);

            TextBlock tb = new TextBlock() { Text = thisModel.Header.Description };
            tb.Margin = margin;
            customDataGrid.PlaceUIElement(tb, column, 0);
            column++;

            tb = new TextBlock() { Text = thisModel.Header.CriterionWeight };
            tb.Margin = margin;
            customDataGrid.PlaceUIElement(tb, column, 0);
            column++;

            foreach (string s in thisModel.Header.LevelDescriptions)
            {
                tb = new TextBlock() { Text = s };
                tb.Margin = margin;
                customDataGrid.PlaceUIElement(tb, column, 0);
                column++;
            }

            tb = new TextBlock() { Text = thisModel.Header.Score };
            tb.Margin = margin;
            customDataGrid.PlaceUIElement(tb, column, 0);
            column++;

            tb = new TextBlock() { Text = thisModel.Header.Comment };
            tb.Margin = margin;
            customDataGrid.PlaceUIElement(tb, column, 0);
            column++;
        }

        private void addCriterionToView(int row)
        {
            int column = 0;
            Thickness margin = new Thickness(2);
            //row +1 when placing UIElement because we need to account for the header row

            TextBlock tb = new TextBlock() { Text = thisModel.Criterions[row].Description };
            tb.Margin = margin;
            customDataGrid.PlaceUIElement(tb, column, row + 1);
            column++;

            tb = new TextBlock() { Text = thisModel.Criterions[row].CriterionWeight };
            tb.Margin = margin;
            customDataGrid.PlaceUIElement(tb, column, row + 1);
            column++;

            foreach (string s in thisModel.Criterions[row].LevelDescription)
            {
                tb = new TextBlock() { Text = s };
                tb.Margin = margin;
                customDataGrid.PlaceUIElement(tb, column, row + 1);
                column++;
            }

            ComboBox cb = new ComboBox();
            cb.Margin = margin;
            //less than or equal because 0 works and the number itself works
            for (int i = 0; i <= thisModel.HighestScore; i++)
            {
                cb.Items.Add(new ComboBoxItem() { Content = i.ToString() });
            }

            Binding binding = new Binding("Score");

            binding.Mode = BindingMode.TwoWay;

            binding.Source = thisModel.Criterions[row];

            cb.SetBinding(ComboBox.SelectedIndexProperty, binding);

            customDataGrid.PlaceUIElement(cb, column, row + 1);
            column++;

            TextBox textBox = new TextBox();

            binding = new Binding("Comment");

            binding.Mode = BindingMode.TwoWay;

            binding.Source = thisModel.Criterions[row];

            textBox.MinWidth = 150;
            textBox.MaxWidth = 150;
            textBox.TextWrapping = TextWrapping.Wrap;
            textBox.Margin = margin;
            textBox.SetBinding(TextBox.TextProperty, binding);

            customDataGrid.PlaceUIElement(textBox, column, row + 1);
            column++;
        }
    }
}