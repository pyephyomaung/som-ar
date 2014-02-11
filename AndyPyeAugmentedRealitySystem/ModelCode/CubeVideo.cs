//----------------------------------------------
// (c) 2007 by casey chesnut, brains-N-brawn LLC
//----------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media.Media3D;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Markup;

namespace AndyPyeAugmentedRealitySystem
{
    class CubeVideo : ModelVisual3D, IModelCode
    {
        private MyModel mm;
        private MediaElement me;

        public void Init(MyModel mm)
        {
            this.mm = mm;

            INameScope ins = NameScope.GetNameScope(mm.root);
            me = ins.FindName("mediaElement1") as MediaElement;
            if (me != null)
            {
                me.MediaFailed += new EventHandler<ExceptionRoutedEventArgs>(me_MediaFailed);                
            }
        }

        void me_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            MessageBox.Show(e.ErrorException.ToString());
        }

        public void Start()
        {
            me.Play();
        }

        public void Stop()
        {
            me.Pause();
        }
    }
}
