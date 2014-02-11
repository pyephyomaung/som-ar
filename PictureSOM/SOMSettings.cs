using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace PictureSOM
{
    public class SOMConstants
    {
        // Some constants that may change from app to app.
        public static int NUM_NODES_DOWN { get { return 10;} }     // Should be changed as # of pics increase.
        public static int NUM_NODES_ACROSS { get { return 10;} }     // Should be changed as # of pics increase.

        // The following should add up to NUM_WEIGHTS.. or else.
        public static int INPUT_VECTOR_SIZE_HISTOGRAM { get { return 16; } }
        public static int INPUT_VECTOR_AREA_REGIONS_WIDE { get { return 3; } }
        public static int INPUT_VECTOR_AREA_REGIONS_HIGH { get { return 3; } }
        public static int INPUT_VECTOR_SIZE_AREA { get { return (INPUT_VECTOR_AREA_REGIONS_WIDE * INPUT_VECTOR_AREA_REGIONS_HIGH) * 3; } }  // * 3 since RGB
        //public static int NUM_WEIGHTS { get { return (INPUT_VECTOR_SIZE_HISTOGRAM + INPUT_VECTOR_SIZE_AREA); } }
        public static int NUM_WEIGHTS { get { return (168); } }
    }

    /// <summary>
    /// Tell how all of the neurons should be initialized
    /// </summary>
    public enum INIT_FILL
    {
        random,
        gradient
    }

    public enum NET_STATE
    {
        neutral,    // Nothing needs to be done. Waiting.
        init,       // We need to initialize all the windows.
        training,   // Need to update (window 2) to show progress.
        finished    // Display the results (calculate windows 3 and 4).
    }

    public enum TRAINING_PHASE
    {
        phase_1,
        phase_2
    }

    public class AppSettings
    {
        public INIT_FILL initFill;
        public float width, height;
        public float nodeWidth, nodeHeight;     // Used to represent nodes as colored squares.
        public float startLearningRate_P1, startLearningRate_P2;
        public int numIterations_P1, numIterations_P2;
        public float mapRadius;
        public string imageDirectory;

        public ArrayList images;    // Used to store the image data each application run.

        public AppSettings()
        {
            images = new ArrayList();
        }
    }
}
