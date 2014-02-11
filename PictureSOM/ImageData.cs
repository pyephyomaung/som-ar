using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace PictureSOM
{
    public class ImageData
    {
        public string m_fileName;
        public string m_directoryName;
        public string m_fullName { get { return (m_directoryName + @"\" + m_fileName); } }
        public Point m_BMU;   // "Most recent" BMU index.   < 0 if no associated node.
        public float[] m_vectorHistogram;
        public float[] m_vectorArea;
        public InputVector inputVector;
        public int MapTo3DModelID = -1;

        // ccd features
        public float[] CEDD;
        public float[] FCTH;
        public float[] JCD;

        // experimental
        public int numGotPickup = 0;

        public ImageData()
        {
            m_vectorHistogram = new float[SOMConstants.INPUT_VECTOR_SIZE_HISTOGRAM];
            m_vectorArea = new float[SOMConstants.INPUT_VECTOR_SIZE_AREA];

            // ccd feature initialization
            CEDD = new float[144];
            FCTH = new float[192];
            JCD = new float[168];

            inputVector = new InputVector();
            m_BMU.X = -1; m_BMU.Y = -1;
        }
    }

    public class InputVector
    {
        public float[] weights;

        public InputVector()
        {
            weights = new float[SOMConstants.NUM_WEIGHTS];
        }

        public static InputVector operator +(InputVector v1, InputVector v2)
        {
            InputVector result = new InputVector();

            for (int i = 0; i < SOMConstants.NUM_WEIGHTS; ++i)
            {
                result.weights[i] = v1.weights[i] + v2.weights[i];
            }

            return result;
        }
    }
}
