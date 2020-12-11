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
            this.player.FramePerSecond = 10;
            this.player.Dock = DockStyle.Fill;
            this.player.Location = new System.Drawing.Point(0, 0);
            this.Controls.Add(this.player);
            this.player.SetFrameFilter(BeautyIt);
        }


        private void BeautyIt(Mat srcMat)
        {
            Cv2.Blur(srcMat, srcMat, new Size(10, 10));
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
