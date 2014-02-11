using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

namespace PictureSOM
{
    public class SOMHelper
    {
        public static Random rand = new Random();

        public static float Random_Float_OneToZero() 
        {
            double tmpRand = rand.NextDouble();
            return (float)tmpRand;
        }
        
        public static int Random_Int() 
        { 
            return rand.Next(); 
        }
        
        public static int Random_Int(int maxVal) 
        { 
            return rand.Next(maxVal); 
        }
        
        public static int Random_Int(int minVal, int maxVal) 
        { 
            return rand.Next(minVal, maxVal); 
        }

        public static byte Convert_FloatToByte(float color)
        {
            byte result = (byte)(color * 255.0);

            // Guard against a reported (but not comfirmed) roundoff bug.
            if (result < 0) result = 0;
            if (result > 255) result = 255;

            return result;
        }

        public static ImageData GetImageData_Old(Bitmap img)
        {
            ImageData result = new ImageData();
            BitmapData imgData = img.LockBits( new Rectangle( 0, 0, img.Width, img.Height ), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb );
            Features.rgbColor[,] imgArray = new Features.rgbColor[imgData.Height, imgData.Width];

            unsafe
            {
                byte* imgPtr = (byte*)(imgData.Scan0);

                for (int i = 0; i < imgData.Height; ++i)
                {
                    for (int j = 0; j < imgData.Width; ++j)
                    {
                        imgArray[i, j].b = (float)*imgPtr;
                        ++imgPtr;
                        imgArray[i, j].g = (int)*imgPtr;
                        ++imgPtr;
                        imgArray[i, j].r = (int)*imgPtr;
                        ++imgPtr;
                    }
                    imgPtr += imgData.Stride - imgData.Width * 3;
                }
            } // End unsafe code.

            // Needed or else RELEASE build will crash! eep!
            img.UnlockBits(imgData);

            float[] histogramVector, areaVector;
            histogramVector = result.m_vectorHistogram = Features.calculateInputVector_Histogram(imgArray, imgData.Height, imgData.Width);
            areaVector = result.m_vectorArea = Features.calculateInputVector_Area(imgArray, imgData.Height, imgData.Width);
            
            System.Diagnostics.Debug.Assert(histogramVector.Length + areaVector.Length == SOMConstants.NUM_WEIGHTS);

            // Pack all vectors into the single vector.
            for (int i = 0, j = 0; i < SOMConstants.NUM_WEIGHTS; ++i)
            {
                if (i < histogramVector.Length)
                {
                    result.inputVector.weights[i] = histogramVector[i];
                }
                else if (j < areaVector.Length)
                {
                    result.inputVector.weights[i] = areaVector[j];
                    ++j;
                }
            }

            return result;
        }

        public static Bitmap Resize(Bitmap img, int width=100, int height=100)
        {
            Bitmap result = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage((System.Drawing.Image)result))
                g.DrawImage(img, 0, 0, width, height);
            return result;
        }

        public static ImageData GetImageData(Bitmap img)
        {
            ImageData imgData = new ImageData();
            using (Bitmap resized = Resize(img))
            {
                float[] cedd = Features.calculateCEDD(img); // size 144
                float[] fcth = Features.calculateFCTH(img); // size 192
                float[] jcd = Features.calculateJCD(cedd, fcth);    // size 168

                imgData.CEDD = cedd;
                imgData.FCTH = fcth;
                imgData.JCD = jcd;

                // Change the feature to be used in SOM here. 
                // Important!!! Also need to adject SOM constants in SOMSetting.cs (especially NUM_WEIGHTS)
                imgData.inputVector.weights = jcd;
            }
            return imgData;
        }

        /// <summary>
        /// Used in recognition
        /// </summary>
        /// <returns></returns>
        public static InputVector GetInputVector(Bitmap img)
        {
            ImageData imgData = GetImageData(img);
            return imgData.inputVector;
        }

        public static float[] Add(float[] w1, float[] w2)
        {
            System.Diagnostics.Debug.Assert(w1.Length == w2.Length, "Trying to add 2 weights of unequal length!");

            float[] result = new float[w1.Length];
            for (int i = 0; i < result.Length; ++i)
                result[i] = w1[i] * w2[i];
            return result;
        }

        public static float Calculate_EuclideanDistanceSq(float[] p1, float[] p2)
        {
            System.Diagnostics.Debug.Assert(p1.Length == p2.Length, "Trying to compute distance of 2 unequal element vectors!");

            float distance = 0.0f;

            for (int i = 0; i < p1.Length; ++i)
            {
                distance += (p1[i] - p2[i]) *
                    (p1[i] - p2[i]);
            }

            return distance;
        }

        public static float Calculate_EuclideanDistance(float[] p1, float[] p2)
        {
            System.Diagnostics.Debug.Assert(p1.Length == p2.Length, "Trying to compute distance of 2 unequal element vectors!");

            float distance = 0.0f;

            for (int i = 0; i < p1.Length; ++i)
            {
                distance += (p1[i] - p2[i]) *
                    (p1[i] - p2[i]);
            }

            return (float)Math.Sqrt(distance);
        }

        public static float Calculate_TanimotoCoefficient(float[] p1, float[] p2)
        {
            System.Diagnostics.Debug.Assert(p1.Length == p2.Length, "Trying to compute distance of 2 unequal element vectors!");
            float result = 0;
            float tmp1 = 0;
            float tmp2 = 0;

            float T1 = 0, T2 = 0, T3 = 0;

            for (int i = 0; i < p1.Length; i++)
            {
                tmp1 += p1[i];
                tmp2 += p2[i];
            }

            if (tmp1 == 0 || tmp2 == 0) result = 100;
            if (tmp1 == 0 && tmp2 == 0) result = 0;

            if (tmp1 > 0 && tmp2 > 0)
            {
                for (int i = 0; i < p1.Length; i++)
                {
                    T1 += (p1[i] / tmp1) * (p2[i] / tmp2);
                    T2 += (p2[i] / tmp2) * (p2[i] / tmp2);
                    T3 += (p1[i] / tmp1) * (p1[i] / tmp1);
                }
                result = (100 - 100 * (T1 / (T2 + T3 - T1)));
            }

            return (result);
        }

        // To bo used as a default metric function
        public static float Calculate_Distance(float[] p1, float[] p2)
        {
            return Calculate_TanimotoCoefficient(p1, p2);
        }

        public static string ToString(float[] vec)
        {
            string str = "";
            foreach (float f in vec)
            {
                str += f.ToString() + " ";
            }

            return str;
        }
    }
}
