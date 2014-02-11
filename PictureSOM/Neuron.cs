using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace PictureSOM
{
    public class Neuron
    {
        private float[] _Weights;
        private int _X;
        private int _Y; // position in neuron lattice
        private ArrayList _imageDataList;
        
        public Neuron(int x, int y)
        {
            this._X = x;
            this._Y = y;
            _Weights = new float[SOMConstants.NUM_WEIGHTS];
            _imageDataList = new ArrayList();
        }

        #region Properties
        public float[] Weights
        {
            get
            {
                return _Weights;
            }

            set
            {
                for (int i = 0; i < value.Length; i++)
                {
                    _Weights[i] = value[i];
                }
            }
        }

        public int X 
        { 
            get { return _X; } 
        }

        public int Y 
        { 
            get 
            { 
                return _Y; 
            } 
        }

        public ArrayList ImageDataList
        {
            get 
            {
                return _imageDataList;
            }
        }
        #endregion Properties

        public void AddImage(ImageData imageData)
        {
            _imageDataList.Add(imageData);
        }

        public int GetImageCount()
        {
            return _imageDataList.Count;
        }

        public string[] GetImageFileNames()
        {
            string[] result = new string[_imageDataList.Count];

            for (int i = 0; i < result.Length; i++)
            {
                ImageData tmpImageData = (ImageData)_imageDataList[i];
                result[i] = tmpImageData.m_fileName;
            }

            return result;
        }

        public float CalculateDistanceSquared(InputVector inputVec)
        { 
            float distance = 0.0f;

            for ( int i = 0; i < inputVec.weights.Length; ++i ) {
                distance += (inputVec.weights[ i ] - this._Weights[ i ]) *
                    (inputVec.weights[ i ] - this._Weights[ i ]);
            }

            return distance;
        }

        /// <summary>
        /// Calculates the new weight for this node.
        /// Equation: W(t+1) = W(t) + THETA(t)*L(t)*(V(t) - W(t)) 
        /// </summary>
        /// <param name="targetVector"></param>
        /// <param name="learningRate"></param>
        /// <param name="influence"></param>
        public void AdjustWeights(InputVector targetVector, float learningRate, float influence)
        {
            for (int w = 0; w < targetVector.weights.Length; ++w)
            {
                this._Weights[w] += learningRate * influence * (targetVector.weights[w] - this._Weights[w]);
            }
        }
    }
}
