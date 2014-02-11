using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;
using System.Drawing;

using OpenCvSharp;

namespace AndyPyeAugmentedRealitySystem
{
    public class OpenCVSharpHelper
    {
        /// <summary>
        /// Detect the square in the image using contours
        /// </summary>
        /// <param name="img">Image</param>
        /// <param name="modifiedImg">Modified image to be return</param>
        /// <param name="storage">Memory storage</param>
        /// <returns></returns>
        public static CvPoint[] DetectSquares(IplImage img)
        {
            // Debug
            //System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
            //stopWatch.Start();

            using (CvMemStorage storage = new CvMemStorage())
            {
                // create empty sequence that will contain points -
                // 4 points per square (the square's vertices)
                CvSeq<CvPoint> squares = new CvSeq<CvPoint>(SeqType.Zero, CvSeq.SizeOf, storage);

                using (IplImage timg = img.Clone())
                using (IplImage gray = new IplImage(timg.Size, BitDepth.U8, 1))
                using (IplImage dstCanny = new IplImage(timg.Size, BitDepth.U8, 1))
                {
                    // Get gray scale
                    timg.CvtColor(gray, ColorConversion.BgrToGray);

                    // Canny
                    Cv.Canny(gray, dstCanny, 70, 300);

                    // dilate canny output to remove potential
                    // holes between edge segments 
                    Cv.Dilate(dstCanny, dstCanny, null, 2);

                    // find contours and store them all as a list
                    CvSeq<CvPoint> contours;
                    dstCanny.FindContours(storage, out contours);

                    // Debug
                    //Cv.ShowImage("Edge", dstCanny);
                    //if (contours != null) Console.WriteLine(contours.Count());
                    
                    // Test each contour
                    while (contours != null)
                    {
                        // Debug
                        //if (stopWatch.ElapsedMilliseconds > 100)
                        //{
                        //    Console.WriteLine("ROI detection is taking too long and is skipped.");
                        //}

                        // approximate contour with accuracy proportional
                        // to the contour perimeter
                        CvSeq<CvPoint> result = Cv.ApproxPoly(contours, CvContour.SizeOf, storage, ApproxPolyMethod.DP, contours.ContourPerimeter() * 0.02, false);

                        // square contours should have 4 vertices after approximation
                        // relatively large area (to filter out noisy contours)
                        // and be convex.
                        // Note: absolute value of an area is used because
                        // area may be positive or negative - in accordance with the
                        // contour orientation
                        if (result.Total == 4 &&
                            Math.Abs(result.ContourArea(CvSlice.WholeSeq)) > 250 &&
                            result.CheckContourConvexity())
                        {
                            double s = 0;

                            for (int i = 0; i < 5; i++)
                            {
                                // find minimum Angle between joint
                                // edges (maximum of cosine)
                                if (i >= 2)
                                {
                                    double t = Math.Abs(Angle(result[i].Value, result[i - 2].Value, result[i - 1].Value));
                                    s = s > t ? s : t;
                                }
                            }

                            // if cosines of all angles are small
                            // (all angles are ~90 degree) then write quandrange
                            // vertices to resultant sequence 
                            if (s < 0.3)
                            {
                                //Console.WriteLine("ROI found!");  // Debug
                                for (int i = 0; i < 4; i++)
                                {
                                    //Console.WriteLine(result[i]);
                                    squares.Push(result[i].Value);
                                }
                            }
                        }


                        // Take the next contour
                        contours = contours.HNext;
                    }
                }
                //stopWatch.Stop();
                //Console.WriteLine("ROI Detection : {0} ms", stopWatch.ElapsedMilliseconds); // Debug
                return squares.ToArray();
            }
        }



        /// <summary>
        /// helper function:
        /// finds a cosine of Angle between vectors
        /// from pt0->pt1 and from pt0->pt2 
        /// </summary>
        /// <param name="pt1"></param>
        /// <param name="pt2"></param>
        /// <param name="pt0"></param>
        /// <returns></returns>
        public static double Angle(CvPoint pt1, CvPoint pt2, CvPoint pt0)
        {
            double dx1 = pt1.X - pt0.X;
            double dy1 = pt1.Y - pt0.Y;
            double dx2 = pt2.X - pt0.X;
            double dy2 = pt2.Y - pt0.Y;
            return (dx1 * dx2 + dy1 * dy2) / Math.Sqrt((dx1 * dx1 + dy1 * dy1) * (dx2 * dx2 + dy2 * dy2) + 1e-10);
        }

        /// <summary>
        /// the function draws all the squares in the image
        /// </summary>
        /// <param name="img"></param>
        /// <param name="squares"></param>
        public static void DrawSquares(IplImage img, CvPoint[] squares)
        {
            // read 4 sequence elements at a time (all vertices of a square)
            for (int i = 0; i < squares.Length; i += 4)
            {
                CvPoint[] pt = new CvPoint[4];

                // read 4 vertices
                pt[0] = squares[i + 0];
                pt[1] = squares[i + 1];
                pt[2] = squares[i + 2];
                pt[3] = squares[i + 3];

                // draw the square as a closed polyline
                Cv.DrawCircle(img, pt[0], 10, CvColor.Red, 3);
                Cv.PolyLine(img, new CvPoint[][] { pt }, true, CvColor.Orange, 3, LineType.AntiAlias, 0);
            }
        }

        public static void DrawROI(IplImage img, CvRect roi, CvPoint[] pts)
        {
            //Cv.DrawRect(img, roi, CvColor.Gray, Cv.FILLED);
            
            /*/ Detect the color of a pixel to blend (hack for content-aware fill)
            int x = roi.BottomRight.X + 2;
            int y = roi.BottomRight.Y + 2;
            CvScalar color = Cv.Get2D(img, x, y);
            Console.WriteLine(color.Val0.ToString() + " " + color.Val1.ToString() + " " + color.Val2.ToString() + " " + color.Val3.ToString());
            Cv.DrawCircle(img, new CvPoint(x, y), 2, CvColor.Red, 2);
            */
            //CvScalar color = new CvColor(142, 128, 107);
            Cv.DrawPolyLine(img, new CvPoint[][] {pts}, true, CvColor.Orange);
        }

        /// <summary>
        /// Delete a GDI object
        /// </summary>
        /// <param name="o">The poniter to the GDI object to be deleted</param>
        /// <returns></returns>
        [DllImport("gdi32")]
        private static extern int DeleteObject(IntPtr o);

        /// <summary>
        /// Convert an IImage to a WPF BitmapSource. The result can be used in the Set Property of Image.Source
        /// </summary>
        /// <param name="image">OpevnCV Image</param>
        /// <returns>The equivalent BitmapSource</returns>
        public static BitmapSource ToBitmapSource(IplImage image)
        {
            using (Bitmap source = (Bitmap)image.ToBitmap().Clone())
            {
                IntPtr ptr = source.GetHbitmap(); //obtain the Hbitmap
                BitmapSource bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    ptr,
                    IntPtr.Zero,
                    System.Windows.Int32Rect.Empty,
                    System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());

                DeleteObject(ptr); //release the HBitmap
                return bs;
            }
        }

        public static IplImage GetTrainingExample(System.Drawing.Size size, string fileName)
        {
            IplImage src = new IplImage(fileName);
            IplImage dst = new IplImage(src.Size, src.Depth, src.NChannels);
            Cv.Resize(src, dst, Interpolation.Linear);
            src.Dispose();
            return dst;
        }

        public static IplImage DrawHist(float[] hist, int scaleX = 1, int scaleY = 1)
        {
            int histMax = 0;
            histMax = (int)hist.Max();
            IplImage imgHist = new IplImage((int)(256 * scaleX), (int)(64 * scaleY), BitDepth.U8, 3);
            imgHist.SetZero();
            for (int i = 0; i < 255; i++)
            {
                int histValue = (int)hist[i];
                int nextValue = (int)hist[i + 1];

                CvPoint pt1 = new CvPoint(i * scaleX, 64 * scaleY);
                CvPoint pt2 = new CvPoint(i * scaleX + scaleX, 64 * scaleY);
                CvPoint pt3 = new CvPoint(i * scaleX + scaleX, (64 - nextValue * 64 / histMax) * scaleY);
                CvPoint pt4 = new CvPoint(i * scaleX, (64 - histValue * 64 / histMax) * scaleY);
                int numPts = 5;
                CvPoint[] pts = { pt1, pt2, pt3, pt4, pt1 };
                CvInvoke.cvFillConvexPoly(imgHist.CvPtr, pts, numPts, CvColor.Red, LineType.Link8, 0);
            }

            return imgHist;
        }

        /// <summary>
        /// Corrects gamma
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        /// <param name="gamma"></param>
        public static void CorrectGamma(CvArr src, CvArr dst, double gamma)
        {
            byte[] lut = new byte[256];
            for (int i = 0; i < lut.Length; i++)
            {
                lut[i] = (byte)(Math.Pow(i / 255.0, 1.0 / gamma) * 255.0);
            }

            Cv.LUT(src, dst, lut);
        }

    }
}
