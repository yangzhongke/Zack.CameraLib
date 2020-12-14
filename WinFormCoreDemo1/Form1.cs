using OpenCvSharp;
using System;
using System.Threading.Tasks;
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
            this.player.FramePerSecond = 20;
            this.player.Dock = DockStyle.Fill;
            this.player.Location = new System.Drawing.Point(0, 0);
            this.Controls.Add(this.player);
            this.player.SetFrameFilter(BeautyIt);
            this.player.Click += Player_Click;
            this.FormClosed += Form1_FormClosed;
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.player.Dispose();
        }

        private async void Player_Click(object sender, EventArgs e)
        {
            this.player.SignalToStop();
            await this.player.WaitForStopAsync();
            await Task.Delay(100);
            this.player.Start(3, this.ClientSize);
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
