using Architect.Behaviour.Utility;
using Architect.Config;
using Architect.Config.Types;
using Architect.Storage;
using UnityEngine;

namespace Architect.Updater;

public class DcmSawMovement : PreviewableBehaviour
{
    public float span;
    public float speed;
    public float offset;
    private float _angle;
    private Vector2 _center;

    public static void Init()
    {
        ConfigurationManager.RegisterConfigType(new FloatConfigType("DcM Span", "dcm_span", (o, value) =>
        {
            o.GetOrAddComponent<DcmSawMovement>().span = value.GetValue();
        }));
        
        ConfigurationManager.RegisterConfigType(new FloatConfigType("DcM Speed", "dcm_speed", (o, value) =>
        {
            o.GetOrAddComponent<DcmSawMovement>().speed = value.GetValue();
        }));
        
        ConfigurationManager.RegisterConfigType(new FloatConfigType("DcM Offset", "dcm_offset", (o, value) =>
        {
            o.GetOrAddComponent<DcmSawMovement>().offset = value.GetValue();
        }));
    }

    private void Awake()
    {
        _angle = transform.GetRotation2D();
        transform.SetRotation2D(0);
        _center = transform.position;
    }

    private void Update()
    {
        if (isAPreview && !Settings.Preview.IsPressed)
        {
            transform.SetPosition2D(_center);
            return;
        }
        var dealtDis = span * Mathf.Sin(speed * Time.time + offset * Mathf.PI / 2);
        var dx = dealtDis * Mathf.Cos(_angle * Mathf.PI / 180f);
        var dy = dealtDis * Mathf.Sin(_angle * Mathf.PI / 180f);
        
        transform.SetPosition2D(new Vector2(_center.x + dx, _center.y + dy));
    }
}