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
using System.Windows.Media.Media3D;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Diagnostics;
using System.Windows.Markup;
using System.IO;
using System.Xml;
using System.Reflection;

// Wpf 3d Rendering (Used for converting 2D points to 3D points)
using Petzold.Media3D;

// OpenCV Wrapper
using OpenCvSharp;

// Picture SOM
using PictureSOM;
using PictureSOMWindow;
using System.Collections;
using System.IO.Ports;
using System.Threading;
using ThinkGearNET;

namespace AndyPyeAugmentedRealitySystem
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private OpenCVSharpCam cam;
        private bool cameraRunning = false;
        private Size imageSize = new Size(20, 20);
        private CvMemStorage storage = new CvMemStorage(0);
        private bool takeSnapShotDetected = false;

        private PictureSOMWindow.MainWindow pictureSOMWindow;
        private SOM _som;
        private SOMVisual _somVisual;

        private Stopwatch stopwatch = new Stopwatch();
        private double frameCounter = 0;
        private MarkerInfo previousMarkerInfo;
        private MarkerInfo currentMarkerInfo;

        private Stopwatch stopwatch_model = new Stopwatch();
        private bool isFlushed = false;
        private int modelLife = 200; // rendering timeout in milliseconds
        //public delegate void zeroArgDelegate

        private ThinkGearWrapper thinkGear;
        private bool JediMode = false;
        private bool Energized = false;

        public MainWindow()
        {
            // Show console
            AndyPyeAugmentedRealitySystem.ConsoleManager.Show();

            InitializeComponent();
            InitializeOpenCV();
            InitializeSOM();
            Initialize3D();
            InitializeMindwave();

            CompositionTarget.Rendering += CompositionTarget_Rendering;
        }

        public void InitializeOpenCV()
        {
            // Initialize camera
            cam = new OpenCVSharpCam();
        }

        public void InitializeSOM()
        {
            pictureSOMWindow = new PictureSOMWindow.MainWindow();
            pictureSOMWindow.ShowDialog();
            _som = pictureSOMWindow.som;
            _somVisual = new SOMVisual(SOMVisualType.COMPETITION_LAYER_MAP, _som.Size,
                (int)canvas_CompetitionLayer.Width, (int)canvas_CompetitionLayer.Height, _som);
            canvas_CompetitionLayer.Children.Add(_somVisual);

            if (_som == null)
            {
                MessageBox.Show("SOM is null");
            }
        }

        public void InitializeMindwave()
        {
            if (thinkGear == null)
            {
                thinkGear = new ThinkGearWrapper();
            }
            else
            {
                thinkGear.Disconnect();
            }

            // setup the event
            //thinkGear.ThinkGearChanged += ThinkGearChanged;

            Console.WriteLine("Connecting to mindwave...");

            // connect to the device on the specified COM port at 57600 baud
            bool ready = false;
            foreach (string port in SerialPort.GetPortNames())
            {
                Console.WriteLine("Trying to connect mindwave at port {0}...", port);
                ready = thinkGear.Connect(port, 57600, true);
                if (ready)
                {
                    Console.WriteLine("Connection successful!");
                    break;
                }
            }
        }

        private IplImage DetectImage(IplImage img, out CvRect roi, out CvPoint[] pts)
        {
            CvPoint[] squares = OpenCVSharpHelper.DetectSquares(img);
            storage.Clear();

            roi = new CvRect();
            pts = new CvPoint[4];
            if (squares.Count() >= 4)
            {
                for (int i = 0; i < squares.Length; i += 4)
                {
                    CvPoint[] pt = new CvPoint[4];

                    // read 4 vertices
                    pt[0] = squares[i + 0];
                    pt[1] = squares[i + 1];
                    pt[2] = squares[i + 2];
                    pt[3] = squares[i + 3];
                    CvRect tmproi = Cv.BoundingRect(pt);
                    int tmparea = tmproi.Width * tmproi.Height;
                    if (tmparea > (roi.Width * roi.Height))
                    {
                        roi = tmproi;
                        pts = pt;
                    } 
                }

                // Show detected image (rectangle of interest)
                IplImage detected = img.GetSubImage(roi);
                return detected;
            }
            else
            {
                return null;
            }
            
        }

        private int RecognizeImage(IplImage detected)
        {
            int id = -1;
            ImageData bestest = null;
            using (System.Drawing.Bitmap detected_Bitmap = detected.ToBitmap())
            {
                ImageData inputImgData = PictureSOM.SOMHelper.GetImageData(detected_Bitmap);
                Neuron neuronFired = _som.Recognize(inputImgData.inputVector);
                float lowestDistance = float.MaxValue;

                foreach (ImageData imgData in neuronFired.ImageDataList)
                {
                    //float dist = SOMHelper.Calculate_Distance(imgData.inputVector.weights, inputVec.weights);
                    float dist = SOMHelper.Calculate_Distance(imgData.inputVector.weights, inputImgData.inputVector.weights);

                    if (dist < lowestDistance)
                    {
                        lowestDistance = dist;
                        bestest = imgData;
                    }
                }

                if (bestest != null && (100-lowestDistance) > 50)
                {
                    id = bestest.MapTo3DModelID;

                    // recharge
                    stopwatch_model.Restart();

                    // Show the confidence level (Work only if the Taninoto similarity metric)
                    this.progressBar_confidence.Value = 100-lowestDistance;
                    this.label_tanimoto.Content = 100-lowestDistance;

                    BitmapImage recognized = new BitmapImage(new Uri(bestest.m_fullName));
                    if (recognized != null)
                    {
                        // Show the image that is recognized by the SOM
                        this.recognizedImage.Source = recognized;
                    }
                }
                _somVisual.HighLightCell(neuronFired.X, neuronFired.Y);
            }

            return id;
        }

        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            // Clear views
            this.detectedImage.Source = null;
            this.recognizedImage.Source = null;
            this.progressBar_confidence.Value = 0;
            this.label_tanimoto.Content = "";

            // mindwave
            if (thinkGear != null)
            {
                thinkGear.UpdateState();
                float attention = thinkGear.ThinkGearState.Attention;
                float meditation = thinkGear.ThinkGearState.Meditation;

                this.progressBar_attention.Value = attention;
                this.progressBar_meditation.Value = meditation;
                this.label_attention.Content = attention;
                this.label_meditation.Content = meditation;
                
                if (attention > 50 && meditation > 50)
                {
                    if(JediMode) Energized = true;
                    this.button_jedi.Background = Brushes.Green;
                }
                else
                {
                    Energized = false;
                    this.button_jedi.Background = Brushes.Red;
                }
            }

            textBlock_timeout.Text = stopwatch_model.ElapsedMilliseconds.ToString();
            if (stopwatch_model.ElapsedMilliseconds > modelLife)
            {
                //MyModel mm = dicModels[currentMarkerInfo.modelId];
                modeler.Children.Clear();
                isFlushed = true;
                previousMarkerInfo.modelId = -1;
            }

            if (cameraRunning == true)
            {
                #region FPSMeasurement
                if (frameCounter++ == 0)
                {
                    // Starting timing.
                    stopwatch.Start();
                }

                // Determine frame rate in fps (frames per second).
                long frameRate = (long)(frameCounter / this.stopwatch.Elapsed.TotalSeconds);
                if (frameRate > 0)
                {
                    // Update elapsed time, number of frames, and frame rate.
                    this.textBlock_VideoPerformance.Text = string.Format("Elapsed : {0}, Frame : {1}, FPS : {2}",
                        stopwatch.Elapsed.ToString(),
                        frameCounter,
                        frameRate);
                }
                #endregion FPSMeasurement

                cam.InitializeCapture();
                cam.getFrame();
                int id = -1;
                using (IplImage image = cam.getFrame())
                {
                    if (image != null)
                    {
                        // Detection
                        CvRect roi;
                        CvPoint[] pts;
                        IplImage detected = DetectImage(image, out roi, out pts);
                        if (takeSnapShotDetected)
                        {
                            Cv.SaveImage(@"C:\Users\Pye\Desktop\TrainingSet\detected.bmp", detected);
                            takeSnapShotDetected = false;
                        }

                        int roiArea = roi.Size.Width * roi.Size.Height;
                        //Console.WriteLine(roiArea.ToString());  // Debug
                        if (detected != null && roiArea > 10000)
                        {
                            OpenCVSharpHelper.CorrectGamma(detected, detected, slider_Gamma.Value);
                            detectedImage.Source = OpenCVSharpHelper.ToBitmapSource(detected);

                            // Recognition
                            if (_som.IsTrained)
                            {
                                id = RecognizeImage(detected);
                            }

                            // Draw ROI (after recognition)
                            if (displayROICheckbox.IsChecked == true)
                            {
                                OpenCVSharpHelper.DrawROI(image, roi, pts);
                            }

                            detected.Dispose();
                        }

                        // Display 3D
                        if (Render3DCheckbox.IsChecked == true)
                        {
                            if (id != -1) Cv.FillPoly(image, new CvPoint[][] { pts }, CvColor.Gray);
                            // Find the center of roi
                            CvPoint renderPt = new CvPoint(roi.Left + (roi.Width / 2), roi.Top + (roi.Height / 2));
                            Point3D point3D = GetPoint3D(new Point(renderPt.X, renderPt.Y), MainViewport);

                            previousMarkerInfo = currentMarkerInfo;
                            currentMarkerInfo = new MarkerInfo(id, point3D);
                            UpdateViewport();
                        }
                        video.Source = OpenCVSharpHelper.ToBitmapSource(image);
                    }
                    
                }
            }
            else
            {
                stopwatch.Reset();
                cam.DisposeCapture();
            }

            /*/ Debug 3D
            CvPoint tmpPt = new CvPoint(200, 200);
            Point3D pt3D = GetPoint3D(new Point(tmpPt.X, tmpPt.Y), MainViewport);

            previousMarkerInfo = currentMarkerInfo;
            currentMarkerInfo = new MarkerInfo(5, pt3D);
            UpdateViewport();
            */
        }
    
        #region 3D Rendering
        XmlDocument xdModels = new XmlDocument();
        Dictionary<int, MyModel> dicModels = new Dictionary<int, MyModel>();

        private void UpdateViewport()
        {
            MyModel prevModel = null;
            MyModel currentModel = null;
            if (dicModels.ContainsKey(previousMarkerInfo.modelId)) prevModel = dicModels[previousMarkerInfo.modelId];
            if (dicModels.ContainsKey(currentMarkerInfo.modelId)) currentModel = dicModels[currentMarkerInfo.modelId];

            if (prevModel == null && currentModel != null)      // Not contain AND Not contain => add and transform
            {
                Add(currentModel);
                Transform(currentModel);
            }
            else if (prevModel != null && currentModel == null)
            {
                Remove(prevModel);
            }
            else if (prevModel != null && prevModel.id == currentModel.id)
            {
                if (isFlushed)
                {
                    Add(currentModel);
                    isFlushed = false;
                }

                Transform(currentModel);
            }
            else if (prevModel != null && prevModel.id != currentModel.id)
            {
                Remove(prevModel);
                Add(currentModel);
                Transform(currentModel);
            }
        }

        private void Remove(MyModel model)
        {
            if (model.sb != null) model.sb.Stop();
            if (model.imc != null)
            {
                model.imc.Stop();
                modeler.Children.Remove(model.effect_mv3d);
            } 
            modeler.Children.Remove(model.mv3d);
        }

        private void Add(MyModel model)
        {
            if (model.sb != null) model.sb.Begin();

            try
            {
                if (model.imc != null)
                {
                    model.imc.Start();
                }
            }
            catch (Exception ex) { }

            try
            {
                modeler.Children.Add(model.mv3d);
            }
            catch (Exception ex) { }
        }

        private void Transform(MyModel mm)
        {
            TranslateTransform3D ttransform3d = new TranslateTransform3D(
                    currentMarkerInfo.position.X,
                    currentMarkerInfo.position.Y,
                    currentMarkerInfo.position.Z);

            if (mm.sb != null) mm.sb.Pause();
            mm.mv3d.Transform = ttransform3d;
            if (mm.sb != null) mm.sb.Resume();

            // Update particle effect
            if (mm.imc != null && mm.effect_mv3d != null)
            {
                mm.effect_mv3d.Transform = ttransform3d;
                ParticleModel pm = (ParticleModel)mm.imc;
                pm.Update();

                if (Energized)
                {
                    try
                    {
                        modeler.Children.Add(mm.effect_mv3d); 
                    }
                    catch (Exception) { }
                } 
            }
        }

        public void Initialize3D()
        {
            try
            {
                xdModels.Load("../../models.xml");
                foreach (XmlNode xnNode in xdModels.DocumentElement.ChildNodes)
                {
                    if (xnNode.NodeType != XmlNodeType.Element)
                    {
                        continue;
                    }
                    XmlElement xeNode = (XmlElement)xnNode;
                    MyModel mm = new MyModel();
                    mm.id = Int32.Parse(xeNode.Attributes["id"].Value);
                    mm.trans = bool.Parse(xeNode.Attributes["trans"].Value);
                    mm.path = xeNode.Attributes["path"].Value;
                    mm.code = xeNode.Attributes["code"].Value;
                    mm.rotX = Int32.Parse(xeNode.Attributes["rotX"].Value);
                    mm.rotY = Int32.Parse(xeNode.Attributes["rotY"].Value);
                    mm.rotZ = Int32.Parse(xeNode.Attributes["rotZ"].Value);
                    mm.sizeX = Int32.Parse(xeNode.Attributes["sizeX"].Value);
                    mm.sizeY = Int32.Parse(xeNode.Attributes["sizeY"].Value);
                    mm.sizeZ = Int32.Parse(xeNode.Attributes["sizeZ"].Value);
                    mm.offX = Int32.Parse(xeNode.Attributes["offX"].Value);
                    mm.offY = Int32.Parse(xeNode.Attributes["offY"].Value);
                    mm.offZ = Int32.Parse(xeNode.Attributes["offZ"].Value);

                    FileStream fs = new FileStream(mm.path, FileMode.Open, FileAccess.Read); //
                    /*
                    Viewbox vb = (Viewbox)XamlReader.Load(fs);
                    fs.Close();
                    mm.root = vb; //for INameScope
                    Viewport3D v3d = (Viewport3D)vb.Child;
                    */

                    Viewport3D v3d = (Viewport3D)XamlReader.Load(fs);
                    fs.Close();

                    mm.root = v3d;

                    // extract storyboard
                    if (v3d.Triggers.Count > 0)
                    {
                        var trigger = (EventTrigger)v3d.Triggers[0];
                        var beginSb = (BeginStoryboard)trigger.Actions[0];
                        mm.sb = beginSb.Storyboard;
                    }                    

                    ModelVisual3D mv3d = (ModelVisual3D)v3d.Children[0];
                    Model3DGroup m3dgScene = (Model3DGroup)mv3d.Content;
                    Model3DGroup m3dg = (Model3DGroup)m3dgScene.Children[m3dgScene.Children.Count - 1];
                    mm.m3dg = m3dg;

                    // make modelvisual3d
                   
                    //mv3d = new ModelVisual3D();
                    //mv3d.Content = mm.m3dg;
                    //mm.mv3d = mv3d;


                    //m3dg.Transform = null;
                    Transform3DGroup t3dg = new Transform3DGroup();
                    m3dg.Transform = t3dg;

                    //change size
                    double scaleX = mm.sizeX / m3dg.Bounds.SizeX;
                    double scaleY = mm.sizeY / m3dg.Bounds.SizeY;
                    double scaleZ = mm.sizeZ / m3dg.Bounds.SizeZ;
                    scaleX = FixDouble(scaleX);
                    scaleY = FixDouble(scaleY);
                    scaleZ = FixDouble(scaleZ);
                    ScaleTransform3D stransform3D = new ScaleTransform3D(scaleX, scaleY, scaleZ);
                    mm.scaleTransform = stransform3D;
                    t3dg.Children.Add(stransform3D);

                    //move to origin
                    double offX = (-1 * m3dg.Bounds.X) - (m3dg.Bounds.SizeX / 2);
                    double offY = (-1 * m3dg.Bounds.Y) - (m3dg.Bounds.SizeY / 2);
                    double offZ = (-1 * m3dg.Bounds.Z) - (m3dg.Bounds.SizeZ / 2);
                    offX = FixDouble(offX);
                    offY = FixDouble(offY);
                    offZ = FixDouble(offZ);
                    TranslateTransform3D ttransform3d = new TranslateTransform3D(offX, offY, offZ);
                    mm.translateTranform = ttransform3d;
                    t3dg.Children.Add(ttransform3d);

                    //rotate
                    t3dg.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), mm.rotX)));
                    t3dg.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), mm.rotY)));
                    t3dg.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), mm.rotZ)));

                    //move to top of Z
                    t3dg.Children.Add(new TranslateTransform3D(0, 0, m3dg.Bounds.SizeZ / 2));
                    //t3dg.Children.Add(new TranslateTransform3D(0, m3dg.Bounds.SizeY / 2, 0));

                    //move to offset
                    t3dg.Children.Add(new TranslateTransform3D(mm.offX, mm.offY, mm.offZ)); //0,0,0

                    AddModel(mm);

                    dicModels.Add(mm.id, mm);
                }
                Console.WriteLine("{0} Models added to the 3D model dictionary.", dicModels.Count);

                // Map 3d models to images
                ModelMapping();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

            previousMarkerInfo = new MarkerInfo(-1, new Point3D());
            currentMarkerInfo = previousMarkerInfo;
        }

        private double FixDouble(double d)
        {
            if (double.IsNaN(d) == true || double.IsPositiveInfinity(d) == true || double.IsNegativeInfinity(d) == true)
            {
                return 0;
            }
            return d;
        }

        private void ModelMapping()
        {
            foreach (ImageData imgData in _som.GetTrainingExamples_ImageDataList())
            {
                if (imgData.m_fileName.Contains("miku")) 
                {
                    imgData.MapTo3DModelID = 1;
                    Console.WriteLine("{0} maps to {1}", imgData.m_fileName, 1);
                }
                else if (imgData.m_fileName.Contains("cube"))
                {
                    imgData.MapTo3DModelID = 2;
                    Console.WriteLine("{0} maps to {1}", imgData.m_fileName, 2);
                }
                else if(imgData.m_fileName.Contains("3sphere"))
                {
                    imgData.MapTo3DModelID = 3;
                    Console.WriteLine("{0} maps to {1}", imgData.m_fileName, 3);
                }
                else if (imgData.m_fileName.Contains("starfish"))
                {
                    imgData.MapTo3DModelID = 4;
                    Console.WriteLine("{0} maps to {1}", imgData.m_fileName, 4);
                }
                else if (imgData.m_fileName.Contains("brain"))
                {
                    imgData.MapTo3DModelID = 5;
                    Console.WriteLine("{0} maps to {1}", imgData.m_fileName, 5);
                }
            }
        }

        Random rand = new Random();
        private Brush GetRandomBrush()
        {
            Type t = typeof(Brushes);
            MemberInfo[] mia = t.GetMembers(BindingFlags.Public | BindingFlags.Static);
            List<string> lstBrushes = new List<string>();
            foreach (MemberInfo mi in mia)
            {
                if (mi.Name.StartsWith("get_") == true)
                    continue;
                lstBrushes.Add(mi.Name);
            }
            int val = rand.Next(lstBrushes.Count);
            string colorName = lstBrushes[val];
            BrushConverter bc = new BrushConverter();
            Brush b = (Brush)bc.ConvertFromString(colorName);
            return b; // Brushes.Cyan;
        }

        private void AddModel(MyModel mm)
        {
            ModelVisual3D mv3d = new ModelVisual3D();
            mv3d.Content = mm.m3dg;
            mm.mv3d = mv3d;

            if (mm.code != null && mm.code != String.Empty)
            {
                string strType = mm.code;
                Type type = Type.GetType(strType);
                object modelCode = Activator.CreateInstance(type);

                IModelCode imc = (IModelCode)modelCode;
                imc.Init(mm);
                mm.imc = imc;
            }
        }

        private ModelVisual3D AddModel(int id)
        {
            ModelVisual3D mv3d = new ModelVisual3D();
            if (dicModels.ContainsKey(id) == true)
            {
                MyModel mm = dicModels[id];
                Model3DGroup m3dg = mm.m3dg;

                if (mm.code == null || mm.code == String.Empty)
                {
                    mv3d = new ModelVisual3D();
                    mv3d.Content = m3dg;
                    AddModelBasedOnTransparency(mv3d, mm.trans);

                    //change MaterialProperty randomly
                    //TODO this will screw up model with transparency
                    GeometryModel3D gm3d = (GeometryModel3D)m3dg.Children[0];
                    MaterialGroup mg = (MaterialGroup)gm3d.Material;
                    DiffuseMaterial dm = (DiffuseMaterial)mg.Children[0];
                    dm.Brush = GetRandomBrush();
                }
                else
                {
                    string strType = mm.code;
                    Type type = Type.GetType(strType);
                    object modelCode = Activator.CreateInstance(type);
                    mv3d = (ModelVisual3D)modelCode;
                    mv3d.Content = m3dg;
                    AddModelBasedOnTransparency(mv3d, mm.trans);

                    IModelCode imc = (IModelCode)modelCode;
                    imc.Init(mm);
                    imc.Start();
                }
            }
            else
            {
                System.Console.WriteLine("markerID : " + id.ToString());
            }

            return mv3d;
        }

        private void AddModelBasedOnTransparency(ModelVisual3D mv3d, bool trans)
        {
            if (trans == true)
            {
                //transparent items must come last because of z-buffer
                modeler.Children.Add(mv3d);
            }
            else //not transparent, so add before transparent items
            {
                modeler.Children.Insert(0, mv3d);
            }
        }

        private Point3D GetPoint3D(Point pt, Viewport3D viewportArg)
        {
            Point3D point3D = new Point3D();
            var range = new LineRange();
            var isValid = ViewportInfo.Point2DtoPoint3D(viewportArg, pt, out range);
            if (isValid)
            {
                point3D = range.PointFromZ(0);
            }
            
            return point3D;
        }

        #endregion 3D Rendering
        
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            CompositionTarget.Rendering -= CompositionTarget_Rendering;
            cam.DisposeCapture();
        }

        private void change3dButton_Click(object sender, RoutedEventArgs e)
        {
            Random rand = new Random(); 
            currentMarkerInfo.modelId = dicModels.Keys.ElementAt(rand.Next(dicModels.Count));
        }

        private void button_CameraStart_Click(object sender, RoutedEventArgs e)
        {
            if (!cameraRunning)
            {
                cameraRunning = true;
                button_CameraStart.Content = "Stop";
            }
            else
            {
                cameraRunning = false;
                button_CameraStart.Content = "Start";
            }
        }

        private void detectedImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            takeSnapShotDetected = true;
        }

        private void button_jedi_Click(object sender, RoutedEventArgs e)
        {
            if (this.button_jedi.Content.ToString() != "Stop Jedi")
            {
                InitializeMindwave();
                this.grid_mindwave.Visibility = Visibility.Visible;
                this.button_jedi.Content = "Stop Jedi";
                JediMode = true;
            }
            else
            {
                thinkGear.Disconnect();
                Console.WriteLine("Mindwave Disconnected!");
                this.button_jedi.Content = "Jedi Mode";
                thinkGear = null;
                this.grid_mindwave.Visibility = Visibility.Hidden;
                JediMode = false;
            }
        }
    }

    public class MyModel
    {
        public int id;
    public bool trans = false;
    public string path;
    public string code;
    public Viewport3D root;
    public ModelVisual3D mv3d;
    public ModelVisual3D effect_mv3d;
    public Model3DGroup m3dg;
    public Storyboard sb;
    public IModelCode imc;
    public TranslateTransform3D translateTranform;
    public ScaleTransform3D scaleTransform;
    public int rotX;
    public int rotY;
    public int rotZ;
    public int sizeX;
    public int sizeY;
    public int sizeZ;
    public int offX;
    public int offY;
    public int offZ;
}

public class MarkerInfo
{
    public int modelId;
    public Point3D position;

    public MarkerInfo(int id, Point3D pos)
    {
        modelId = id;
        position = pos;
    }
}
}

