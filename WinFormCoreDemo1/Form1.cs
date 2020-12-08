using OpenCvSharp;
using System;
using System.Windows.Forms;
using Zack.WinFormCoreCameraPlayer;

namespace WinFormCoreDemo1
{
    public partial class Form1 : Form
    {
        private CameraPlayer player;
        public Form1()
        {
            InitializeComponent();

            this.player = new CameraPlayer();
            this.player.Dock = DockStyle.Fill;
            this.player.Location = new System.Drawing.Point(0, 0);
            this.Controls.Add(this.player);
            this.player.Click += Player_Click;

            this.player.Click += (s, e) => {
                if(this.player.FrameFilterFunc==null)
                {
                    this.player.SetFrameFilter(BeautyIt);
                }
                else
                {
                    this.player.SetFrameFilter(null);
                }
            };
        }

        private async void Player_Click(object sender, EventArgs e)
        {
            player.SignalToStop();
            await player.WaitForStop();
            player.Start(0, new System.Drawing.Size(500, 500));
        }

        private Mat zeros_like(Mat mat)
        {
            return new Mat(mat.Rows, mat.Cols, MatType.CV_8U, Scalar.All(0));
        }

        private Mat BeautyIt(Mat srcMat)
        {
            //https://blog.csdn.net/Raink_LH/article/details/107040161
            /*
            var dest = new Mat();
            Cv2.BilateralFilter(srcMat, dest, 10, 60, 60);
            return dest;*/
            //磨皮程度与细节程度的确定
            int v1 = 3;
            int v2 = 1;
            int dx = v1 * 5;// # 双边滤波参数之一 
            double fc = v1 * 12.5;//// # 双边滤波参数之一 
            double p = 0.1;


            var temp1 = new Mat();
            Cv2.BilateralFilter(srcMat, temp1, dx, fc, fc);
            Mat temp2 = new Mat();
            Cv2.Subtract(temp1, srcMat, temp2);

            Cv2.Add(temp2, new Scalar(10, 10, 10, 128), temp2);
            var temp3 = new Mat();
            Cv2.GaussianBlur(temp2, temp3, new Size(2 * v2 - 1, 2 * v2 - 1), 0);
            var temp4 = new Mat();
            Cv2.Add(srcMat, temp3, temp4);
            var dst = new Mat();
            Cv2.AddWeighted(srcMat, p, temp4, 1 - p, 0.0, dst);

            Cv2.Add(dst, new Scalar(10, 10, 10, 255), dst);

            temp1.Dispose();
            temp2.Dispose();
            temp3.Dispose();
            temp4.Dispose();
            return dst;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            FormSelectCamera selectForm = new FormSelectCamera();
            if(selectForm.ShowDialog()== DialogResult.OK)
            {
                player.Start(selectForm.DeviceIndex,selectForm.Resolution);
            }
            else
            {
                this.Close();
            }
        }
    }
}
