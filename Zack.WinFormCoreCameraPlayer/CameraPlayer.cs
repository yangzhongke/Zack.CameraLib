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
        public Func<Mat, Mat> FrameFilterFunc { get; private set; }

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

        public void SetFrameFilter(Func<Mat, Mat> frameFilterFunc)
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
                if (frameMat.Width <= 0) return;
                Bitmap bitmap;
                lock (syncLock)
                {
                    var newFrameMat = this.frameMat;
                    if (this.FrameFilterFunc != null)
                    {
                        newFrameMat = this.FrameFilterFunc(newFrameMat);
                    }
                    bitmap = BitmapConverter.ToBitmap(newFrameMat);
                    //dispose the Mat that frameFilterFunc returns
                    if (newFrameMat != this.frameMat)
                    {
                        newFrameMat.Dispose();
                    }
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
            this.repaintTimer.Tick += (se, e) => { this.Invalidate(); };
            this.repaintTimer.Interval = _framePerSecond;
            this.repaintTimer.Start();

            var hwnd = this.Handle;
            var threadFetchFrame = new Thread(() => {
                //the 2nd parameter is needed, or the constructor will cost a lot of time.
                using (VideoCapture camera = new VideoCapture(deviceIndex, VideoCaptureAPIs.DSHOW))
                {
                    camera.FrameWidth = frameSize.Width;
                    camera.FrameHeight = frameSize.Height;

                    this.Status = PlayerStatus.Started;
                    while (this.Status == PlayerStatus.Started)
                    {
                        if (IsDisposed || Disposing)
                            return;
                        lock (syncLock)
                        {
                            //if this control is not visible, stop the fetch frame, so that it can reduce the pressure on CPU
                            if (!Win32.IsWindowPall(hwnd))
                            {
                                camera.Read(frameMat);
                                //if the size of Mat is bigger than the size of player,
                                //shrink the Mat to the size of player, so that it's more performant
                                if (frameMat.Width > ClientSize.Width && frameMat.Height > ClientSize.Height)
                                {
                                    var newMat = frameMat.Resize(new OpenCvSharp.Size(ClientSize.Width, ClientSize.Height));
                                    frameMat.Dispose();
                                    this.frameMat = newMat;
                                }
                            }
                        }
                        Thread.Sleep(1000 / 60);//reduce CPU pressure.
                    }                    
                }
                this.Status = PlayerStatus.Stopped;
            });
            threadFetchFrame.Start();
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

        public async Task WaitForStop()
        {
            while(this.Status!= PlayerStatus.Stopped)
            {
                await Task.Delay(10);
            }
        }

        protected override void Dispose(bool disposing)
        {
            this.Status = PlayerStatus.Stopping;
            base.Dispose(disposing);            
            this.repaintTimer.Dispose();
            //prevent AccessViolationException on exit
            lock (syncLock)
            {
                this.frameMat.Dispose();
            }                
        }
    }
}
