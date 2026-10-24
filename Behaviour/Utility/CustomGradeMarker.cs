using System.Collections.Generic;
using System.Linq;
using GlobalEnums;
using TeamCherry;
using UnityEngine;

namespace Architect.Behaviour.Utility;

public class CustomGradeMarker : MonoBehaviour
{
    public MapZone mapZone = MapZone.NONE;

    public bool setAmbientColour;
    public bool setAmbientIntensity;
    public bool setHeroLight;
    public bool setSaturation;

    public bool custom;

    private void Start()
    {
        if (!custom) return;
        
        var references = Resources.Load<References>("ReferencesData");
        var settings = references.sceneDefaultSettings.settingsList
            .FirstOrDefault(o => o.mapZone == mapZone);
        if (settings == null) return;
        
        Resources.UnloadAsset(references);

        var gm = GetComponent<GradeMarker>();
        
        if (setSaturation) gm.saturation = settings.saturation;
        if (setHeroLight) gm.heroLightColor = settings.heroLightColor;
        if (setAmbientIntensity) gm.ambientIntensity = settings.defaultIntensity;
        if (setAmbientColour) gm.ambientColor = settings.defaultColor;
        gm.redChannel = new AnimationCurve(settings.redChannel.keys);
        gm.blueChannel = new AnimationCurve(settings.blueChannel.keys);
        gm.greenChannel = new AnimationCurve(settings.greenChannel.keys);
    }

    private static readonly List<GradeMarker> GradeMarkers = [];
    
    public int priority;
    public bool shutDown;
    
    public static void Init()
    {
        On.GradeMarker.OnEnable += (orig, self) =>
        {
            orig(self);
            GradeMarkers.Add(self);
        };

        On.GradeMarker.OnDisable += (orig, self) =>
        {
            orig(self);
            GradeMarkers.Remove(self);
            ActivateShutDownMarker();
        };

        On.GradeMarker.Activate += (orig, self) =>
        {
            var cgm = self.gameObject.GetOrAddComponent<CustomGradeMarker>();
            if (IsStrongestGradeMarker(cgm, self)) orig(self);
            else cgm.shutDown = true;
        };

        On.GradeMarker.ActivateGradual += (orig, self) =>
        {
            var cgm = self.gameObject.GetOrAddComponent<CustomGradeMarker>();
            if (IsStrongestGradeMarker(cgm, self)) orig(self);
            else cgm.shutDown = true;
        };

        On.GradeMarker.Deactivate += (orig, self) =>
        {
            orig(self);
            ActivateShutDownMarker();
        };
        
        On.GradeMarker.Update += (orig, self) =>
        {
            var cgm = self.GetComponent<CustomGradeMarker>();
            if (IsStrongestGradeMarker(cgm, self))
            {
                var gradeEnabled = self.enableGrade;
                orig(self);
                if (gradeEnabled && !self.enableGrade) ActivateShutDownMarker();
            }
        };
    }

    private static void ActivateShutDownMarker()
    {
        foreach (var gm in GradeMarkers)
        {
            if (!gm.enableGrade) continue;
            var cgm = gm.GetComponent<CustomGradeMarker>();
            if (cgm && cgm.shutDown && IsStrongestGradeMarker(cgm, gm))
            {
                cgm.shutDown = false;
                gm.Activate();
            }
        }
    }

    private static bool IsStrongestGradeMarker(CustomGradeMarker cgm, GradeMarker gm)
    {
        var priority = cgm ? cgm.priority : 0;
        var heroPos = gm.hero.transform.position;
        
        foreach (var marker in GradeMarkers)
        {
            var otherCgm = marker.GetComponent<CustomGradeMarker>();
            if (!otherCgm) continue;

            if (Vector2.Distance(heroPos, marker.transform.position) - marker.cutoffRadius < 0
                && otherCgm.priority > priority) return false;
            if (priority > otherCgm.priority) otherCgm.shutDown = true;
        }

        return true;
    }
}