using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Whisper.Utils
{
    public static class TextUtils
    {
        /// <summary>
        /// Copy null-terminated Utf8 string from native memory to managed.
        /// </summary>
        public static string StringFromNativeUtf8(IntPtr nativeUtf8)
        {
            // check input null
            if (nativeUtf8 == IntPtr.Zero)
                return null;
            
            // find null terminator
            var len = 0;
            while (Marshal.ReadByte(nativeUtf8, len) != 0) ++len;
            
            // check empty string
            if (len == 0)
                return "";
            
            // copy buffer from beginning to null position 
            var buffer = new byte[len];
            Marshal.Copy(nativeUtf8, buffer, 0, buffer.Length);
            return Encoding.UTF8.GetString(buffer);
        }
    }
}