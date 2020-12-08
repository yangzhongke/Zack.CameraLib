using DirectShowLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Zack.CameraLib.Core
{
    public class CameraUtils
    {
        public static CameraInfo[] ListCameras()
        {
            DsDevice[] capDevicesDS = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
            return capDevicesDS.Select((d,index)=>new CameraInfo { FriendlyName=d.Name,Index=index,
                VideoCapabilities= GetAllAvailableResolution(d).ToArray()}).ToArray();
        }

        private static IEnumerable<VideoCapabilities> GetAllAvailableResolution(DsDevice vidDev)
        {
            //I used to use SharpDX.MediaFoundation to enumerate all camera and its supported resolution
            //however, according to https://stackoverflow.com/questions/24612174/mediafoundation-can%C2%B4t-find-video-capture-emulator-driver-but-directshow-does,
            //MediaFoundation cannot find virtual camera, so I turned to use IPin.EnumMediaTypes to fetch supported resolution
            //https://stackoverflow.com/questions/20414099/videocamera-get-supported-resolutions
            int hr, bitCount = 0;

            IBaseFilter sourceFilter;

            var m_FilterGraph2 = new FilterGraph() as IFilterGraph2;
            hr = m_FilterGraph2.AddSourceFilterForMoniker(vidDev.Mon, null, vidDev.Name, out sourceFilter);
            DsError.ThrowExceptionForHR(hr);
            var pRaw2 = DsFindPin.ByCategory(sourceFilter, PinCategory.Capture, 0);
            var availableResolutions = new List<VideoCapabilities>();

            VideoInfoHeader v = new VideoInfoHeader();
            IEnumMediaTypes mediaTypeEnum;
            hr = pRaw2.EnumMediaTypes(out mediaTypeEnum);
            DsError.ThrowExceptionForHR(hr);

            AMMediaType[] mediaTypes = new AMMediaType[1];
            IntPtr fetched = IntPtr.Zero;
            hr = mediaTypeEnum.Next(1, mediaTypes, fetched);
            DsError.ThrowExceptionForHR(hr);

            while (fetched != null && mediaTypes[0] != null)
            {
                Marshal.PtrToStructure(mediaTypes[0].formatPtr, v);
                if (v.BmiHeader.Size != 0 && v.BmiHeader.BitCount != 0)
                {
                    if (v.BmiHeader.BitCount > bitCount)
                    {
                        availableResolutions.Clear();
                        bitCount = v.BmiHeader.BitCount;
                    }

                    VideoCapabilities cap = new VideoCapabilities();
                    cap.Height = v.BmiHeader.Height;
                    cap.Width = v.BmiHeader.Width;
                    //the unit of AvgTimePerFrame is 100 nanoseconds,
                    //and 10^9 nanosenconds = 1 second
                    cap.FrameRate = (int)(1000_000_000/100/v.AvgTimePerFrame);
                    cap.BitRate = v.BitRate;
                    availableResolutions.Add(cap);
                }
                hr = mediaTypeEnum.Next(1, mediaTypes, fetched);
                DsError.ThrowExceptionForHR(hr);
            }
            return availableResolutions;
        }
    }
}
