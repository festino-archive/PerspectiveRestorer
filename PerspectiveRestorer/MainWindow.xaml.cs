using Microsoft.Win32;
using PerspectiveInversion;
using System;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MathNet.Numerics.LinearAlgebra;

namespace PerspectiveRestorer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private BitmapImage ImageOrig;
        private CroppedBitmap ImageCropped;
        private readonly double SCALE_FACTOR = 1.6;
        private int Scale = 0;
        private double ScaleFactor = 1;
        private Point minCorner = new Point(0, 0);
        private Point lastMousePos;

        private int SelectedPoint = -1;
        private bool MovingPoint = false;

        private ParamManager paramManager;
        private Solver solver;
        private Drawer drawer;

        CropInfo cropInfo;

        public MainWindow()
        {
            InitializeComponent();
            CultureInfo ci = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentCulture = ci;
            Thread.CurrentThread.CurrentUICulture = ci;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            paramManager = new ParamManager(paramPanel, resultPanel);
            paramManager.Init();
            BindEvents();
            drawer = new Drawer(paramManager, canvas);
        }

        private void BindEvents()
        {
            foreach (Param param in paramManager.ParamList)
                param.ParamChanged += (name, newVal) => Recalc();
            paramManager.FaceBox.SelectionChanged += (a, b) => Recalc();
        }

        private void Recalc()
        {
            if (scopedImage.Source == null)
                return;
            bool validPoints = solver.HasSolution;
            if (validPoints)
                solver.Solve();
            Redraw();
        }

        private void Redraw()
        {
            if (solver != null && solver.HasSolution)
            {
                double a = paramManager.GetParam("1->2 length")[0, 0] / 2;
                double b = paramManager.GetParam("1->3 length")[0, 0] / 2;
                var transMatrix = Matrix<double>.Build.DenseOfArray(paramManager.GetFaceTrans()).Transpose();
                double[][] initPoints = new double[4][];
                initPoints[0] = new double[] { -a, -b, 0 };
                initPoints[1] = new double[] { a, -b, 0 };
                initPoints[2] = new double[] { -a, b, 0 };
                initPoints[3] = new double[] { a, b, 0 };
                for (int i = 0; i < 4; i++)
                    initPoints[i] = (transMatrix * Vector<double>.Build.DenseOfArray(initPoints[i])).ToArray();
                PointD[] testPoints = PerspectiveInversion.Transform.Straight(initPoints, solver.Solution);
                double width = ImageOrig.PixelWidth, height = ImageOrig.PixelHeight;
                double reToImg = width / CameraParameters.GetFovWidth(paramManager.GetParam("hor_fov")[0, 0]);
                for (int i = 0; i < testPoints.Length; i++)
                    testPoints[i] = new PointD(width / 2 + reToImg * testPoints[i].X, height / 2 + reToImg * testPoints[i].Y);

                double distance = 0;
                for (int i = 0; i < 4; i++)
                {
                    double dx = testPoints[i].X - solver.ImagePoints[i].X;
                    double dy = testPoints[i].Y - solver.ImagePoints[i].Y;
                    distance += dx * dx + dy * dy;
                }
                distance = Math.Sqrt(distance);

                if (distance > 0.001)
                    miniLog.Text = "bad res: " + distance.ToString();
                else
                    miniLog.Text = "";
            }
            cropInfo = new CropInfo(Scale, ScaleFactor, minCorner, ImageOrig, ImageCropped);
            drawer?.Redraw(cropInfo,
                    solver, SelectedPoint,
                    planeCheckBox.IsChecked == true, axesCheckBox.IsChecked == true, zCheckBox.IsChecked == true);
        }

        private void SetImage(string path)
        {
            imagePath.Text = path;
            ImageOrig = new BitmapImage(new Uri(path));
            solver = new Solver(paramManager, minecraftCommand, ImageOrig);
            SetCrop(new Point(0, 0), 0);
        }

        private void SetCrop(Point newMinCorner, int newScale)
        {
            if (newScale < 0)
                return;
            int width = ImageOrig.PixelWidth, height = ImageOrig.PixelHeight;
            double scaleFactor = Math.Pow(SCALE_FACTOR, -newScale);
            double cropWidth = (int)(width * scaleFactor);
            double cropHeight = (int)(height * scaleFactor);
            if (cropWidth < 5 || cropHeight < 5)
                return;

            Scale = newScale;
            ScaleFactor = scaleFactor;
            scaleTextBlock.Text = (100 / scaleFactor).ToString("f0") + '%';

            minCorner = newMinCorner;
            if (minCorner.X < 0)
                minCorner.X = 0;
            if (minCorner.Y < 0)
                minCorner.Y = 0;
            if (minCorner.X + cropWidth > width)
                minCorner.X = width - cropWidth;
            if (minCorner.Y + cropHeight > height)
                minCorner.Y = height - cropHeight;
            Int32Rect crop = new Int32Rect((int)minCorner.X, (int)minCorner.Y, (int)cropWidth, (int)cropHeight); // rework to floats
            ImageCropped = new CroppedBitmap(ImageOrig, crop);
            scopedImage.Source = ImageCropped;
            Redraw();
        }

        private void OpenImageButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog op = new OpenFileDialog();
            op.Title = "Select a picture";
            op.Filter = "All supported graphics|*.jpg;*.jpeg;*.png|" +
              "JPEG (*.jpg;*.jpeg)|*.jpg;*.jpeg|" +
              "Portable Network Graphic (*.png)|*.png";
            if (op.ShowDialog() == true)
            {
                SetImage(op.FileName);
            }
        }

        private void scopedImage_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            int scale = Scale;
            if (e.Delta >= 0)
                scale++;
            else
                scale--;
            if (scale < 0)
                return;

            int width = ImageOrig.PixelWidth, height = ImageOrig.PixelHeight;
            Point onImage = e.GetPosition(scopedImage);
            Point normalized = new Point(onImage.X / scopedImage.ActualWidth, onImage.Y / scopedImage.ActualHeight);
            Point shift = new Point(normalized.X * width * ScaleFactor, normalized.Y * height * ScaleFactor);
            Point fullShift = new Point(minCorner.X + shift.X, minCorner.Y + shift.Y);

            double scaleFactor = Math.Pow(SCALE_FACTOR, -scale);
            shift = new Point(normalized.X * width * scaleFactor, normalized.Y * height * scaleFactor);
            Point newMinCorner = new Point(fullShift.X - shift.X, fullShift.Y - shift.Y);
            SetCrop(newMinCorner, scale);
        }

        private void scopedImage_MouseMove(object sender, MouseEventArgs e)
        {
            Point curPos = e.GetPosition(scopedImage);
            if (!e.LeftButton.HasFlag(MouseButtonState.Pressed))
            {
                lastMousePos = curPos;
                return;
            }
            if (MovingPoint)
            {
                MoveSelected(curPos);
            }
            else
            {
                int width = ImageOrig.PixelWidth, height = ImageOrig.PixelHeight;
                double dx = curPos.X - lastMousePos.X;
                double dy = curPos.Y - lastMousePos.Y;
                lastMousePos = curPos;
                double dxImg = dx / scopedImage.ActualWidth * width * ScaleFactor;
                double dyImg = dy / scopedImage.ActualHeight * height * ScaleFactor;
                Point newMinCorner = new Point(minCorner.X - dxImg, minCorner.Y - dyImg);
                SetCrop(newMinCorner, Scale);
            }
        }
        private void MoveSelected(Point canvasCursor)
        {
            PointD cropped = cropInfo.ScopeToCropBM(canvasCursor, scopedImage);
            PointD image = cropInfo.CropBMToImage(cropped);
            paramManager.SetParam((SelectedPoint + 1).ToString(),
                new double[,] { { image.X }, { image.Y } }, 6);
        }

        private void scopedImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            MovingPoint = false;
            Point cursor = e.GetPosition(scopedImage);
            int point = FindNearestPoint(cursor, 8);
            if (point >= 0)
            {
                SetSelected(point);
                MovingPoint = true;
            }
            else if (SelectedPoint >= 0)
            {
                MoveSelected(cursor);
                MovingPoint = true;
            }
        }
        private void scopedImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            MovingPoint = false;
        }
        private void scopedImage_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            SetSelected(-1);
            /*int point = FindNearestPoint(e.GetPosition(scopedImage), 5);
            SetSelected(point);
            if (point >= 0)
            //    remove point*/
        }

        private int FindNearestPoint(Point cursor, double radius)
        {
            double minDist2 = double.MaxValue;
            int nearestPoint = -1;
            for (int i = 0; i < 4; i++)
            {
                PointD cropPoint = cropInfo.ImageToCropBM(solver.ImagePoints[i]);
                Point scopePoint = cropInfo.CropBMToScope(cropPoint, scopedImage);
                double dx = cursor.X - scopePoint.X;
                double dy = cursor.Y - scopePoint.Y;
                double dist2 = dx * dx + dy * dy;
                if (minDist2 > dist2)
                {
                    minDist2 = dist2;
                    nearestPoint = i;
                }
            }
            miniLog.Text = minDist2.ToString();
            if (minDist2 < radius * radius)
                return nearestPoint;
            return -1;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!(e.OriginalSource is Visual))
                return;
            string paramName = paramManager.GetParamName(e.OriginalSource as Visual);
            for (int i = 1; i <= 4; i++)
                if (paramName == i.ToString())
                {
                    SetSelected(i - 1);
                    break;
                }
        }

        private void SetSelected(int point)
        {
            if (SelectedPoint >= 0)
                paramManager.SetSelection((SelectedPoint + 1).ToString(), false);
            SelectedPoint = point;
            if (SelectedPoint >= 0)
                paramManager.SetSelection((SelectedPoint + 1).ToString(), true);
            Redraw();
        }

        private void canvasCheckBox_Changed(object sender, RoutedEventArgs args)
        {
            Redraw();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Redraw();
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            Redraw();
        }
    }
}
