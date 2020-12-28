using OpenCvSharp;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Zack.WinFormCoreCameraPlayer
{
    public class CameraPlayer : Control
    {
        private HeadlessCameraPlayer headlessPlayer = new HeadlessCameraPlayer();
        public CameraPlayer()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw |
             ControlStyles.DoubleBuffer | ControlStyles.UserPaint, true);

            headlessPlayer.NewFrameReceived += this.BeginInvalidate;
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
                var frame = this.headlessPlayer.CurrentFrame;
                if (frame == null) return;
                Stopwatch sw = new Stopwatch();
                sw.Start();
                e.Graphics.DrawImage(frame, this.ClientRectangle);
                Debug.WriteLine(sw.ElapsedMilliseconds);
            }
            else
            {
                e.Graphics.Clear(Color.Black);
                e.Graphics.DrawString(headlessPlayer.Status.ToString(), this.Font, Brushes.White, new PointF(0, 0));
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
        }
    }
}
