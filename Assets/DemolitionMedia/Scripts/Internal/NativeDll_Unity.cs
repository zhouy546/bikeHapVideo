// on OpenGL ES there is no way to query texture extents from native texture id
#if (UNITY_IPHONE || UNITY_ANDROID) && !UNITY_EDITOR
    #define UNITY_GLES_RENDERER
#endif

using System;
using System.Collections;
using System.Runtime.InteropServices;


namespace DemolitionStudios.DemolitionMedia
{
    internal partial class NativeDll
    {
#if UNITY_IPHONE && !UNITY_EDITOR
		private const string _dllName = "__Internal";
#else
        private const string _dllName = "AudioPluginDemolitionMedia";
#endif

#if false
    // We'll also pass native pointer to a texture in Unity.
    // The plugin will fill texture data from native code.
    [DllImport(_dllName)]
//#if UNITY_GLES_RENDERER
	public static extern void DemolitionVideoSetTexture(System.IntPtr texture, int w, int h);
//#else
//    public static extern void DemolitionVideoSetTexture(System.IntPtr texture);
//#endif
#endif

        [DllImport(_dllName)]
        public static extern IntPtr GetRenderEventFunc();
    }
}