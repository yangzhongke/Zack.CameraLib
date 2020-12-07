using System;
using System.Collections.Generic;
using System.Text;

namespace Zack.CameraLib.Core
{
    public class CameraInfo
    {
        public string FriendlyName{get; internal set; }
        public int Index { get; internal set; }
        public VideoCapabilities[] VideoCapabilities { get; internal set; }
    }
}
