using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PerspectiveRestorer
{
    public class ParamManager
    {
        public readonly List<Param> ParamList = new List<Param>();
        public readonly List<Param> ResList = new List<Param>();
        public ListBox FaceBox;

        private readonly Panel paramPanel, resultPanel;

        public ParamManager(Panel paramPanel, Panel resultPanel)
        {
            this.paramPanel = paramPanel;
            this.resultPanel = resultPanel;
        }

        public void Init()
        {
            AddParam("hor_fov", 70);
            AddParam("1", new double[,] { { 238.5 }, { 587.5 } }, new string[] { "x", "y" });
            AddParam("2", new double[,] { { 422.5 }, { 505 } }, new string[] { "x", "y" });
            AddParam("3", new double[,] { { 152 }, { 345.4 } }, new string[] { "x", "y" });
            AddParam("4", new double[,] { { 379 }, { 325.5 } }, new string[] { "x", "y" });

            AddParam("1->2 length", 1);
            AddParam("1->3 length", 1);
            AddParam("block", 3, 1, new string[] { "x", "y", "z" });

            FaceBox = new ListBox() { VerticalAlignment = VerticalAlignment.Top };
            FaceBox.Items.Add("-x");
            FaceBox.Items.Add("+x");
            FaceBox.Items.Add("-y");
            FaceBox.Items.Add("+y");
            FaceBox.Items.Add("-z");
            FaceBox.Items.Add("+z");
            FaceBox.SelectedIndex = 5;
            paramPanel.Children.Add(FaceBox);

            AddRes(new Param("C", 3, 1));
            AddRes(new Param("R", 3, 3));
            AddRes(new Param("Angles", 3, 1, new string[] { "yaw", "pitch", "roll" }));
        }

        public string GetParamName(Visual element)
        {
            if (element == FaceBox)
                return "face";
            foreach (Param param in ParamList)
            {
                //if (element == param.Holder)
                //    return param.Name;
                Point relative = element.TransformToVisual(param.Holder).Transform(new Point(0, 0));
                if (0 <= relative.X && relative.X <= param.Holder.ActualWidth
                        && 0 <= relative.Y && relative.Y <= param.Holder.ActualHeight)
                    return param.Name;
            }
            return null;
        }
        public void SetSelection(string name, bool selected)
        {
            foreach (Param param in ParamList)
                if (param.Name == name)
                    SetSelection(param.Holder, param.Values.GetLength(0), selected);
        }
        private void SetSelection(Grid grid, int rowSpan, bool selected)
        {
            Border oldBorder = null;
            foreach (UIElement elem in grid.Children)
                if (elem is Border)
                {
                    oldBorder = elem as Border;
                    break;
                }
            if (oldBorder == null && selected)
            {
                Border border = new Border()
                {
                    Margin = new Thickness(-2, 7, 8, 6),
                    BorderThickness = new Thickness(1),
                    VerticalAlignment = VerticalAlignment.Stretch,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    BorderBrush = Brushes.Gray
                };
                Grid.SetRowSpan(border, rowSpan);
                grid.Children.Add(border);
            }
            if (oldBorder != null && !selected)
                grid.Children.Remove(oldBorder);
        }

        public void SetParam(string name, double[,] value, int precDigits)
        {
            double prec = Math.Pow(10, precDigits);
            for (int i = 0; i < value.GetLength(0); i++)
                for (int j = 0; j < value.GetLength(1); j++)
                    value[i, j] = Math.Round(value[i, j] * prec) / prec;
            foreach (Param param in ParamList)
                if (param.Name == name)
                    param.Set(value);
        }
        public double[,] GetParam(string name)
        {
            foreach (Param param in ParamList)
                if (param.Name == name)
                    return param.Values;
            return null;
        }
        public double[] GetFace()
        {
            string item = (string)FaceBox.SelectedItem;
            double[] dir = new double[] { 0, 0, 1 };
            if (item == "-z")
                dir = new double[] { 0, 0, -1 };
            if (item == "+x")
                dir = new double[] { 1, 0, 0 };
            if (item == "-x")
                dir = new double[] { -1, 0, 0 };
            if (item == "+y")
                dir = new double[] { 0, 1, 0 };
            if (item == "-y")
                dir = new double[] { 0, -1, 0 };
            return dir;
        }
        public double[,] GetFaceTrans()
        {
            string item = (string)FaceBox.SelectedItem;
            double[,] trans = new double[,] { { 1, 0, 0 }, { 0, 1, 0 }, { 0, 0, 1 } };
            if (item == "-z")
                trans = new double[,] { { -1, 0, 0 }, { 0, 1, 0 }, { 0, 0, -1 } };
            if (item == "+x")
                trans = new double[,] { { 0, 0, -1 }, { 0, 1, 0 }, { 1, 0, 0 } };
            if (item == "-x")
                trans = new double[,] { { 0, 0, 1 }, { 0, 1, 0 }, { -1, 0, 0 } };
            if (item == "+y")
                trans = new double[,] { { 1, 0, 0 }, { 0, 0, 1 }, { 0, -1, 0 } };
            if (item == "-y")
                trans = new double[,] { { 1, 0, 0 }, { 0, 0, -1 }, { 0, 1, 0 } };
            return trans;
        }

        public void SetRes(string name, double[,] values)
        {
            foreach (Param param in ResList)
                if (param.Name == name)
                    param.Set(values);
        }

        public void AddParam(Param param)
        {
            paramPanel.Children.Add(param.Holder);
            ParamList.Add(param);
        }
        public void AddParam(string name, double val, string[] labels = null)
        {
            AddParam(new Param(name, val, labels));
        }
        public void AddParam(string name, double[,] val, string[] labels = null)
        {
            Param param = new Param(name, val.GetLength(0), val.GetLength(1), labels);
            AddParam(param);
            param.Set(val);
        }
        public void AddParam(string name, int rows, int columns, string[] labels = null)
        {
            AddParam(new Param(name, rows, columns, labels));
        }

        public void AddRes(Param param)
        {
            resultPanel.Children.Add(param.Holder);
            ResList.Add(param);
        }
    }
}
