using UnityEngine;
using System.Collections;


namespace DemolitionStudios.DemolitionMedia
{
    [AddComponentMenu("Demolition Media/Render to IMGUI")]
    public class RenderToIMGUI : MonoBehaviour
    {
        /// Source media component
        public Media sourceMedia;

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

        public void OnGUI()
        {
            if (sourceMedia == null || sourceMedia.VideoRenderTexture == null)
                return;

            GUI.depth = depth;
            GUI.color = color;

            Rect drawRect = GetDrawRect();

            var scale = new Vector2(sourceMedia.VideoNeedFlipX ? -1.0f :  1.0f,
                                    sourceMedia.VideoNeedFlipY ?  1.0f : -1.0f);
            GUIUtility.ScaleAroundPivot(scale, drawRect.center);

			// Note: the conversion is performed in the Media itself now
            // For HapQ we need a custom material to convert to RGB
            // https://docs.unity3d.com/ScriptReference/Graphics.DrawTexture.html
            //Graphics.DrawTexture(drawRect, sourceMedia.RenderTexture, material);

			GUI.DrawTexture(drawRect, sourceMedia.VideoRenderTexture, scaleMode, alphaBlend);
        }

        public Rect GetDrawRect()
        {
            if (fullScreen)
            {
                return new Rect(0.0f, 0.0f, Screen.width, Screen.height);
            }

            return new Rect(position.x * (Screen.width - 1),
                            position.y * (Screen.height - 1),
                            size.x * Screen.width,
                            size.y * Screen.height);
        }
    }
}