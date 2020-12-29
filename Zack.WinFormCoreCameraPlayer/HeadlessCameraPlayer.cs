using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace Zack.WinFormCoreCameraPlayer
{
    public class HeadlessCameraPlayer : IDisposable
    {
        public event Action<Bitmap> NewFrameReceived;
        public PlayerStatus Status { get; private set; } = PlayerStatus.NotStarted;
        public Action<Mat> frameFilterFunc { get; private set; }

        private int _framePerSecond = 20;
        public int FramePerSecond
        {
            get
            {
                return this._framePerSecond;
            }
            set
            {
                if (value < 1 || value > 60)
                {
                    throw new ArgumentOutOfRangeException("FramePerSecond shoud >=1 and <=60");
                }
                this._framePerSecond = value;
            }
        }

        public void SetFrameFilter(Action<Mat> frameFilterFunc)
        {
            this.frameFilterFunc = frameFilterFunc;
        }
        public bool IsDisposing { get; private set; }
        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            IsDisposing = true;
            this.Status = PlayerStatus.Stopping;
            IsDisposed = true;
        }

        public void Start(int deviceIndex, System.Drawing.Size frameSize)
        {
            if (Status != PlayerStatus.NotStarted && Status != PlayerStatus.Stopped)
            {
                throw new InvalidOperationException("Current Status is neither  NotStarted nor Stopped");
            }
            var threadFetchFrame = new Thread(() =>
            {
                //the 2nd parameter is needed, or the constructor will cost a lot of time.
                //MSMF can help a very low overhead of readding frame(5ms-20ms), however, it takes minutes to start
                //the VideoCapture instance using my Logitech C920 webcamera.
                //Therefore, I turned to DShow+"MJPG", as a result, the reading of a frame takes about 50ms.
                using (VideoCapture camera = new VideoCapture(deviceIndex, VideoCaptureAPIs.DSHOW))
                {
                    camera.BufferSize = 3;
                    camera.FrameWidth = frameSize.Width;
                    camera.FrameHeight = frameSize.Height;
                    camera.FourCC = "MJPG";// it will reduce the overhead of VideoCapture.Read, and increase FPS
                    fetchFrameLoop(camera);
                }
                this.Status = PlayerStatus.Stopped;
            });
            threadFetchFrame.Start();
        }



        private void ProcessFrame(Mat frameMat)
        {
            if (this.frameFilterFunc != null)
            {
                this.frameFilterFunc(frameMat);
                if (frameMat.IsDisposed)
                {
                    throw new InvalidOperationException("Don't dispose the Mat parameter passed to FrameFilterFunc. We will dispose it later.");
                }
            }
            using (var frame = BitmapConverter.ToBitmap(frameMat))
            {
                if (NewFrameReceived != null)
                {
                    this.NewFrameReceived(frame);
                }
            }                
        }

        private void fetchFrameLoop(VideoCapture camera)
        {
            this.Status = PlayerStatus.Started;
            while (this.Status == PlayerStatus.Started)
            {
                Thread.Sleep(10);//reduce CPU pressure.
                Stopwatch sw = new Stopwatch();
                sw.Start();
                if (IsDisposed || IsDisposing)
                    break;
                Stopwatch stopwatch = Stopwatch.StartNew();
                Mat frameMat = new Mat();
                if (!camera.Read(frameMat))
                {
                    frameMat.Dispose();
                    continue;
                }
                if (frameMat.Empty())
                {
                    frameMat.Dispose();
                    continue;
                }
                //To order to archieve the required FPS, calc the millseconds to sleep.
                //if there is no left time to sleep, just sleep 1 ms.
                int msToSleep = Math.Max(1, 1000 / FramePerSecond - (int)stopwatch.ElapsedMilliseconds);
                Thread.Sleep(msToSleep);
                //run ProcessFrame() in a thread, so it will no affect the FPS of camera.Read()
                ThreadPool.QueueUserWorkItem(obj => {
                    Mat mat = (Mat)obj;
                    ProcessFrame(mat);
                    mat.Dispose();
                }, frameMat);
            }
        }

        public void SignalToStop()
        {
            if (this.Status != PlayerStatus.Started)
            {
                throw new InvalidOperationException("Not started");
            }
            this.Status = PlayerStatus.Stopping;
        }

        public async Task WaitForStopAsync()
        {
            while (this.Status != PlayerStatus.Stopped)
            {
                await Task.Delay(10);
            }
        }
    }
}
