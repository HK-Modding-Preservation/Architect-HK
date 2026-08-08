using System.Collections.Generic;
using System.Linq;
using Architect.Behaviour.Utility;
using Architect.Objects.Placeable;
using UnityEngine;

namespace Architect.Utils;

public static class PreviewUtils
{
    private static readonly int ColorProperty = Shader.PropertyToID("_Color");
    private static readonly int TintColorProperty = Shader.PropertyToID("_TintColor");

    public static Vector3 FixPreview(SpriteRenderer renderer, PlaceableObject type, bool flipped, float rotation, float scale)
    {
        if (type.Tk2dRotation / 90 % 2 != 0)
        {
            renderer.flipY = false;
            renderer.flipX = type.LossyScale.x < 0;
        }
        else
        {
            renderer.flipX = false;
            renderer.flipY = type.LossyScale.y < 0;
        }

        var childOffset = type.ChildOffset;

        childOffset.x /= type.LossyScale.x;
        childOffset.y /= type.LossyScale.y;
        
        var offset = type.Offset + childOffset;
        
        offset.x *= type.ParentScale.x;
        offset.y *= type.ParentScale.y;
        
        if (flipped) offset.x = -offset.x;

        return Quaternion.Euler(0, 0, rotation) * (offset * scale);
    }

    public static Sprite ConvertFrom2DToolkit(tk2dSpriteDefinition def, float ppu)
    {
        if (def.material.mainTexture is not Texture2D texture) return null;
        
        var minX = def.uvs[0].x;
        var minY = def.uvs[0].y;
        var maxX = def.uvs[0].x;
        var maxY = def.uvs[0].y;
        for (var i = 1; i < def.uvs.Length; i++)
        {
            minX = Mathf.Min(minX, def.uvs[i].x);
            minY = Mathf.Min(minY, def.uvs[i].y);
            maxX = Mathf.Max(maxX, def.uvs[i].x);
            maxY = Mathf.Max(maxY, def.uvs[i].y);
        }

        var x = minX * texture.width;
        var y = minY * texture.height;
        var width = (maxX - minX) * texture.width;
        var height = (maxY - minY) * texture.height;

        return Sprite.Create(texture, new Rect(x, y, width, height), new Vector2(0.5f, 0.5f), ppu);
    }

    public class Preview : MonoBehaviour
    {
        public readonly List<PreviewRenderer> Renderers = [];

        public PreviewSettings Settings
        {
            set
            {
                foreach (var renderer in Renderers) renderer.Apply(value);
            }
        }

        public void Setup(PlaceableObject type)
        {
            var set = false;

            if (type.SpritePreview)
            {
                Renderers.Add(new PreviewSpriteRenderer(GetComponent<SpriteRenderer>(), false));
                foreach (var r in GetComponentsInChildren<Renderer>(true)) 
                    if (r.gameObject != gameObject) r.enabled = false;
            }
            else
            {
                foreach (var mr in GetComponentsInChildren<MeshRenderer>())
                {
                    var sp = mr.GetComponent<tk2dSprite>();
                    if (sp)
                    {
                        Renderers.Add(new PreviewTk2dSprite(sp, mr.GetComponent<MeshFilter>()));
                        set = true;
                    }
                    else Renderers.Add(new PreviewMeshRenderer(mr, mr.GetComponent<MeshFilter>()));
                }

                foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
                {
                    if (sr.name.Contains("haze") || sr.name.Contains("Light")) continue;
                    Renderers.Add(new PreviewSpriteRenderer(sr, set));
                }
            }

            foreach (var fsm in gameObject.GetComponentsInChildren<PlayMakerFSM>(true)) 
                fsm.enabled = false;
            foreach (var aso in gameObject.GetComponentsInChildren<AudioSource>(true)) 
                aso.enabled = false;
            foreach (var pb in GetComponentsInChildren<PreviewableBehaviour>(true)) 
                pb.isAPreview = true;
            foreach (var tsa in GetComponentsInChildren<tk2dSpriteAnimator>(true))
            {
                if (tsa.DefaultClip != null) tsa.Play(tsa.DefaultClip);
                Destroy(tsa);
            }
            
            gameObject.RemoveComponentsInChildren<SpawnJarControl>();
            gameObject.RemoveComponentsInChildren<PersistentBoolItem>();
            gameObject.RemoveComponentsInChildren<FSMActivator>();
            gameObject.RemoveComponentsInChildren<PersistentIntItem>();
            gameObject.RemoveComponentsInChildren<HealthManager>();
            gameObject.RemoveComponentsInChildren<IHitResponder>();
            gameObject.RemoveComponentsInChildren<JellyEgg>();
            gameObject.RemoveComponentsInChildren<tk2dSpriteAnimator>();
            gameObject.RemoveComponentsInChildren<PlayFromRandomFrameMecanim>();
            gameObject.RemoveComponentsInChildren<Animator>();
            gameObject.RemoveComponentsInChildren<EnemyBullet>();
            gameObject.RemoveComponentsInChildren<Climber>();
            gameObject.RemoveComponentsInChildren<Walker>();
            gameObject.RemoveComponentsInChildren<BigCentipedeSection>();
            gameObject.RemoveComponentsInChildren<BigCentipede>();
            gameObject.RemoveComponentsInChildren<Crawler>();
            gameObject.RemoveComponentsInChildren<LiftPlatform>();
            gameObject.RemoveComponentsInChildren<ParticleSystemRenderer>();
            gameObject.RemoveComponentsInChildren<FlipPlatform>();
            if (!Storage.Settings.HitboxesInEditor.Value && !type.HitboxPreview)
            {
                gameObject.RemoveComponentsInChildren<Collider2D>();
            }
        }

        public bool Touching(Vector3 pos)
        {
            return Renderers.Any(renderer => renderer.Touching(pos));
        }
    }

    public abstract class PreviewRenderer(Color startColour)
    {
        protected abstract void SetColour(Color color);
        
        public void Apply(PreviewSettings settings)
        {
            SetColour(settings.Apply(startColour));
        }

        public abstract bool Touching(Vector3 pos);
    }

    private class PreviewSpriteRenderer(SpriteRenderer sr, bool ignoreTouching) : PreviewRenderer(sr.color)
    {
        protected override void SetColour(Color color) => sr.color = color;

        public override bool Touching(Vector3 pos)
        {
            if (ignoreTouching) return false;
            
            var localPos = sr.transform.InverseTransformPoint(pos);
        
            var bounds = sr.sprite.bounds;
            var min = bounds.min;
            var max = bounds.max;
            return localPos.x >= min.x && localPos.x <= max.x && localPos.y >= min.y && localPos.y <= max.y;
        }
    }

    private class PreviewTk2dSprite(tk2dSprite sp, MeshFilter mf) : PreviewRenderer(sp.color)
    {
        protected override void SetColour(Color color) => sp.color = color;

        public override bool Touching(Vector3 pos)
        {
            return MeshRendererTouching(mf, pos);
        }
    }

    private class PreviewMeshRenderer(MeshRenderer mr, MeshFilter mf) : PreviewRenderer(
        mr.material.HasProperty(ColorProperty) ? 
            mr.material.color : 
            mr.material.GetColor(TintColorProperty))
    {
        protected override void SetColour(Color color)
        {
            if (mr.material.HasProperty(ColorProperty))
            {
                mr.material.color = color;
            }
            else if (mr.material.HasProperty(TintColorProperty))
            {
                mr.material.SetColor(TintColorProperty, color);
            }
        }

        public override bool Touching(Vector3 pos)
        {
            return MeshRendererTouching(mf, pos);
        }
    }

    private static bool MeshRendererTouching(MeshFilter mf, Vector3 pos)
    {
        var localPos = mf.transform.InverseTransformPoint(pos);
        
        var bounds = mf.GetComponent<MeshFilter>().sharedMesh.bounds;
        var min = bounds.min;
        var max = bounds.max;
        return localPos.x >= min.x && localPos.x <= max.x && localPos.y >= min.y && localPos.y <= max.y;
    }

    public readonly struct PreviewSettings(float r, float g, float b, float a)
    {
        public Color Apply(Color c)
        {
            return new Color(c.r * r, c.g * g, c.b * b, c.a * a);
        }
    }

    public static GameObject MakeSpritePreview(
        GameObject preview, 
        PlaceableObject type,
        bool flipped, float rotation, float scale,
        out Vector3 offset)
    {
        var previewSprite = new GameObject("[Architect] Preview Parent")
        {
            transform =
            {
                localPosition = preview.transform.position,
                rotation = Quaternion.Euler(0, 0, preview.transform.eulerAngles.z + type.Tk2dRotation + type.ChildRotation),
                localScale = type.IgnoreScale ? Vector3.one : preview.transform.localScale
            }
        };
        preview.transform.SetParent(previewSprite.transform, true);

        var sr = previewSprite.AddComponent<SpriteRenderer>();
        sr.sprite = type.Sprite;

        offset = FixPreview(sr, type, flipped, rotation, scale);

        return previewSprite;
    }
}