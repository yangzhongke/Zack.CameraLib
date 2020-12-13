using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Zack.WinFormCoreCameraPlayer
{
    public class CameraPlayer : Control
    {
        private Bitmap frameBitmap;
        private object syncLock = new object();
        public PlayerStatus Status { get; private set; } = PlayerStatus.NotStarted;
        public Action<Mat> FrameFilterFunc { get; private set; }

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
        public CameraPlayer()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw |
             ControlStyles.DoubleBuffer | ControlStyles.UserPaint, true);
        }

        public void SetFrameFilter(Action<Mat> frameFilterFunc)
        {
            this.FrameFilterFunc = frameFilterFunc;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            //https://stackoverflow.com/questions/11020710/is-graphics-drawimage-too-slow-for-bigger-images
            /* just rely on the bitmap to fill the screen */
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (this.Status == PlayerStatus.Started)
            {
                lock (syncLock)
                {
                    if (this.frameBitmap == null) return;
                    //https://stackoverflow.com/questions/11020710/is-graphics-drawimage-too-slow-for-bigger-images
                    //var oldCompositingMode = e.Graphics.CompositingMode;
                    //improve performance
                    e.Graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                    e.Graphics.DrawImage(this.frameBitmap, this.ClientRectangle);
                    //e.Graphics.CompositingMode = oldCompositingMode;
                }
            }
            else
            {
                e.Graphics.Clear(Color.Black);
                e.Graphics.DrawString(this.Status.ToString(), this.Font, Brushes.White, new PointF(0, 0));
            }
        }

        private void BeginInvalidate()
        {
            if (!this.IsHandleCreated || this.IsDisposed || this.Disposing)
                return;
            try
            {
                this.BeginInvoke(new Action(this.Invalidate));
            }
            catch (InvalidOperationException ex)//maybe occured when control is being disposed.
            {
                Debug.WriteLine(ex);
            }
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
            if (this.FrameFilterFunc != null)
            {
                this.FrameFilterFunc(frameMat);
                if (frameMat.IsDisposed)
                {
                    throw new InvalidOperationException("Don't dispose the Mat parameter passed to FrameFilterFunc. We will dispose it later.");
                }
            }
            lock (syncLock)
            {
                if (this.frameBitmap != null)
                {
                    this.frameBitmap.Dispose();
                }
                this.frameBitmap = BitmapConverter.ToBitmap(frameMat);
            }
            BeginInvalidate();
        }

        private void fetchFrameLoop(VideoCapture camera)
        {            
            this.Status = PlayerStatus.Started;
            while (this.Status == PlayerStatus.Started)
            {
                Thread.Sleep(10);//reduce CPU pressure.
                Stopwatch sw = new Stopwatch();
                sw.Start();
                if (IsDisposed || Disposing)
                    break;
                if (!this.Visible)
                {
                    continue;
                }
                int clientWidth = ClientSize.Width;
                int clientHeight = ClientSize.Height;
                if (clientWidth <= 0 || clientHeight <= 0)
                {
                    continue;
                }
                Stopwatch stopwatch = Stopwatch.StartNew();
                Mat frameMat = new Mat();
                if (!camera.Read(frameMat))
                {
                    frameMat.Dispose();
                    continue;
                }
                //To order to archieve the required FPS, calc the millseconds to sleep.
                //if there is no left time to sleep, just sleep 1 ms.
                int msToSleep = Math.Max(1,1000/FramePerSecond- (int)stopwatch.ElapsedMilliseconds);
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

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            this.Status = PlayerStatus.Stopping;
            //prevent AccessViolationException on exit
            lock (syncLock)
            {
                if(this.frameBitmap!=null)
                {
                    this.frameBitmap.Dispose();
                }                
            }
        }
    }
}
