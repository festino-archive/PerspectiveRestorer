using PerspectiveInversion;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace PerspectiveRestorer
{
    struct CropInfo
    {
        public int Scale;
        public double ScaleFactor;
        public Point MinCorner;
        public BitmapImage ImageOrig;
        public CroppedBitmap ImageCropped;

        public CropInfo(int scale, double scaleFactor, Point minCorner, BitmapImage image, CroppedBitmap cropped)
        {
            Scale = scale;
            ScaleFactor = scaleFactor;
            MinCorner = minCorner;
            ImageOrig = image;
            ImageCropped = cropped;
        }

        public PointD ImageToCropBM(PointD p)
        {
            return new PointD(p.X - MinCorner.X, p.Y - MinCorner.Y);
        }

        public PointD CropBMToImage(PointD p)
        {
            return new PointD(MinCorner.X + p.X, MinCorner.Y + p.Y);
        }

        public PointD ImageToCrop(PointD p)
        {
            return new PointD((p.X - MinCorner.X) / ScaleFactor, (p.Y - MinCorner.Y) / ScaleFactor);
        }

        public PointD CropToImage(PointD p)
        {
            return new PointD(MinCorner.X + p.X * ScaleFactor, MinCorner.Y + p.Y * ScaleFactor);
        }

        public PointD ScopeToCropBM(Point p, Image actualImage)
        {
            return new PointD(p.X / actualImage.ActualWidth * ImageCropped.PixelWidth,
                              p.Y / actualImage.ActualHeight * ImageCropped.PixelHeight);
        }

        public Point CropBMToScope(PointD p, Image actualImage)
        {
            return new Point(p.X / ImageCropped.PixelWidth * actualImage.ActualWidth,
                              p.Y / ImageCropped.PixelHeight * actualImage.ActualHeight);
        }
    }
}
