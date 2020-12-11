using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Zack.WinFormCoreCameraPlayer
{
    public class CameraPlayer:Control
    {
        private Mat frameMat = new Mat();
        private object syncLock = new object();
        public PlayerStatus Status { get; private set; } =PlayerStatus.NotStarted;
        private System.Windows.Forms.Timer repaintTimer = new System.Windows.Forms.Timer();
        public Action<Mat> FrameFilterFunc { get; private set; }

        private int _framePerSecond = 10;
        public int FramePerSecond 
        { 
            get
            {
                return this._framePerSecond;
            }
            set
            {
                if(value<1||value>60)
                {
                    throw new ArgumentOutOfRangeException("FramePerSecond shoud >=1 and <=60");
                }
                this._framePerSecond = value;
                repaintTimer.Interval = 1000 / value;
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
            if(this.Status== PlayerStatus.Started)
            {
                if (frameMat.Width <= 0||frameMat.Height<=0) return;
                Bitmap bitmap;
                lock (syncLock)
                {
                    if (this.FrameFilterFunc != null)
                    {
                        this.FrameFilterFunc(frameMat);
                        if (frameMat.IsDisposed)
                        {
                            throw new InvalidOperationException("Don't dispose the Mat parameter passed to FrameFilterFunc. We will dispose it later.");
                        }
                    }
                    bitmap = BitmapConverter.ToBitmap(frameMat);
                }
                using (bitmap)
                {
                    //https://stackoverflow.com/questions/11020710/is-graphics-drawimage-too-slow-for-bigger-images
                    var oldCompositingMode = e.Graphics.CompositingMode;
                    //improve performance
                    e.Graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                    e.Graphics.DrawImage(bitmap, this.ClientRectangle);
                    e.Graphics.CompositingMode = oldCompositingMode;
                }
            }
            else
            {
                e.Graphics.Clear(Color.Black);
                e.Graphics.DrawString(this.Status.ToString(), this.Font, Brushes.White, new PointF(0, 0));
            }
        }

        public void Start(int deviceIndex,System.Drawing.Size frameSize)
        {
            if(Status!= PlayerStatus.NotStarted&& Status != PlayerStatus.Stopped)
            {
                throw new InvalidOperationException("Current Status is neither  NotStarted nor Stopped");
            }

            //https://stackoverflow.com/questions/11020710/is-graphics-drawimage-too-slow-for-bigger-images
            this.repaintTimer.Tick += (se, e) => { 
                this.Invalidate(); 
            };
            this.repaintTimer.Interval = _framePerSecond;
            this.repaintTimer.Start();

            var threadFetchFrame = new Thread(() => {
                //the 2nd parameter is needed, or the constructor will cost a lot of time.
                using (VideoCapture camera = new VideoCapture(deviceIndex, VideoCaptureAPIs.DSHOW))
                {
                    camera.FrameWidth = frameSize.Width;
                    camera.FrameHeight = frameSize.Height;
                    fetchFrameLoop(camera);
                }
                this.Status = PlayerStatus.Stopped;
            });
            threadFetchFrame.Start();
        }

        private void fetchFrameLoop(VideoCapture camera)
        {
            this.Status = PlayerStatus.Started;
            while (this.Status == PlayerStatus.Started)
            {
                if (IsDisposed || Disposing)
                    break;
                //if this control is not visible, stop the fetch frame, so that it can reduce the pressure on CPU
                //if (!Win32.IsWindowPall(hwnd))
                if (!this.Visible)
                {
                    Thread.Sleep(10);
                    continue;
                }
                int clientWidth = ClientSize.Width;
                int clientHeight = ClientSize.Height;
                if (clientWidth <= 0 || clientHeight <= 0)
                {
                    Thread.Sleep(10);
                    continue;
                }
                lock (syncLock)
                {
                    if (IsDisposed || Disposing)
                        break;
                    camera.Read(frameMat);
                }
                Thread.Sleep(1000 / 60);//reduce CPU pressure.
            }
        }

        public void SignalToStop()
        {
            if (this.Status!= PlayerStatus.Started)
            {
                throw new InvalidOperationException("Not started");
            }
            this.repaintTimer.Stop();
            this.Status = PlayerStatus.Stopping;
        }

        public async Task WaitForStopAsync()
        {
            while(this.Status!= PlayerStatus.Stopped)
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
                this.frameMat.Dispose();
            }
            this.repaintTimer.Dispose();
        }
    }
}
