using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using PictureSOM;
using System.Drawing;
using System.Collections.ObjectModel;
using System.Collections;
using System.Globalization;

using AmCharts.Windows.Column;

namespace PictureSOMWindow
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private AppSettings _appSettings;
        private SOM _som;
        private SOMVisual visualHost_CompetitionLayer;
        private SOMVisual visualHost_ErrorMap;
        private ObservableCollection<string> imageUriList = new ObservableCollection<string>();
        private ObservableCollection<ImageData> imageDataList = new ObservableCollection<ImageData>();

        public SOM som { get { return _som; } }

        public MainWindow()
        {
            InitializeComponent();
            // Initialize application setting for SOM and its visual
            _appSettings = new AppSettings();
            UpdateAppSettings();

            // Initialize SOM
            _som = new SOM(_appSettings);

            // Initialize visual for competition layer
            visualHost_CompetitionLayer = new SOMVisual(SOMVisualType.COMPETITION_LAYER_MAP, SOMConstants.NUM_NODES_ACROSS,
                (int)_appSettings.width, (int)_appSettings.height, _som);
            canvas_CompetitionLayer.Children.Add(visualHost_CompetitionLayer);

            // Initialize visual for error map
            visualHost_ErrorMap = new SOMVisual(SOMVisualType.ERROR_MAP, SOMConstants.NUM_NODES_ACROSS,
                (int)_appSettings.width, (int)_appSettings.height, _som);
            canvas_ErrorMap.Children.Add(visualHost_ErrorMap);

            // Initialize histograms
            for (int i = 0; i < SOMConstants.NUM_WEIGHTS; i++)
            {
                NeuronHistogram.DataItems.Add(new ColumnDataPoint());
                FeatureHistogram.DataItems.Add(new ColumnDataPoint());
            }

            CompositionTarget.Rendering += CompositionTarget_Rendering;
        }

        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            if (_som != null)
            {
                if (_som.IsTraining)
                {
                    _som.Run();
                }

                // Update the status bar
                float tme = _som.TotalMapError;
                textBlock_numTrainingExamples.Text = "Total Training Examples : " + _appSettings.images.Count.ToString();
                textBlock_NetState.Text = _som.NetState;
                if (tme <= 0.0f)
                {
                    textBlock_TotalError.Text = "Total Map Error : -";
                }
                else
                {
                    textBlock_TotalError.Text = "Total Map Error : " + tme.ToString();
                }

                visualHost_CompetitionLayer.CreateDrawingVisual_Text(_som);
                visualHost_ErrorMap.CreateDrawingVisual_ErrorMap(_som);
            }
        }

        #region Helper Functions(UpdateAppSettings, GetInputVectors, Reset, MousePosition_to_CompetitionLayerPosition, ToDoubleArray)
        private void UpdateAppSettings()
        {
            if (_appSettings == null)
            {
                _appSettings = new AppSettings();
            }

            // Initialization fill
            if (radioButton_Random.IsChecked == true)
            {
                _appSettings.initFill = INIT_FILL.random;
            }
            else if (radioButton_Gradient.IsChecked == true)
            {
                _appSettings.initFill = INIT_FILL.gradient;
            }

            // Assumes both rendering canvases are of the same dimension
            _appSettings.width = (float)canvas_CompetitionLayer.Width;
            _appSettings.height = (float)canvas_CompetitionLayer.Height;

            // Calculate competition layer dimension
            _appSettings.nodeWidth = _appSettings.width / (float)SOMConstants.NUM_NODES_ACROSS;
            _appSettings.nodeHeight = _appSettings.height / (float)SOMConstants.NUM_NODES_DOWN;

            _appSettings.mapRadius = Math.Max(_appSettings.width, _appSettings.height) / 2.0f;

            // Initialize learning rates
            _appSettings.startLearningRate_P1 = (float)decimalUpDown_LearningRateP1.Value;
            _appSettings.startLearningRate_P2 = (float)decimalUpDown_LearningRateP2.Value;

            // Initialize iterations
            _appSettings.numIterations_P1 = (int)integerUpDown_IterationP1.Value;
            _appSettings.numIterations_P2 = (int)integerUpDown_IterationP2.Value;

            _appSettings.imageDirectory = textBox_ImageDirectory.Text;

            GetInputVectors();
        }
 
        private void GetInputVectors()
        {
            if (_appSettings.imageDirectory == "")
            {
                return;
            }

            _appSettings.images.Clear();

            // Get image files by filtering by extensions
            System.IO.DirectoryInfo directoryInfo = new System.IO.DirectoryInfo(_appSettings.imageDirectory);
            string[] extensions = new string[] { ".bmp", ".jpg", ".png", ".gif" };
            var allowedExtensions = new HashSet<string>(extensions, StringComparer.OrdinalIgnoreCase);
            System.IO.FileInfo[] fileInfos = directoryInfo
                .EnumerateFiles("*", System.IO.SearchOption.AllDirectories)
                .Where(f => allowedExtensions.Contains(f.Extension)).ToArray();

            foreach (System.IO.FileInfo f in fileInfos)
            {
                using (Bitmap img = new Bitmap(f.FullName))
                {
                    try
                    {
                        // Feature Extraction
                        ImageData imgData = PictureSOM.SOMHelper.GetImageData(img);
                        imgData.m_fileName = f.Name;
                        imgData.m_directoryName = f.DirectoryName;
                        Console.WriteLine(imgData.m_fullName);

                        // Test experimental features here
                        //float[] testFeature = Features.calculateCEDD_Compact(img);

                        _appSettings.images.Add(imgData);
                    }
                    catch(Exception)
                    {
                        Console.WriteLine("Could not add " + f.FullName);
                    }
                    
                    
                }
            }
        }

        private void Reset()
        {
            if (_som != null)
            {
                _som.StopTraining();
            }

            UpdateAppSettings();
            _som = new SOM(_appSettings);
            button_TrainSOM.IsEnabled = true;
        }

        private System.Drawing.Point MousePosition_to_CompetitonLayerPoition(System.Windows.Point mousePos)
        {
            // System.Drawing.Point is an integer pair
            System.Drawing.Point SOMPos = new System.Drawing.Point();

            // Yay integer division to the rescue!
            SOMPos.X = (int)(mousePos.X - visualHost_CompetitionLayer.BorderSize) / (int)_appSettings.nodeWidth;
            SOMPos.Y = (int)(mousePos.Y - visualHost_CompetitionLayer.BorderSize) / (int)_appSettings.nodeHeight;

            return SOMPos;
        }

        public double[] ToDoubleArray(float[] f)
        {
            double[] d = new double[f.Length];

            for (int i = 0; i < f.Length; i++)
            {
                d[i] = (double)f[i];
            }

            return d;
        }
        #endregion Helper Functions(UpdateAppSettings, GetInputVectors, Reset, MousePosition_to_CompetitionLayerPosition, ToDoubleArray)

        #region Event Handlers
        private void button_Browse_Click(object sender, RoutedEventArgs e)
        {
            var dirBrowseDialog = new System.Windows.Forms.FolderBrowserDialog();

            if (dirBrowseDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                textBox_ImageDirectory.Text = dirBrowseDialog.SelectedPath;
                if (!textBox_ImageDirectory.Text.EndsWith("\\"))
                {
                    textBox_ImageDirectory.Text += "\\";
                }
            }
        }

        private void button_ResetSOM_Click(object sender, RoutedEventArgs e)
        {
            Reset();
        }

        private void button_TrainSOM_Click(object sender, RoutedEventArgs e)
        {
            if (_appSettings.imageDirectory == "")
            {
                MessageBox.Show("Please select a directory to read images from.");
                return;
            }

            if (_som.IsTrained)
            {
                MessageBox.Show("_som already trained. Hit RESET then TRAIN to re-train.");
                return;
            }

            if (!_som.IsTraining)
            {
                _som.StartTraining();
            }
        }

        private void listBox_ImageList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ImageData selected = (ImageData)listBox_ImageList.SelectedItem;
            if (selected != null)
            {
                /* / Debug
                string stringBuilder = "";
                foreach (float f in selected.inputVector.weights)
                {
                    stringBuilder += String.Format("{0: 0.0000}", f);
                }
                MessageBox.Show(stringBuilder);
                */

                float[] tmp = selected.inputVector.weights;
                for (int i = 0; i < tmp.Length; i++ )
                {
                    FeatureHistogram.DataItems.ElementAt(i).Value = tmp[i];
                }
            }
        }

        private void canvas_CompetitionLayer_MouseDown(object sender, MouseButtonEventArgs e)
        {
            System.Drawing.Point pt = MousePosition_to_CompetitonLayerPoition(e.GetPosition((UIElement)sender));

            //MessageBox.Show(pt.X.ToString() + ", " + pt.Y.ToString()); // Debug

            if (e.ChangedButton == MouseButton.Left)
            {
                if (_som != null && !_som.IsTraining)
                {
                    imageDataList.Clear();

                    ArrayList lst = _som.GetImageDataList((int)pt.X, (int)pt.Y);
                    if (lst != null)
                    {
                        foreach (ImageData d in lst)
                        {
                            imageDataList.Add(d);
                        }
                    }
                }
                this.listBox_ImageList.ItemsSource = imageDataList;

                // Update neuron histogram
                try
                {
                    Neuron selectedNeuron = _som.CompetitionLayer[(int)pt.X, (int)pt.Y];
                    for (int i = 0; i < selectedNeuron.Weights.Length; i++)
                    {
                        NeuronHistogram.DataItems.ElementAt(i).Value = selectedNeuron.Weights[i];
                    }
                }
                catch(Exception)
                {}
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            CompositionTarget.Rendering -= CompositionTarget_Rendering;
            if (_som.IsTrained)
            {
                //this.DialogResult = true;
            }
            else
            {
                //this.DialogResult = false;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        #endregion Event Handlers
        
    }

    #region Helper Classes(ImageConverter,  ImageConverter_Histogram)
    public sealed class ImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
                              object parameter, CultureInfo culture)
        {
            try
            {
                ImageData tmp = (ImageData)value;
                BitmapImage img = new BitmapImage(new Uri(tmp.m_fullName));
                img.DecodePixelHeight = 100;
                return img;
            }
            catch
            {
                return new BitmapImage();
            }
        }

        public object ConvertBack(object value, Type targetType,
                                  object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class ImageConverter_Histogram : IValueConverter
    {
        public object Convert(object value, Type targetType,
                              object parameter, CultureInfo culture)
        {
            try
            {
                ImageData tmp = (ImageData)value;
                return new BitmapImage();
            }
            catch
            {
                return new BitmapImage();
            }
        }

        public object ConvertBack(object value, Type targetType,
                                  object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    #endregion Helper Classes(ImageConverter,  ImageConverter_Histogram)
}
