using SharpDX;
using SharpDX.MediaFoundation;
using System;
using System.Runtime.InteropServices;

namespace Zack.CameraLib.Core
{
    public static class DXHelper
    {
        public static Activate[] EnumDeviceSources(MediaAttributes attributesRef)
        {

            IntPtr devicePtr;
            int devicesCount;

            EnumDeviceSources(attributesRef, out devicePtr, out devicesCount);

            var result = new Activate[devicesCount];

            unsafe
            {
                var address = (void**)devicePtr;
                for (var i = 0; i < devicesCount; i++)
                    result[i] = new Activate(new IntPtr(address[i]));
            }

            return result;
        }

        [DllImport("mf.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "MFEnumDeviceSources")]
        private unsafe static extern int MFEnumDeviceSources_(void* param0, void* param1, void* param2);

        internal unsafe static void EnumDeviceSources(MediaAttributes attributesRef, out IntPtr pSourceActivateOut, out int cSourceActivateRef)
        {
            IntPtr zero = IntPtr.Zero;
            IntPtr value = CppObject.ToCallbackPtr<MediaAttributes>(attributesRef);
            Result __result__;
            fixed (int* ptr = &cSourceActivateRef)
            {
                void* cSourceActivateRef_ = ptr;
                fixed (IntPtr* ptr2 = &pSourceActivateOut)
                {
                    void* pSourceActivateOut_ = ptr2;
                    __result__ = MFEnumDeviceSources_((void*)value, pSourceActivateOut_, cSourceActivateRef_);
                }
            }
            __result__.CheckError();
        }
    }
}
