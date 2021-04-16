
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PerspectiveRestorer
{
    public class Param
    {
        public delegate void OnParamChange(string name, double[,] values);
        public event OnParamChange ParamChanged;

        public string Name { get; }
        public double[,] Values { get; private set; }

        public Grid Holder { get; private set; }

        private TextBlock Title { get; set; }
        private TextBlock[] RowLables { get; set; }
        private TextBox[,] Input { get; set; }

        public Param(string name, int rows, int columns, string[] labels)
        {
            Name = name;
            Values = new double[rows, columns];
            Title = new TextBlock() { Text = name };
            Input = new TextBox[rows, columns];

            int offset = 1;
            if (labels != null)
                offset++;
            Holder = new Grid();
            Holder.Margin = new Thickness(0, 5, 0, 5);
            DockPanel.SetDock(Holder, Dock.Top);
            for (int i = 0; i < rows; i++)
                Holder.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0, GridUnitType.Auto) });
            for (int j = 0; j < offset + columns; j++)
                Holder.ColumnDefinitions.Add(new ColumnDefinition() { MinWidth = 20, Width = new GridLength(0, GridUnitType.Auto) });

            Grid.SetRowSpan(Title, rows);
            Title.VerticalAlignment = VerticalAlignment.Center;
            Title.HorizontalAlignment = HorizontalAlignment.Center;
            Title.Margin = new Thickness(0, 0, 10, 0);
            Holder.Children.Add(Title);

            if (labels != null)
            {
                RowLables = new TextBlock[rows];
                for (int i = 0; i < rows && i < labels.Length; i++)
                {
                    RowLables[i] = new TextBlock() { Text = labels[i] };
                    RowLables[i].Margin = new Thickness(0, 0, 5, 0);
                    Grid.SetRow(RowLables[i], i);
                    Grid.SetColumn(RowLables[i], 1);
                    Holder.Children.Add(RowLables[i]);
                }
            }

            for (int i = 0; i < rows; i++)
                for (int j = 0; j < columns; j++)
                {
                    Input[i, j] = new TextBox() { Text = "0", MinWidth = 30 };
                    Grid.SetRow(Input[i, j], i);
                    Grid.SetColumn(Input[i, j], offset + j);
                    Holder.Children.Add(Input[i, j]);
                    int fixed_i = i;
                    int fixed_j = j;
                    Input[i, j].TextChanged += (a, b) => TrySet(fixed_i, fixed_j, (a as TextBox).Text);
                }
        }
        public Param(string name, int rows, int columns) : this(name, rows, columns, null)
        { }
        public Param(string name, double defaultVal, string[] labels) : this(name, 1, 1, labels)
        {
            Set(new double[,] { { defaultVal } });
        }
        public Param(string name, double defaultVal) : this(name, defaultVal, null)
        { }

        public bool TrySet(int i, int j, string newVal)
        {
            double res;
            if (!double.TryParse(newVal, out res))
            {
                Input[i, j].Foreground = Brushes.Red;
                return false;
            }
            Input[i, j].Foreground = Brushes.Black;
            Values[i, j] = res;
            ParamChanged?.Invoke(Name, Values);
            return true;
        }

        public void Set(double[,] values)
        {
            int rows = Math.Min(values.GetLength(0), Values.GetLength(0));
            int columns = Math.Min(values.GetLength(1), Values.GetLength(1));
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < columns; j++)
                {
                    Values[i, j] = values[i, j];
                    Input[i, j].Text = values[i, j].ToString();
                }
            ParamChanged?.Invoke(Name, Values);
        }
    }
}
