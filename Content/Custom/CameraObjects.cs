using System;
using System.Reflection;
using Architect.Content.Preloads;
using Architect.Objects.Categories;
using Architect.Objects.Groups;
using Architect.Objects.Placeable;
using Architect.Placements;
using UnityEngine;
using UnityStandardAssets.ImageEffects;
using Object = UnityEngine.Object;

namespace Architect.Content.Custom;

public static class CameraObjects
{
    private static readonly FieldInfo RgbChannelTex = typeof(ColorCorrectionCurves).GetField("rgbChannelTex",
        BindingFlags.NonPublic | BindingFlags.Instance);

    public static void Init()
    {
        PreloadManager.RegisterPreload(new BasicPreload(
            "Menu_Title",
            "_GameCameras",
            o =>
            {
                var tk2dCam = o.transform.GetChild(1).GetChild(0).gameObject;

                tk2dCam.SetActive(false);
                var cam = Object.Instantiate(tk2dCam);
                tk2dCam.SetActive(true);
                Object.DontDestroyOnLoad(cam);

                cam.RemoveComponent<CameraController>();
                cam.RemoveComponent<AudioListener>();
                cam.RemoveComponent<FastNoise>();
                cam.RemoveComponent<ForceCameraAspect>();
                
                cam.AddComponent<CustomCamera>();

                cam.transform.SetPositionZ(0);
                
                tk2dCam.SetActive(true);

                Categories.Utility.AddStart(new CustomObject("Camera", "camera_object", cam,
                        description: "An extra camera that can be rendered using a Camera View.",
                        postSpawnAction: c => { c.transform.SetPositionZ(-38.1f); },
                        sprite: ResourceUtils.LoadSpriteResource("camera"))
                    .WithConfigGroup(ConfigGroup.Camera));
            }));


        var cam = new GameObject("[Architect] Camera View");
        cam.SetActive(false);
        Object.DontDestroyOnLoad(cam);

        var back = new GameObject("Backing")
        {
            transform =
            {
                parent = cam.transform,
                localPosition = Vector3.zero,
                localScale = new Vector2(0.5f, 0.5f)
            }
        };
        var sr = back.AddComponent<SpriteRenderer>();
        sr.color = Color.black;
        sr.sprite = UIUtils.Square;

        cam.AddComponent<MeshRenderer>();
        var mesh = new Mesh
        {
            vertices =
            [
                new Vector3(-2.5f, -2.5f, 0),
                new Vector3(2.5f, -2.5f, 0),
                new Vector3(-2.5f, 2.5f, 0),
                new Vector3(2.5f, 2.5f, 0)
            ],
            triangles = [0, 2, 1, 2, 3, 1],
            normals = [-Vector3.forward, -Vector3.forward, -Vector3.forward, -Vector3.forward],
            uv = [new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1)]
        };
        cam.AddComponent<MeshFilter>().mesh = mesh;

        Categories.Utility.AddStart(new CustomObject("Camera View", "camera_view", cam,
                description: "Renders the view of an extra camera.",
                sprite: ResourceUtils.LoadSpriteResource("camera_view", ppu: 40, filterMode: FilterMode.Point))
            .WithConfigGroup(ConfigGroup.CameraView)
            .DoIgnoreScale());
    }

    public class CustomCamera : MonoBehaviour
    {
        public string id;
        public int resolution = 1024;

        private bool _setup;
        private RenderTexture _rt;

        private Camera _camera;
        private ColorCorrectionCurves _ccc;
        private ColorCorrectionCurves _accc;

        private void Start()
        {
            _camera = GetComponent<Camera>();

            _ccc = GetComponent<ColorCorrectionCurves>();
            _accc = GameCameras.instance.tk2dCam.GetComponent<ColorCorrectionCurves>();
        }

        private void Update()
        {
            _ccc.saturation = _accc.saturation;
            _ccc.blueChannel = _accc.blueChannel;
            _ccc.redChannel = _accc.redChannel;
            _ccc.greenChannel = _accc.greenChannel;
            RgbChannelTex.SetValue(_ccc, RgbChannelTex.GetValue(_accc));

            if (!_setup)
            {
                _setup = true;
                if (id.IsNullOrWhiteSpace() || !PlacementManager.Objects.TryGetValue(id, out var target))
                {
                    gameObject.SetActive(false);
                    return;
                }

                var mr = target.GetComponent<MeshRenderer>();
                if (!mr)
                {
                    gameObject.SetActive(false);
                    return;
                }

                var x = target.transform.GetScaleX();
                var y = target.transform.GetScaleY();

                int width;
                int height;

                if (x > y)
                {
                    width = resolution;
                    height = Math.Abs(Mathf.FloorToInt(y / x * resolution));
                }
                else
                {
                    height = resolution;
                    width = Math.Abs(Mathf.FloorToInt(x / y * resolution));
                }

                var rt = new RenderTexture(width, height, 64, RenderTextureFormat.ARGB64)
                {
                    name = "[Architect] Camera Target Texture"
                };
                mr.sharedMaterial = new Material(Shader.Find("Sprites/Default"))
                {
                    mainTexture = _camera.targetTexture = _rt = rt
                };
                _camera.enabled = true;
            }
        }

        private void OnDestroy()
        {
            Destroy(_rt);
        }
    }
}