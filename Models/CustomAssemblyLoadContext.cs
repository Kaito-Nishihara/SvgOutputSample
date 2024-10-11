using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System;

namespace SvgOutputSample.Models
{
    public class CustomAssemblyLoadContext : AssemblyLoadContext
    {
        public IntPtr LoadUnmanagedLibrary(string absolutePath)
        {
            // NativeLibrary.Loadを使用してDLLをロードする
            IntPtr handle = NativeLibrary.Load(absolutePath);
            if (handle == IntPtr.Zero)
            {
                throw new Exception($"Failed to load unmanaged library from path: {absolutePath}");
            }
            return handle;
        }

    }
}
