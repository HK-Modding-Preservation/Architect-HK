using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Architect.Content.Preloads;
using UnityEngine;
using UnityEngine.Audio;

namespace Architect.Behaviour.Utility;

public class AudioPlayer : PreviewableBehaviour
{
    public bool isAtmos;
    public bool playOnStart;

    public bool LockMusic
    {
        get;
        set
        {
            field = value;
            if (!value) Players.Remove(this);
            else if (gameObject.activeSelf && !Players.Contains(this)) Players.Add(this);
        }
    }

    public string cueId;

    public float fadeTime;

    public static readonly Dictionary<string, MusicCue> MusicCues = [];
    public static readonly Dictionary<string, AtmosCue> AtmosCues = [];
    
    private static readonly List<AudioPlayer> Players = [];
    private static bool _isUnlocked;

    private static AudioManager _audioManagerInstance;

    public static void Init()
    {
        On.AudioManager.Start += (orig, self) =>
        {
            orig(self);
            _audioManagerInstance = self;
        };
        
        typeof(AudioManager).Hook(nameof(AudioManager.ApplyAtmosCue),
            (Action<AudioManager, AtmosCue, float> orig, AudioManager self, AtmosCue atmosCue,
                float transitionTime) =>
            {
                Players.RemoveAll(i => !i);
                if (Players.Count > 0 && !_isUnlocked) return;
                orig(self, atmosCue, transitionTime);
            });
        
        typeof(AudioManager).Hook(nameof(AudioManager.ApplyMusicCue),
            (Action<AudioManager, MusicCue, float, float, bool> orig, AudioManager self, MusicCue musicCue,
                float delayTime, float transitionTime, bool markWaitForAtmos) =>
            {
                Players.RemoveAll(i => !i);
                if (Players.Count > 0 && !_isUnlocked) return;
                orig(self, musicCue, delayTime, transitionTime, markWaitForAtmos);
            });
        
        typeof(AudioManager).Hook(nameof(AudioManager.ApplyMusicSnapshot),
            (Action<AudioManager, AudioMixerSnapshot, float, float> orig, AudioManager self,
                AudioMixerSnapshot snapshot, float delayTime, float transitionTime) =>
            {
                Players.RemoveAll(i => !i);
                if (Players.Count > 0 && !_isUnlocked) return;
                orig(self, snapshot, delayTime, transitionTime);
            });
        
        typeof(AudioMixerSnapshot).Hook(nameof(AudioMixerSnapshot.TransitionTo),
            (Action<AudioMixerSnapshot, float> orig, AudioMixerSnapshot self, float timeToReach) =>
            {
                Players.RemoveAll(i => !i);
                if (Players.Count > 0 && !_isUnlocked && self.audioMixer.name is "Music" or "Atmos" or "Music Effects")
                    return;
                orig(self, timeToReach);
            });

        string[] audioCueScenes = [
            "RestingGrounds_05",
            "Mines_31",
            "Crossroads_04",
            "Hive_01",
            "Deepnest_26b",
            "Fungus3_50",
            "Fungus1_20_v02",
            "Fungus2_04",
            "Ruins1_03",
            "Fungus3_26",
            "Ruins2_08",
            "Ruins1_30",
            "Waterways_01",
            "Fungus3_34",
            "Deepnest_East_07",
            "Dream_03_Infected_Knight",
            "GG_Workshop",
            "White_Palace_07",
            "White_Palace_20",
            "Abyss_06_Core",
            "Menu_Title"
        ];
        foreach (var scene in audioCueScenes)
        {
            PreloadManager.RegisterPreload(new BasicPreload(scene, "_SceneManager", o =>
            {
                var s = o.GetComponent<SceneManager>();
                var cue = s.musicCue;
                var acue = s.atmosCue;
                MusicCues[cue.name] = cue;
                AtmosCues[acue.name] = acue;
            }));
        }
    }

    private void OnEnable()
    {
        if (!Players.Contains(this) && LockMusic) Players.Add(this);
    }

    private void OnDisable()
    {
        if (LockMusic) Players.Remove(this);
    }

    private void Start()
    {
        if (isAPreview) return;
        if (playOnStart) Play();
    }

    public void Play()
    {
        if (cueId.IsNullOrWhiteSpace()) return;
        StartCoroutine(DoPlay());
    }
    
    private static AudioMixerSnapshot NormalSnapshot => field ??=
        Resources.FindObjectsOfTypeAll<AudioMixerSnapshot>()
            .FirstOrDefault(o => o.name == "Normal" && o.audioMixer && o.audioMixer.name == "Music");

    public static AudioMixerSnapshot AtmosSnapshot => field ??=
        Resources.FindObjectsOfTypeAll<AudioMixerSnapshot>()
            .FirstOrDefault(o => o.name == "at All Layers");

    private IEnumerator DoPlay()
    {
        if (isAtmos)
        {
            if (!AtmosCues.TryGetValue(cueId, out var cue)) yield break;

            _isUnlocked = true;
            _audioManagerInstance.ApplyAtmosCue(cue, fadeTime);
            _audioManagerInstance.ApplyMusicSnapshot(AtmosSnapshot, 0, fadeTime);
            GameManager.instance.sm.atmosCue = cue;
            GameManager.instance.sm.atmosSnapshot = cue.snapshot;
        }
        else
        {
            if (!MusicCues.TryGetValue(cueId, out var cue)) yield break;
            
            _isUnlocked = true;
            _audioManagerInstance.ApplyMusicCue(cue, 0, fadeTime, true);
            _audioManagerInstance.ApplyMusicSnapshot(NormalSnapshot, 0, fadeTime);
            GameManager.instance.sm.musicCue = cue;
            GameManager.instance.sm.musicSnapshot = cue.snapshot;
        }

        _isUnlocked = false;
    }
}