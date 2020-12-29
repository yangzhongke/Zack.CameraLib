using OpenCvSharp;
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Zack.WinFormCoreCameraPlayer
{
    public class CameraPlayer : Control
    {
        private Bitmap currentFrame = null;
        private object asyncLock = new object();
        private HeadlessCameraPlayer headlessPlayer = new HeadlessCameraPlayer();
        private Timer timerPaint = new Timer();
        public CameraPlayer()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw |
             ControlStyles.DoubleBuffer | ControlStyles.UserPaint, true);

            headlessPlayer.NewFrameReceived += HeadlessPlayer_NewFrameReceived;
            timerPaint.Interval = 17;
            timerPaint.Tick += TimerPaint_Tick;
            timerPaint.Enabled = true;
        }

        private void TimerPaint_Tick(object sender, EventArgs e)
        {
            this.Invalidate();
        }

        private void HeadlessPlayer_NewFrameReceived(Bitmap obj)
        {
            lock(asyncLock)
            {
                //必须把obj的内容复制一份出来，因为之后可能有若干次OnPaint，如果直接用这里传进来的bitmap，可能bitmap已经被Dispose了
                Bitmap newBitmap = new Bitmap(obj);
                this.currentFrame.TryDispose();
                this.currentFrame = newBitmap;
            }            
        }

        public PlayerStatus Status
        {
            get
            {
                return this.headlessPlayer.Status;
            }
        }

        public void SetFrameFilter(Action<Mat> frameFilterFunc)
        {
            headlessPlayer.SetFrameFilter(frameFilterFunc);
        }

        public int FramePerSecond
        {
            get
            {
                return this.headlessPlayer.FramePerSecond;
            }
            set
            {
                this.headlessPlayer.FramePerSecond = value;
            }
        }

        public void Start(int deviceIndex, System.Drawing.Size frameSize)
        {
            this.headlessPlayer.Start(deviceIndex, frameSize);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            //https://stackoverflow.com/questions/11020710/is-graphics-drawimage-too-slow-for-bigger-images
            /* just rely on the bitmap to fill the screen */
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (headlessPlayer.Status == PlayerStatus.Started)
            {
                lock(asyncLock)
                {
                    if (currentFrame == null) return;
                    e.Graphics.DrawImage(currentFrame, this.ClientRectangle);
                }                
            }
            else
            {
                e.Graphics.Clear(Color.Black);
                e.Graphics.DrawString(headlessPlayer.Status.ToString(), this.Font, Brushes.White, new PointF(0, 0));
            }
        }

        public void SignalToStop()
        {
            this.headlessPlayer.SignalToStop();
        }

        public Task WaitForStopAsync()
        {
            return this.headlessPlayer.WaitForStopAsync();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            headlessPlayer.Dispose();
            timerPaint.Enabled = false;
            timerPaint.Dispose();
        }
    }
}
