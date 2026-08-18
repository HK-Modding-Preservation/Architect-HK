using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Architect.Behaviour.Custom;
using Architect.Content.Preloads;
using Architect.Events.Blocks.Operators;
using GlobalEnums;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Satchel.Futils;
using UnityEngine;

namespace Architect.Behaviour.Fixers;

public static class MiscFixers
{
    public static Material SpriteMaterial;

    // Dive Coffin
    private static GameObject _coffinFloor;

    // Conveyors
    private static GameObject _conveyorBlock;
    private static readonly Dictionary<ConveyorMovement, List<ConveyorBelt>> CurrentBelts = [];
    private static readonly List<ConveyorBelt> CurrentVerticalBelts = [];

    public static void Init()
    {
        // So custom persistent int items have data
        On.PersistentIntItem.Awake += (orig, self) =>
        {
            self.persistentIntData ??= new PersistentIntData();
            orig(self);
        };
        
        PreloadManager.RegisterPreload(new BasicPreload("Tutorial_01", "_Scenery/plat_float_01",
            o => { SpriteMaterial = o.GetComponent<SpriteRenderer>().material; }));

        PreloadManager.RegisterPreload(new BasicPreload("RestingGrounds_05", "Quake Floor",
            o => _coffinFloor = o));

        PreloadManager.RegisterPreload(new BasicPreload("Mines_31", "Conveyor Block",
            o => _conveyorBlock = o));

        ModHooks.LanguageGetHook += (key, title, orig) =>
        {
            if (title == "ArchitectMod") return SubstituteVars(key);
            return key.StartsWith("ArchitectMod_") ? SubstituteVars(key[13..]) : orig;
        };

        On.HeroController.SceneInit += (orig, self) =>
        {
            orig(self);
            CurrentVerticalBelts.Clear();
            CurrentBelts.Clear();
            self.GetComponent<ConveyorMovementHero>().StopConveyorMove();
            HeroController.instance.cState.onConveyor = false;
            HeroController.instance.cState.onConveyorV = false;
        };

        On.ConveyorBelt.OnCollisionExit2D += (orig, self, collision) =>
        {
            if (self.vertical)
            {
                if (collision.gameObject.GetComponent<HeroController>())
                {
                    CurrentVerticalBelts.Remove(self);
                    if (CurrentVerticalBelts.Count > 0) return;
                }
            }
            else
            {
                var move = collision.gameObject.GetComponent<ConveyorMovement>();
                if (move)
                    if (CurrentBelts.TryGetValue(move, out var group))
                    {
                        group.Remove(self);
                        if (group.Count > 0) return;
                    }
            }

            orig(self, collision);
        };

        On.ConveyorBelt.OnCollisionEnter2D += (orig, self, collision) =>
        {
            orig(self, collision);
            if (self.vertical)
            {
                if (collision.gameObject.GetComponent<HeroController>()) CurrentVerticalBelts.Add(self);
            }
            else
            {
                var move = collision.gameObject.GetComponent<ConveyorMovement>();
                if (move)
                {
                    if (!CurrentBelts.Keys.Contains(move)) CurrentBelts[move] = [self];
                    else CurrentBelts[move].Add(self);
                }
            }
        };
        
        On.GameManager.FindEntryPoint += (orig, self, name, scene) =>
        {
            var point = orig(self, name, scene);
            if (!point.HasValue)
            {
                var hrm = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()
                    .SelectMany(obj => obj.GetComponentsInChildren<HazardRespawnMarker>(true))
                    .First();
                return hrm.transform.position;
            }

            return point;
        };

        On.HeroController.LocateSpawnPoint += (orig, self) =>
        {
            var point = orig(self);
            if (!point)
            {
                var hrm = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()
                    .SelectMany(obj => obj.GetComponentsInChildren<HazardRespawnMarker>(true))
                    .First();
                return hrm.transform;
            }

            return point;
        };
    }

    private static string SubstituteVars(string txt)
    {
        return Regex.Replace(txt, @"\[\[(.*?)\]\]", match =>
        {
            var key = match.Groups[1].Value;
            return StringVarBlock.GetVar(key);
        });
    }

    public class ColorLock : MonoBehaviour
    {
        public bool permanent;
    }

    public class TriggerActivator : MonoBehaviour
    {
        public int layer;
    }

    public static void KeepActive(GameObject obj)
    {
        obj.RemoveComponent<DeactivateIfPlayerdataFalse>();
        obj.RemoveComponent<DeactivateIfPlayerdataTrue>();
    }

    public static void AddComponent<T>(GameObject obj) where T : MonoBehaviour
    {
        obj.AddComponent<T>();
    }

    public class Npc : MonoBehaviour
    {
        public string dialogue;
    }

    public static void FixTiso(GameObject obj)
    {
        obj.AddComponent<Tiso>();
        KeepActive(obj);
    }

    public class Tiso : Npc
    {
        private void Start()
        {
            var cc = gameObject.LocateMyFSM("Conversation Control");
            cc.GetState("Convo Choice").AddAction(() => cc.SendEvent("REPEAT"), 0);
            var p = ((CallMethodProper)cc.GetState("Repeat").Actions[1]).parameters;
            p[0].stringValue = dialogue;
            p[1].stringValue = "ArchitectMod";

            cc.GetState("Talk Finish").AddAction(() => gameObject.BroadcastEvent("OnFinish"), 0);
        }
    }

    public class Elderbug : Npc
    {
        private void Start()
        {
            var cc = gameObject.LocateMyFSM("Conversation Control");
            cc.GetState("Grimm?").DisableAction(0);
            cc.GetState("Convo Choice").AddAction(() => cc.SendEvent("GENERIC"), 0);
            cc.GetState("Generic Choice").AddAction(() => cc.SendEvent("FINISHED"), 0);
            var p = ((CallMethodProper)cc.GetState("Normal").Actions[0]).parameters;
            p[0].stringValue = dialogue;
            p[1].stringValue = "ArchitectMod";

            cc.GetState("Talk Finish").AddAction(() => gameObject.BroadcastEvent("OnFinish"), 0);
        }
    }

    public class Quirrel : Npc
    {
        private void Start()
        {
            var cc = gameObject.LocateMyFSM("Conversation Control");

            cc.GetState("Convo Choice").AddAction(() => cc.SendEvent("TEMPLE"), 0);
            cc.GetState("Egg Temple").AddAction(() => cc.SendEvent("EGG 1"), 0);

            var et1 = cc.GetState("Egg Temple 1");
            et1.DisableAction(0);

            var p = ((CallMethodProper)et1.Actions[4]).parameters;
            p[0].stringValue = dialogue;
            p[1].stringValue = "ArchitectMod";

            cc.GetState("End").AddAction(() => gameObject.BroadcastEvent("OnFinish"), 0);
        }
    }

    public class Hornet : Npc
    {
        private void Start()
        {
            var cc = gameObject.LocateMyFSM("Conversation Control");

            cc.GetState("Choice").AddAction(() => cc.SendEvent("REPEAT"), 1);

            var p = ((CallMethodProper)cc.GetState("Repeat").Actions[1]).parameters;
            p[0].stringValue = dialogue;
            p[1].stringValue = "ArchitectMod";

            cc.GetState("End").AddAction(() => gameObject.BroadcastEvent("OnFinish"), 0);
        }
    }

    public class Midwife : Npc
    {
        private void Start()
        {
            var go = transform.GetChild(1).gameObject;

            var cc = go.LocateMyFSM("Conversation Control");

            cc.GetState("Convo Choice").AddAction(() => cc.SendEvent("FINISHED"), 1);

            var p = ((CallMethodProper)cc.GetState("Repeat").Actions[0]).parameters;
            p[0].stringValue = dialogue;
            p[1].stringValue = "ArchitectMod";

            cc.GetState("End").AddAction(() => gameObject.BroadcastEvent("OnFinish"), 0);
        }
    }

    public static void FixZote(GameObject obj)
    {
        obj.AddComponent<Zote>();
        obj.RemoveComponent<DeactivateIfPlayerdataTrue>();
    }

    public class Zote : Npc
    {
        private PlayMakerFSM _cc;

        private void Awake()
        {
            _cc = gameObject.LocateMyFSM("Conversation Control");
            _cc.GetState("Check Active").AddAction(() => _cc.SendEvent("FINISHED"), 0);
        }

        private void Start()
        {
            _cc.GetState("Convo Choice").AddAction(() => _cc.SendEvent("REPEAT"), 6);
            var p = ((CallMethodProper)_cc.GetState("Repeat").actions[1]).parameters;
            p[0].stringValue = dialogue;
            p[1].stringValue = "ArchitectMod";

            _cc.GetState("Talk Finish").AddAction(() => gameObject.BroadcastEvent("OnFinish"), 0);
        }
    }

    public class Godseeker : Npc
    {
        private void Start()
        {
            var cc = gameObject.LocateMyFSM("Conversation Control");

            var revisitCell = cc.FsmVariables.FindFsmString("Revisit Cell");
            var gts = cc.FsmVariables.FindFsmString("GameText Sheet");
            cc.GetState("Revisit?").AddAction(() =>
            {
                revisitCell.value = dialogue;
                gts.value = "ArchitectMod";
                cc.SendEvent("REVISIT");
            }, 2);

            cc.GetState("End").AddAction(() => gameObject.BroadcastEvent("OnFinish"), 0);
        }
    }

    public class Ghost : Npc
    {
        private void Start()
        {
            var cc = gameObject.LocateMyFSM("Conversation Control");
            
            cc.GetState("Convo Choice").AddAction(() => cc.SendEvent("REPEAT"), 0);
            
            var p = ((CallMethodProper)cc.GetState("Repeat").actions[0]).parameters;
            p[1].stringValue = "ArchitectMod";
            
            cc.GetState("Set Convos")
                .AddAction(() => cc.FsmVariables.FindFsmString("Repeat Convo").value = dialogue);

            cc.GetState("End").AddAction(() => gameObject.BroadcastEvent("OnFinish"), 0);
        }
    }

    public static void RotateBench(GameObject obj, float rot)
    {
        obj.transform.SetRotation2D(rot);
        if (rot == 0) return;
        var fsm = obj.GetComponentsInChildren<PlayMakerFSM>()
            .First(fsm => fsm.FsmName == "Bench Control");
        fsm.FsmVariables.FindFsmBool("Tilter").Value = true;
        obj.AddComponent<RestBenchTilt>().tilt = rot;
    }

    public class Tablet : Npc
    {
        private void Start()
        {
            var fsm = gameObject.LocateMyFSM("Inspection");

            fsm.FsmVariables.GetFsmString("Convo Name").Value = dialogue;
            fsm.FsmVariables.GetFsmString("Sheet Name").Value = "ArchitectMod";
        }
    }

    public static void FixRotation(GameObject obj)
    {
        obj.transform.SetRotation2D(0);
    }

    public static void BreakableZ(GameObject obj)
    {
        obj.transform.SetPositionZ(0.015f);
    }

    public static void FixGrubBottle(GameObject obj)
    {
        var fsm = obj.LocateMyFSM("Bottle Control");

        fsm.GetState("Destroy Self").AddAction(() =>
        {
            obj.BroadcastEvent("OnBreak");
            obj.BroadcastEvent("FirstBreak");
        }, 0);
        fsm.GetState("Activate").AddAction(() =>
        {
            obj.BroadcastEvent("OnBreak");
            obj.BroadcastEvent("LoadedBroken");
        }, 0);
    }

    public static void FixLever(GameObject obj)
    {
        var fsm = obj.LocateMyFSM("Switch Control");
        fsm.GetState("Open").AddAction(() =>
        {
            obj.BroadcastEvent("OnPull");
            obj.BroadcastEvent("FirstPull");
        });
        var activated = fsm.GetState("Activated");
        activated.transitions = [];
        activated.AddAction(() =>
        {
            obj.BroadcastEvent("OnPull");
            obj.BroadcastEvent("LoadedPulled");
        });
        (fsm.FsmVariables.FindFsmGameObject("Target") ?? fsm.FsmVariables.FindFsmGameObject("Target 1"))
            .value = null;
        fsm.FsmVariables.FindFsmString("Player Data").value = "";
    }

    public static void FixStationBell(GameObject obj)
    {
        var fsm = obj.LocateMyFSM("Stag Bell");
        fsm.GetState("Init").AddAction(() =>
        {
            fsm.SendEvent("OPENED");
        }, 5);
        var bellFsm = obj.transform.GetChild(0).GetComponent<PlayMakerFSM>();
        
        var rr = bellFsm.GetState("Ring R");
        var rl = bellFsm.GetState("Ring L");
        
        rr.DisableAction(1);
        rl.DisableAction(1);
        
        rr.AddAction(() => obj.BroadcastEvent("OnActivate"), 0);
        rl.AddAction(() => obj.BroadcastEvent("OnActivate"), 0);
    }

    public static void FixChest(GameObject obj)
    {
        var fsm = obj.LocateMyFSM("Chest Control");
        fsm.GetState("Opened").AddAction(() =>
        {
            obj.BroadcastEvent("OnOpen");
            obj.BroadcastEvent("FirstOpen");
        });
        fsm.GetState("Activated").AddAction(() =>
        {
            obj.BroadcastEvent("OnOpen");
            obj.BroadcastEvent("LoadedOpen");
        });
    }

    public static void FixToll(GameObject obj)
    {
        var fsm = obj.LocateMyFSM("Toll Machine");
        fsm.fsm.globalTransitions = [];
        fsm.GetState("Activated").AddAction(() =>
        {
            obj.BroadcastEvent("OnPay");
            obj.BroadcastEvent("LoadedPaid");
        }, 0);
        var og = fsm.GetState("Open Gates");
        og.DisableAction(1);
        og.AddAction(() =>
        {
            obj.BroadcastEvent("OnPay");
            obj.BroadcastEvent("FirstPay");
        }, 0);

        obj.AddComponent<Toll>().fsm = fsm;
    }

    public class Toll : Npc
    {
        public PlayMakerFSM fsm;

        public void Start()
        {
            var p = ((CallMethodProper)fsm.GetState("Send Text").actions[2]).parameters;
            p[0].stringValue = dialogue;
            p[1].stringValue = "ArchitectMod";

            fsm.FsmVariables.FindFsmString("Prompt Convo").value = dialogue;
        }
    }

    public static void FixNpc<T>(GameObject obj) where T : Npc
    {
        obj.AddComponent<T>();

        foreach (var fsm in obj.GetComponents<PlayMakerFSM>())
            if (fsm.FsmName == "FSM")
                fsm.enabled = false;
    }

    public static void FlipMidwife(GameObject obj, bool flip)
    {
        if (!flip) return;
        obj.transform.SetScaleX(-obj.transform.GetScaleX());

        obj.transform.GetChild(1).gameObject.LocateMyFSM("npc_control").FsmVariables
            .FindFsmFloat("Move To Offset")
            .Value = -3;
    }

    public static void FixDiveCoffin(GameObject obj)
    {
        var floor = Object.Instantiate(_coffinFloor, obj.transform);
        floor.SetActive(true);
        floor.transform.localPosition = Vector3.zero;
        floor.name = obj.name + " Floor";
    }

    public static void FixBreakableWall(GameObject obj)
    {
        foreach (Transform child in obj.transform)
        {
            if (child.name is "Masks" or "Camera Locks") Object.Destroy(child.gameObject);
        }
    }

    public class WpLift : MonoBehaviour
    {
        public Vector2 move;

        public void DoRise(PlayMakerFSM fsm, Vector2 end)
        {
            StartCoroutine(Rise(fsm, end));
        }

        private IEnumerator Rise(PlayMakerFSM fsm, Vector2 end)
        {
            var yMoved = false;
            var xMoved = false;

            while (!xMoved || !yMoved)
            {
                var pos = fsm.transform.position;

                yMoved = move.y == 0 ||
                         (move.y > 0 && pos.y >= end.y) ||
                         (move.y < 0 && pos.y <= end.y);

                xMoved = move.x == 0 ||
                         (move.x > 0 && pos.x >= end.x) ||
                         (move.x < 0 && pos.x <= end.x);

                yield return null;
            }

            fsm.SendEvent("TOP");
        }

        private void Start()
        {
            var fsm = gameObject.LocateMyFSM("Control");

            var rise = fsm.GetState("Rise");
            var translate = (Translate)rise.actions[4];
            var setVel = (SetVelocity2d)rise.actions[5];

            var moveDir = move.normalized * 30;

            translate.x = moveDir.x;
            translate.y = moveDir.y;
            setVel.vector = moveDir;

            var end = transform.position + (Vector3)move;

            rise.DisableAction(7);

            rise.AddAction(() => DoRise(fsm, end), 7);

            var sp = (SetPosition)fsm.GetState("Hit Top").actions[0];
            sp.x = end.x;
            sp.y = end.y;

            ((SetPosition)fsm.GetState("Return").actions[1]).x = transform.position.x;
        }
    }

    public static void FixMillibelle(GameObject obj)
    {
        var fsm = obj.LocateMyFSM("Hit Around");
        var init = fsm.GetState("Init");
        init.DisableAction(1);
        init.DisableAction(2);

        fsm.GetState("Cancel NPC").AddAction(() => fsm.SendEvent("OnHit"), 0);

        for (var i = 1; i < 4; i++) obj.transform.GetChild(i).gameObject.SetActive(false);
        obj.AddComponent<TriggerActivator>();
    }

    public static void FixZoteHead(GameObject obj)
    {
        obj.transform.SetRotation2D(0);
        obj.AddComponent<PngObject>();
        obj.AddComponent<Landable>();
    }

    public class Landable : TriggerActivator
    {
        private Collider2D _col2d;
        private bool _ground;

        private void Start()
        {
            _col2d = GetComponent<Collider2D>();
        }

        private void OnCollisionEnter2D(Collision2D _)
        {
            if (CheckTouchingGround())
            {
                _ground = true;
                gameObject.BroadcastEvent("OnLand");
            }
        }

        private void OnCollisionExit2D(Collision2D _)
        {
            if (_ground && !CheckTouchingGround())
            {
                _ground = false;
                gameObject.BroadcastEvent("OnAir");
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject.GetComponentInChildren<NailSlash>())
                gameObject.BroadcastEvent("OnHit");
        }

        public bool CheckTouchingGround()
        {
            if (!_col2d) return false;
            var bounds1 = _col2d.bounds;
            double x1 = bounds1.min.x;
            bounds1 = _col2d.bounds;
            double y1 = bounds1.center.y;
            var vector21 = new Vector2((float)x1, (float)y1);
            Vector2 center = _col2d.bounds.center;
            var bounds2 = _col2d.bounds;
            double x2 = bounds2.max.x;
            bounds2 = _col2d.bounds;
            double y2 = bounds2.center.y;
            var vector22 = new Vector2((float)x2, (float)y2);
            bounds2 = _col2d.bounds;
            var distance = bounds2.extents.y + 0.16f;
            Debug.DrawRay(vector21, Vector2.down, Color.yellow);
            Debug.DrawRay(center, Vector2.down, Color.yellow);
            Debug.DrawRay(vector22, Vector2.down, Color.yellow);
            var raycastHit2D1 = Physics2D.Raycast(vector21, Vector2.down, distance, 256);
            var raycastHit2D2 = Physics2D.Raycast(center, Vector2.down, distance, 256);
            var raycastHit2D3 = Physics2D.Raycast(vector22, Vector2.down, distance, 256);
            return raycastHit2D1.collider || raycastHit2D2.collider || raycastHit2D3.collider;
        }
    }

    public static void FixColoWall(GameObject obj)
    {
        var child = obj.transform.GetChild(0);

        child.GetChild(0).gameObject.SetActive(false);
        foreach (var i in new[] { 0, 2, 4, 5, 6 }) child.GetChild(1).GetChild(i).gameObject.SetActive(false);

        var col = child.GetComponent<BoxCollider2D>();
        col.size = new Vector2(1.4581f, 6.4249f);
        col.offset = Vector2.zero;

        obj.AddComponent<ColoWall>();
    }

    public class ColoWall : MonoBehaviour
    {
        public float moveDistance;
        public float moveSpeed = 28;
        public bool flipped;
        public float rotation;

        private void Start()
        {
            var fsm = gameObject.LocateMyFSM("Control");

            var val = new Vector2(moveSpeed, 0);
            if (!flipped) val.x = -val.x;
            val = Quaternion.Euler(0, 0, rotation) * val;

            fsm.FsmVariables.FindFsmVector2("In Speed").Value = val;
            fsm.FsmVariables.FindFsmVector2("Out Speed").Value = -val;
        }
    }

    public static void FlipColoWall(GameObject obj, bool flip)
    {
        if (!flip) return;
        obj.transform.SetScaleX(-obj.transform.GetScaleX());
        obj.GetComponent<ColoWall>().flipped = true;
    }

    public static void RotateColoWall(GameObject obj, float rot)
    {
        obj.transform.SetRotation2D(rot);
        obj.GetComponent<ColoWall>().rotation = rot;
    }

    public static void FixConveyorPart(GameObject obj)
    {
        FixConveyor(obj, new Vector3(5.1f, 0.5f, 1), new Vector3(0, -0.0703f), new Vector2(0.9293f, 1.0295f), 8);
    }

    public static void FixConveyorS(GameObject obj)
    {
        FixConveyor(obj, new Vector3(6.66f, 2, 1), new Vector3(1.5521f, -2.3676f), new Vector2(1, 1), 5);
    }

    public static void FixConveyorL(GameObject obj)
    {
        FixConveyor(obj, new Vector3(10.76f, 2, 1), new Vector2(-0.2104f, -2.3576f), new Vector2(0.971f, 1), 5);
    }

    private static void FixConveyor(GameObject obj, Vector3 scale, Vector2 pos, Vector2 bc2dSize, int speed)
    {
        var block = Object.Instantiate(_conveyorBlock, obj.transform);

        block.GetComponent<BoxCollider2D>().size = bc2dSize;
        block.GetComponent<ConveyorBelt>().speed = speed;

        block.transform.localScale = scale;
        block.transform.localPosition = pos;

        block.SetActive(true);
    }

    public static void FlipConveyor(GameObject obj, bool flip)
    {
        if (flip)
        {
            obj.transform.SetScaleX(-obj.transform.GetScaleX());
            return;
        }
        
        var belt = obj.GetComponentInChildren<ConveyorBelt>();
        belt.speed = -belt.speed;
    }
    
    public static void RotateConveyor(GameObject obj, float rot)
    {
        obj.transform.SetRotation2D(rot);
        
        if (rot is 90 or 270)
        {
            var belt = obj.GetComponentInChildren<ConveyorBelt>();
            if (rot is 270) belt.speed = -belt.speed;
            belt.vertical = true;
        }
    }
    
    private static readonly int EnemyLayer = LayerMask.NameToLayer("Enemies");

    public static void FixCloth(GameObject obj)
    {
        var fsm = obj.LocateMyFSM("Control");
        
        fsm.GetState("Wait").AddAction(() => fsm.SendEvent("CLOTH ENTER"));

        var gt = fsm.GetState("Get Target");
        gt.DisableAction(0);
        var target = fsm.FsmVariables.FindFsmGameObject("Target");
        gt.AddAction(() =>
        {
            var enemy = Object
                .FindObjectsOfType<Collider2D>()
                .Where(h => h.gameObject != obj && h.gameObject.layer == EnemyLayer)
                .OrderBy(h => (h.transform.position - obj.transform.position).sqrMagnitude)
                .FirstOrDefault();
            if (enemy && (enemy.transform.position - obj.transform.position).sqrMagnitude < 512) 
                target.value = enemy.gameObject;
        }, 0);
    }

    public class AlphaClamp : MonoBehaviour;

    public static void FixAcid(GameObject obj)
    {
        obj.transform.localScale = Vector3.one;
        obj.transform.SetPositionZ(0);
        
        obj.transform.GetChild(1).gameObject.SetActive(false);
        obj.transform.GetChild(2).gameObject.SetActive(false);

        var bc1 = obj.transform.GetChild(0).GetComponent<BoxCollider2D>();
        var bc2 = obj.transform.GetChild(3).GetComponent<BoxCollider2D>();
        bc1.size = new Vector2(5, 5);
        bc2.size = new Vector2(5, 5);

        bc1.offset += new Vector2(7.3787f, 5.5951f);
        bc2.offset += new Vector2(7.3787f, 5.5951f);
    }

    public static void FixWater(GameObject obj)
    {
        obj.transform.localScale = Vector3.one;
        obj.transform.SetPositionZ(0);

        var bc1 = obj.GetComponent<BoxCollider2D>();
        var bc2 = obj.transform.GetChild(0).GetComponent<BoxCollider2D>();
        bc1.size = new Vector2(5, 5);
        bc2.size = new Vector2(5, 5);

        bc1.offset += new Vector2(14.6142f, 0.7963f);
        bc2.offset += new Vector2(14.6142f, 0.7963f);
    }

    public static void ScaleEnemyBullet(GameObject obj, float scale)
    {
        obj.transform.localScale *= scale;
        var eb = obj.GetComponent<EnemyBullet>();
        eb.scale *= scale;
        eb.scaleMin *= scale;
        eb.scaleMax *= scale;
    }

    public static void ScaleSoulOrb(GameObject obj, float scale)
    {
        obj.transform.localScale *= scale;
        var setScale = (SetScale)obj.LocateMyFSM("Orb Control").GetState("Init").actions[1];
        setScale.x.value *= scale;
        setScale.y.value *= scale;
    }

    public static GameObject PogoableExplosionL;
    public static GameObject PogoableExplosionM;

    public static void FixOomaCore(GameObject obj)
    {
        var explosionPrefab = ((SpawnObjectFromGlobalPool)obj.LocateMyFSM("Lil Jelly")
                .GetState("Die").actions[1]).gameObject.value;

        explosionPrefab.SetActive(false);
        PogoableExplosionL = Object.Instantiate(explosionPrefab);
        PogoableExplosionL.name = "[Architect] Bounceable Explosion L";
        explosionPrefab.SetActive(true);
        
        var fsm = PogoableExplosionL.LocateMyFSM("Explosion Control");
        fsm.AddState("Prefab");

        var oldStart = fsm.fsm.startState;
        fsm.fsm.startState = "Prefab";
        foreach (Transform child in PogoableExplosionL.transform) child.gameObject.SetActive(false);
        
        Object.DontDestroyOnLoad(PogoableExplosionL);
        PogoableExplosionL.SetActive(true);

        fsm.fsm.startState = oldStart;

        PogoableExplosionL.GetComponent<NonBouncer>().active = false;
        PogoableExplosionL.layer = (int)PhysLayers.ENEMIES;

        PogoableExplosionL.AddComponent<EnableCollider>();
        PogoableExplosionL.CreatePool();

        PogoableExplosionL.GetComponent<Collider2D>().enabled = false;
    }

    public static void FixSporgBullet(GameObject obj)
    {
        var explosionPrefab = ((SpawnObjectFromGlobalPool)obj.LocateMyFSM("Spore Bomb")
            .GetState("Explode").actions[1]).gameObject.value;

        explosionPrefab.SetActive(false);
        PogoableExplosionM = Object.Instantiate(explosionPrefab);
        PogoableExplosionM.name = "[Architect] Bounceable Explosion M";
        explosionPrefab.SetActive(true);
        
        var fsm = PogoableExplosionM.LocateMyFSM("Explosion Control");
        fsm.AddState("Prefab");

        var oldStart = fsm.fsm.startState;
        fsm.fsm.startState = "Prefab";
        foreach (Transform child in PogoableExplosionM.transform) child.gameObject.SetActive(false);
        
        Object.DontDestroyOnLoad(PogoableExplosionM);
        PogoableExplosionM.SetActive(true);

        fsm.fsm.startState = oldStart;

        PogoableExplosionM.GetComponent<NonBouncer>().active = false;
        PogoableExplosionM.layer = (int)PhysLayers.ENEMIES;

        PogoableExplosionM.AddComponent<EnableCollider>();
        PogoableExplosionM.CreatePool();

        PogoableExplosionM.GetComponent<Collider2D>().enabled = false;
    }

    public class EnableCollider : MonoBehaviour
    {
        public void OnEnable()
        {
            GetComponent<Collider2D>().enabled = true;
        }
    }

    public static void FixPkCorpse(GameObject obj)
    {
        obj.LocateMyFSM("Control").GetState("Break").AddAction(() => obj.BroadcastEvent("OnBreak"), 0);
    }

    public static void FixScream(GameObject obj)
    {
        var fsm = obj.LocateMyFSM("FSM");
        fsm.fsmTemplate = null;
        fsm.GetState("Reposition?").DisableAction(2);
        fsm.GetState("Set Rotation?").DisableAction(1);
        fsm.GetState("Unparent?").DisableAction(3);
        fsm.GetState("Reparent?").DisableAction(1);

        obj.LocateMyFSM("Deactivate on Hit").enabled = false;

        foreach (var dfsm in obj.GetComponentsInChildren<PlayMakerFSM>()
                     .Where(f => f.FsmName == "Set Damage"))
            dfsm.enabled = false;
    }

    public static void FixVs(GameObject obj)
    {
        var fsm = obj.LocateMyFSM("Fireball Control");
        var rb2d = obj.GetComponent<Rigidbody2D>();

        var sd = fsm.GetState("Set Damage");
        sd.DisableAction(0);
        sd.AddAction(() => fsm.SendEvent("FINISHED"), 2);
        
        fsm.GetState("L").DisableAction(1);

        var vel = Vector2.zero;
        fsm.GetState("Init").AddAction(() =>
        {
            vel = new Vector2(45, 0);
            if (obj.transform.GetScaleX() < 0) vel.x *= -1;
            vel = obj.transform.rotation * vel;
            rb2d.velocity = vel;
        }, 0);
        
        var idle = fsm.GetState("Idle");
        idle.DisableAction(4);
        idle.DisableAction(6);
        
        idle.AddAction(new FsmUtils.EveryFrameAction(() =>
        {
            if (rb2d.velocity.magnitude < 0.1f) fsm.SendEvent("WALL");
        }), 6);
        idle.AddAction(() => rb2d.velocity = vel, 4);
    }

    public static void FixSs(GameObject obj)
    {
        var fsm = obj.LocateMyFSM("Fireball Control");
        var rb2d = obj.GetComponent<Rigidbody2D>();

        var sd = fsm.GetState("Set Damage");
        sd.DisableAction(0);
        sd.AddAction(() => fsm.SendEvent("FINISHED"), 2);
        
        fsm.GetState("L").DisableAction(1);

        fsm.GetState("Init").AddAction(() =>
        {
            var vel = new Vector2(45, 0);
            if (obj.transform.GetScaleX() < 0) vel.x *= -1;
            vel = obj.transform.rotation * vel;
            rb2d.velocity = vel;
        }, 0);
        
        var idle = fsm.GetState("Idle");
        idle.DisableAction(2);
        
        idle.AddAction(new FsmUtils.EveryFrameAction(() =>
        {
            if (rb2d.velocity.magnitude < 0.1f) fsm.SendEvent("WALL");
        }));
    }

    public static void RotateFireball(GameObject obj, float rot)
    {
        obj.transform.SetRotation2D(rot);
        obj.LocateMyFSM("damages_enemy").FsmVariables.FindFsmFloat("direction").value = rot;
    }

    public static void FixGhost(GameObject obj)
    {
        KeepActive(obj);
        obj.AddComponent<Ghost>();
    }

    private static readonly LayerMask DamagingCrystalsMask = LayerMask.GetMask("Player", "Terrain");
    private static readonly ParticleSystem.MinMaxGradient DamagingCrystalsGradient = new(new Color(1, 0.4f, 0.5f));
    
    public static void FixDamagingCrystals(GameObject obj)
    {
        var ps = obj.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.startColor = DamagingCrystalsGradient;
        var collision = ps.collision;
        collision.sendCollisionMessages = true;
        collision.collidesWith = DamagingCrystalsMask;
        obj.AddComponent<DamagingCrystals>();
    }

    private class DamagingCrystals : MonoBehaviour
    {
        private void OnParticleCollision(GameObject other)
        {
            if (other.layer == 9) HeroController.instance.TakeDamage(null, 0, 1, 1);
        }
    }

    public class Lift : MonoBehaviour
    {
        public float speed = 8;
        public bool startUp;
        public float offset = 10;

        private void Awake()
        {
            var fsm = gameObject.LocateMyFSM("Lift Control");
            
            fsm.GetState("Check Active").AddAction(() => fsm.SendEvent("ACTIVE"), 0);

            var md = fsm.GetState("Move Down");
            var mu = fsm.GetState("Move Up");
            ((Translate)md.actions[6]).y = -speed;
            ((Translate)mu.actions[5]).y = speed;
            
            fsm.GetState("Init").AddAction(() => fsm.SendEvent(startUp ? "START UP" : "START DOWN"), 13);
            
            fsm.GetState("Start Down").DisableAction(0);
            ((SetPosition)fsm.GetState("Start Up").actions[0]).y = transform.GetPositionY() + offset;

            ((FloatCompare)md.actions[8]).float2 = transform.GetPositionY();
            ((FloatCompare)mu.actions[7]).float2 = transform.GetPositionY() + offset;
            
            ((SetPosition)fsm.GetState("Reached Bot").actions[4]).y = transform.GetPositionY();
            ((SetPosition)fsm.GetState("Reached Top").actions[4]).y = transform.GetPositionY() + offset;
            
            fsm.GetState("Down").DisableAction(4);
            fsm.GetState("Up").DisableAction(4);
        }
    }

    public static void FixGeoRock(GameObject obj)
    {
        obj.RemoveComponent<GeoRock>();
        
        var fsm = obj.LocateMyFSM("Geo Rock");
        fsm.fsmTemplate = null;
        
        fsm.GetState("Broken").AddAction(() => obj.BroadcastEvent("OnBreak"), 0);
        fsm.GetState("Destroy").AddAction(() => obj.BroadcastEvent("OnBreak"), 0);
        var hit = fsm.GetState("Hit");
        hit.AddAction(() => obj.BroadcastEvent("OnHit"), 0);
        
        fsm.AddVariable<FsmInt>("Value");
        obj.AddComponent<PersistentIntItem>();

        var hits = fsm.FsmVariables.FindFsmInt("Hits");
        var value = fsm.FsmVariables.FindFsmInt("Value");
        
        fsm.GetState("Pause Frame Init").AddAction(() => value.Value = hits.Value, 0);
        fsm.GetState("Initiate").AddAction(() => hits.Value = value.Value, 0);
        hit.AddAction(() => value.Value = hits.Value, 4);
    }
}
