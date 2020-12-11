
It supports .NET Standard 2.0( and above), so it supports .NET Core 3.1 (and above) and .NET framework(above 4.6.1).

# Zack.CameraLib.Core
Used for listing connected cameras(Windows-Only currently).

Step 1:

```
Install-Package Zack.CameraLib.Core
```

Step 2:
```csharp
var cameras = CameraUtils.ListCameras();
foreach(CameraInfo camera in cameras)
{
	Console.WriteLine(camera.FriendlyName+","+camera.Index);
	foreach(VideoCapabilities v in camera.VideoCapabilities)
	{
		Console.WriteLine($"{v.FrameRate},{v.Height},{v.Width},{v.BitRate},{v.FrameRate}");
	}
}
```

# Zack.WinFormCoreCameraPlayer
A control used for displaying video of camera.
Sample code of WinForm for .NET Core.

Step 1:

```
Install-Package  Zack.WinFormCoreCameraPlayer
```

Step 2:

Add a CameraPlayer to a form. 

Due to a possible bug of Visual Studio, a CameraPlayer instance in the InitializeComponent may lead to error during design time. If this bug happens, please add the CameraPlayer instance by code. for example:

```csharp
        private CameraPlayer player;
        public Form1()
        {
            InitializeComponent();

            this.player = new CameraPlayer();
			this.player.Location = new System.Drawing.Point(0, 0);
            this.player.Dock = DockStyle.Fill;
            this.Controls.Add(this.player);
            this.player.Click += Player_Click;
		}
```

Step 3:
```csharp
cameraPlayer.Start(0, new System.Drawing.Size(1024, 768));
```

The first parameter of Start is the index of selected camera.

If set a filter using SetFrameFilter(), the Image can be processed before the Mat is rendered. OpenCVSharp can be used to process the Mat.

Sample code:

```csharp
cameraPlayer.SetFrameFilter(srcMat=> {
	Cv2.Blur(srcMat, srcMat, new Size(10, 10));
});
```

WinFormCoreDemo1 is the sample project.

# Zack.WinFormCameraPlayer
This is the .NET framework version of Zack.WinFormCoreCameraPlayer.

All the code of this .NET framework version is the same as the .NET core version. The only difference is the NuGet package name is Zack.WinFormCameraPlayer

WinFormDemo1 is the sample project.

