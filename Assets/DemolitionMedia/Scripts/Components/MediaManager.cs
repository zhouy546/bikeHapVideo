using UnityEngine;
using System.Collections;


namespace DemolitionStudios.DemolitionMedia
{
    /// Native plugin version which should work with the current C# scripts
    public static class NativePluginVersion
    {
        public const int MAJOR    = 0;
        public const int MINOR    = 9;
        public const int REVISION = 5;

        /// Returns the version string
        public static string GetString() 
        {
            return MAJOR + "." + MINOR + "." + REVISION;
        }
    }

    /// <summary>
    ///     A singleton media manger class.
    ///     Handles rendering and other global stuff.
    /// </summary>
    [AddComponentMenu("Demolition Media/Media Manager (must have)")]
    public class MediaManager : MonoBehaviour
    {
        /// Whether initialized
        private bool _initialized;

        /// Singleton instance
        private static MediaManager _instance;
        public static MediaManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Try to find component in the scene
                    _instance = (MediaManager)GameObject.FindObjectOfType(
                        typeof(MediaManager));

                    if (_instance == null)
                    {
                        Debug.LogError("[DemolitionMedia] you need to add DemolitionMediaManager component to the scene!");
                        return null;
                    }

                    if (!_instance._initialized)
                        _instance.Initialize();
                }

                return _instance;
            }
        }

        void Awake()
        {
            if (!_initialized)
            {
                _instance = this;
                Initialize();
            }
        }

        void OnDestroy()
        {
            Deinitialize();
        }

        //void OnApplicationQuit()
        //{
        //}

        private bool Initialize()
        {
            try
            {
                // Initialize the native plugin
                if (!NativeDll.Initialize())
                {
                    Debug.LogError("[DemolitionMedia] native plugin initialization failed!");
                    Deinitialize();
                    this.enabled = false;
                    return false;
                }

                // Get the native plugin version
                int major, minor, revision;
                NativeDll.GetPluginVersion(out major, out minor, out revision);

                Debug.Log("[DemolitionMedia] native plugin version: " + major + "." + minor + "." + revision);

#if !UNITY_EDITOR_OSX && !UNITY_STANDALONE_OSX
                if (major != NativePluginVersion.MAJOR ||
                    minor != NativePluginVersion.MINOR ||
                    revision != NativePluginVersion.REVISION)
                {
                    Debug.LogWarning("[DemolitionMedia] this version of C# scripts is supposed to work with native plugin version " +
                                     NativePluginVersion.MAJOR + "." + 
                                     NativePluginVersion.MINOR + "." + 
                                     NativePluginVersion.REVISION);
                    Debug.LogWarning("[DemolitionMedia] you might need to update the C# scripts in order to make it work correctly with the current native plugin version");
                }
#endif

                if (NativeDll.IsDemoVersion())
                {
                    Debug.LogWarning("[DemolitionMedia] this is demo version of the DemolitionMedia plugin! Video texture will periodically have some distortions. Get the full version on the Asset Store!");
                }
            }
            catch (System.DllNotFoundException e)
            {
                Debug.LogError("[DemolitionMedia] couldn't load the native plugin DLL");
                throw e;
            }

            // Start the rendering coroutine
            /*yield return*/
            StartCoroutine("UpdateNativeTexturesAtEndOfFrames");

            _initialized = true;
            return true;
        }

        private void Deinitialize()
        {
            // Clean up any open movies
            Media[] medias = (Media[])FindObjectsOfType(typeof(Media));

            if (medias != null)
            {
                for (int i = 0; i < medias.Length; i++) {
                    medias[i].Close();
                }
            }

            _instance = null;
            _initialized = false;

            Debug.Log("[DemolitionMedia] Shutting down native plugin");
            NativeDll.Deinitialize();
        }

        void Update()
        {
            /// Could also GL.IssuePluginEvent here (avpro does like this)
        }

        private IEnumerator UpdateNativeTexturesAtEndOfFrames()
        {
            while (Application.isPlaying)
            {
                // Wait until all frame rendering is done
                yield return new WaitForEndOfFrame();

                // Issue a plugin event with arbitrary integer identifier.
                // The plugin can distinguish between different things it needs to do based on this ID.
                GL.IssuePluginEvent(NativeDll.GetRenderEventFunc(), 1);
            }
        }
    }
}