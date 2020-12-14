using System;

namespace Zack.WinFormCoreCameraPlayer
{
    static class Utils
    {
        public static void TryDispose(this IDisposable obj)
        {
            if (obj!=null)
            {
                obj.Dispose();
            }
        }
    }
}
