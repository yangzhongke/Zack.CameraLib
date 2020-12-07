namespace Zack.CameraLib.Core
{
    public class VideoCapabilities
    {
        public int Width { get; internal set; }
        public int Height { get; internal set; }
        public int FrameRate { get; internal set; }
        public string VideoFormat { get; internal set; }
    }
}
