using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using OpenCvSharp;

namespace AndyPyeAugmentedRealitySystem
{
    public class OpenCVSharpCam
    {
        private CvCapture capture;
        private IplImage imageFrame;
        private bool captureDisposed = true;

        public IplImage getFrame()
        {
            InitializeCapture();
            if(imageFrame != null) imageFrame.Dispose();
            imageFrame = capture.QueryFrame(); 
            return imageFrame;
        }

        public void DisposeCapture()
        {
            if(!captureDisposed) capture.Dispose();
            captureDisposed = true;
        }

        public bool captureIsDisposed()
        {
            return captureDisposed;
        }

        public void InitializeCapture()
        {
            if (captureDisposed)
            {
                try
                {
                    capture = new CvCapture(0);
                    captureDisposed = false;
                }
                catch (Exception e)
                {
                    Console.Out.WriteLine(e.ToString());
                }  
            }
        }
    }
}
