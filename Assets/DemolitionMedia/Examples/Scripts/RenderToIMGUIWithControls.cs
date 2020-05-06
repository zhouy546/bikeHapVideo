using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;


namespace DemolitionStudios.DemolitionMedia
{
    [AddComponentMenu("Demolition Media/Render to IMGUI with controls")]
    public class RenderToIMGUIWithControls : MonoBehaviour
    {
        /// Imgui renderer
        private RenderToIMGUI _videoIMGUI;
        /// Whether the controls are currently active
        private bool _active;

        /// Target media
        public Media media;
        /// GUI skin
        public GUISkin skin;
        /// IMGUI color
        public Color color = Color.white;
        /// Whether to use the IMGUI alpha blending
        public bool alphaBlend = false;
        /// IMGUI scale mode
        public ScaleMode scaleMode = ScaleMode.ScaleToFit;
        /// IMGUI depth
        public int depth = 0;
        /// Whether to draw in fullscreen mode
        public bool fullScreen = true;
        /// Video rectangle position
        public Vector2 position = Vector2.zero;
        /// Video rectangle size
        public Vector2 size = Vector2.one;
        /// Current media url
        private string _currentMediaUrl;
        /// Events
        class EventEntry
        {
            public EventEntry(string eventName, float timer)
            {
                EventName = eventName;
                Timer = timer;
            }

            public bool DecrementTimer(float dt)
            {
                Timer -= dt;
                return Timer > 0.0f;
            }

            public string EventName;
            public float Timer;
        }
        private List<EventEntry> _events = new List<EventEntry>();
        /// Event name presnt on screen time
        private const float _eventDisplayTime = 3.0f;

        void Start()
        {
            // Note: this is a convinient way to add a component for MonoBehavior 
            _videoIMGUI = gameObject.AddComponent<RenderToIMGUI>();

			if (media == null) {
				Debug.LogError ("[DemolitionMedia] RenderToIMGUIWithControls: no Media instance set");
				return;
			}
			
            media.Events.AddListener(OnMediaPlayerEvent);
        }

        void Update()
        {
            // Pass params to IMGUI video renderer
            _videoIMGUI.sourceMedia = media;
            _videoIMGUI.color = color;
            _videoIMGUI.alphaBlend = alphaBlend;
            _videoIMGUI.scaleMode = scaleMode;
            _videoIMGUI.depth = depth;
            _videoIMGUI.fullScreen = fullScreen;
            _videoIMGUI.position = position;
            _videoIMGUI.size = size;

            // Screen-space mouse position
            Vector2 mousePosScreenSpace = new Vector2(
                Input.mousePosition.x, Screen.height - Input.mousePosition.y);

            // Activate on mouse
            Rect rect = _videoIMGUI.GetDrawRect();
            if (rect.Contains(mousePosScreenSpace))
            {
                _active = true;
                _videoIMGUI.color = Color.white;
            }
            else
            {
                _active = false;
                //_videoIMGUI.color = Color.gray;
            }

            // Process events
            for (int i = 0; i < _events.Count; ++i)
            {
                var elem = _events[i];
                if (!elem.DecrementTimer(Time.deltaTime))
                {
                    _events.Remove(elem);
                    i -= 1;
                }
            }
        }

        void OnGUI()
        {
            // Check whether the source media is set
            if (_videoIMGUI.sourceMedia == null)
                return;

            // Get the media
            Media media = _videoIMGUI.sourceMedia;
            if (media == null)
                return;

            // Store the initial media url
            if (_currentMediaUrl == null)
                _currentMediaUrl = media.mediaUrl;

            // Draw the video
            _videoIMGUI.OnGUI();
            // Reset the gui matrix to identity
            GUI.matrix = Matrix4x4.identity;

            // Hide the controls when the mouse is outside the video draw area
            if (!_active)
                return;

			// Set the font size
//			var fontSize = 50;
//			GUI.skin.textField.fontSize = fontSize;
//			GUI.skin.textArea.fontSize = fontSize;
//			GUI.skin.label.fontSize = fontSize;
//			GUI.skin.button.fontSize = fontSize;
//			GUI.skin.toggle.fontSize = fontSize;
//			GUI.skin.box.fontSize = fontSize;
//			GUIStyle myStyle = new GUIStyle("box");
//			myStyle.fontSize = fontSize;

            var buttonOptions = new GUILayoutOption[] { GUILayout.ExpandWidth(false),
                                                        GUILayout.Width(100)};
            var area = _videoIMGUI.GetDrawRect();
            int guiRows = 6, guiHeight = guiRows * 30;
            GUI.skin = skin;
            GUI.depth = _videoIMGUI.depth - 1;
            string mainLabel = "Demolition Media Hap " + NativePluginVersion.GetString();
            if (NativeDll.IsDemoVersion())
                mainLabel += " — Demo version";
			GUILayout.Box(mainLabel);

			string sysInfoLabel = "Graphics device: " + SystemInfo.graphicsDeviceType.ToString();
#if UNITY_64 || UNITY_EDITOR_64
			sysInfoLabel += " @ 64-bit";
#else
			sysInfoLabel += " @ 32-bit";
#endif 
			GUILayout.Box(sysInfoLabel);
			//GUILayout.Box("Graphics device: " + SystemInfo.graphicsDeviceVersion);

            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            if (GUILayout.Button("Hap", GUILayout.ExpandWidth(false)))
            {
                _currentMediaUrl = "DemolitionMedia/SampleVideos/test_street_Hap.mov";
                //media.urlType = Media.UrlType.RelativeToDataPath;
                media.Open(_currentMediaUrl);
            }
            if (GUILayout.Button("Hap Alpha", GUILayout.ExpandWidth(false)))
            {
                _currentMediaUrl = "DemolitionMedia/SampleVideos/Loop3_All_BW_HapAlpha.mov";
                //media.urlType = Media.UrlType.RelativeToDataPath;
                media.Open(_currentMediaUrl);
            }
            if (GUILayout.Button("Hap Q Alpha", GUILayout.ExpandWidth(false)))
            {
                _currentMediaUrl = "DemolitionMedia/SampleVideos/flying_birds_HapQAlpha.mov";
                //media.urlType = Media.UrlType.RelativeToDataPath;
                media.Open(_currentMediaUrl);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginArea(new Rect(area.x, area.y + (area.height - guiHeight),
                                         area.width, guiHeight));
            GUILayout.BeginVertical();

            // Row 1
            GUILayout.BeginHorizontal();
            if (media.IsPlaying && GUILayout.Button("Pause", buttonOptions))
                media.Pause();
            else if (!media.IsPlaying && GUILayout.Button("Play", buttonOptions))
                media.Play();
            if (GUILayout.Button("Prev frame", GUILayout.ExpandWidth(false)))
                media.StepBackward();
            if (GUILayout.Button("Next frame", GUILayout.ExpandWidth(false)))
                media.StepForward();
            if (GUILayout.Button("Random frame", GUILayout.ExpandWidth(false)))
            {
                media.SeekToFrame(Random.Range(0, media.VideoNumFrames));
            }
            // Loop
            bool looping = media.IsLooping;
            bool loopingNew = GUILayout.Toggle(looping, "Loop", GUILayout.ExpandWidth(false));
            if (loopingNew != looping)
            {
                if (loopingNew)
                    media.Loops = -1;
                else
                    media.Loops = 1;
            }
            // Framedrop enabled
            bool framedrop = media.FramedropEnabled;
            bool framedropNew = GUILayout.Toggle(framedrop, "Framedrop", GUILayout.ExpandWidth(false));
            if (framedropNew != framedrop)
            {
           
                media.FramedropEnabled = framedropNew;
            }
            // Playback rate
            GUILayout.Space(100);
            float oldPlaybackSpeed = media.PlaybackSpeed;
            GUILayout.TextField("Speed: " + oldPlaybackSpeed.ToString("F2"),
                                GUILayout.ExpandWidth(false));
            float newPlaybackSpeed = GUILayout.HorizontalSlider(
                oldPlaybackSpeed, 0.5f, 15.0f, GUILayout.Width(200));
            if (Math.Abs(oldPlaybackSpeed - newPlaybackSpeed) > 0.01f)
            {
                media.PlaybackSpeed = newPlaybackSpeed;
            }
            // Timecode
            //GUILayout.TextField("Timecode: " + media.ExternalClockValue.ToString("F2"),
            //                    GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            // Get the active segment
            var currentStartFrame = media.StartFrame;
            var currentEndFrame = media.EndFrame;

            // Row 2
            GUILayout.BeginHorizontal();
            GUILayout.TextField("Time: " + media.CurrentTime.ToString("F2") +
                                " / " + media.DurationSeconds.ToString("F2"),
                                GUILayout.Width(150));
            GUILayout.TextField("Frame: " + media.VideoCurrentFrame.ToString("F0") +
                                " / " + media.VideoNumFrames.ToString("F0"),
                                GUILayout.Width(150));
            GUILayout.TextField("Resolution: " + media.VideoWidth + "x" + media.VideoHeight,
                                GUILayout.ExpandWidth(false));
            GUILayout.TextField("Video FPS: " + media.VideoFramerate,
                                GUILayout.ExpandWidth(false));
			GUILayout.TextField("Decode FPS: " + media.VideoDecodeFramerate.ToString("F1"),
								GUILayout.ExpandWidth(false));
            int earlyDrops, lateDrops;
            media.GetFramedropCount(out earlyDrops, out lateDrops);
            GUILayout.TextField("Drops: " + earlyDrops + "/" + lateDrops,
                                GUILayout.ExpandWidth(false));
            GUILayout.TextField("Start frame: " + currentStartFrame,
                                GUILayout.ExpandWidth(false));
            GUILayout.TextField("End frame: " + Math.Max(currentEndFrame - 1, 0),
                                GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            // Row 1.5
            var maxFrame = media.VideoNumFrames + 1;
            var epsFrame = 1.0f; // 1.0f / media.VideoFramerate;
            //float newStartFrame = GUILayout.HorizontalSlider(
            //    currentStartFrame, 0, maxFrame, GUILayout.ExpandWidth(true));
            float newStartFrame = GUILayout.HorizontalScrollbar(
                currentStartFrame, 0, 0.0f, maxFrame, GUILayout.ExpandWidth(true));
            if (Math.Abs(currentStartFrame - newStartFrame) > epsFrame)
            {
                media.StartFrame = (int)newStartFrame;
            }
            //float newDuration = GUILayout.HorizontalSlider(
            //    currentDuration, 0, maxFrame, GUILayout.ExpandWidth(true));
            float newEndFrame = GUILayout.HorizontalScrollbar(
                currentEndFrame, 0, 0.0f, maxFrame, GUILayout.ExpandWidth(true));
            if (Math.Abs(currentEndFrame - newEndFrame) > epsFrame && currentEndFrame <= maxFrame)
            {
                media.EndFrame = (int)newEndFrame;
            }

            // Row 3
            var epsTime = media.VideoWidth > 3600 
                ? 5.0f / media.VideoFramerate // For large videos skip every 5 frames when seeking
                : 1.0f / media.VideoFramerate;
            float currentTime = media.CurrentTime;
            float newTime = GUILayout.HorizontalSlider(
                currentTime, 0.0f, media.DurationSeconds, GUILayout.ExpandWidth(true));
            if (Math.Abs(currentTime - newTime) > epsTime && currentEndFrame < maxFrame)
            {
                media.SeekToTime(newTime);
            }

            // Row 4
            GUILayout.BeginHorizontal();
            GUILayout.Label("Url: ", "box", GUILayout.Width(60));
            _currentMediaUrl = GUILayout.TextField(_currentMediaUrl, 256, GUILayout.MinWidth(200));
#if UNITY_64 || UNITY_EDITOR_64
            media.preloadToMemory = GUILayout.Toggle(media.preloadToMemory, "Preload to memory");
#endif
            media.playOnOpen = GUILayout.Toggle(media.playOnOpen, "Play on open");
            media.enableAudio = GUILayout.Toggle(media.enableAudio, "Open with audio");
            //media.urlType = GUILayout.SelectionGrid(media.urlType, ); // TODO
            if (GUILayout.Button("Open", GUILayout.Width(100)))
            {
                media.Open(_currentMediaUrl);
            }
            GUILayout.EndHorizontal();

			// Row 5 (optional)
			//if (media.IsPlaying && media.VideoDecodeFramerate < 0.8f * media.VideoFramerate) {
			//	GUIStyle style = new GUIStyle("textField");
			//	style.normal.textColor = Color.red;
			//	//GUILayout.ExpandWidth(false)

			//    GUILayout.TextField(
			//        media.preloadToMemory
			//            ? "Your PC seems to be too slow for this video! Try on a faster one or use a video with smaller resolution and/or framerate"
			//            : "Slow disk? Try preloading to memory or wait for the next loop. A faster harddisk/SSD/RAID is another option",
			//        style);
			//}
			if (media.enableAudio && Math.Abs (newPlaybackSpeed - 1.0) > 1e-5) 
			{
				GUIStyle style = new GUIStyle("textField");
				style.normal.textColor = Color.red;
				//GUILayout.ExpandWidth(false)

			    GUILayout.TextField(
					"If you want to change the playback speed, it's recommended to disable the audio (uncheck \"Open with audio\" above and press \"Open\")",
			        style);
			}

            GUILayout.EndVertical();
            GUILayout.EndArea();


            // State/events area
            var eventsAreaRelativePosX = 0.7f;
            GUILayout.BeginArea(new Rect(area.x + area.width * eventsAreaRelativePosX, 
                                         area.y,
                                         area.width * (1.0f - eventsAreaRelativePosX),
                                         area.width * 0.5f));
            GUILayout.BeginVertical("box");
            // 1. State
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("State: " + media.State.ToString());
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            // 2. Events
            GUILayout.Label("Events: ", "box");
            if (_events.Count > 0)
            {
                GUILayout.BeginVertical("box");
                foreach (EventEntry elem in _events)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                        GUI.color = new Color(1.0f, 1.0f, 1.0f, elem.Timer);
                        GUILayout.Label(elem.EventName/*, GUILayout.ExpandWidth(false)*/);
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        // Callback function to handle events
        public void OnMediaPlayerEvent(Media source, MediaEvent.Type type, MediaError error)
        {
            if (error == MediaError.NoError)
            {
                //Debug.Log("[RenderToIMGUIWithControls] Event: " + type.ToString());
                _events.Add(new EventEntry(type.ToString(), _eventDisplayTime));
            }
            else
            {
                //Debug.LogError("[RenderToIMGUIWithControls] Error: " + error.ToString());
                _events.Add(new EventEntry(type.ToString() + ": " + error.ToString(), _eventDisplayTime));
            }
        }
    }
}