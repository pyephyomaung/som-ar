using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Collections;

namespace PictureSOM
{
    public class SOM
    {
        private AppSettings _appSettings;
        private Neuron[,] _competitionLayer;
        private NET_STATE _netState;
        private TRAINING_PHASE _trainingPhase;
        private int _currentIteration;
        private float _neighborhoodRadius;
        private float _timeConstant_P1;
        private float _learningRate_P1, _learningRate_P2;
        private float _totalMapError;
        private bool _isNeuronSelected;
        private Point _selectedNeuronCoords;
        private bool _isTrained;
        private float[, ,] _errorMap;

        #region Properties
        public bool IsTrained
        {
            get
            {
                return _isTrained;
            }
        }

        public bool IsTraining
        {
            get
            {
                if (_netState == NET_STATE.init ||
                _netState == NET_STATE.training ||
                _netState == NET_STATE.finished)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public float TotalMapError
        {
            get
            {
                return _totalMapError;
            }
        }

        public string NetState
        { 
            get
            {
                if (_netState == NET_STATE.finished)
                {
                    return "Network Status: Finished...";
                }
                else if (_netState == NET_STATE.init)
                {
                    return "Network Status: Initializing...";
                }
                else if (_netState == NET_STATE.training)
                {
                    if (_trainingPhase == TRAINING_PHASE.phase_1)
                        return "Network Status: Training... Phase 1: Iteration #" + (_currentIteration - 1).ToString();
                    else
                        return "Network Status: Training... Phase 2: Iteration #" + (_currentIteration - 1).ToString();
                }
                else
                {
                    return "Network Status: Neutral.";
                }
            }
        }

        public Neuron[,] CompetitionLayer
        {
            get
            {
                return _competitionLayer;
            }
        }

        public float[, ,] ErrorMap
        {
            get 
            {
                return _errorMap;
            }
        }

        public int Size
        {
            get 
            {
                return SOMConstants.NUM_NODES_ACROSS;
            }
        }
        #endregion Properties

        public SOM(AppSettings appSettings ) {
            _appSettings = appSettings;
            _netState = NET_STATE.init;
            _trainingPhase = TRAINING_PHASE.phase_1;
            _currentIteration = 1;
            _totalMapError = 0.0f;
            _isNeuronSelected = false;
            _selectedNeuronCoords = new Point( 0, 0 );
            _isTrained = false;

            _timeConstant_P1 = (float)_appSettings.numIterations_P1 / (float)Math.Log( _appSettings.mapRadius );
            _learningRate_P1 = _appSettings.startLearningRate_P1;
            _learningRate_P2 = _appSettings.startLearningRate_P2;

            // Allocate memory.
            _competitionLayer = new Neuron[ SOMConstants.NUM_NODES_DOWN, SOMConstants.NUM_NODES_ACROSS ];
            for ( int i = 0; i < SOMConstants.NUM_NODES_DOWN; ++i ) {
                for ( int j = 0; j < SOMConstants.NUM_NODES_ACROSS; ++j ) {
                    _competitionLayer[ i,j ] = new Neuron( i, j );
                }
            }
            _errorMap = new float[ SOMConstants.NUM_NODES_DOWN, SOMConstants.NUM_NODES_ACROSS, 3 ];

            Run();
        }

        public void StartTraining()
        {
            _netState = NET_STATE.training;
            Run();
        }

        public void StopTraining()
        {
            _netState = NET_STATE.neutral;
        }

        public void Run()
        {
            switch (_netState)
            {
                case NET_STATE.neutral:
                    {
                        break;
                    }
                case NET_STATE.init:
                    {
                        _isTrained = false;
                        Init();

                        _netState = NET_STATE.neutral;
                        break;
                    }
                case NET_STATE.training:
                    {
                        if (Epoch())
                            _netState = NET_STATE.finished;

                        break;
                    }
                case NET_STATE.finished:
                    {
                        _isTrained = true;
                        _totalMapError = CalculateErrorMap();
                        Mapping();

                        _netState = NET_STATE.neutral;
                        break;
                    }
            } // End switch.
        }

        private void Init()
        {
            float[] weight = new float[SOMConstants.NUM_WEIGHTS];

            /************************************************************************/
            /* STEP 1: Each nodes' weights are initialized.                                                           
            /************************************************************************/
            switch (_appSettings.initFill)
            {
                case INIT_FILL.random:
                    {
                        Random rand = new Random();
                        // Initialize all weights to random floats (0.0 to 1.0).
                        for (int i = 0; i < SOMConstants.NUM_NODES_DOWN; ++i)
                        {
                            for (int j = 0; j < SOMConstants.NUM_NODES_ACROSS; ++j)
                            {
                                Neuron currentNeuron = _competitionLayer[i, j];

                                for (int w = 0; w < SOMConstants.NUM_WEIGHTS; ++w)
                                {
                                    weight[w] = SOMHelper.Random_Float_OneToZero();
                                    if (w < 3)
                                        _errorMap[i, j, w] = 0.0f; // Black.
                                }
                                currentNeuron.Weights = weight;
                            } // End for each column.
                        } // End for each row.
                        break;
                    }
                case INIT_FILL.gradient:
                    {
                        for (int i = 0; i < SOMConstants.NUM_NODES_DOWN; ++i)
                        {
                            for (int j = 0; j < SOMConstants.NUM_NODES_ACROSS; ++j)
                            {
                                Neuron currentNeuron = _competitionLayer[i, j];

                                float gradVal = (float)(i + j) / (float)(SOMConstants.NUM_NODES_DOWN + SOMConstants.NUM_NODES_ACROSS - 2);
                                for (int w = 0; w < SOMConstants.NUM_WEIGHTS; w++)
                                {
                                    weight[w] = gradVal;
                                    if (w < 3)
                                        _errorMap[i, j, w] = 0.0f; // Black.
                                }
                                currentNeuron.Weights = weight;
                            } // End for each column.
                        } // End for each row.
                        break;
                    }
            } // End switch.
        }

        int chosen = 0;
        private bool Epoch()
        {
            if (_trainingPhase == TRAINING_PHASE.phase_1 && _currentIteration > _appSettings.numIterations_P1)
            {
                _trainingPhase = TRAINING_PHASE.phase_2;
                _currentIteration = 1;
            }
            else if (_trainingPhase == TRAINING_PHASE.phase_2 && _currentIteration > _appSettings.numIterations_P2)
            {
                return true;
            }

            /************************************************************************/
            /* STEP 2: Choose a random input vector from the set of training data.                                                           
            /************************************************************************/
            int randomNum = SOMHelper.Random_Int(_appSettings.images.Count);
            ImageData thisImage = (ImageData)_appSettings.images[randomNum];
            thisImage.numGotPickup++;
            InputVector inVec = thisImage.inputVector;

            /************************************************************************/
            /* STEP 3: Find the BMU.                                                           
            /************************************************************************/
            Neuron bmu = FindBMU(inVec);

            // Update this image's most recent BMU for later identification.
            thisImage.m_BMU.X = bmu.X;
            thisImage.m_BMU.Y = bmu.Y;

            /************************************************************************/
            /* STEP 4: Calculate the radius of the BMU's neighborhood.                                                           
            /************************************************************************/
            if (_trainingPhase == TRAINING_PHASE.phase_1)
                _neighborhoodRadius = _appSettings.mapRadius * (float)Math.Exp(-(float)_currentIteration / _timeConstant_P1);
            else if (_trainingPhase == TRAINING_PHASE.phase_2)
                _neighborhoodRadius = 1.0f;

            /************************************************************************/
            /* STEP 5: Each neighboring node's weights are adjusted to make them more
             *         like the input vector.                                                       
            /************************************************************************/
            for (int i = 0; i < SOMConstants.NUM_NODES_DOWN; ++i)
            {
                for (int j = 0; j < SOMConstants.NUM_NODES_ACROSS; ++j)
                {
                    float distToNodeSquared = 0.0f;

                    // Get the Euclidean distance (squared) to this node[i,j] from the BMU. Use
                    //  this formula to account for base 0 arrays, and the fact that our neighborhood
                    //  radius is actually HALF of the DRAWING WINDOW.
                    float bmuX = (float)(bmu.X + 1) * _appSettings.nodeHeight;
                    float bmuY = (float)(bmu.Y + 1) * _appSettings.nodeWidth;
                    float nodeX = (float)(_competitionLayer[i, j].Y + 1) * _appSettings.nodeHeight;
                    float nodeY = (float)(_competitionLayer[i, j].Y + 1) * _appSettings.nodeWidth;
                    distToNodeSquared = (bmuX - nodeX) *
                        (bmuX - nodeX) +
                        (bmuY - nodeY) *
                        (bmuY - nodeY);

                    float widthSquared = _neighborhoodRadius * _neighborhoodRadius;

                    // If within the neighborhood radius, adjust this nodes' weights.
                    if (distToNodeSquared < widthSquared)
                    {
                        // Calculate how much it's weights are adjusted.
                        float influence = (float)Math.Exp(-distToNodeSquared / (2.0f * widthSquared));

                        if (_trainingPhase == TRAINING_PHASE.phase_1)
                            _competitionLayer[i, j].AdjustWeights(inVec, _learningRate_P1, influence);
                        else if (_trainingPhase == TRAINING_PHASE.phase_2)
                            _competitionLayer[i, j].AdjustWeights(inVec, _learningRate_P2, influence);
                    }
                } // End for each column.
            } // End for each row.

            // Reduce the learning rate.
            if (_trainingPhase == TRAINING_PHASE.phase_1)
            {
                _learningRate_P1 = _appSettings.startLearningRate_P1 * (float)Math.Exp(-(float)_currentIteration / (float)_appSettings.numIterations_P1);
            }
            else if (_trainingPhase == TRAINING_PHASE.phase_2)
            {
                _learningRate_P2 = _appSettings.startLearningRate_P2 * (float)Math.Exp(-(float)_currentIteration / (float)_appSettings.numIterations_P2);
            }
             ++_currentIteration;

            return false;
        }

        private float CalculateErrorMap()
        {

            // Sum of all the average errors for each node. Can be used to determine a relative
            //  effectiveness of a particular mapping.
            float totalError = 0.0f;
            bool takeSquareRoot = true;
            float numWeightSquareRoot = (float)Math.Sqrt((double)SOMConstants.NUM_WEIGHTS);

            // Find the average of all the node distances (add up the 8 surrounding nodes / 8).
            for (int i = 0; i < SOMConstants.NUM_NODES_DOWN; ++i)
            {
                for (int j = 0; j < SOMConstants.NUM_NODES_ACROSS; ++j)
                {
                    float sumDistance = 0.0f;
                    float[] centerPoint = _competitionLayer[i, j].Weights;
                    int neighborCount = 0;

                    /*      i-1,j-1 | i-1,j | i-1,j+1 
                     *      -------------------------
                     *      i,j-1   |   X   | i,j+1
                     *      -------------------------
                     *      i+1,j-1 | i+1,j | i+1,j+1
                     */

                    // Top row.
                    if (i >= 1)
                    {
                        // Top left.
                        if (j >= 1)
                        {
                            sumDistance = SOMHelper.Calculate_Distance(centerPoint, _competitionLayer[i - 1, j - 1].Weights);
                            ++neighborCount;
                        }

                        // Top middle.
                        sumDistance = SOMHelper.Calculate_Distance(centerPoint, _competitionLayer[i - 1, j].Weights);
                        ++neighborCount;

                        // Top right.
                        if (j < SOMConstants.NUM_NODES_ACROSS - 1)
                        {
                            sumDistance = SOMHelper.Calculate_Distance(centerPoint, _competitionLayer[i - 1, j + 1].Weights);
                            ++neighborCount;
                        }
                    }

                    // Left (1).
                    if (j >= 1)
                    {
                        sumDistance = SOMHelper.Calculate_Distance(centerPoint, _competitionLayer[i, j - 1].Weights);
                        ++neighborCount;
                    }

                    // Right (1).
                    if (j < SOMConstants.NUM_NODES_ACROSS - 1)
                    {
                        sumDistance = SOMHelper.Calculate_Distance(centerPoint, _competitionLayer[i, j + 1].Weights);
                        ++neighborCount;
                    }

                    // Bottom row.
                    if (i < SOMConstants.NUM_NODES_DOWN - 1)
                    {
                        // Bottom left.
                        if (j >= 1)
                        {
                            sumDistance = SOMHelper.Calculate_Distance(centerPoint, _competitionLayer[i + 1, j - 1].Weights);
                            ++neighborCount;
                        }

                        // Bottom middle.
                        sumDistance = SOMHelper.Calculate_Distance(centerPoint, _competitionLayer[i + 1, j].Weights);
                        ++neighborCount;

                        // Bottom right.
                        if (j < SOMConstants.NUM_NODES_ACROSS - 1)
                        {
                            sumDistance = SOMHelper.Calculate_Distance(centerPoint, _competitionLayer[i + 1, j + 1].Weights);
                            ++neighborCount;
                        }
                    }

                    // Compute the average.
                    float averageDistance = sumDistance / (float)neighborCount;
                    totalError += averageDistance;

                    // This produces a nice scale from 0 (black) to 1 (white) for the error map.
                    //   The max distance possible is when a node is 0.0, and all of it's neighbors
                    //   have 1.0 weights. This distance, then, would be NUM_WEIGHTS if no square root
                    //   is taken of the distances, or sqrt( NUM_WEIGHTS ) if the square root is taken.
                    //   The checks for out of bounds shouldn't be necessary, but are left in for extra
                    //   precaution.
                    float scaledDistance;
                    if (takeSquareRoot) { scaledDistance = numWeightSquareRoot * averageDistance; }
                    else { scaledDistance = (float)SOMConstants.NUM_WEIGHTS * averageDistance; }
                    if (scaledDistance > 1.0f) { scaledDistance = 1.0f; }
                    else if (scaledDistance < 0.0f) { scaledDistance = 0.0f; }

                    // Create a greyscale (i.e. r = g = b).
                    _errorMap[i, j, 0] = _errorMap[i, j, 1] = _errorMap[i, j, 2] = scaledDistance;

                } // End for each column.
            } // End for each row.

            return totalError;
        }

        private void Mapping()
        {
            for (int i = 0; i < _appSettings.images.Count; ++i)
            {
                ImageData anImage = (ImageData)_appSettings.images[i];

                // Skip this image if no valid BMU assigned.
                if (anImage.m_BMU.X < 0 || anImage.m_BMU.Y < 0)
                    continue;

                _competitionLayer[anImage.m_BMU.X, anImage.m_BMU.Y].AddImage(anImage);
            }
        }

        private Neuron FindBMU(InputVector inputVec)
        {
            Neuron winner = null;

            float lowestDistance = float.MaxValue;

            for (int i = 0; i < SOMConstants.NUM_NODES_DOWN; ++i)
            {
                for (int j = 0; j < SOMConstants.NUM_NODES_ACROSS; ++j)
                {
                    float dist = SOMHelper.Calculate_Distance(_competitionLayer[i, j].Weights, inputVec.weights);

                    if (dist < lowestDistance)
                    {
                        lowestDistance = dist;
                        winner = _competitionLayer[i, j];
                    }
                }
            }

            System.Diagnostics.Debug.Assert(winner != null);
            return winner;
        }

        public Neuron Recognize(InputVector inputVec)
        {
            Neuron winner = null;
            if (IsTrained)
            {
                float lowestDistance = float.MaxValue;

                for (int i = 0; i < SOMConstants.NUM_NODES_DOWN; ++i)
                {
                    for (int j = 0; j < SOMConstants.NUM_NODES_ACROSS; ++j)
                    {
                        if (_competitionLayer[i, j].GetImageCount() > 0)
                        {
                            float dist = SOMHelper.Calculate_Distance(_competitionLayer[i, j].Weights, inputVec.weights);

                            if (dist < lowestDistance)
                            {
                                lowestDistance = dist;
                                winner = _competitionLayer[i, j];
                            }
                        }
                    }
                }

                System.Diagnostics.Debug.Assert(winner != null);
                return winner;
            }
            return winner;
        }

        public ArrayList GetImageDataList(int i, int j)
        {
            if (i >= 0 && i < SOMConstants.NUM_NODES_ACROSS &&
                j >= 0 && j < SOMConstants.NUM_NODES_DOWN)
            {
                return _competitionLayer[i, j].ImageDataList;
            }
            else
            {
                return null;
            }
        }


        public string[] GetImageFileNames(int i, int j)
        {
            string[] imageFileNames = null;

            if (i >= 0 && i < SOMConstants.NUM_NODES_ACROSS &&
                j >= 0 && j < SOMConstants.NUM_NODES_DOWN)
            {
                _selectedNeuronCoords.X = i;
                _selectedNeuronCoords.Y = j;
                _isNeuronSelected = true;
                imageFileNames = _competitionLayer[i, j].GetImageFileNames();
            }

            return imageFileNames;
        }

        public ArrayList GetTrainingExamples_ImageDataList()
        {
            return _appSettings.images;
        }
    }
}
