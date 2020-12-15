这个库支持.NET Standard 2.0及以上，也就是说它支持.NET Core 3.1（及以上）、.NET framework 4.6.1（及以上）版本。

# Zack.CameraLib.Core
这个库用于列出所有摄像头（目前仅支持Windows）

第一步:

```
Install-Package Zack.CameraLib.Core
```

第二步:
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
用来展示摄像头中视频的.NET Core Windows Forms 控件。
下面是代码：

第一步:

```
Install-Package  Zack.WinFormCoreCameraPlayer
```

第二步:
向窗口中增加一个CameraPlayer控件。
由于Visual Studio的Bug，如果直接在设计器中增加CameraPlayer控件，可能会导致设计器崩溃。如果发生了这个问题，请使用代码来在界面中增加控件，而不是直接在设计器中增加（也就是InitializeComponent方法中）。参考代码如下：


```csharp
private CameraPlayer player;
public Form1()
{
	InitializeComponent();

	this.player = new CameraPlayer();
	this.player.Location = new System.Drawing.Point(0, 0);
	this.player.Dock = DockStyle.Fill;
	this.Controls.Add(this.player);
}
```

第三步:
```csharp
cameraPlayer.Start(0, new System.Drawing.Size(1024, 768));
```

Start方法的第一个参数代表要使用的摄像头的序号。可以使用上面提到的Zack.CameraLib.Core中的CameraUtils.ListCameras()来列出所有摄像头。

通过使用CameraPlayer类的SetFrameFilter()方法设定了一个过滤器，那么可以在Mat被渲染到界面之前对Mat进行处理。可以使用OpenCVSharp对Mat进行处理。

例子代码:

```csharp
cameraPlayer.SetFrameFilter(srcMat=> {
	Cv2.Blur(srcMat, srcMat, new Size(10, 10));
});
```

WinFormCoreDemo1 是例子项目。

# Zack.WinFormCameraPlayer
这是Zack.WinFormCoreCameraPlayer的.NET framework版本。

.NET framework版本的所有代码都和.NET core版本一摸一样，所以这里不再赘述。唯一的不同就是.NET framework版本的播放器的NuGet包名是Zack.WinFormCameraPlayer

WinFormDemo1 是例子项目。
