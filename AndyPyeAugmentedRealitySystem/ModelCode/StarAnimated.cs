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
using System.Windows.Media.Animation;

namespace AndyPyeAugmentedRealitySystem
{
    class StarAnimated : ModelVisual3D, IModelCode
    {
        private MyModel mm;
        AnimationClock clockx;
        AnimationClock clocky;
        AnimationClock clockz;

        public void Init(MyModel mm)
        {
            this.mm = mm;

            Transform3DGroup t3dg = (Transform3DGroup)this.mm.m3dg.Transform;
            RotateTransform3D rtx = (RotateTransform3D)t3dg.Children[2];
            AxisAngleRotation3D aar3dx = (AxisAngleRotation3D)rtx.Rotation;
            RotateTransform3D rty = (RotateTransform3D)t3dg.Children[3];
            AxisAngleRotation3D aar3dy = (AxisAngleRotation3D)rty.Rotation;
            RotateTransform3D rtz = (RotateTransform3D)t3dg.Children[4];
            AxisAngleRotation3D aar3dz = (AxisAngleRotation3D)rtz.Rotation;

            DoubleAnimation dax = new DoubleAnimation(360, new Duration(new TimeSpan(0, 0, 10)));
            dax.RepeatBehavior = RepeatBehavior.Forever;
            //aar3dx.BeginAnimation(AxisAngleRotation3D.AngleProperty, dax);
            clockx = dax.CreateClock();

            DoubleAnimation day = new DoubleAnimation(360, new Duration(new TimeSpan(0, 0, 5)));
            day.RepeatBehavior = RepeatBehavior.Forever;
            clocky = day.CreateClock();

            DoubleAnimation daz = new DoubleAnimation(360, new Duration(new TimeSpan(0, 0, 5)));
            daz.RepeatBehavior = RepeatBehavior.Forever;
            clockz = daz.CreateClock();

            aar3dx.ApplyAnimationClock(AxisAngleRotation3D.AngleProperty, clockx);
            aar3dy.ApplyAnimationClock(AxisAngleRotation3D.AngleProperty, clocky);
            aar3dz.ApplyAnimationClock(AxisAngleRotation3D.AngleProperty, clockx);
            clockx.Controller.Begin();
            clocky.Controller.Begin();
            clockz.Controller.Begin();
        }

        public void Start()
        {
            clockx.Controller.Resume();
            clocky.Controller.Resume();
            clockz.Controller.Resume();
        }

        public void Stop()
        {
            clockx.Controller.Pause();
            clocky.Controller.Pause();
            clockz.Controller.Pause();
        }
    }
}
