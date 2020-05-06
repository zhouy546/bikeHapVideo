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
        // Native plugin rendering events are only called if a plugin is used
        // by some script. This means we have to DllImport at least
        // one function in some active script.

        #region enums
        // Available texture formats
        public enum NativeTextureFormat
        {
            Unknown = 0,
            Ru8,
            RGu8,
            RGBu8,
            RGBAu8,

            Ru16,
            RGu16,
            RGBu16,
            RGBAu16,

            Rf16,
            RGf16,
            RGBAf16,

            Ri16,
            RGi16,
            RGBAi16,

            Rf32,
            RGf32,
            RGBAf32,
            Ri32,
            RGi32,
            RGBAi32,

            DXT1,   // aka BC1
            DXT5,   // aka BC3
            RGTC1,  // aka BC4
        };
        
        #endregion

        #region dllimports

        [DllImport(_dllName)]
        public static extern bool Initialize();

        [DllImport(_dllName)]
        public static extern void Deinitialize();

        [DllImport(_dllName)]
        public static extern int GetPluginVersion(out int major, out int minor, out int revision);

        [DllImport(_dllName)]
        public static extern bool IsDemoVersion();

        [DllImport(_dllName)]
        public static extern int CreateNewMediaId();

        [DllImport(_dllName)]
        public static extern void DestroyMediaId(int mediaId);

        [DllImport(_dllName)]
        public static extern bool Open([MarshalAs(UnmanagedType.LPStr)] string url,
                                       bool enableAudio, bool useNativeAudioPlugin,
                                       bool preloadToMemory, SyncMode syncMode, 
                                       int mediaId);

        [DllImport(_dllName)]
        public static extern void Close(int mediaId);

		[DllImport(_dllName)]
		public static extern PixelFormat GetPixelFormat(int mediaId);

        [DllImport(_dllName)]
        public static extern bool AreNativeTexturesCreated(int mediaId);

		[DllImport(_dllName)]
		public static extern int GetNativeTexturesCount(int mediaId);

        [DllImport(_dllName)]
		public static extern bool GetNativeTexturePtrByIndex(int mediaId, int textureIndex,
                                                             out System.IntPtr texture,
                                                             out System.IntPtr shaderResourceView,
                                                             out int width, out int height,
                                                             out NativeTextureFormat format);
        [DllImport(_dllName)]
        public static extern bool GetResolution(int mediaId, out int width, out int height);

        [DllImport(_dllName)]
        public static extern void GetNeedFlipVideo(int mediaId, out bool flipX, out bool flipY);

        [DllImport(_dllName)]
        public static extern bool GetFramedropEnabled(int mediaId);

        [DllImport(_dllName)]
        public static extern void GetFramedropCount(int mediaId, out int earlyDrops, out int lateDrops);

        [DllImport(_dllName)]
        public static extern void SetFramedropEnabled(int mediaId, bool enabled);

        [DllImport(_dllName)]
        public static extern bool IsDecodingHardwareAccelerated(int mediaId);

        [DllImport(_dllName)]
        public static extern void GetActiveSegment(int mediaId, out float startTime, out float endTime);

        [DllImport(_dllName)]
        public static extern void SetActiveSegment(int mediaId, float startTime, float endTime);

        [DllImport(_dllName)]
        public static extern void GetActiveSegmentFrames(int mediaId, out int startFrame, out int endFrame);

        [DllImport(_dllName)]
        public static extern void SetActiveSegmentFrames(int mediaId, int startFrame, int endFrame);

        [DllImport(_dllName)]
        public static extern void ResetActiveSegment(int mediaId);

        [DllImport(_dllName)]
        public static extern void SetLoops(int mediaId, int loops);

        [DllImport(_dllName)]
        public static extern int GetLoops(int mediaId);

        [DllImport(_dllName)]
        public static extern void ToggleMute(int mediaId);

        [DllImport(_dllName)]
        public static extern void SetVolume(int mediaId, float volume);

        [DllImport(_dllName)]
        public static extern void Play(int mediaId);

        [DllImport(_dllName)]
        public static extern void Pause(int mediaId);

        [DllImport(_dllName)]
        public static extern void TogglePause(int mediaId);

        [DllImport(_dllName)]
        public static extern void Stop(int mediaId);

        [DllImport(_dllName)]
        public static extern SyncMode GetSyncMode(int mediaId);

        [DllImport(_dllName)]
        public static extern void SetSyncMode(int mediaId, SyncMode mode);

        [DllImport((_dllName))]
        public static extern float GetPlaybackRate(int mediaId);

        [DllImport((_dllName))]
        public static extern void SetPlaybackRate(int mediaId, float rate);

        [DllImport(_dllName)]
        public static extern void SeekToTime(int mediaId, float seconds);

        [DllImport(_dllName)]
        public static extern void SeekToFrame(int mediaId, int frame);

        [DllImport(_dllName)]
        public static extern void SeekToStart(int mediaId);

        [DllImport(_dllName)]
        public static extern void SeekToEnd(int mediaId);

        [DllImport(_dllName)]
        public static extern int GetNumFrames(int mediaId);

        [DllImport(_dllName)]
        public static extern float GetFramerate(int mediaId);

        [DllImport(_dllName)]
        public static extern void StepForward(int mediaId);

        [DllImport(_dllName)]
        public static extern void StepBackward(int mediaId);

        [DllImport(_dllName)]
        public static extern float GetDuration(int mediaId);

        [DllImport(_dllName)]
        public static extern float GetCurrentTime(int mediaId);

        [DllImport(_dllName)]
        public static extern int GetCurrentFrame(int mediaId);

        [DllImport(_dllName)]
        public static extern MediaState GetMediaState(int mediaId);

        [DllImport(_dllName)]
        public static extern bool CanPlay(int mediaId);

        [DllImport(_dllName)]
        public static extern bool IsPlaying(int mediaId);

        [DllImport(_dllName)]
        public static extern bool IsLooping(int mediaId);

        [DllImport(_dllName)]
        public static extern bool IsFinished(int mediaId);

        [DllImport(_dllName)]
        public static extern MediaError GetError(int mediaId);

		[DllImport(_dllName)]
		public static extern void SetAudioParams(SampleFormat sampleFormat, int sampleRate, int bufferLength, int channels);

		[DllImport(_dllName)]
		public static extern void FillAudioBuffer(int mediaId, float[] buffer, int offset, int length, int channels);
        #endregion
    }
}