using System;
using System.Collections.Generic;
using System.Linq;
using Architect.Workshop.Items;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using JetBrains.Annotations;
using MonoMod.RuntimeDetour;
using Satchel;
using TMPro;
using UnityEngine;

namespace Architect.Utils;

public static class MapUtils
{
    private static readonly Dictionary<string, MapZoneInfo> Zones = [];

    public static void Init()
    {
        foreach (var method in typeof(GameMap).GetMethods())
        {
            if (!method.Name.StartsWith("QuickMap")) continue;
            _ = new Hook(method, (Action<GameMap> orig, GameMap self) =>
            {
                orig(self);
                SceneGroup.HideMaps();
            });
        }
        
        On.GameMap.CloseQuickMap += (orig, self) =>
        {
            orig(self);
            SceneGroup.HideMaps();
        };

        On.GameMap.WorldMap += (orig, self) =>
        {
            orig(self);
            SceneGroup.ShowMaps(self);
        };
        
        On.GameManager.GetCurrentMapZone += (orig, self) =>
        {
            if (SceneUtils.CustomScenes.TryGetValue(self.sceneName, out var scene)
                && SceneUtils.SceneGroups.TryGetValue(scene.Group, out var group)
                && group.HasMap) return group.Id;
            return orig(self);
        };

        HookUtils.OnFsmAwake += fsm =>
        {
            if (fsm.FsmName != "Quick Map") return;

            var custom = fsm.AddState("Custom Map");
            var check = fsm.GetState("Check Area");

            var mz = fsm.FsmVariables.FindFsmString("Map Zone");
            check.AddTransition("ARCHITECT", custom.name);
            check.AddAction(() =>
            {
                if (Zones.ContainsKey(mz.Value)) fsm.SendEvent("ARCHITECT");
            }, 2);

            var dirtmouth = fsm.GetState("Dirtmouth");
            var text = dirtmouth.GetAction<SetTextMeshProText>(1).gameObject;
            var sm = dirtmouth.GetAction<SendMessage>(3).gameObject;
            
            custom.AddAction(dirtmouth.GetAction<FadeGroupUp>(2));
            custom.AddAction(() =>
            {
                if (!Zones.TryGetValue(mz.Value, out var zone) 
                    || zone.CustomGroup == null || !zone.CustomGroup.ShowMap())
                {
                    fsm.SendEvent("NO MAP");
                    return;
                }
                
                var tmp = text.gameObject.Value.GetComponent<TextMeshPro>();
                tmp.text = zone.CustomGroup.GroupName;
                
                var gm = sm.gameObject.Value.GetComponent<GameMap>();
                gm.QuickMapDirtmouth();
                gm.areaDirtmouth.SetActive(false);
                zone.CustomGroup.GetGameMap().SetActive(true);

                gm.transform.position = zone.ZoomToPos.Where(z: -20.1f);
            });
            
            custom.AddTransition("CLOSE QUICK MAP", "Check State");
        };

        HookUtils.OnFsmAwake += fsm =>
        {
            if (fsm.FsmName != "Position" || fsm.name != "Compass Icon") return;
            
            fsm.GetState("Nowhere").AddAction(() =>
            {
                if (SceneUtils.CustomScenes.TryGetValue(GameManager.instance.sceneName, out var scene)
                    && SceneUtils.SceneGroups.TryGetValue(scene.Group, out var group)
                    && group.ShowMap())
                {
                    fsm.transform.localPosition = (group.Pos + group.CompassPos)
                        .Where(z: fsm.transform.localPosition.z);
                }
            });
        };

        On.GameMap.PositionCompass += (orig, self, posShade) =>
        {
            var sceneName = self.inRoom ? self.doorScene : self.gm.sceneName;
            
            if (!SceneUtils.CustomScenes.TryGetValue(sceneName, out var scene))
            {
                orig(self, posShade);
                return;
            }

            if (!SceneUtils.SceneGroups.TryGetValue(scene.Group, out var group))
            {
                self.compassIcon.SetActive(false);
                self.displayingCompass = false;
                return;
            }

            var sceneGroup = group.GetGameMap();
            self.currentScene = scene.GetGameMapPiece();
            
            if (!sceneGroup || !self.currentScene)
            {
                self.compassIcon.SetActive(false);
                self.displayingCompass = false;
                return;
            }
            
            self.currentScenePos =
                new Vector3(self.currentScene.transform.localPosition.x + sceneGroup.transform.localPosition.x,
                    self.currentScene.transform.localPosition.y + sceneGroup.transform.localPosition.y, 0.0f);

            if (posShade)
            {
                if (!self.inRoom)
                {
                    self.shadeMarker.transform.localPosition =
                        new Vector3(self.currentScenePos.x, self.currentScenePos.y, 0.0f);
                }
                else
                {
                    var x1 = self.currentScenePos.x;
                    var rect = self.currentScene.GetComponent<SpriteRenderer>().sprite.rect;
                    var num1 = rect.size.x / 100.0 / 2.0;
                    var num2 = x1 - num1;
                    var num3 = (self.doorX + self.doorOriginOffsetX) / self.doorSceneWidth;
                    rect = self.currentScene.GetComponent<SpriteRenderer>().sprite.rect;
                    var num4 = rect.size.x / 100.0 * self.transform.localScale.x;
                    var num5 = num3 * num4 / self.transform.localScale.x;
                    var x2 = (float)(num2 + num5);
                    var y1 = self.currentScenePos.y;
                    rect = self.currentScene.GetComponent<SpriteRenderer>().sprite.rect;
                    var num6 = rect.size.y / 100.0 / 2.0;
                    var num7 = y1 - num6;
                    var num8 = (self.doorY + self.doorOriginOffsetY) / self.doorSceneHeight;
                    rect = self.currentScene.GetComponent<SpriteRenderer>().sprite.rect;
                    var num9 = rect.size.y / 100.0 * self.transform.localScale.y;
                    var num10 = num8 * num9 / self.transform.localScale.y;
                    var y2 = (float)(num7 + num10);
                    self.shadeMarker.transform.localPosition = new Vector3(x2, y2, 0.0f);
                }

                self.pd.SetVector3SwappedArgs(new Vector3(self.currentScenePos.x, self.currentScenePos.y, 0.0f),
                    "shadeMapPos");
            } else if (self.posGate)
            {
                self.dreamGateMarker.transform.localPosition =
                    new Vector3(self.currentScenePos.x, self.currentScenePos.y, 0.0f);
                self.pd.SetVector3SwappedArgs(new Vector3(self.currentScenePos.x, self.currentScenePos.y, 0.0f),
                    "dreamgateMapPos");
            }
            else
            {
                if (self.pd.GetBool("equippedCharm_2"))
                {
                    self.displayingCompass = true;
                    self.compassIcon.SetActive(true);
                }
                else
                {
                    self.compassIcon.SetActive(false);
                    self.displayingCompass = false;
                }
            }
        };

        On.PlayerData.HasMapForScene += (orig, self, name) =>
        {
            if (SceneUtils.CustomScenes.TryGetValue(name, out var scene))
            {
                return SceneUtils.SceneGroups.TryGetValue(scene.Group, out var group) && group.ShowMap();
            }
            return orig(self, name);
        };
        
        HookUtils.OnFsmAwake += fsm =>
        {
            if (fsm.name != "World Map" || fsm.FsmName != "UI Control") return;
            
            Zones.Clear();
            MapZoneInfo lastZone = null;
            
            var mapState = fsm.AddState("Map");
            fsm.AddGlobalTransition("ARCHITECT", mapState.name);
            
            var town = fsm.GetState("Town");
            mapState.AddAction(new SendEventByName
            {
                sendEvent = "UNSELECT",
                eventTarget = town.GetAction<SendEventByName>(0).eventTarget,
                delay = 0
            });
            
            var selectAction = new SendEventByName
            {
                sendEvent = "SELECT",
                delay = 0
            };
            mapState.AddAction(selectAction);

            var nextDir = Dir.None;
            var lastDefaultMap = string.Empty;

            foreach (var state in fsm.FsmStates)
            {
                var dir = Dir.None;

                var mapName = string.Empty;

                var i = 0;
                if (state.name.StartsWith("To Map"))
                {
                    dir = state.name == "To Map 2" ? Dir.Left : Dir.Right;
                    state.AddAction(() => lastZone = null, 0);
                    i += 2;
                }
                else
                {
                    var gls = state.GetFirstActionOfType<GetLanguageString>();
                    if (gls == null) continue;
                    if (gls.storeValue.name != "Area String") continue;

                    var id = state.GetFirstActionOfType<SetStringValue>().stringValue.Value;
                    var et = state.GetActions<SendEventByName>()
                        .First(send => send.sendEvent.value == "SELECT").eventTarget;
                    var zoneInfo = new MapZoneInfo
                    {
                        Id = id,
                        ZoomToPos = state.GetFirstActionOfType<SetVector3Value>().vector3Value.Value,
                        EventTarget = et
                    };
                    Zones.Add(id, zoneInfo);
                    mapName = id;

                    zoneInfo.DirectionalZones = [[], [], [], []];
                    for (var d = 0; d < 4; d++)
                    {
                        var trans = state.GetTransition($"UI {((Dir)d).ToString().ToUpper()}");
                        if (trans != null)
                        {
                            zoneInfo.DirectionalZones[d] = trans.toFsmState
                                .GetActions<PlayerDataBoolTest>()
                                .Where(t => t.isTrue != null)
                                .Select(t => t.isTrue.name)
                                .Concat(trans.toFsmState
                                    .GetActions<SendEvent>()
                                    .Select(t => t.sendEvent.name)
                                    .Where(n => !n.StartsWith("ARROW"))).ToList();
                        }
                    }
                }

                state.AddAction(() =>
                {
                    lastDefaultMap = mapName;
                    nextDir = dir;
                    fsm.SendEvent("ARCHITECT");
                }, i);
            }

            var zoomToPos = fsm.FsmVariables.FindFsmVector3("Zoom To Pos");
            var currentSel = fsm.FsmVariables.FindFsmString("Current Selection");
            
            var cursorItem = fsm.gameObject.LocateMyFSM("Update Cursor").FsmVariables
                .FindFsmGameObject("Item");
            
            var wideMap = fsm.transform.Find("Wide Map");

            var mapZoom = fsm.GetState("Map Zoom");
            var add1 = mapZoom.GetAction<FloatAdd>(7).add;
            var add2 = mapZoom.GetAction<FloatAdd>(8).add;
            var setScale = mapZoom.GetAction<SetScale>(12);

            var zoomOut = fsm.GetState("Zoom Out");
            var tweenScale = zoomOut.GetAction<iTweenScaleTo>(6).vectorScale;

            var rescale = () =>
            {
                var scale = CalculateMapScale();

                wideMap.position = (wideMap.localPosition + new Vector3(-4.05f, 7.55f)) * scale
                    + wideMap.parent.position - new Vector3(-4.05f, 7.55f);
                wideMap.localScale = (Vector3.one * scale).Where(z: wideMap.localScale.z);

                add1.value = 3.81f * scale;
                add2.value = -7.77f * scale;
                setScale.x = setScale.y = setScale.z = 0.436f * scale;
                tweenScale.value = Vector3.one * (0.436f * scale);
            };
            fsm.GetState("Pos 1").AddAction(rescale);
            fsm.GetState("Pos 2").AddAction(rescale);
            fsm.GetState("Pos 3").AddAction(rescale);
            
            mapState.AddAction(() =>
            {
                var nd = nextDir;
                nextDir = Dir.None;

                MapZoneInfo toZone;

                if (nd != Dir.None)
                {
                    foreach (var mzi in Zones.Values) mzi.EnsureSetup();
                    toZone = lastZone == null ? 
                        FindClosestZone(cursorItem.value.transform.position, wideMap) 
                        : lastZone.GetInDirection(nd);
                }
                else
                {
                    if (!Zones.TryGetValue(lastDefaultMap, out toZone) || !toZone.IsAvailable())
                    {
                        toZone = Zones["TOWN"];
                    }
                }
                
                if (toZone == null)
                {
                    switch (nd)
                    {
                        case Dir.Left:
                            fsm.SendEvent("ARROW L");
                            break;
                        case Dir.Right:
                            fsm.SendEvent("ARROW R");
                            break;
                    }
                    
                    return;
                }

                lastZone = toZone;
                selectAction.eventTarget = toZone.EventTarget;
                zoomToPos.Value = toZone.ZoomToPos;
                currentSel.Value = toZone.Id;
            }, 0);

            var zoomShortcut = fsm.FsmVariables.FindFsmBool("Zoom Shortcut");
            mapState.AddAction(() =>
            {
                if (zoomShortcut.Value) fsm.SendEvent("MAP ZOOM");
            });

            var left = fsm.AddState("Move Left");
            var right = fsm.AddState("Move Right");
            var up = fsm.AddState("Move Up");
            var down = fsm.AddState("Move Down");
            
            mapState.AddTransition("UI DOWN", down.name);
            mapState.AddTransition("UI UP", up.name);
            mapState.AddTransition("UI RIGHT", right.name);
            mapState.AddTransition("UI LEFT", left.name);
            mapState.AddTransition("UI CONFIRM", "To Zoom 1");
            
            left.AddAction(() => nextDir = Dir.Left);
            right.AddAction(() => nextDir = Dir.Right);
            up.AddAction(() => nextDir = Dir.Up);
            down.AddAction(() => nextDir = Dir.Down);
            
            left.AddTransition("FINISHED", mapState.name);
            right.AddTransition("FINISHED", mapState.name);
            up.AddTransition("FINISHED", mapState.name);
            down.AddTransition("FINISHED", mapState.name);
            
            fsm.GetState("Reset").AddAction(() =>
            {
                lastDefaultMap = currentSel.Value;
                fsm.SendEvent("ARCHITECT");
            });

            var currentArea = fsm.FsmVariables.FindFsmString("Current Area");
            fsm.GetState("Activate").AddAction(() =>
            {
                lastDefaultMap = currentArea.Value;
                fsm.SendEvent("ARCHITECT");
            });

            SceneGroup.RefreshMaps();
        };

        On.GameMap.Start += (orig, self) =>
        {
            orig(self);
            SceneGroup.RefreshMaps();
        };
    }

    private static float CalculateMapScale()
    {
        var scale = 1f;
        
        foreach (var group in SceneGroup.Groups.Where(g => g.HasMap && g.MapSprite))
        {
            var max = group.Pos + group.MapSprite.bounds.max;
            var min = group.Pos + group.MapSprite.bounds.min;
            
            scale = Mathf.Min(
                scale,
                6 / Mathf.Abs(max.y), 6 / Mathf.Abs(min.y),
                10 / Mathf.Abs(max.x), 10 / Mathf.Abs(min.x)
            );
        }

        return scale;
    }

    private static MapZoneInfo FindClosestZone(Vector2 pos, Transform wideMap)
    {
        var mzr = wideMap.Cast<Transform>()
            .Select(i => i.GetComponent<MapZoneReference>())
            .Where(i => i && i.gameObject.activeSelf)
            .OrderBy(i => ((Vector2)i.renderer.bounds.ClosestPoint(pos) - pos).magnitude)
            .FirstOrDefault();
        return mzr ? mzr.ZoneInfo : null;
    }

    public enum Dir
    {
        Left,
        Right,
        Up,
        Down,
        None
    }

    public class MapZoneReference : MonoBehaviour
    {
        public MapZoneInfo ZoneInfo;
        public SpriteRenderer renderer;
    }

    public class MapZoneInfo
    {
        public string Id;
        public Vector3 ZoomToPos;
        public FsmEventTarget EventTarget;
        public List<string>[] DirectionalZones;
        public string[][] InverseDirectionalZones;

        [CanBeNull] public SceneGroup CustomGroup;

        private GameObject Obj
        {
            get
            {
                if (!field) field = EventTarget.gameObject.gameObject.value;
                return field;
            }
        }

        public bool IsAvailable() => Obj.activeSelf;

        public void EnsureSetup()
        {
            if (Obj.GetComponent<MapZoneReference>()) return;

            var mzr = Obj.AddComponent<MapZoneReference>();
            mzr.ZoneInfo = this;
            mzr.renderer = Obj.GetComponent<SpriteRenderer>();

            if (InverseDirectionalZones == null) return;
            for (var i = 0; i < 4; i++)
            {
                foreach (var zoneId in InverseDirectionalZones[i])
                {
                    if (!Zones.TryGetValue(zoneId, out var zone)) continue;
                    zone.DirectionalZones[i].Insert(0, Id);
                }
            }
        }

        public MapZoneInfo GetInDirection(Dir dir)
        {
            return GetInDirection(dir, []);
        }

        private MapZoneInfo GetInDirection(Dir dir, List<MapZoneInfo> seen)
        {
            if (seen.Contains(this)) return null;

            seen.Add(this);
            List<MapZoneInfo> continueFrom = [];
            foreach (var zoneId in DirectionalZones[(int)dir])
            {
                if (!Zones.TryGetValue(zoneId, out var zone)) continue;
                if (zone.IsAvailable()) return zone;
                continueFrom.Add(zone);
            }

            return continueFrom
                .Select(zone => zone.GetInDirection(dir, seen))
                .FirstOrDefault(recursiveZone => recursiveZone != null);
        }
    }

    public static void RegisterCustomMap(MapZoneInfo info)
    {
        Zones.Add(info.Id, info);
    }
    
    public static void UnregisterCustomMap(string id)
    {
        Zones.Remove(id);
        foreach (var directionalZones in Zones.Values.SelectMany(zone => zone.DirectionalZones))
        {
            directionalZones.Remove(id);
        }
    }
}