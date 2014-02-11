using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace PictureSOM
{
    public class Features
    {
        public struct rgbColor
        {
            public float r, g, b;
        }

        /* This vector represents the lights/darks of an image. Convert each pixel to gray-
         * scale (0-255). Create X bins, and divide each pixel into the bin with like-pixels.
         * Increment the count (numPixelsInBin) of each bin every time a pixel is added. Normalize
         * this value, dividing by the total # of pixels. This gives a unique X element vector
         * for this image describing its' lights/darks.
         */
        public static float[] calculateInputVector_Histogram(rgbColor[,] imgArray, int imgHeight, int imgWidth)
        {
            float[] grayArray = new float[imgArray.Length];
            float maxNumInBucket = (float)(imgHeight * imgWidth);     // Used to normalize this vector.

            // Convert each pixel to a single gray-scale value.
            for (int i = 0; i < imgHeight; ++i)
            {
                for (int j = 0; j < imgWidth; ++j)
                {
                    grayArray[(i * imgWidth) + j] = (imgArray[i, j].r * .333333f) +
                                                      (imgArray[i, j].g * .333333f) +
                                                      (imgArray[i, j].b * .333333f);
                }
            }

            int numberOfBins = SOMConstants.INPUT_VECTOR_SIZE_HISTOGRAM;
            float[] resultArray = new float[numberOfBins];

            for (int i = 0; i < grayArray.Length; ++i)
            {
                int binNumber = (int)grayArray[i] / numberOfBins;
                ++resultArray[binNumber];
            }

            // Normalize.
            for (int i = 0; i < resultArray.Length; ++i)
            {
                resultArray[i] /= maxNumInBucket;
            }

            return resultArray;
        }

        /* Chop the image into X regions. Calculate the average RGB values for each region (1
         * RGB value per region). This gives you X * 3 values describing the color arrangement
         * of this image.
         */
        public static float[] calculateInputVector_Area(rgbColor[,] imgArray, int imgHeight, int imgWidth)
        {
            float[] result = new float[SOMConstants.INPUT_VECTOR_SIZE_AREA];

            int pixPerRegionHigh = imgHeight / SOMConstants.INPUT_VECTOR_AREA_REGIONS_HIGH;
            int pixPerRegionWide = imgWidth / SOMConstants.INPUT_VECTOR_AREA_REGIONS_WIDE;

            /* A 9 region image is traversed as such:
             * 
             *      0 | 1 | 2
             *      ---------
             *      3 | 4 | 5
             *      ---------
             *      6 | 7 | 8
             */

            // Traverse through all regions.
            for (int rHigh = 0; rHigh < SOMConstants.INPUT_VECTOR_AREA_REGIONS_HIGH; ++rHigh)
            {
                for (int rWide = 0; rWide < SOMConstants.INPUT_VECTOR_AREA_REGIONS_WIDE; ++rWide)
                {

                    float totalPixels = 0.0f;
                    rgbColor totalColor;
                    totalColor.r = totalColor.g = totalColor.b = 0.0f;

                    for (int i = rHigh * pixPerRegionHigh; i < (rHigh + 1) * pixPerRegionHigh; ++i)
                    {
                        for (int j = rWide * pixPerRegionWide; j < (rWide + 1) * pixPerRegionWide; ++j)
                        {

                            totalColor.r += imgArray[i, j].r;
                            totalColor.g += imgArray[i, j].g;
                            totalColor.b += imgArray[i, j].b;
                            ++totalPixels;

                        } // End for each pixel across.
                    } // End for each pixel down.

                    // Calculate the average.
                    totalColor.r /= totalPixels;
                    totalColor.g /= totalPixels;
                    totalColor.b /= totalPixels;

                    // Store the averages (separate rgb values).
                    result[3 * ((rHigh * SOMConstants.INPUT_VECTOR_AREA_REGIONS_WIDE) + rWide)] = totalColor.r;
                    result[3 * ((rHigh * SOMConstants.INPUT_VECTOR_AREA_REGIONS_WIDE) + rWide) + 1] = totalColor.g;
                    result[3 * ((rHigh * SOMConstants.INPUT_VECTOR_AREA_REGIONS_WIDE) + rWide) + 2] = totalColor.b;

                } // End for each region across.
            } // End for each region down.

            // Normalize to 0.0 to 1.0 range;
            for (int i = 0; i < result.Length; ++i)
            {
                result[i] /= 255.0f;
            }

            return result;
        }
        
        /*
        public static float[] calculateInputVector_SURFHist(Bitmap img)
        {
            // Resize and create integral image
            int width = 300;
            int height = (300 * img.Height) / img.Width;
            IntegralImage iimg;
            Console.WriteLine(width.ToString() + "x" + height.ToString()); // Debug
            using (Bitmap resized = new Bitmap(width, height))
            using(Graphics g = Graphics.FromImage(resized))
            {
                g.DrawImage(img, 0, 0, width, height);
                iimg = IntegralImage.FromImage(resized);
            }
            
            
            // Extract the interest points
            var ipts = FastHessian.getIpoints(0.0002f, 5, 2, iimg);
            
            // Describe the interest points
            SurfDescriptor.DecribeInterestPoints(ipts, false, false, iimg);

            // Create global represesntation by histogram
            int n = ipts.First().descriptorLength;
            float[] resultArray = new float[n];
            System.Threading.Tasks.Parallel.ForEach(ipts, currentElement =>
            {
                for (int i = 0; i < n; i++)
                {
                    resultArray[i] += currentElement.descriptor[i];
                }
            });

            // Debug
            Console.WriteLine(ipts.Count.ToString());
            foreach (float f in resultArray)
            {
                Console.Write(f.ToString() + " ");
            }
            Console.WriteLine();

            return resultArray;
        }
        */

        /// <summary>
        /// Citation : S. Α. Chatzichristofis and Y. S. Boutalis, 
        /// “CEDD: COLOR AND EDGE DIRECTIVITY DESCRIPTOR - A COMPACT DESCRIPTOR FOR IMAGE INDEXING AND RETRIEVAL.” 
        /// « 6th International Conference in advanced research on Computer Vision Systems ICVS 2008 », 
        /// May 12 to May 15, 2008, Santorini, Greece
        /// </summary>
        /// <param name="img"></param>
        /// <returns></returns>
        public static float[] calculateCEDD(Bitmap img)
        {
            float[] result = new float[144];
            CEDD_Descriptor.CEDD cedd = new CEDD_Descriptor.CEDD();
            result = convertDoublesToFloats(cedd.Apply(img));

            /*/ Debug 
            foreach (float c in result) Console.Write(c + " ");
            Console.WriteLine("\n\n");
            */
            return result;
        }

        /// <summary>
        /// Citation : Savvas A. Chatzichristofis and Yiannis S. Boutalis
        /// FCTH: FUZZY COLOR AND TEXTURE HISTOGRAM. A LOW LEVEL FEATURE FOR ACCURATE IMAGE RETRIEVAL
        /// « 9th International Workshop on Image Analysis for Multimedia Interactive Services »,
        /// 2008, Xanthi, Greece
        /// </summary>
        /// <param name="img"></param>
        /// <returns></returns>
        public static float[] calculateFCTH(Bitmap img)
        {
            float[] result = new float[192];
            FCTH_Descriptor.FCTH fcth = new FCTH_Descriptor.FCTH();
            result = convertDoublesToFloats(fcth.Apply(img, 2));

            /*/ Debug 
            foreach (float c in result) Console.Write(c + " ");
            Console.WriteLine("\n\n");
            */
            return result;
        }

        public static float[] calculateJCD(float[] CEDD, float[] FCTH)
        {
            float[] JointDescriptor = new float[168];

            float[] TempTable1 = new float[24];
            float[] TempTable2 = new float[24];
            float[] TempTable3 = new float[24];
            float[] TempTable4 = new float[24];

            for (int i = 0; i < 24; i++)
            {
                TempTable1[i] = FCTH[0 + i] + FCTH[96 + i];
                TempTable2[i] = FCTH[24 + i] + FCTH[120 + i];
                TempTable3[i] = FCTH[48 + i] + FCTH[144 + i];
                TempTable4[i] = FCTH[72 + i] + FCTH[168 + i];
            }

            // 

            for (int i = 0; i < 24; i++)
            {
                JointDescriptor[i] = (TempTable1[i] + CEDD[i]) / 2; //ok
                JointDescriptor[24 + i] = (TempTable2[i] + CEDD[48 + i]) / 2; //ok
                JointDescriptor[48 + i] = CEDD[96 + i]; //ok
                JointDescriptor[72 + i] = (TempTable3[i] + CEDD[72 + i]) / 2;//ok
                JointDescriptor[96 + i] = CEDD[120 + i]; //ok
                JointDescriptor[120 + i] = TempTable4[i];//ok
                JointDescriptor[144 + i] = CEDD[24 + i];//ok
            }
            
            /*/ Debug 
            foreach (float c in JointDescriptor) Console.Write(c + " ");
            Console.WriteLine("\n\n");
             */
            for (int i = 0; i < JointDescriptor.Length; i++ )
            {
                JointDescriptor[i] /= 8;
            }

            return (JointDescriptor);
        }

        private static float[] convertDoublesToFloats(double[] input)
        {
            if (input == null)
            {
                return null; // Or throw an exception - your choice
            }
            float[] output = new float[input.Length];
            for (int i = 0; i < input.Length; i++)
            {
                output[i] = (float)input[i];
            }
            return output;
        }
    }
}
