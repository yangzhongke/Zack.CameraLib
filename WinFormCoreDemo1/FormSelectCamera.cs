using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Zack.CameraLib.Core;

namespace WinFormCoreDemo1
{
    public partial class FormSelectCamera : Form
    {
        public int DeviceIndex { get; set; }
        public Size Resolution { get; set; }

        public FormSelectCamera()
        {
            InitializeComponent();

            var cameras = CameraUtils.ListCameras();
            cmbCamera.DisplayMember = nameof(CameraInfo.FriendlyName);
            cmbCamera.ValueMember = nameof(CameraInfo.Index);
            cmbCamera.DataSource = cameras;

            this.StartPosition = FormStartPosition.CenterScreen;
        }


        private void btnOK_Click(object sender, EventArgs e)
        {
            VideoCapabilities videoCap = (VideoCapabilities)cmbResolution.SelectedItem;
            this.Resolution = new Size(videoCap.Width,videoCap.Height);
        }

        private void cmbCamera_SelectedIndexChanged(object sender, EventArgs e)
        {
            CameraInfo camera = (CameraInfo)cmbCamera.SelectedItem;
            this.DeviceIndex = camera.Index;
            cmbResolution.DataSource = camera.VideoCapabilities;
        }
    }
}
