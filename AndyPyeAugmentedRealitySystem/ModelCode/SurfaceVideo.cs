using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media.Media3D;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;

namespace AndyPyeAugmentedRealitySystem
{
    class SurfaceVideo : ModelVisual3D, IModelCode
    {
        private MyModel mm;
        private VideoDrawing vd;

        public void Init(MyModel mm)
        {
            this.mm = mm;
            Uri myValidMediaUri = new Uri(@"D:\Download\OdeToTheBrainCut.avi");

            MediaPlayer mp = new MediaPlayer();
            mp.Open(myValidMediaUri);
            vd = new VideoDrawing();
            vd.Player = mp;
            DrawingBrush db = new DrawingBrush();
            db.Drawing = vd;

            INameScope ins = NameScope.GetNameScope(mm.root);
            DiffuseMaterial dif = ins.FindName("dif") as DiffuseMaterial;
            dif.Brush = db;
            vd.Player.Play();
        }

        void me_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            MessageBox.Show(e.ErrorException.ToString());
        }

        public void Start()
        {
            vd.Player.Play();
        }

        public void Stop()
        {
            vd.Player.Pause();
        }
    }
}
