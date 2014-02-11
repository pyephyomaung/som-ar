//----------------------------------------------
// (c) 2007 by casey chesnut, brains-N-brawn LLC
//----------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media.Media3D;
using System.Windows.Controls;

namespace AndyPyeAugmentedRealitySystem
{
    public interface IModelCode
    {
        void Init(MyModel m3dg);
        void Start();
        void Stop();
    }
}
