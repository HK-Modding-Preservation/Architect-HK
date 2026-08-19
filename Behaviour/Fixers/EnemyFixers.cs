using System.Linq;
using Architect.Content.Preloads;
using HutongGames.PlayMaker.Actions;
using UnityEngine;
using ApplyMusicCue = On.HutongGames.PlayMaker.Actions.ApplyMusicCue;
using Object = UnityEngine.Object;
using TransitionToAudioSnapshot = On.HutongGames.PlayMaker.Actions.TransitionToAudioSnapshot;

namespace Architect.Behaviour.Fixers;

public static class EnemyFixers
{
    // Gruz Mother
    private static GameObject _gruzSpawns;

    // Aspid Mother
    private static GameObject _aspidCage;

    // Husk Hive
    private static GameObject _huskHiveCage;

    // Carver Hatcher
    private static GameObject _carverCage;

    // Flukemarm
    private static GameObject _flukeCage;

    // Marmu
    private static ContactFilter2D _marmuFilter;

    public static void Init()
    {
        PreloadManager.RegisterPreload(new BasicPreload("Crossroads_04", "_Enemies/Fly Spawn",
            o =>
            {
                _gruzSpawns = o;

                foreach (var hm in _gruzSpawns.GetComponentsInChildren<HealthManager>(true))
                    hm.battleScene = null;
            }));

        PreloadManager.RegisterPreload(new BasicPreload("Crossroads_19", "Hatcher Cage (1)",
            o => _aspidCage = o));

        PreloadManager.RegisterPreload(new BasicPreload("Hive_01", "Hatcher Cage (1)",
            o => _huskHiveCage = o));

        PreloadManager.RegisterPreload(new BasicPreload("Deepnest_26b", "Centipede Cage",
            o => _carverCage = o));

        PreloadManager.RegisterPreload(new BasicPreload("GG_Flukemarm", "Hatcher Cage (2)",
            o => _flukeCage = o));

        ApplyMusicCue.OnEnter += (orig, self) =>
        {
            if (self.fsmComponent && self.fsmComponent.GetComponent<BlockMusic>())
            {
                self.Finish();
                return;
            }

            orig(self);
        };

        TransitionToAudioSnapshot.OnEnter += (orig, self) =>
        {
            if (self.fsmComponent && self.fsmComponent.GetComponent<BlockMusic>())
            {
                self.Finish();
                return;
            }

            orig(self);
        };

        On.CorpseHatcher.Smash += (orig, self) =>
        {
            var hatcher = self.GetComponent<HatcherCorpse>();
            if (hatcher)
            {
                var toEnable = DisableOthers(hatcher);
                orig(self);
                EnableOthers(toEnable);
            }
            else
            {
                orig(self);
            }
        };

        On.CorpseZomHive.LandEffects += (orig, self) =>
        {
            var hatcher = self.GetComponent<HatcherCorpse>();
            if (hatcher)
            {
                var toEnable = DisableOthers(hatcher);
                orig(self);
                EnableOthers(toEnable);
            }
            else
            {
                orig(self);
            }
        };

        var marmuMask = 0;
        for (var i = 0; i < 32; i++) {
            if (!Physics.GetIgnoreLayerCollision(11, i) && 
                LayerMask.LayerToName(i) != "Hero Box") {
                marmuMask |= 1 << i;
            }
        }

        _marmuFilter = new ContactFilter2D
        {
            layerMask = marmuMask,
            useTriggers = false
        };
    }

    public static void RotateShardmite(GameObject obj, float rot)
    {
        if (rot is >= 180 and <= 270)
        {
            rot -= 180;
            obj.transform.SetScaleX(-obj.transform.GetScaleX());
            obj.transform.SetScaleY(-obj.transform.GetScaleY());
        }

        obj.transform.SetRotation2D(rot);
    }

    public static void FixGruzMother(GameObject obj)
    {
        var child = obj.transform.GetChild(2).gameObject;
        child.gameObject.RemoveComponent<PolygonCollider2D>();
        var box = child.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size *= 100;

        obj.AddComponent<GruzMother>();
        BlockMusicOn(obj);
    }

    public static void BlockMusicOn(GameObject obj)
    {
        obj.AddComponent<BlockMusic>();
        var ede = obj.GetComponent<EnemyDeathEffects>();
        if (ede)
        {
            ede.PreInstantiate();
            if (ede.corpse) ede.corpse.AddComponent<BlockMusic>();
        }
    }

    public class BlockMusic : MonoBehaviour;

    public class GruzMother : Wakeable
    {
        public bool spawnGruzzers = true;

        public override void Wake()
        {
            var fsm = gameObject.LocateMyFSM("Big Fly Control");
            fsm.SendEvent("TAKE DAMAGE");
            fsm.GetState("Sleep").AddAction(() => fsm.SendEvent("TAKE DAMAGE"));
        }

        public void Start()
        {
            var ede = GetComponent<EnemyDeathEffects>();
            ede.PreInstantiate();
            var corpseFsm = ede.corpse.LocateMyFSM("corpse");

            var spawn = spawnGruzzers;
            var flyName = $"{name} Spawns";
            corpseFsm.GetState("Blow").AddAction(() =>
            {
                var bursterFsm = corpseFsm.FsmVariables.FindFsmGameObject("Burster").Value.LocateMyFSM("burster");
                bursterFsm.GetState("Geo").DisableAction(0);

                if (spawn)
                {
                    var flySpawn = Instantiate(_gruzSpawns);
                    flySpawn.name = flyName;
                    flySpawn.SetActive(true);
                    ((FindGameObject)bursterFsm.GetState("Initiate").actions[4]).objectName = flySpawn.name;
                }
                else
                {
                    var check = (GGCheckIfBossScene)bursterFsm.GetState("Stop").actions[1];
                    check.regularSceneEvent = check.bossSceneEvent;
                }
            });
        }
    }

    public abstract class Wakeable : MonoBehaviour
    {
        public abstract void Wake();
    }

    public class MossKnight : Wakeable
    {
        public override void Wake()
        {
            var fsm = gameObject.LocateMyFSM("Moss Knight Control");
            fsm.FsmVariables.FindFsmBool("Dormant").Value = false;
            fsm.SendEvent("WAKE");
        }
    }

    public class Mistake : Wakeable
    {
        public override void Wake()
        {
            var fsm = gameObject.LocateMyFSM("Blob");
            fsm.FsmVariables.FindFsmBool("Spawns").value = false;
            ((WaitRandom)fsm.GetState("Spawn Pause").actions[0]).timeMax = 0;
            fsm.SendEvent("SPAWN");
        }
    }

    public class Folly : Wakeable
    {
        public override void Wake()
        {
            var fsm = gameObject.LocateMyFSM("Control");
            fsm.FsmVariables.FindFsmBool("Spawns").value = false;
            ((WaitRandom)fsm.GetState("Spawn Pause").actions[0]).timeMax = 0;
            fsm.SendEvent("SPAWN");
        }
    }

    public class InfectedBalloon : Wakeable
    {
        public override void Wake()
        {
            var fsm = gameObject.LocateMyFSM("Control");
            fsm.GetState("Initiate").AddAction(() => fsm.SetState("Chase - In Sight"));
            
            ((WaitRandom)fsm.GetState("Spawn Pause").actions[0]).timeMax = 0;
            fsm.SendEvent("SPAWN");
        }
    }

    public static void FixElderBaldur(GameObject obj)
    {
        var fsm = obj.LocateMyFSM("Blocker Control");
        fsm.GetState("Idle").DisableAction(2);
        fsm.GetState("Close").DisableAction(1);
    }

    public static void FlipElderBaldur(GameObject obj, bool flip)
    {
        if (!flip) return;
        obj.transform.SetScaleX(-obj.transform.GetScaleX());
        obj.LocateMyFSM("Blocker Control").FsmVariables.FindFsmBool("Facing Right").Value = false;
    }

    public static void RotateMawlurk(GameObject obj, float rot)
    {
        obj.transform.SetRotation2D(rot);

        var fsm = obj.LocateMyFSM("Mawlek Turret");

        var spawnPos = fsm.FsmVariables.FindFsmVector3("Spawn Pos");
        spawnPos.Value = Quaternion.Euler(0, 0, rot) * spawnPos.Value;

        var lerpRot = rot / 180;
        if (lerpRot > 1) lerpRot = 1 - lerpRot;
        fsm.FsmVariables.FindFsmFloat("Shot Speed").Value = Mathf.Lerp(25, 10, lerpRot);

        fsm.FsmVariables.FindFsmFloat("Angle Max L").Value = rot + 110 + Mathf.Lerp(0, 80, lerpRot);
        fsm.FsmVariables.FindFsmFloat("Angle Max R").Value = rot + 90;
        fsm.FsmVariables.FindFsmFloat("Angle Min L").Value = rot + 90;
        fsm.FsmVariables.FindFsmFloat("Angle Min R").Value = rot + 70 - Mathf.Lerp(0, 80, lerpRot);
    }

    public class ShrumalOgre : Wakeable
    {
        public override void Wake()
        {
            var fsm = gameObject.LocateMyFSM("Shroom Brawler");
            fsm.SendEvent("WAKE");
            fsm.GetState("Sleep").AddAction(() => fsm.SendEvent("WAKE"), 0);
        }
    }

    public static void FlipHopper(GameObject obj, bool flip)
    {
        if (!flip)
        {
            obj.transform.SetScaleX(-obj.transform.GetScaleX());
            obj.LocateMyFSM("Hopper").FsmVariables.FindFsmBool("Moving Right").Value = true;
        }
    }

    public static void FlipGreatHopper(GameObject obj, bool flip)
    {
        if (flip)
        {
            obj.transform.SetScaleX(-obj.transform.GetScaleX());
            obj.LocateMyFSM("Hopper").FsmVariables.FindFsmBool("Moving Right").Value = true;
        }
    }

    public static void FixMenderbug(GameObject obj)
    {
        var fsm = obj.LocateMyFSM("Mender Bug Ctrl");
        fsm.GetState("Killed").DisableAction(0);

        var bypass = () => fsm.SendEvent("FINISHED");
        fsm.GetState("Dead?").AddAction(bypass, 0);
        fsm.GetState("Sign Broken?").AddAction(bypass, 0);
        fsm.GetState("Chance").AddAction(bypass, 0);
    }

    public static void FixAspidMother(GameObject obj)
    {
        var cage = Object.Instantiate(_aspidCage);
        cage.SetActive(true);
        cage.name = obj.name + " Cage";

        ((FindGameObject)obj.LocateMyFSM("Hatcher").GetState("Initiate").actions[2]).objectName = cage.name;

        obj.AddComponent<Hatcher>().cage = cage;

        var ede = obj.GetComponent<EnemyDeathEffects>();
        ede.PreInstantiate();
        if (ede.corpse) ede.corpse.AddComponent<HatcherCorpse>().cage = cage;
    }

    public static void FixHuskHive(GameObject obj)
    {
        var cage = Object.Instantiate(_huskHiveCage);
        cage.SetActive(true);
        cage.name = obj.name + " Cage";

        ((FindGameObject)obj.LocateMyFSM("Hive Zombie").GetState("Init").actions[1]).objectName = cage.name;

        obj.AddComponent<Hatcher>().cage = cage;

        var ede = obj.GetComponent<EnemyDeathEffects>();
        ede.PreInstantiate();
        if (ede.corpse) ede.corpse.AddComponent<HatcherCorpse>().cage = cage;
    }

    public class Hatcher : MonoBehaviour
    {
        public GameObject cage;
    }

    public static void ScaleHatcher(GameObject obj, float scale)
    {
        obj.transform.localScale *= scale;
        obj.GetComponent<Hatcher>().cage.transform.localScale *= scale;
    }

    private static void EnableOthers(GameObject[] toEnable)
    {
        foreach (var obj in toEnable) obj.tag = "Extra Tag";
    }

    private static GameObject[] DisableOthers(HatcherCorpse hatcherCorpse)
    {
        var toEnable = GameObject.FindGameObjectsWithTag("Extra Tag")
            .Where(o => o != hatcherCorpse.cage && o.activeInHierarchy).ToArray();

        foreach (var obj in toEnable) obj.tag = "Untagged";

        return toEnable;
    }

    private class HatcherCorpse : MonoBehaviour
    {
        public GameObject cage;
    }

    public static void FixCarverHatcher(GameObject obj)
    {
        var cage = Object.Instantiate(_carverCage);
        cage.SetActive(true);
        cage.name = obj.name + " Cage";

        ((FindGameObject)obj.LocateMyFSM("Centipede Hatcher").GetState("Init").actions[1]).objectName = cage.name;

        obj.AddComponent<Hatcher>().cage = cage;
    }

    public static void FixFlukemarm(GameObject obj)
    {
        var fsm = obj.LocateMyFSM("Fluke Mother");
        fsm.GetState("GG?").AddAction(() => fsm.SendEvent("FINISHED"), 0);

        var cage = Object.Instantiate(_flukeCage);
        cage.SetActive(true);
        cage.name = obj.name + " Cage";

        ((FindGameObject)fsm.GetState("Init").actions[2]).objectName = cage.name;

        obj.AddComponent<Flukemarm>().fsm = fsm;
        obj.AddComponent<Hatcher>().cage = cage;
    }

    public class Flukemarm : Wakeable
    {
        public PlayMakerFSM fsm;

        public override void Wake()
        {
            fsm.SendEvent("TOOK DAMAGE");
            fsm.GetState("Idle").AddAction(() => fsm.SendEvent("TOOK DAMAGE"));
            fsm.GetState("Play Idle").AddAction(() => fsm.SendEvent("TOOK DAMAGE"));
        }
    }
    
    public static void FixVoltTwister(GameObject obj)
    {
        FixTwister(obj, "Electric Mage");
    }
    
    public static void FixSoulTwister(GameObject obj)
    {
        FixTwister(obj, "Mage");
    }
    
    private static void FixTwister(GameObject obj, string fsmName)
    {
        var fsm = obj.LocateMyFSM(fsmName);

        var teleplane = new GameObject(obj.name + " Teleplane")
        {
            tag = "Teleplane",
            transform =
            {
                position = obj.transform.position
            }
        };
        var collider = teleplane.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(10, 10);
        collider.isTrigger = true;

        var select = fsm.GetState("Select Target");
        
        select.DisableAction(1);
        select.AddAction(new FindGameObject
        {
            withTag = "Teleplane",
            objectName = teleplane.name,
            store = fsm.FsmVariables.GetFsmGameObject("Teleplane")
        }, 2);

        obj.AddComponent<Teleplane>().collider = collider;
    }
    
    public class Teleplane : MonoBehaviour
    {
        public BoxCollider2D collider;

        private void Start()
        {
            transform.position = collider.gameObject.transform.position;
        }
    }

    public static void FixZoteling(GameObject obj) => FixGenericZoteling(obj);

    private static PlayMakerFSM FixGenericZoteling(GameObject obj)
    {
        var fsm = obj.LocateMyFSM("Control");
        var antic = fsm.GetState("Spawn Antic");
        for (var i = 1; i <= 5; i++) antic.DisableAction(i);
        fsm.GetState("Dormant").AddAction(() => fsm.SendEvent("SPAWN"));
        obj.GetComponent<HealthManager>().hasSpecialDeath = false;

        return fsm;
    }

    public static void FixHeavyZoteling(GameObject obj)
    {
        var fsm = FixGenericZoteling(obj);
        fsm.GetState("Land Waves").AddAction(() =>
        {
            fsm.FsmVariables.FindFsmFloat("Shockwave Y").Value = fsm.transform.position.y - 2.3516f;
        }, 2);
    }

    public static void FixLankyZoteling(GameObject obj)
    {
        obj.RemoveComponent<ConstrainPosition>();
        var fsm = FixGenericZoteling(obj);
        fsm.GetState("Grav").DisableAction(1);
    }

    public static void FixWingedZoteling(GameObject obj) => FixBallZoteling(obj, "BUZZER");

    public static void FixHopZoteling(GameObject obj) => FixBallZoteling(obj, "HOPPER");

    private static void FixBallZoteling(GameObject obj, string type)
    {
        var fsm = obj.LocateMyFSM("Control");
        
        var ball = fsm.GetState("Ball");
        ball.DisableAction(2);
        var random = (WaitRandom)ball.actions[6];
        random.timeMin = 0.0001f;
        random.timeMax = 0.001f;

        fsm.GetState("Dormant").AddAction(() => fsm.SendEvent("SPAWN"));
        obj.GetComponent<HealthManager>().hasSpecialDeath = false;

        fsm.GetState("Choice").AddAction(() => fsm.SendEvent(type), 3);
    }
    
    public static void FixVolatileZoteling(GameObject obj)
    {
        var fsm = obj.LocateMyFSM("Control");
        fsm.GetState("Dormant").AddAction(() => fsm.SendEvent("BALLOON SPAWN"));
        obj.GetComponent<HealthManager>().hasSpecialDeath = false;

        fsm.GetState("Set Pos").DisableAction(6);

        fsm.GetState("Reset").transitions = [];
    }
    
    public static void FixFlukeZoteling(GameObject obj)
    {
        var fsm = obj.LocateMyFSM("Control");
        fsm.GetState("Dormant").AddAction(() => fsm.SendEvent("GO"));
        obj.GetComponent<HealthManager>().hasSpecialDeath = false;

        fsm.GetState("Pos").DisableAction(3);
        
        fsm.GetState("Climb").DisableAction(3);
    }

    public static void FixVengeflyKing(GameObject obj)
    {
        BlockMusicOn(obj);
        var fsm = obj.LocateMyFSM("Big Buzzer");

        fsm.GetState("Init").DisableAction(3);

        var pos = obj.transform.position;

        fsm.FsmVariables.FindFsmFloat("Swoop Height").Value = pos.y - 3.28f;

        var summon = fsm.GetState("Summon");

        var s1 = (CreateObject)summon.actions[1];
        var s2 = (CreateObject)summon.actions[3];
        
        s2.storeObject = s1.storeObject;

        var bi = fsm.FsmVariables.FindFsmGameObject("Buzzer Instance");
        
        summon.AddAction(() =>
        {
            var bobj = bi.Value;
            var mpos = obj.transform.position;
            if (bobj)
            {
                bobj.transform.position = new Vector3(mpos.x + 20.3926f *
                    (obj.transform.GetScaleX() > 0
                        ? -1
                        : 1), mpos.y + 6.9023f, 17);
            }
        }, 4);

        summon.AddAction(() =>
        {
            var bobj = bi.Value;
            var mpos = obj.transform.position;
            if (bobj)
            {
                bobj.transform.position = new Vector3(mpos.x + 1.3926f *
                    (obj.transform.GetScaleX() > 0
                        ? -1
                        : 1), mpos.y + 6.9023f, 17);
            }
        }, 2);
    }

    public static void FixSoulWarrior(GameObject obj)
    {
        BlockMusicOn(obj);
        var body = obj.GetComponent<Rigidbody2D>();
        var fsm = obj.LocateMyFSM("Mage Knight");

        fsm.GetState("Sleep").AddAction(() => fsm.SendEvent("WAKE"));

        var floorY = fsm.FsmVariables.FindFsmFloat("Floor Y");
        fsm.GetState("Side Tele Aim").AddAction(() => floorY.Value = fsm.gameObject.transform.position.y, 0);
        fsm.GetState("Up Tele Aim").AddAction(() => body.gravityScale = 0, 0);
        fsm.GetState("Idle").AddAction(() => body.gravityScale = 1, 0);
    }

    public static void FixTamerBeast(GameObject obj)
    {
        BlockMusicOn(obj);
        var fsm = obj.LocateMyFSM("Control");
        fsm.GetState("Dormant").AddAction(() => fsm.SendEvent("WAKE"));
    }

    public static void FixWatcherKnight(GameObject obj)
    {
        BlockMusicOn(obj);
        obj.transform.SetPositionZ(0.1102f);
        obj.AddComponent<WatcherKnight>();
        for (var i = 0; i < 4; i++) obj.transform.GetChild(i).gameObject.SetActive(false);
    }

    public class WatcherKnight : Wakeable
    {
        public override void Wake()
        {
            var fsm = gameObject.LocateMyFSM("Black Knight");
            fsm.SendEvent("WAKE");
            fsm.GetState("Rest").AddAction(() => fsm.SendEvent("WAKE"));
        }
    }

    public class MassiveMossCharger : Wakeable
    {
        public override void Wake()
        {
            var fsm = gameObject.LocateMyFSM("Mossy Control");
            fsm.SendEvent("WAKE");
            fsm.GetState("Sleep").AddAction(() => fsm.SendEvent("WAKE"));
        }
    }

    public static void FixMassiveMossCharger(GameObject obj)
    {
        obj.LocateMyFSM("Mossy Control").GetState("Init").DisableAction(28);
        obj.AddComponent<MassiveMossCharger>();
    }

    public static void ScaleMassiveMossCharger(GameObject obj, float scale)
    {
        var fsm = obj.LocateMyFSM("Mossy Control");
        ((SetScale)fsm.GetState("Emerge Left").actions[6]).x = scale;
        ((SetScale)fsm.GetState("Emerge Right").actions[6]).x = -scale;

        obj.transform.localScale *= scale;
    }

    private static PlayMakerFSM FixBrokenVessel(GameObject obj)
    {
        BlockMusicOn(obj);
        
        var fsm = obj.LocateMyFSM("IK Control");
        
        fsm.GetState("Roar").DisableAction(5);
        fsm.GetState("Waiting").AddAction(() => fsm.SendEvent("BATTLE START"), 3);
        
        fsm.FsmVariables.FindFsmFloat("Min Dstab Height").Value = -100;

        fsm.GetState("Aim Jump").AddAction(() =>
        {
            var newPos = obj.transform.position;
            fsm.FsmVariables.FindFsmFloat("Left X").Value = newPos.x - 10.235f;
            fsm.FsmVariables.FindFsmFloat("Right X").Value = newPos.x + 10.235f;
        }, 0);

        var aimJump2 = (RandomFloat)fsm.GetState("Aim Jump 2").actions[0];
        aimJump2.min = fsm.FsmVariables.FindFsmFloat("Left X");
        aimJump2.max = fsm.FsmVariables.FindFsmFloat("Right X");

        fsm.GetState("Set Height").DisableAction(0);

        var balloonFsm = obj.LocateMyFSM("Spawn Balloon");
        
        balloonFsm.GetState("Spawn").AddAction(() =>
        {
            var newPos = obj.transform.position;
            balloonFsm.FsmVariables.FindFsmFloat("X Min").Value = newPos.x - 9.55f;
            balloonFsm.FsmVariables.FindFsmFloat("X Max").Value = newPos.x + 9.55f;

            balloonFsm.FsmVariables.FindFsmFloat("Y Min").Value = newPos.y;
            balloonFsm.FsmVariables.FindFsmFloat("Y Max").Value = newPos.y + 5.26f;
        }, 0);
        
        return fsm;
    }

    public static void FixNormalBrokenVessel(GameObject obj)
    {
        var fsm = FixBrokenVessel(obj);
        fsm.GetState("Set Pos").DisableAction(1);
        fsm.GetState("Close Gates").DisableAction(0);
        fsm.GetState("Init").AddAction(() => fsm.SendEvent("ACTIVE"), 2);
    }

    public static void FixLostKin(GameObject obj)
    {
        var fsm = FixBrokenVessel(obj);
        var sx = fsm.GetState("Set X");
        sx.DisableAction(0);
        sx.DisableAction(2);
        fsm.GetState("Intro Fall").DisableAction(2);
        var cg = fsm.GetState("Close Gates");
        cg.DisableAction(3);
        cg.DisableAction(0);
    }

    public static void FixHatchling(GameObject obj)
    {
        var fsm = obj.LocateMyFSM("Control");
        fsm.GetState("Inert").AddAction(() => fsm.SendEvent("SPAWN"));
        var death = fsm.GetState("Death");
        for (var i = 1; i <= 5; i++) death.DisableAction(i);

        obj.GetComponent<HealthManager>().OnDeath += () => obj.SetActive(false);
    }
    
    public class Shade : MonoBehaviour
    {
        public int friendly;
        public int spirit;
        public int dive;
        public int wraiths;
        public int hp = -1;
        public bool countDead;
        
        private void Start()
        {
            var fsm = gameObject.LocateMyFSM("Shade Control");
            var killed = fsm.GetState("Killed");

            if (!countDead)
            {
                killed.AddAction(() =>
                {
                    var corpseFsm = fsm.FsmVariables.FindFsmGameObject("Corpse").value.LocateMyFSM("Shade Control");
                    corpseFsm.GetState("Give Geo").AddAction(() => corpseFsm.SendEvent("FINISHED"), 0);
                }, 3);
            }
            
            fsm.GetState("Special Type").AddAction(() => fsm.SendEvent("FINISHED"), 0);

            var init = fsm.GetState("Init");
            
            var ic = (IntCompare)fsm.GetState("Friendly?").actions[1];
            switch (friendly)
            {
                case 1:
                    ic.lessThan = ic.greaterThan = ic.equal;
                    break;
                case 2:
                    ic.equal = ic.lessThan;
                    break;
            }

            if (hp != -1)
            {
                ((SetHP)fsm.GetState("Friendly Idle").actions[1]).hp = hp;
                ((SetHP)init.actions[10]).hp = hp;
            }
            
            init.AddAction(() =>
            {
                if (spirit != 0) fsm.FsmVariables.FindFsmInt("Fireball Level").value = spirit - 1;
                if (dive != 0) fsm.FsmVariables.FindFsmInt("Quake Level").value = dive - 1;
                if (wraiths != 0) fsm.FsmVariables.FindFsmInt("Scream Level").value = wraiths - 1;
            }, 11);
        }
    }

    public class Gorb : MonoBehaviour
    {
        public bool posPlayer;

        public void Start()
        {
            gameObject.LocateMyFSM("Set Ghost PD Int").GetState("Set").DisableAction(0);
            
            var fsm = gameObject.LocateMyFSM("Movement");
            var p1 = fsm.FsmVariables.FindFsmVector3("P1");
            var p2 = fsm.FsmVariables.FindFsmVector3("P2");
            var p3 = fsm.FsmVariables.FindFsmVector3("P3");
            var p4 = fsm.FsmVariables.FindFsmVector3("P4");
            var p5 = fsm.FsmVariables.FindFsmVector3("P5");
            var p6 = fsm.FsmVariables.FindFsmVector3("P6");
            var p7 = fsm.FsmVariables.FindFsmVector3("P7");

            var heroTrans = HeroController.instance.transform;
            if (posPlayer)
            {
                fsm.GetState("Choose Target").AddAction(() =>
                {
                    // Adjusted by +- 5 so Gorb can't just spawn on the player
                    Reposition(heroTrans.position + new Vector3(Random.value > 0.5f ? 5 : -5, 0));
                }, 0);
            } else Reposition(transform.position);
            
            var hover = fsm.GetState("Hover");
            hover.DisableAction(4);
            hover.DisableAction(5);
            hover.DisableAction(6);
            
            fsm.GetState("Set Warp").AddAction(() =>
            {
                fsm.SendEvent(transform.GetPositionX() > heroTrans.GetPositionX() ? "WARP L" : "WARP R");
            }, 0);
            
            return;

            void Reposition(Vector3 pos)
            {
                pos.z = 0.006f;
                p1.value = pos;
                p2.value = pos + new Vector3(0, -5);
                p3.value = pos + new Vector3(-9.5f, -5);
                p4.value = pos + new Vector3(9.5f, -5);
                p5.value = pos + new Vector3(-9.5f, -3.3f);
                p6.value = pos + new Vector3(9.5f, -3.3f);
                p7.value = pos + new Vector3(0, -3.3f);
            }
        }
    }

    public static void FixZote(GameObject obj)
    {
        obj.LocateMyFSM("Constrain X").enabled = false;
        var fsm = obj.LocateMyFSM("Control");

        fsm.GetState("Dormant").AddAction(() => fsm.SendEvent("ZOTE APPEAR"), 0);
        fsm.GetState("Enter 2").AddAction(() => fsm.SendEvent("SHORT"), 0);
        fsm.GetState("Roar").AddAction(() => fsm.SendEvent("ZOTE TITLE END"), 0);
        fsm.GetState("Music").AddAction(() => fsm.SendEvent("FINISHED"), 0);
    }

    public static void FixMarmu(GameObject obj)
    {
        BlockMusicOn(obj);
        
        obj.LocateMyFSM("Broadcast Ghost Death").enabled = false;
        var fsm = obj.LocateMyFSM("Control");

        var xMax = fsm.FsmVariables.FindFsmFloat("Tele X Max");
        var xMin = fsm.FsmVariables.FindFsmFloat("Tele X Min");
            
        var yMax = fsm.FsmVariables.FindFsmFloat("Tele Y Max");
        var yMin = fsm.FsmVariables.FindFsmFloat("Tele Y Min");

        var warpPos = fsm.FsmVariables.FindFsmVector3("Warp Pos");

        var sp = fsm.GetState("Set Pos");
        var sp2 = fsm.GetState("Set Pos 2");
        
        fsm.GetState("Warp?").AddAction(() => fsm.SendEvent("FINISHED"), 0);
            
        sp.AddAction(AdjustBounds, 0);
        sp2.AddAction(AdjustBounds, 0);
        sp.AddAction(CheckValidTp);
        sp2.AddAction(CheckValidTp);

        var ede = obj.GetComponent<EnemyDeathEffects>();
        ede.PreInstantiate();
        var corpse = ede.corpse;
        if (corpse)
        {
            var end = corpse.LocateMyFSM("Control").GetState("End");
            end.DisableAction(2);
            end.transitions = [];
            end.AddAction(() => Object.Destroy(corpse), 0);
        }

        return;

        void AdjustBounds()
        {
            var heroTrans = HeroController.instance.transform;
            xMin.Value = heroTrans.GetPositionX() - 15;
            xMax.Value = heroTrans.GetPositionX() + 15;

            yMin.Value = heroTrans.GetPositionY() - 6;
            yMax.Value = heroTrans.GetPositionY() + 6;
        }

        void CheckValidTp()
        {
            var hits = new RaycastHit2D[1];
            
            Physics2D.Linecast(
                warpPos.Value,
                HeroController.instance.transform.position,
                _marmuFilter, hits);
            
            if (hits[0]) fsm.SendEvent("CANCEL");
        }
    }

    public abstract class Swooper : MonoBehaviour
    {
        public bool swoopIn;
    }

    public class Oblobble : Swooper
    {
        public int getAngry;
        public bool angerOthers;

        private void Awake()
        {
            BlockMusicOn(gameObject);
            
            var fsm = gameObject.LocateMyFSM("fat fly bounce");
            if (!swoopIn) fsm.GetState("Swoop In").AddAction(() => fsm.SendEvent("SUMMON"), 0);
            var f2 = fsm.GetState("Fly 2");
            f2.DisableAction(7);
            f2.DisableAction(8);
            
            if (!angerOthers) gameObject.LocateMyFSM("Rager").enabled = false;
            
            if (getAngry != 1)
            {
                var rageFsm = gameObject.LocateMyFSM("Set Rage");
                if (getAngry == 0) rageFsm.GetState("Idle").transitions = [];
                else fsm.GetState("Aim").AddAction(() => rageFsm.SendEvent("OBLOBBLE RAGE"), 0);
            }
        }
    }

    public static void FixBroodingMawlek(GameObject obj)
    {
        BlockMusicOn(obj);
        
        var fsm = obj.LocateMyFSM("Mawlek Control");

        fsm.GetState("Dormant").AddAction(() => fsm.SendEvent("GG BOSS"), 0);
        var roar = fsm.GetState("Wake Roar");
        roar.DisableAction(0);
        roar.DisableAction(1);
    }

    public static void FixTraitorLord(GameObject obj)
    {
        BlockMusicOn(obj);
        
        var fsm = obj.LocateMyFSM("Mantis");
        
        fsm.GetState("Cloth?").AddAction(() => fsm.SendEvent("FINISHED"), 0);
        fsm.GetState("Emerge Dust").AddAction(() => fsm.SendEvent("FINISHED"), 0);
        fsm.GetState("Fall").AddAction(() => fsm.SendEvent("LAND"), 0);
        fsm.GetState("Intro Land").DisableAction(0);
        var roar = fsm.GetState("Roar");
        roar.DisableAction(2);
        roar.DisableAction(3);
        
        fsm.GetState("DSlash").DisableAction(13);
        var land = fsm.GetState("Land");
        land.DisableAction(0);
        land.AddAction(() => obj.transform.position += new Vector3(0, 1.15f), 0);
        
        
        fsm.GetState("Check L").AddAction(() => fsm.SendEvent("CAN REPEAT"), 0);
        fsm.GetState("Check R").AddAction(() => fsm.SendEvent("CAN REPEAT"), 0);

        var throws = 0;
        var throwState = fsm.GetState("Throw?");
        throwState.DisableAction(0);
        throwState.AddAction(() =>
        {
            if (throws / 2f > Random.value)
            {
                throws = 0;
                fsm.SendEvent("FINISHED");
            }
            throws++;
        }, 0);
        
        fsm.GetState("Walk").AddAction(() => throws = 0, 0);
    }

    public static void FixThk(GameObject obj)
    {
        BlockMusicOn(obj);
        
        var fsm = obj.LocateMyFSM("Control");
        
        fsm.GetState("Long Roar End").DisableAction(2);
    }

    public static void FixHornetProtector(GameObject obj)
    {
        var fsm = obj.LocateMyFSM("Control");
        fsm.GetState("Inert").AddAction(() => fsm.SendEvent("WAKE"), 0);
    }

    public static void FixFk(GameObject obj)
    {
        var fsm = obj.LocateMyFSM("FalseyControl");

        fsm.FsmVariables.FindFsmFloat("Final Point X").value = obj.transform.GetPositionX();
        fsm.FsmVariables.FindFsmFloat("Rage Point X").value = obj.transform.GetPositionX();

        var rMin = fsm.FsmVariables.FindFsmFloat("Range Min");
        var rMax = fsm.FsmVariables.FindFsmFloat("Range Max");
        
        
        fsm.GetState("Dormant").AddAction(() => fsm.SendEvent("BATTLE START"), 1);
        
        fsm.GetState("Idle").AddAction(() =>
        {
            var heroPos = HeroController.instance.transform.position;
            rMin.value = heroPos.x - 13.5f;
            rMax.value = heroPos.x + 13.5f;
        }, 0);
    }

    public class Garpede : MonoBehaviour
    {
        public int segments = 4;
        private bool _setup;

        private void Start()
        {
            if (_setup) return;
            Setup();
        }

        public void Setup()
        {
            _setup = true;
            
            GameObject prefab = null;
            foreach (Transform child in transform)
                if (child.name.StartsWith("Big Centipede Seg"))
                {
                    child.gameObject.SetActive(false);
                    if (child.name == "Big Centipede Seg") prefab = child.gameObject;
                }

            if (!prefab) return;
                    
            var sections = new BigCentipedeSection[segments + 1];
            for (var i = 0; i < segments; i++)
            {
                var spawn = Instantiate(prefab, transform);
                spawn.transform.localPosition = new Vector3((i + 1) * -2.3f, 0, 0.001f * (i + 1));
                spawn.SetActive(true);
                sections[i] = spawn.GetComponent<BigCentipedeSection>();
            }

            var tail = transform.Find("Big Centipede Tail");
            tail.localPosition = new Vector3((segments + 1) * -2.3f + 0.56f, 0.09f, 0.001f *
                (segments + 1));
            sections[segments] = tail.GetComponent<BigCentipedeSection>();

            var bc = GetComponent<BigCentipede>();
            if (bc) bc.sections = sections;
        }
    }

    public class TieComponents : MonoBehaviour
    {
        private MeshRenderer _driver;
        private BoxCollider2D _driven;

        private void Start()
        {
            _driver = GetComponent<MeshRenderer>();
            _driven = GetComponent<BoxCollider2D>();
        }

        private void Update()
        {
            if (_driven.enabled != _driver.enabled) _driven.enabled = _driver.enabled;
        }
    }

    public static void FixGarpede(GameObject obj)
    {
        var bc2d = obj.GetComponent<BoxCollider2D>();
        bc2d.offset = new Vector2(-0.35f, 0);
        bc2d.size = new Vector2(1.8f, 1.6274f);
        obj.AddComponent<TieComponents>();

        var bct = obj.transform.Find("Big Centipede Tail").gameObject;
        bct.AddComponent<DamageHero>();
        var tailBc = bct.AddComponent<BoxCollider2D>();
        tailBc.size = new Vector2(1.75f, 1.6274f);
        tailBc.offset = new Vector2(0, -0.09f);
        bct.AddComponent<TieComponents>();

        var prefab = obj.transform.Find("Big Centipede Seg").gameObject;
        prefab.AddComponent<DamageHero>();
        prefab.AddComponent<BoxCollider2D>().size = new Vector2(2.3f, 1.6274f);
        prefab.AddComponent<TieComponents>();
     
        obj.AddComponent<Garpede>();
    }

    public static void PostFixGarpede(GameObject obj)
    {
        var bc = obj.GetComponent<BigCentipede>();
        var entry = new GameObject("Entry")
        {
            transform =
            {
                parent = obj.transform,
                localPosition = Vector3.zero
            }
        };
        bc.entry = entry.transform;
        var exit = new GameObject("Exit")
        {
            transform =
            {
                parent = obj.transform,
                localPosition = Vector3.zero
            }
        };
        bc.exit = exit.transform;
    }

    public static void FixFlamebearer(GameObject obj, int level)
    {
        obj.LocateMyFSM("hp_scaler").enabled = false;
        var fsm = obj.LocateMyFSM("Control");
        fsm.fsmTemplate = null;
        
        fsm.GetState("Init").AddAction(() =>
        {
            fsm.FsmVariables.FindFsmInt("Grimmchild Level").Value = level;
            fsm.SendEvent("START");
        });
        
        fsm.GetState("Fanfare Level").AddAction(() => fsm.SendEvent("FINISHED"), 0);
        var destroy = fsm.GetState("Destroy");
        destroy.DisableAction(0);
        destroy.AddAction(() =>
        {
            obj.GetComponent<HealthManager>().SetIsDead(true);
            var pbi = obj.GetComponent<PersistentBoolItem>();
            if (pbi) pbi.SaveState();
        }, 0);
    }
}