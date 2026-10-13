using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Architect.Editor;
using Architect.Events;
using Architect.Multiplayer.Hkmp.Data;
using Architect.Objects.Placeable;
using Architect.Placements;
using Architect.Storage;
using Hkmp.Api.Client;
using Vector3 = UnityEngine.Vector3;
using Vector2 = Hkmp.Math.Vector2;

namespace Architect.Multiplayer.Hkmp;

public class ArchitectClientAddon : ClientAddon
{
    public IClientApi API;

    protected override string Name => "Architect";
    protected override string Version => Assembly.GetExecutingAssembly().GetName().Version.ToString();
    public override bool NeedsNetwork => true;
    
    private PropertyInfo _updateManagerProperty;
    private object _updateManager;
    private MethodInfo _setEnterSceneData;
    private MethodInfo _getCurrentAnimationClip;
    
    public override void Initialize(IClientApi clientApi)
    {
        API = clientApi;
        
        clientApi.ClientManager.ConnectEvent += () =>
        {
            CoopManager.Instance.LockType = EditorLockType.Unknown;
            clientApi.NetClient.GetNetworkSender<PacketId>(this)
                .SendSingleData(PacketId.LockEditor, new LockEditorPacketData());
        };
        
        clientApi.ClientManager.DisconnectEvent += () =>
        {
            CoopManager.Instance.LockType = EditorLockType.Unlocked;
        };

        var netReceiver = clientApi.NetClient.GetNetworkReceiver<PacketId>(this,
            id => id.GetData());
        
        netReceiver.RegisterPacketHandler<ClearPacketData>(PacketId.Clear, HandleClear);
        netReceiver.RegisterPacketHandler<PlacePacketData>(PacketId.Place, HandlePlace);
        netReceiver.RegisterPacketHandler<ErasePacketData>(PacketId.Erase, HandleErase);
        netReceiver.RegisterPacketHandler<LockPacketData>(PacketId.Lock, HandleLock);
        netReceiver.RegisterPacketHandler<EventPacketData>(PacketId.Event, HandleEvent);
        netReceiver.RegisterPacketHandler<MovePacketData>(PacketId.Move, HandleMove);
        netReceiver.RegisterPacketHandler<TilePacketData>(PacketId.Tiles, HandleTiles);
        netReceiver.RegisterPacketHandler<LockEditorPacketData>(PacketId.LockEditor, HandleLockEditor);
        
        _updateManagerProperty = API.NetClient.GetType().GetProperty("UpdateManager");
        _setEnterSceneData = _updateManagerProperty!.PropertyType.GetMethod("SetEnterSceneData");
        
        var animationManagerField = API.ClientManager.GetType().GetField("_animationManager", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        var animationManager = animationManagerField!.GetValue(API.ClientManager);
        _getCurrentAnimationClip = animationManager.GetType().GetMethod("GetCurrentAnimationClip",
            BindingFlags.Public | BindingFlags.Static);
    }

    private static void HandleClear(ClearPacketData packet)
    {
        ArchitectPlugin.Instance.Log("Receiving Reset Packet");
        if (packet.SceneName == GameManager.instance.sceneName) ActionManager.ReceiveAction(new ResetRoom());
        else ResetRoom.Execute(packet.SceneName);
    }

    private static readonly List<PlacePacketData> PlaceInfo = [];
    private static string _lastPlaceGuid;

    private static void HandlePlace(PlacePacketData packet)
    {
        ArchitectPlugin.Instance.Log("Receiving Place Packet");

        if (packet.Guid != _lastPlaceGuid)
        {
            _lastPlaceGuid = packet.Guid;
            PlaceInfo.Clear();
        }
        
        PlaceInfo.Add(packet);
        if (PlaceInfo.Count != packet.Length) return;

        PlaceInfo.Sort((o1, o2) => o1.Index.CompareTo(o2.Index));
        var json = ZipUtils.Unzip(PlaceInfo.Select(o => o.SerializedObjects)
            .Aggregate((a, b) => a.Concat(b).ToArray()));

        if (packet.IsFullScene)
        {
            var levelData = StorageManager.DeserializeLevel(json);
            ArchitectPlugin.Instance.StartCoroutine(SaveData(levelData, packet));
        }
        else
        {
            var action = new PlaceObjects(StorageManager.DeserializePlacements(json));
            if (packet.SceneName == GameManager.instance.sceneName) ActionManager.ReceiveAction(action);
            else action.Execute(packet.SceneName);
        }
    }

    private static IEnumerator SaveData(LevelData levelData, PlacePacketData packet)
    {
        if (packet.IsScriptOnly)
        {
            var scene = StorageManager.LoadScene(packet.SceneName);
            scene.ScriptBlocks.Clear();
            scene.Comments.Clear();
            scene.ScriptBlocks.AddRange(levelData.ScriptBlocks);
            scene.Comments.AddRange(levelData.Comments);
            StorageManager.SaveScene(packet.SceneName, scene);
        }
        else
        {
            foreach (var placement in levelData.Placements
                         .Where(p => p.GetPlacementType() is PreloadObject { Loaded: false })) 
                yield return placement.GetPlacementType().EnsureLoaded();
            StorageManager.WipeScheduledEdits(packet.SceneName);
            StorageManager.SaveScene(packet.SceneName, levelData);
        }

        if (packet.SceneName == GameManager.instance.sceneName || packet.SceneName == StorageManager.GLOBAL)
        {
            PlacementManager.InvalidateScene();
            PlacementManager.VerifyLevelData();
            EditManager.ReloadRequired = true;
        }
    }

    private static void HandleErase(ErasePacketData packet)
    {
        ArchitectPlugin.Instance.Log("Receiving Erase Packet");
        if (packet.SceneName == GameManager.instance.sceneName)
        {
            ActionManager.ReceiveAction(new EraseObject(PlacementManager.GetLevelData().Placements
                .Where(p => packet.Removals.Remove(p.GetId())).ToList()));
        }
        else EraseObject.Execute(packet.SceneName, packet.Removals);
    }

    private static void HandleLock(LockPacketData packet)
    {
        ArchitectPlugin.Instance.Log("Receiving Lock Packet");
        if (packet.SceneName == GameManager.instance.sceneName)
        {
            var obj = PlacementManager.GetLevelData().Placements
                .FirstOrDefault(p => p.GetId() == packet.Toggle);
            if (obj != null) ActionManager.ReceiveAction(new ToggleLock(obj));
        }
        else ToggleLock.Execute(packet.SceneName, packet.Toggle);
    }

    private static void HandleTiles(TilePacketData packet)
    {
        ArchitectPlugin.Instance.Log("Receiving Tile Packet");

        var toggle = new ToggleTile(packet.Tiles, packet.Empty);
        
        if (packet.SceneName == GameManager.instance.sceneName) toggle.Execute();
        else toggle.Execute(packet.SceneName);
    }

    private static void HandleLockEditor(LockEditorPacketData packet)
    {
        ArchitectPlugin.Instance.Log("Receiving Editor Lock Packet");
        CoopManager.Instance.LockType = packet.LockType;
        if (packet.LockType == EditorLockType.Locked) EditManager.ToggleEditor(false);
    }

    private static void HandleMove(MovePacketData packet)
    {
        ArchitectPlugin.Instance.Log("Receiving Move Packet");
        if (packet.SceneName == GameManager.instance.sceneName)
        {
            List<(ObjectPlacement, Vector3, Vector3)> movements = [];

            foreach (var (id, newPos) in packet.Movements)
            {
                var o = PlacementManager.GetLevelData().Placements
                    .FirstOrDefault(o => o.GetId() == id);
                if (o == null) continue;
                movements.Add((o, newPos, o.GetPos()));
            }
            ActionManager.ReceiveAction(new MoveObjects(movements));
        }
        else MoveObjects.Execute(packet.SceneName, packet.Movements);
    }

    private static void HandleEvent(EventPacketData packet)
    {
        ArchitectPlugin.Instance.Log("Receiving Event Packet");
        EventManager.BroadcastMp(packet.Event);
    }

    public void RefreshRoom()
    {
        ArchitectPlugin.Instance.Log("Refreshing Room");
        var ht = HeroController.instance.transform;
        
        var id = _getCurrentAnimationClip.Invoke(null, []);
        _updateManager ??= _updateManagerProperty!.GetGetMethod().Invoke(API.NetClient, null);
        _setEnterSceneData
            .Invoke(_updateManager, [
                GameManager.instance.sceneName, 
                new Vector2(ht.position.x, ht.position.y),
                ht.GetScaleX() > 0,
                Convert.ToUInt16(id)
            ]);
    }
}