namespace Zack.CameraLib.Core
{
    public class VideoCapabilities
    {
        public int Width { get; internal set; }
        public int Height { get; internal set; }
        public int FrameRate { get; internal set; }
        public int BitRate { get; internal set; }

        public override string ToString()
        {
            return $"Width={Width},Height={Height},FrameRate={FrameRate},BitRate={BitRate}";
        }
    }
}
