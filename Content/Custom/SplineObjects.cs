using System.Collections.Generic;
using Architect.Objects.Categories;
using Architect.Objects.Groups;
using Architect.Objects.Placeable;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Architect.Content.Custom;

public static class SplineObjects
{
    public static void Init()
    {
        Categories.Utility.Add(CreateStartNode());
        Categories.Utility.Add(CreateNode());
    }

    private static PlaceableObject CreateStartNode()
    {
        var node = new GameObject("Start Node");
        node.SetActive(false);
        Object.DontDestroyOnLoad(node);

        var lr = node.AddComponent<LineRenderer>();
        
        var particleMaterial = new Material(Shader.Find("Sprites/Default"));
        lr.material = particleMaterial;
        
        lr.useWorldSpace = false;
        lr.widthMultiplier = 0.2f;
        node.AddComponent<Spline>().line = lr;

        return new CustomObject("Track Start Point", "start_node",
                node,
                sprite: ResourceUtils.LoadSpriteResource("track_start", FilterMode.Point, ppu:15),
                description: "The start of a track.\n" +
                             "Place Track Points with the same Track ID to link together and form a track.\n\n" +
                             "Setting a track point as the parent of an Object Anchor will cause it to\n" +
                             "follow the track, starting at that point.\n\n" +
                             "Set the Colour A option to 0 to hide the track.")
            .WithConfigGroup(ConfigGroup.TrackStartPoint);
    }

    private static PlaceableObject CreateNode()
    {
        var node = new GameObject("Node");
        node.SetActive(false);
        Object.DontDestroyOnLoad(node);

        node.AddComponent<SplinePoint>();

        return new CustomObject("Track Point", "node",
                node,
                sprite: ResourceUtils.LoadSpriteResource("track_node", FilterMode.Point, ppu:15),
                description: "A point on a track.\n" +
                             "Use the Track Start Point to start a track.")
            .WithConfigGroup(ConfigGroup.TrackPoint)
            .WithReceiverGroup([]);
    }

    private static readonly Dictionary<string, Spline> Splines = [];

    public class SplinePoint : MonoBehaviour
    {
        public string id = "1";
        public Spline Spline
        {
            get
            {
                if (!field) field = Splines.TryGetValue(id, out var spline) ? spline : null;
                return field;
            }
        }

        protected Vector3 Pos;

        protected virtual void Start()
        {
            Pos = transform.position;
            if (Spline)
            {
                Spline.points.Add(transform);
                if (Spline.setup) Spline.UpdatePoints();
            }
        }

        private void Update()
        {
            if (Pos != transform.position)
            {
                Pos = transform.position;
                if (Spline && Spline.setup) Spline.UpdatePoints();
            }
        }

        private void OnDestroy()
        {
            if (Spline)
            {
                Spline.points.Remove(transform);
                Spline.UpdatePoints();
            }
        }
    }
    
    public class Spline : SplinePoint
    {
        public List<Transform> points = [];
        public Vector3[] segments;
        public float speed = 1;
        public Color colour;
        public LineRenderer line;

        public bool setup;

        private void Awake()
        {
            Splines[id] = this;
        }

        protected override void Start()
        {
            points.Add(transform);
            line.enabled = colour.a > 0;
            line.startColor = line.endColor = colour;
        }

        private void Update()
        {
            if (!setup || Pos != transform.position)
            {
                Pos = transform.position;
                setup = true;
                UpdatePoints();
            }
        }

        public void UpdatePoints()
        {
            var pc = points.Count * 25;
            GetSegments(pc);

            if (colour.a > 0)
            {
                line.positionCount = pc;
                line.SetPositions(segments);
            }
        }

        public Vector3 GetSegment(float time)
        {
            time = Mathf.Clamp01(time);

            var p = new Vector3();

            var n = points.Count - 1;
            for (var i = 0; i <= n; i++)
            {
                p += Choose(n, i) * Mathf.Pow(1 - time, n - i) * Mathf.Pow(time, i) * 
                     (points[i].position - transform.position);
            }
            
            return p;
        }

        public void GetSegments(int segCount)
        {
            if (segments == null || segments.Length != segCount) segments = new Vector3[segCount];

            for (var i = 0; i < segCount; i++)
            {
                segments[i] = GetSegment((float)i / segCount);
            }
        }
    }
    
    private static long Choose(long n, long r)
    {
        long k = 1;
        long d;
        if (r > n) return 0;
        for (d = 1; d <= r; d++)
        {
            k *= n--;
            k /= d;
        }
        return k;
    }
}