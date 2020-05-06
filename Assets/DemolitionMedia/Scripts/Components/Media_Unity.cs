using System;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
    using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;


namespace DemolitionStudios.DemolitionMedia
{
    [AddComponentMenu("Demolition Media/Media")]
    public partial class Media : MonoBehaviour
    {
    #region fields
        /// Possible types of media url
		public enum UrlType
        {
            Absolute,
            RelativeToProjectPath,
            RelativeToStreamingAssetsPath,
            RelativeToDataPath,
            RelativeToPeristentPath,
        }
        /// Current mediaUrl type
        public UrlType urlType = UrlType.Absolute;

        /// Native textures list
		private List<Texture2D> _nativeTextures = new List<Texture2D>();
        /// Output texture for rendering the video stream frames
        public Texture VideoRenderTexture
        {
            get
            {
                if (_colorConversionMaterial != null)
                {
                    // Could be null in the beginning
                    return _colorConversionRenderTexture;
                }
                if (_nativeTextures.Count == 0)
                    return null;
                // Assuming _nativeTextures.Count == 1
                return _nativeTextures[0];
            }
        }

        /// Unity audio mixer
        private AudioMixer _audioMixer;

        // Render texture for video color conversion.
        // Being used if material is not null
        private RenderTexture _colorConversionRenderTexture = null;

        // Hap Q shader (YCoCg -> RGB)
        private static Shader _shaderHapQ;
        // Hap Q Alpha shader ((YCoCg, A) -> RGBA)
        private static Shader _shaderHapQAlpha;

        /// Material used for video color conversion
        private Material _colorConversionMaterial = null;

#if UNITY_EDITOR
        /// Whether the media was playing before the in-editor pause
        bool _wasPlayingBeforeEditorPause = false;
#endif
    #endregion

    #region monobehavior impl
        public virtual void Start()
        {
#if UNITY_EDITOR
            // Subscribe for the editor playmode state changed events (pause/play)
            EditorApplication.playmodeStateChanged = HandleOnPlayModeChanged;
#endif

            // Check whether MediaManager comonnent presents in the scene
            Debug.Assert(MediaManager.Instance != null,
                         "[DemolitionMedia] Please add DemolitionMediaManager component to your scene. " +
                         "You can simply attach it to an empty actor. See the documentation pdf file for more details.");

            // Check whether the active graphics device type is supported by the plugin
            var gfxDevice = SystemInfo.graphicsDeviceType;
            Debug.Assert(gfxDevice == UnityEngine.Rendering.GraphicsDeviceType.Direct3D9 ||
                         gfxDevice == UnityEngine.Rendering.GraphicsDeviceType.Direct3D11 ||
                         gfxDevice == UnityEngine.Rendering.GraphicsDeviceType.OpenGLCore ||
                         gfxDevice == UnityEngine.Rendering.GraphicsDeviceType.Metal,
                         "[DemolitionMedia] unsupported graphics device type: " + gfxDevice +
                         ". Please use one of the supported graphics device types, which are [Direct3D9, " +
                         "Direct3D11, OpenGLCore] on Windows platform and [Metal, OpenGLCore] on OS X platform.");

            // Set the global audio params anyways
            var audioConfiguration = AudioSettings.GetConfiguration();
            //var bufferSize = AudioSettings.GetDSPBufferSize();
            var bufferSize = audioConfiguration.dspBufferSize;
            var sampleRate = audioConfiguration.sampleRate;
            var channels = GetAudioChannelCount(audioConfiguration.speakerMode);
            NativeDll.SetAudioParams(SampleFormat.Float, sampleRate, bufferSize, channels);

            // Handle audio: can be enabled in run-time, so init the audio mixer anyway
            //if (enableAudio)
            initAudioMixer();

            // Load the global shaders
            if (_shaderHapQ == null)
            {
                _shaderHapQ = Shader.Find("DemolitionMedia/HapQ");
                if (_shaderHapQ == null)
                {
                    Debug.LogError("[DemolitionMedia] unable to load \"DemolitionMedia/HapQ\" shader from resources. Check the plugin installation completeness.");
                }
            }
            if (_shaderHapQAlpha == null)
            {
                _shaderHapQAlpha = Shader.Find("DemolitionMedia/HapQAlpha");
                if (_shaderHapQAlpha == null)
                {
                    Debug.LogError("[DemolitionMedia] unable to load \"DemolitionMedia/HapQAlpha\" shader from resources. Check the plugin installation completeness.");
                }
            }

            // Load on start mechanism
            if (openOnStart)
                Open(mediaUrl);
        }

        private void Update()
        {
            // Handle errors and events
            PopulateEvents();
            PopulateErrors();

            // Color conversion / custom material
            PerformColorConversionIfNeeded();

            // Create external texture(s) if needed
            if (_nativeTextures.Count == 0)
            {
                CreateExternalTextures();
            }

            // Map the video texture to the Renderer component material (if found one)
            if (!_textureSet && VideoRenderTexture != null)
            {
                // Note: using the try/catch (MissingComponentException) caused console garbage on ios
                var rendererComp = GetComponent<Renderer>();
                // Check whether the Renderer component is present
                if (rendererComp != null)
                {
                    rendererComp.material.mainTexture = VideoRenderTexture;
                    _textureSet = true;
                }
            }

            if (_mediaId >= 0)
            {
                //_videoCurrentFrame = NativePlugin.GetCurrentFrame(_mediaId);
                //_videoDroppedFrames += 
                //	Math.Max(Math.Abs(_videoCurrentFrame - _videoLastFrame) - 2, 0);
                //_videoLastFrame = _videoCurrentFrame;

                _videoDecodeFramerateTime += Time.deltaTime;

                // FIXME: hack for small intervals
                //if (_videoDecodeFramerateInterval < 0.5f)
                if (_endFrame > _startFrame && _endFrame - _startFrame < 2.0 * VideoFramerate)
                {
                    // Make it constant, since can't measure
                    _videoDecodeFramerate = VideoFramerate;
                }
                else if (_videoDecodeFramerateTime >= _videoDecodeFramerateInterval)
                {
                    var currentFrame = VideoCurrentFrame;
                    var decodedFrames = Math.Abs(currentFrame - _videoDecodeFramerateFrameCount);
                    _videoDecodeFramerate = decodedFrames / _videoDecodeFramerateTime;
                    _videoDecodeFramerate = Mathf.Max(_videoDecodeFramerate, 0.0f);
                    _videoDecodeFramerateFrameCount = currentFrame;
                    _videoDecodeFramerateTime = 0.0f;
                }

                // Timecode: update
                //if (State == MediaState.Playing)
                //{
                //    if (Timecode >= DurationSeconds)
                //    {
                //        Timecode = 0.0f;
                //    }
                //    //if (Timecode <= 0.0f) {
                //    //    Timecode = DurationSeconds;
                //    //}
                //    else {
                //        Timecode += 4.0f * Time.deltaTime;
                //    }
                //    NativeDll.SetExternalClockValue(_mediaId, Timecode);
                //}
            }
        }

#if UNITY_EDITOR
        void HandleOnPlayModeChanged()
        {
            // This method is run whenever the playmode state is changed.
            if (EditorApplication.isPaused)
            {
                //Debug.Log("Game paused");

                if (IsPlaying)
                {
                    _wasPlayingBeforeEditorPause = true;
                    Pause();
                }
            }
            else
            {
                //Debug.Log("Game unpaused");

                if (_wasPlayingBeforeEditorPause)
                {
                    Play();
                }
                _wasPlayingBeforeEditorPause = false;
            }
        }
#endif

        // TODO: optional pause with OnApplicationPause
        // https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnApplicationPause.html
        void OnApplicationPause(bool pauseStatus)
        {
            //Debug.Log("Pause status: " + pauseStatus.ToString());
            //isPaused = pauseStatus;
        }

        public void OnDestroy()
        {
            Close();
            NativeDll.DestroyMediaId(_mediaId);
        }
    #endregion

    #region internal
        private UnityEngine.TextureFormat TextureFormatNativeToUnity(NativeDll.NativeTextureFormat format)
        {
            UnityEngine.TextureFormat result;
            switch (format)
            {
                case NativeDll.NativeTextureFormat.RGBAu8: result = TextureFormat.RGBA32; break;
                case NativeDll.NativeTextureFormat.Ru8: result = TextureFormat.Alpha8; break;
                case NativeDll.NativeTextureFormat.RGu8: result = TextureFormat.RGHalf; break;

                case NativeDll.NativeTextureFormat.Ru16: result = TextureFormat.R16; break;

                case NativeDll.NativeTextureFormat.DXT1: result = TextureFormat.DXT1; break;
                case NativeDll.NativeTextureFormat.DXT5: result = TextureFormat.DXT5; break;
#if UNITY_5_5_OR_NEWER || UNITY_2017
                case NativeDll.NativeTextureFormat.RGTC1: result = TextureFormat.BC4; break;
#endif

                default:
                    Debug.LogWarning("[DemolitionMedia] unknown texture format: " + format.ToString());
                    // Try to fallback to rgba texture format
                    result = TextureFormat.RGBA32;
                    break;
            }

            // Note: DXT1/DXT5/BC4 are reported by unity as unsupported, although they works
            //var formatSupported = SystemInfo.SupportsTextureFormat(result);
            //if (!formatSupported) {
            //	Debug.LogError("[DemolitionMedia] unsupported texture format: " +
            //		result.ToString());
            //}

            return result;
        }

        public bool CreateExternalTextures()
        {
            // Get the video frame pixel format: it should be available prior the texture creation
            var pixelFormat = NativeDll.GetPixelFormat(_mediaId);
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Direct3D9 &&
                pixelFormat == PixelFormat.YCoCgAndAlphaP)
            {
                // TODO: at some point use the standard way of generating errors in the native plugin for this one
                InvokeEvent(MediaEvent.Type.PlaybackErrorOccured, MediaError.GraphicsDeviceError);
                Close();

                Debug.LogError("[DemolitionMedia] Hap Q Alpha is unsupported on Direct3D9 graphics device, since it uses BC4 compressed texture");
                return false;
            }

            // Check whether the native texture(s) created already
            bool texturesCreated = NativeDll.AreNativeTexturesCreated(_mediaId);
            if (!texturesCreated)
                return false;

            // FIXME: clear all the textures?
            if (_nativeTextures.Count != 0)
            {
                // Note: this isn't needed
                //_nativeTexture.UpdateExternalTexture(nativeTexture);

                // Apply() is slow as hell, don't use it
                //mMovieTexture.Apply();
            }

            var texturesCount = NativeDll.GetNativeTexturesCount(_mediaId);
            for (int idx = 0; idx < texturesCount; ++idx)
            {
                int width, height;
                NativeDll.NativeTextureFormat nativeFormat;
                IntPtr nativeTexture, shaderResourceView;

                bool result = NativeDll.GetNativeTexturePtrByIndex(_mediaId, idx,
                    out nativeTexture, out shaderResourceView, 
                    out width, out height, out nativeFormat);

                if (!result || nativeTexture == IntPtr.Zero || nativeTexture.ToInt32() == 0 ||
                    width <= 0 || height <= 0 || nativeFormat == NativeDll.NativeTextureFormat.Unknown)
                {
                    // FIXME: clear all the textures?
                    Debug.LogWarning("[DemolitionMedia] native texture is invalid");
                    return false;
                }

                // DX11 render backend requires SRV, others the texture handle
                var texPtr = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D11 
                    ? shaderResourceView : nativeTexture;

                var format = TextureFormatNativeToUnity(nativeFormat);
                var tex = Texture2D.CreateExternalTexture(width, height, format, false, false, texPtr);

                tex.wrapMode = TextureWrapMode.Clamp;
                tex.filterMode = FilterMode.Bilinear;

                // Append the new texture
                _nativeTextures.Add(tex);
            }

            // Perform the color conversion for the first frame if needed
            PerformColorConversionIfNeeded();

            return true;
        }

        public void PerformColorConversionIfNeeded()
        {
            if (_nativeTextures.Count == 0)
                return;

            // Get the current material shader
            Shader shaderCur = null;
            if (_colorConversionMaterial != null)
            {
                shaderCur = _colorConversionMaterial.shader;
            }
            Shader shaderNew = GetShader();

            // Check if we need to do something about the material
            if (shaderNew != shaderCur)
            {
                // Destroy old
                if (_colorConversionMaterial != null)
                {
#if UNITY_EDITOR
                    Material.DestroyImmediate(_colorConversionMaterial);
#else
					Material.Destroy(_colorConversionMaterial);
#endif
                    _colorConversionMaterial = null;
                }
                // Create new
                if (shaderNew != null)
                {
                    _colorConversionMaterial = new Material(shaderNew);
                }
            }

            // Check if the color conversion is needed
            if (_colorConversionMaterial == null)
            {
                return;
            }

            // Create the render texture
            var srcTexture = _nativeTextures[0];
            if (_colorConversionRenderTexture == null)
            {
                _colorConversionRenderTexture = new RenderTexture(
                    srcTexture.width, srcTexture.height, 0 /* no depth */,
                    RenderTextureFormat.ARGB32);
                _colorConversionRenderTexture.Create();

            }

            // Stop Unity complaining about:
            // "Tiled GPU perf. warning: RenderTexture color surface was not cleared/discarded"
            // https://forum.unity3d.com/threads/4-2-any-way-to-turn-off-the-tiled-gpu-perf-warning.191906/
            //_colorConversionRenderTexture.MarkRestoreExpected();
            _colorConversionRenderTexture.DiscardContents();

            // TODO: better if condition
            if (_colorConversionMaterial.shader == _shaderHapQAlpha)
            {
                _colorConversionMaterial.SetTexture("_AlphaTex", _nativeTextures[1]);
            }

            // Perform the conversion
            Graphics.Blit(srcTexture, _colorConversionRenderTexture, _colorConversionMaterial);
            // Restore the default render target (screen)
            Graphics.SetRenderTarget(null);
        }

        delegate Texture2D CreateTextureDelegate(int width, int height, TextureFormat format);

        public static string GetUrlPrefix(UrlType urlType)
        {
            switch (urlType)
            {
                case UrlType.Absolute:
                    return string.Empty;

                case UrlType.RelativeToDataPath:
                    return Application.dataPath;

                case UrlType.RelativeToPeristentPath:
                    return Application.persistentDataPath;

                case UrlType.RelativeToProjectPath:
#if !UNITY_WINRT_8_1
                    string parentPath = "..";
#if UNITY_STANDALONE_OSX && !UNITY_EDITOR_OSX
                    parentPath += "/..";
#endif // UNITY_STANDALONE_OSX && !UNITY_EDITOR_OSX
                    return System.IO.Path.GetFullPath(System.IO.Path.Combine(
                        Application.dataPath, parentPath)).Replace('\\', '/');
#else
                    return string.Empty;
#endif // UNITY_WINRT_8_1

                case UrlType.RelativeToStreamingAssetsPath:
                    return Application.streamingAssetsPath;

                default:
                    return string.Empty;
            }
        }

        public static string GetOpeningUrl(string path, UrlType urlType)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;

            switch (urlType)
            {
                case UrlType.Absolute:
                    return path;

                case UrlType.RelativeToDataPath:
                case UrlType.RelativeToPeristentPath:
                case UrlType.RelativeToProjectPath:
                case UrlType.RelativeToStreamingAssetsPath:
                    return System.IO.Path.Combine(GetUrlPrefix(urlType), path);

                default:
                    return path;
            }
        }

        private string PreOpenImpl(string url)
        {
            if (_audioMixer == null)
                enableAudio = false;

            // Comment this block to enable preload to memory on 32-bit architecture.
            // Note that with large files it will crash Unity (total process memory > 2Gb)
#if !(UNITY_64 || UNITY_EDITOR_64)
            if (preloadToMemory)
            {
                preloadToMemory = false;
                Debug.LogWarning("[DemolitionMedia] disabling preload to memory due to 32-bit architecture");
                Debug.LogWarning("[DemolitionMedia] you can enable it (if you know what you're doing!) by commenting the corresponding code block in Media.cs");
            }
#endif

            var openingUrl = GetOpeningUrl(url, urlType);
            return openingUrl;
        }

        private bool initAudioMixer()
        {
            var audioSourceComponents = GetComponentsInChildren<AudioSource>();
            if (audioSourceComponents.Length == 0)
            {
                // No children AudioMixer component attached: no audio 
                Debug.LogWarning("[DemolitionMedia] No children AudioSource component attached, audio will be disabled");
                return false;
            }
            if (audioSourceComponents.Length > 1)
            {
                // More than one children AudioMixer component attached
                Debug.Log("[DemolitionMedia] More than one children AudioSource component attached, audio will be disabled");
                return false;
            }

            // A single children AudioMixer component attached
            var audioSource = audioSourceComponents[0];
            //Debug.Log("[DemolitionMedia] Using AudioSource: " + audioSource.name);

            var mixerGroup = audioSource.outputAudioMixerGroup;
            if (mixerGroup == null)
            {
                Debug.LogWarning("[DemolitionMedia] No output AudioMixerGroup for AudioSource " + audioSource.name + ", audio will be disabled");
                return false;
            }

            //Debug.Log("[DemolitionMedia] Using AudioMixerGroup: " + mixerGroup.name);

            _audioMixer = mixerGroup.audioMixer;
            if (_audioMixer == null)
            {
                Debug.LogWarning("[DemolitionMedia] No AudioMixer for AudioMixerGroup " + mixerGroup.name + ", audio will be disabled");
                return false;
            }

            //Debug.Log("[DemolitionMedia] Using AudioMixer: " + _audioMixer.name);
            return true;
        }

        private void InvokeEvent(MediaEvent.Type type, MediaError error)
        {
            _events.Invoke(this, type, error);
        }

        private void InvokeEvent(MediaEvent.Type eventType)
        {
            _events.Invoke(this, eventType, MediaError.NoError);
        }

        private void OnOpenedImpl()
        {
            SyncMode = SyncMode.SyncAudioMaster;

            if (enableAudio && useNativeAudioPlugin)
            {
                // Check MediaAudioSource script is attached and enabled
                var mediaAudioSourceComponent = GetComponent<MediaAudioSource>();
                var mediaAudioSourceScriptComponentActive = mediaAudioSourceComponent != null &&
                                                            mediaAudioSourceComponent.isActiveAndEnabled &&
                                                            mediaAudioSourceComponent.media != null;
                if (mediaAudioSourceScriptComponentActive)
                {
                    Debug.LogWarning("[DemolitionMedia] " + name + ": \"Use Native Audio Plugin\" option is enabled, " +
                                     "while MediaAudioSource component is attached and active.\nYou probably want to " +
                                     "disable the \"Use Native Audio Plugin\" option.");
                }

                // Try to set the DemolitionMediaId parameter which is used by the native
                // audio plugin to determine the media sound should played from
                var success = _audioMixer.SetFloat("DemolitionMediaId", MediaId);
                if (success)
                {
                    //Debug.Log("[DemolitionMedia] Setting DemolitionMediaId to " + MediaId + " for " + _audioMixer.name);
                }
                else
                {
                    // Setting failed, so probably there is no "Demolition Audio Source" 
                    // effect attached and the parameter doesn't exist
                    if (!mediaAudioSourceScriptComponentActive)
                    {
                        Debug.LogWarning("[DemolitionMedia] " + name + ": \"Demolition Audio Source\" effect isn't " +
                                         "added to the " + _audioMixer.name + " audio mixer, " +
                                         "which is used in the AudioSource");
                        Debug.LogWarning("[DemolitionMedia] It is recommended adding the " +
                                         "\"Demolition Audio Source\" effect using the Audio Mixer editor " +
                                         "(double-click on the " + _audioMixer.name + " audio mixer asset)");
                        Debug.LogWarning("[DemolitionMedia] Alternatively you can use the " +
                                         "MediaAudioSource script, which will feed an AudioSource with audio data " +
                                         "of the specified media.\nThis option isn't recommended for performance reasons, " +
                                         "especially if you need fast seeking");
                    }
                }
            }

            // TODO: try for DX12
            //CreateTextureDelegate createTexture = delegate (int width, int height, TextureFormat format)
            //{
            //    // Create a compressed texture
            //    Texture2D tex = new Texture2D(width, height, format, false /* no mipmaps!!! */)
            //    {
            //        // GL_CLAMP_TO_EDGE
            //        wrapMode = TextureWrapMode.Clamp,
            //        filterMode = FilterMode.Bilinear
            //    };
            //    // TODO: 
            //    //tex.SetPixels(white);

            //    // Call Apply() so it's actually upopened to the GPU
            //    tex.Apply();

            //    // Set texture onto our matrial
            //    GetComponent<Renderer>().material.mainTexture = tex;

            //    return tex;
            //};

            //mMovieTexture = createTexture(width, height, TextureFormat.DXT1);
            //mMovieTexture = createTexture(width, height, TextureFormat.DXT5);
            // http://docs.unity3d.com/ScriptReference/Texture.GetNativeTexturePtr.html
            //DemolitionVideoSetTexture(mMovieTexture.GetNativeTexturePtr(),
            //                            mMovieTexture.width, mMovieTexture.height);
        }

        private Shader GetShader()
        {
            var pixelFormat = NativeDll.GetPixelFormat(_mediaId);
            switch (pixelFormat)
            {
                case PixelFormat.YCoCg:
                    return _shaderHapQ;
                case PixelFormat.YCoCgAndAlphaP:
                    return _shaderHapQAlpha;
                // TODO
                //case PixelFormat.YUV420P:
                //case PixelFormat.YUV422P10LE:
                default:
                    // No color conversion needed
                    return null;
            }
        }

        private int GetAudioChannelCount(AudioSpeakerMode speakerMode)
        {
            switch (speakerMode)
            {
                case AudioSpeakerMode.Mono:
                    return 1;
                case AudioSpeakerMode.Stereo:
                    return 2;
                case AudioSpeakerMode.Quad:
                    return 4;
                case AudioSpeakerMode.Surround:
                    return 5;
                case AudioSpeakerMode.Mode5point1:
                    return 6;
                case AudioSpeakerMode.Mode7point1:
                    return 8;

                default:
                    Debug.LogError("[DemolitionMedia] " + "AudioSpeakerMode." + speakerMode.ToString() + " is unsupported");
                    return 0;
            }
        }
    #endregion

    #region overloaded_methods

        private void CloseImpl()
        {
            // Destroy native render textures
            foreach (Texture2D tex in _nativeTextures) {
                Destroy(tex);
            }
            _nativeTextures.Clear();

			// Destroy color conversion render texture if one exists
			if (_colorConversionRenderTexture != null) {
                Destroy(_colorConversionRenderTexture);
                _colorConversionRenderTexture = null;
            }
        }
    #endregion
    }
}