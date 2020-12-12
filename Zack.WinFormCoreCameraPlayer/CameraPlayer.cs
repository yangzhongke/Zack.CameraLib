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
        private Mat frameMat = new Mat();
        private object syncLock = new object();
        public PlayerStatus Status { get; private set; } = PlayerStatus.NotStarted;
        //System.Windows.Forms.Timer will be blocked when OnPaint running
        private System.Threading.Timer repaintTimer;
        public Action<Mat> FrameFilterFunc { get; private set; }

        private int _framePerSecond = 30;
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
                if(this.repaintTimer!=null)
                {
                    repaintTimer.Change(1, 1000 / value);
                }                
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
                if (frameMat.Width <= 0 || frameMat.Height <= 0) return;
                using (Mat srcMat = new Mat(this.frameMat.Size(),this.frameMat.Type()))
                {
                    //try to reduce the timespan of lock, so that it will not block the camera.Read() for a long time.
                    lock (syncLock)
                    {
                        this.frameMat.CopyTo(srcMat);
                    }
                    //Debug.WriteLine($"1:{sw.ElapsedMilliseconds}");
                    if (this.FrameFilterFunc != null)
                    {
                        this.FrameFilterFunc(srcMat);
                        if (srcMat.IsDisposed)
                        {
                            throw new InvalidOperationException("Don't dispose the Mat parameter passed to FrameFilterFunc. We will dispose it later.");
                        }
                    }
                    using (var bitmap = BitmapConverter.ToBitmap(srcMat))
                    {
                        //https://stackoverflow.com/questions/11020710/is-graphics-drawimage-too-slow-for-bigger-images
                        var oldCompositingMode = e.Graphics.CompositingMode;
                        //improve performance
                        e.Graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                        e.Graphics.DrawImage(bitmap, this.ClientRectangle);
                        e.Graphics.CompositingMode = oldCompositingMode;
                    }
                }                    
            }
            else
            {
                e.Graphics.Clear(Color.Black);
                e.Graphics.DrawString(this.Status.ToString(), this.Font, Brushes.White, new PointF(0, 0));
            }
        }

        public void Start(int deviceIndex, System.Drawing.Size frameSize)
        {
            if (Status != PlayerStatus.NotStarted && Status != PlayerStatus.Stopped)
            {
                throw new InvalidOperationException("Current Status is neither  NotStarted nor Stopped");
            }
            //https://stackoverflow.com/questions/11020710/is-graphics-drawimage-too-slow-for-bigger-images
            repaintTimer = new System.Threading.Timer(obj => {
                if (!this.IsHandleCreated || this.IsDisposed || this.Disposing)
                    return;
                try
                {
                    this.BeginInvoke(new Action(this.Invalidate));
                }
                catch (Exception ex)//maybe occured when control is being disposed.
                {
                    Debug.WriteLine(ex);
                }
            }, null, 10, 1000 / FramePerSecond);

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

        private void fetchFrameLoop(VideoCapture camera)
        {            
            this.Status = PlayerStatus.Started;
            while (this.Status == PlayerStatus.Started)
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                if (IsDisposed || Disposing)
                    break;
                //if this control is not visible, stop the fetch frame, so that it can reduce the pressure on CPU
                //if (!Win32.IsWindowPall(hwnd))
                if (!this.Visible)
                {
                    Thread.Sleep(5);
                    continue;
                }
                int clientWidth = ClientSize.Width;
                int clientHeight = ClientSize.Height;
                if (clientWidth <= 0 || clientHeight <= 0)
                {
                    Thread.Sleep(5);
                    continue;
                }
                lock (syncLock)
                {                   
                    if (IsDisposed || Disposing)
                        break;
                    Debug.WriteLine($"1:{sw.ElapsedMilliseconds}");
                    //把OnPaint和filterFunc放到不同的线程中，不在OnPaint中进行FilterFunc，用多线程，看能否提升FPS
                    camera.Read(frameMat);//it takes about 50 ms every frame, maybe fps can be increased by multi-threaded reading( into a queue)
                    Debug.WriteLine($"2:{sw.ElapsedMilliseconds}");
                }
                Thread.Sleep(5);//reduce CPU pressure.
            }
        }

        public void SignalToStop()
        {
            if (this.Status != PlayerStatus.Started)
            {
                throw new InvalidOperationException("Not started");
            }
            if (this.repaintTimer != null)
            {
                this.repaintTimer.Dispose();
                this.repaintTimer = null;
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
                this.frameMat.Dispose();
            }
            if (this.repaintTimer != null)
            {
                this.repaintTimer.Dispose();
                this.repaintTimer = null;
            }
        }
    }
}
