using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
//using AForge.Imaging.Filters;
//using ScottClayton.CAPTCHA.Utility;

namespace BTCOnline
{
    class Segmenter
    {
        public Bitmap Image { get; set; }
        private static Random random = new Random();

        public void Resize(int width, int height)
        {
            try
            {
                var brush = new SolidBrush(Color.White);

                var bmp = new Bitmap((int)width, (int)height);
                var graph = Graphics.FromImage(bmp);
                float scale = Math.Min(width / Image.Width, height / Image.Height);
                // uncomment for higher quality output
                //graph.InterpolationMode = InterpolationMode.High;
                //graph.CompositingQuality = CompositingQuality.HighQuality;
                //graph.SmoothingMode = SmoothingMode.AntiAlias;

                var scaleWidth = (int)(Image.Width * scale);
                var scaleHeight = (int)(Image.Height * scale);

                graph.FillRectangle(brush, new RectangleF(0, 0, width, height));
                graph.DrawImage(Image, new Rectangle(((int)width - scaleWidth) / 2, ((int)height - scaleHeight) / 2, scaleWidth, scaleHeight));

                Image = bmp;

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error resizing an Image.", ex);
            }
        }


        public void ColorFillBlobs(double tolerance, Color background, double backgroundTolerance)
        {
            try
            {
                byte[,][] colors2 = new byte[Image.Width, Image.Height][];

                BitmapData bmData = Image.LockBits(new Rectangle(0, 0, Image.Width, Image.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
                int stride = bmData.Stride;
                IntPtr Scan0 = bmData.Scan0;

                bool[,] alreadyFilled = new bool[Image.Width, Image.Height];

                unsafe
                {
                    byte* p = (byte*)(void*)Scan0;
                    int nOffset = stride - Image.Width * 3;

                    for (int y = 0; y < Image.Height; ++y)
                    {
                        for (int x = 0; x < Image.Width; ++x)
                        {
                            // Store in BGR order
                            colors2[x, y] = new byte[] { p[0], p[1], p[2] };
                            p += 3;
                        }
                        p += nOffset;
                    }
                }

                Image.UnlockBits(bmData);

                int similarNeighborPixels;
                int pixelRadius = 1;

                for (int x = pixelRadius; x < Image.Width - pixelRadius; x++)
                {
                    for (int y = pixelRadius; y < Image.Height - pixelRadius; y++)
                    {
                        if (!alreadyFilled[x, y])
                        {
                            if (colors2[x, y].GetEDeltaColorDifference(background) > backgroundTolerance)
                            {
                                similarNeighborPixels = 0;
                                for (int xv = -pixelRadius; xv <= pixelRadius; xv++)
                                {
                                    for (int yv = -pixelRadius; yv <= pixelRadius; yv++)
                                    {
                                        if (yv != 0 || xv != 0)
                                        {
                                            if (colors2[x, y].GetEDeltaColorDifference(colors2[x + xv, y + yv]) < tolerance)
                                            {
                                                similarNeighborPixels++;
                                            }
                                        }
                                    }
                                }

                                if (similarNeighborPixels >= ((pixelRadius * 2 + 1) * (pixelRadius * 2 + 1)) - 1)
                                {
                                    FloodFill(new Point(x, y), (int)tolerance, ref alreadyFilled);
                                }
                            }
                            else
                            {
                                Image.SetPixel(x, y, background);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error trying to fill blobs in an image with random colors.", ex);
            }
        }

        public Color FloodFill(Point origin, int tolerance)
        {
            Color fill = Color.FromArgb(random.Next(20, 225), random.Next(20, 225), random.Next(20, 225));
            FloodFill(origin, tolerance, fill);
            return fill;
        }
        private Color FloodFill(Point origin, int tolerance, ref bool[,] filledSquares)
        {
            Color fill = Color.FromArgb(random.Next(20, 225), random.Next(20, 225), random.Next(20, 225));
            FloodFill(origin, tolerance, fill, ref filledSquares);
            return fill;
        }
        public void FloodFill(Point origin, int tolerance, Color fillColor)
        {
            try
            {
                bool[,] filledSquares = new bool[Image.Width, Image.Height];
                FloodFill(origin, tolerance, fillColor, ref filledSquares);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error trying to flood fill from a point on an image.", ex);
            }
        }
        private void FloodFill(Point origin, int tolerance, Color fillColor, ref bool[,] filledSquares)
        {
            Color initialColor = Image.GetPixel(origin.X, origin.Y);
            BitmapData bmpData = Image.LockBits(new Rectangle(0, 0, Image.Width, Image.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            IntPtr Scan0 = bmpData.Scan0;

            unsafe
            {
                byte* scan0 = (byte*)(void*)Scan0;
                int stride = bmpData.Stride;

                bool[,] doneChecking = new bool[Image.Width, Image.Height];
                Queue<Point> nextPoints = new Queue<Point>();

                // Fill the initial pixel
                FloodFillPoint(scan0, stride, origin, Image.Width, Image.Height, initialColor, tolerance, fillColor, doneChecking, nextPoints, ref filledSquares);

                // Fill pixels in the queue until the queue is empty
                while (nextPoints.Count > 0)
                {
                    Point next = nextPoints.Dequeue();
                    FloodFillPoint(scan0, stride, next, Image.Width, Image.Height, initialColor, tolerance, fillColor, doneChecking, nextPoints, ref filledSquares);
                }
            }

            Image.UnlockBits(bmpData);
        }

        private unsafe int GetDifference(Color c, byte* b, int index)
        {
            return (int)Math.Max(Math.Max(Math.Abs(b[index + 0] - c.B), Math.Abs(b[index + 1] - c.G)), Math.Abs(b[index + 2] - c.R));
        }

        private unsafe bool FloodFillPoint(byte* p, int stride, Point origin, int imageW, int imageH, Color startColor, int tolerance,
            Color fillColor, bool[,] doneChecking, Queue<Point> nextPoints, ref bool[,] floodFilled, bool fakeFill = false)
        {
            int ind = origin.Y * stride + origin.X * 4; // TODO: make sure all index operations multiply by 4 and not 3!

            if (!doneChecking[origin.X, origin.Y] && GetDifference(startColor, p, ind) <= tolerance)
            {
                // Mark this pixel as checked
                doneChecking[origin.X, origin.Y] = true;

                // Fill the color in
                if (!fakeFill)
                {
                    p[ind + 0] = fillColor.B;
                    p[ind + 1] = fillColor.G;
                    p[ind + 2] = fillColor.R;

                    floodFilled[origin.X, origin.Y] = true;
                }

                // Queue up the neighboring 4 pixels
                nextPoints.Enqueue(new Point((origin.X + 1) % imageW, origin.Y));
                nextPoints.Enqueue(new Point((origin.X - 1 + imageW) % imageW, origin.Y));
                nextPoints.Enqueue(new Point(origin.X, (origin.Y + 1) % imageH));
                nextPoints.Enqueue(new Point(origin.X, (origin.Y - 1 + imageH) % imageH));

                return true;
            }

            return false;
        }

        public void Binarize(int threshold)
        {
            try
            {
                BitmapData bmData = Image.LockBits(new Rectangle(0, 0, Image.Width, Image.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
                int stride = bmData.Stride;
                IntPtr Scan0 = bmData.Scan0;

                bool[,] alreadyFilled = new bool[Image.Width, Image.Height];

                unsafe
                {
                    byte* p = (byte*)(void*)Scan0;
                    int nOffset = stride - Image.Width * 3;

                    for (int y = 0; y < Image.Height; ++y)
                    {
                        for (int x = 0; x < Image.Width; ++x)
                        {
                            if ((p[0] * p[1] * p[2]) / (255 * 255) < threshold && (x != 0 && y != 0 && x != Image.Width - 1 && y != Image.Height - 1))
                            {
                                p[0] = 0;
                                p[1] = 0;
                                p[2] = 0;
                            }
                            else
                            {
                                p[0] = 255;
                                p[1] = 255;
                                p[2] = 255;
                            }
                            p += 3;
                        }
                        p += nOffset;
                    }
                }

                Image.UnlockBits(bmData);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public void BlackAndWhite()
        {
            Binarize(254);
        }


    }

    public static class ImageExtensions
    {

        public static double GetEDeltaColorDifference(this byte[] c, Color color)
        {
            // Yep. Very inefficient to create a Color here. TODO: Fix this.
            return GetEDeltaColorDifference(Color.FromArgb(c[2], c[1], c[0]), color);
        }

        public static LAB GetLAB(this Color c)
        {
            return c.GetXYZ().GetLAB();
        }

        public static LAB GetLAB(this XYZ c)
        {
            // Adapted from http://www.easyrgb.com/index.php?X=MATH&H=07#text7

            double var_X = c.X / 95.047;
            double var_Y = c.Y / 100.000;
            double var_Z = c.Z / 108.883;

            if (var_X > 0.008856) var_X = Math.Pow(var_X, (1.0 / 3));
            else var_X = (7.787 * var_X) + (16.0 / 116);
            if (var_Y > 0.008856) var_Y = Math.Pow(var_Y, (1.0 / 3));
            else var_Y = (7.787 * var_Y) + (16.0 / 116);
            if (var_Z > 0.008856) var_Z = Math.Pow(var_Z, (1.0 / 3));
            else var_Z = (7.787 * var_Z) + (16.0 / 116);

            LAB lab = new LAB();
            lab.L = (116 * var_Y) - 16;
            lab.a = 500 * (var_X - var_Y);
            lab.b = 200 * (var_Y - var_Z);

            return lab;
        }

        public static XYZ GetXYZ(this Color c)
        {
            // Adapted from http://www.easyrgb.com/index.php?X=MATH&H=07#text7

            double var_R = (c.R / 255.0);
            double var_G = (c.G / 255.0);
            double var_B = (c.B / 255.0);

            if (var_R > 0.04045) var_R = Math.Pow(((var_R + 0.055) / 1.055), 2.4);
            else var_R = var_R / 12.92;
            if (var_G > 0.04045) var_G = Math.Pow(((var_G + 0.055) / 1.055), 2.4);
            else var_G = var_G / 12.92;
            if (var_B > 0.04045) var_B = Math.Pow(((var_B + 0.055) / 1.055), 2.4);
            else var_B = var_B / 12.92;

            var_R = var_R * 100;
            var_G = var_G * 100;
            var_B = var_B * 100;

            XYZ xyz = new XYZ();
            xyz.X = var_R * 0.4124 + var_G * 0.3576 + var_B * 0.1805;
            xyz.Y = var_R * 0.2126 + var_G * 0.7152 + var_B * 0.0722;
            xyz.Z = var_R * 0.0193 + var_G * 0.1192 + var_B * 0.9505;

            return xyz;
        }

        /// <summary>
        /// A color difference algorithm to get the difference in visible color, and not just the integer difference in RGB values.
        /// NOTE: The JND (Just Noticible Difference) between two colors is about 2.3.
        /// </summary>
        public static double GetEDeltaColorDifference(this Color color, byte r, byte g, byte b)
        {
            return GetEDeltaColorDifference(Color.FromArgb(r, g, b), color);
        }

        /// <summary>
        /// A color difference algorithm to get the difference in visible color, and not just the integer difference in RGB values.
        /// NOTE: The JND (Just Noticible Difference) between two colors is about 2.3.
        /// </summary>
        public static double GetEDeltaColorDifference(this byte[] c, byte[] other)
        {
            // Yep. Very inefficient to create a Color here. TODO: Fix this.
            return GetEDeltaColorDifference(Color.FromArgb(c[2], c[1], c[0]), Color.FromArgb(other[2], other[1], other[0]));
        }

        /// <summary>
        /// A color difference algorithm to get the difference in visible color, and not just the integer difference in RGB values.
        /// NOTE: The JND (Just Noticible Difference) between two colors is about 2.3.
        /// </summary>
        public static double GetEDeltaColorDifference(this Color c, Color color)
        {
            LAB a = c.GetLAB();
            LAB b = color.GetLAB();

            return Math.Sqrt(Math.Pow(a.L - b.L, 2) + Math.Pow(a.a - b.a, 2) + Math.Pow(a.b - b.b, 2));
        }
    }

    public struct LAB // : IColor<LAB>
    {
        public double L { get; set; }
        public double a { get; set; }
        public double b { get; set; }
    }

    /// <summary>
    /// The XYZ color space
    /// http://www.easyrgb.com/index.php?X=MATH&H=07#text7
    /// </summary>
    public struct XYZ
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
    }

}
