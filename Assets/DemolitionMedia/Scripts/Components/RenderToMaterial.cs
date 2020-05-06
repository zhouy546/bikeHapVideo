using UnityEngine;
using System.Collections;


namespace DemolitionStudios.DemolitionMedia
{
    [AddComponentMenu("Demolition Media/Render to material")]
    public class RenderToMaterial : MonoBehaviour
    {
        /// Source media component with video to map
        public Media SourceMedia;
        /// Target material instance
        public Material TargetMaterial;
        /// Target texture name inside the target material
        public string TargetTextureName;
        /// Fallback texture
        public Texture FallbackTexture;
        /// Scale factor
        public Vector2 Scale = Vector2.one;
        /// Offset vecotr
        public Vector2 Offset = Vector2.zero;
        /// Old texture
        private Texture Texture;

        public virtual void Update()
        {
			if (SourceMedia == null || SourceMedia.VideoRenderTexture == null)
            {
                Apply(FallbackTexture, false, false);
                return;
            }

            Apply(SourceMedia.VideoRenderTexture, SourceMedia.VideoNeedFlipX, SourceMedia.VideoNeedFlipY);
        }

        private void Apply(Texture texture, bool flipX, bool flipY)
        {
			if (TargetMaterial == null)
                return;

            if (texture == null)
                texture = Texture2D.blackTexture;

            Vector2 scale = Scale;
            Vector2 offset = Offset;
            if (!flipX)
            {
                scale.Scale(new Vector2(-1.0f, 1.0f));
                offset.x += 1.0f;
            }
            if (flipY)
            {
                scale.Scale(new Vector2(1.0f, -1.0f));
                offset.y += 1.0f;
            }

            if (string.IsNullOrEmpty(TargetTextureName))
            {
                //Debug.Log("Setting main texture");
                TargetMaterial.mainTexture = texture;
                TargetMaterial.mainTextureScale = scale;
                TargetMaterial.mainTextureOffset = offset;
            }
            else
            {
                //Debug.Log("Setting texture" + TargetTextureName);
                TargetMaterial.SetTexture(TargetTextureName, texture);
                TargetMaterial.SetTextureScale(TargetTextureName, scale);
                TargetMaterial.SetTextureOffset(TargetTextureName, offset);
            }
        }

        public virtual void OnEnable()
        {
            if (TargetMaterial != null)
            {
                Scale = TargetMaterial.mainTextureScale;
                Offset = TargetMaterial.mainTextureOffset;
                Texture = TargetMaterial.mainTexture;
            }
            Update();
        }

        public virtual void OnDisable()
        {
            Apply(FallbackTexture, false, false);
            if (TargetMaterial != null)
            {
                TargetMaterial.mainTextureScale = Scale;
                TargetMaterial.mainTextureOffset = Offset;
                TargetMaterial.mainTexture = Texture;
            }
        }
    }
}