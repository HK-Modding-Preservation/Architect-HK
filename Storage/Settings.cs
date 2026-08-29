using System.Collections.Generic;
using InControl;
using Newtonsoft.Json;
using Satchel.BetterMenus;
using UnityEngine;

namespace Architect.Storage;

public static class Settings
{
    public static Keybind ToggleEditor;
    public static Keybind Rotate;
    public static Keybind InvertRotate;
    public static Keybind UnsafeRotation;
    public static Keybind Flip;
    public static Keybind ScaleUp;
    public static Keybind ScaleDown;
    public static Keybind NextHotbar;
    public static Keybind SaveObject;
    public static Keybind LockAxis;
    public static Keybind Undo;
    public static Keybind Redo;
    public static Keybind MultiSelect;
    public static Keybind Copy;
    public static Keybind Paste;
    public static Keybind Preview;
    public static Keybind Overwrite;
    public static Keybind GrabId;
    public static Keybind StartLocked;
    public static Keybind StartScripted;
    
    public static Keybind Blank;
    public static Keybind Cursor;
    public static Keybind Drag;
    public static Keybind Eraser;
    public static Keybind Pick;
    public static Keybind Reset;
    public static Keybind Lock;
    public static Keybind TileChanger;

    public static Keybind CreateNewComment;

    public static readonly List<Element> MiscConfig = [];

    public static ConfigEntry<bool> UseMapiPreloads;
    public static ConfigEntry<bool> TestMode;
    public static ConfigEntry<bool> HitboxesInEditor;
    public static ConfigEntry<bool> ShowRespawnPoint;
    public static ConfigEntry<bool> BlockInventoryInEditMode;
    
    public static ConfigEntry<int> SaveSlot; 
    
    public static ConfigEntry<Color> EditorBackgroundColour;

    public static ConfigEntry<Color> ScriptEditorSelectionColor;
    public static ConfigEntry<Color> ScriptEditorSelectionOutlineColor;
    public static ConfigEntry<Color> ScriptEditorBlockSelectedLerpColor;

    private static bool _init;
    
    public static void Init()
    {
        if (_init) return;
        _init = true;
        
        ToggleEditor = new Keybind(Bind(
            "Keybinds",
            "EditToggle",
            Key.E,
            "Toggles Edit Mode"
        ));
        
        Rotate = new Keybind(Bind(
            "Keybinds",
            "Rotate",
            Key.R,
            "Rotates the object on the cursor"
        ));
        
        InvertRotate = new Keybind(Bind(
            "Keybinds",
            "InvertRotate",
            Key.LeftShift,
            "Inverts the direction of the Rotate keybind"
        ));
        
        UnsafeRotation = new Keybind(Bind(
            "Keybinds",
            "UnsafeRotation",
            Key.LeftAlt,
            "Allows rotating objects at any angle"
        ));
        
        Flip = new Keybind(Bind(
            "Keybinds",
            "Flip",
            Key.F,
            "Flips the object on the cursor"
        ));
        
        ScaleUp = new Keybind(Bind(
            "Keybinds",
            "ScaleUp",
            Key.Equals,
            "Increases the scale of the object on the cursor"
        ));
        
        ScaleDown = new Keybind(Bind(
            "Keybinds",
            "ScaleDown",
            Key.Minus,
            "Decreases the scale of the object on the cursor"
        ));
        
        NextHotbar = new Keybind(Bind(
            "Keybinds",
            "NextHotbar",
            Key.Tab,
            "Moves to the next saved hotbar"
        ));
        
        SaveObject = new Keybind(Bind(
            "Keybinds",
            "SaveObject",
            Key.Return,
            "Saves the object on the cursor as a saved object"
        ));
        
        LockAxis = new Keybind(Bind(
            "Keybinds",
            "LockAxis",
            Key.RightShift,
            "Locks the current X or Y axis to the axis of the last placed object"
        ));
        
        Undo = new Keybind(Bind(
            "Keybinds",
            "Undo",
            Key.Z,
            "Undoes the last action"
        ));
        
        Redo = new Keybind(Bind(
            "Keybinds",
            "Redo",
            Key.Y,
            "Redoes the last action"
        ));
        
        MultiSelect = new Keybind(Bind(
            "Keybinds",
            "MultiSelect",
            Key.LeftControl,
            "Allows selecting multiple objects with the Drag tool"
        ));
        
        Copy = new Keybind(Bind(
            "Keybinds",
            "Copy",
            Key.C,
            "Copies the current selection of objects"
        ));
        
        Paste = new Keybind(Bind(
            "Keybinds",
            "Paste",
            Key.V,
            "Pastes the objects on the clipboard"
        ));
        
        Preview = new Keybind(Bind(
            "Keybinds",
            "Preview",
            Key.P,
            "Preview objects affected by the Object Anchor"
        ));
        
        Overwrite = new Keybind(Bind(
            "Keybinds",
            "Overwrite",
            Key.O,
            "Overwrites a clicked object with the one on your cursor"
        ));
        
        GrabId = new Keybind(Bind(
            "Keybinds",
            "GrabId",
            Key.I,
            "Sets the ID option of the object on the cursor to the selected object's ID"
        ));
        
        StartLocked = new Keybind(Bind(
            "Keybinds",
            "StartLocked",
            Key.None,
            "Makes the placed object be locked instantly upon placing it"
        ));
        
        StartScripted = new Keybind(Bind(
            "Keybinds",
            "StartScripted",
            Key.None,
            "Makes the placed object be added to the script instantly upon placing it"
        ));
        
        Blank = new Keybind(Bind(
            "ToolHotkeys",
            "Blank",
            Key.None,
            "Clears your current selected item"
        ));
        
        Cursor = new Keybind(Bind(
            "ToolHotkeys",
            "Cursor",
            Key.None,
            "Sets your current selected item to the Cursor tool"
        ));
        
        Drag = new Keybind(Bind(
            "ToolHotkeys",
            "Drag",
            Key.None,
            "Sets your current selected item to the Drag tool"
        ));
        
        Eraser = new Keybind(Bind(
            "ToolHotkeys",
            "Eraser",
            Key.None,
            "Sets your current selected item to the Eraser tool"
        ));
        
        Pick = new Keybind(Bind(
            "ToolHotkeys",
            "Pick",
            Key.None,
            "Sets your current selected item to the Pick tool"
        ));
        
        Reset = new Keybind(Bind(
            "ToolHotkeys",
            "Reset",
            Key.None,
            "Sets your current selected item to the Reset tool"
        ));
        
        TileChanger = new Keybind(Bind(
            "ToolHotkeys",
            "TilemapChanger",
            Key.None,
            "Sets your current selected item to the Tilemap Changer tool"
        ));
        
        Lock = new Keybind(Bind(
            "ToolHotkeys",
            "Lock",
            Key.None,
            "Locks an object in place so it cannot be edited until unlocked"
        ));

        CreateNewComment = new Keybind(Bind(
            "ToolHotkeys",
            "CreateNewComment",
            Key.G,
            "Creates a new comment block from the selection in the script editor"
        ));
        
        TestMode = Bind(
            "Options",
            "TestMode",
            false,
            "Stops the game from storing persistent data"
        );
        
        UseMapiPreloads = Bind(
            "Options",
            "UseMapiPreloads",
            false,
            "Whether to use standard modding API preloads (slower)"
        );
        
        HitboxesInEditor = Bind(
            "Options",
            "HitboxesInEditor",
            true,
            "Determines whether objects in edit mode should have hitboxes"
        );
        
        ShowRespawnPoint = Bind(
            "Options",
            "ShowRespawnPoint",
            false,
            "Adds an indicator showing your current hazard respawn point"
        );
        
        BlockInventoryInEditMode = Bind(
            "Options",
            "BlockInventoryInEditMode",
            true,
            "Prevents the inventory from being opened during edit mode"
        );
        
        SaveSlot = Bind(
            "Options",
            "DownloadSlot",
            4,
            "The save slot to download save files from the level sharer into"
        );

        EditorBackgroundColour = Bind(
            "Options",
            "EditorBackgroundColour",
            new Color(0.1f, 0.1f, 0.1f),
            "The background colour of the script editor"
        );

        ScriptEditorSelectionColor = Bind(
             "Options",
             "ScriptEditorSelectionColor",
             new Color(0f, 0.5f, 1f, 0.12f),
             "The color of the selection box in the script editor"
        );

        ScriptEditorSelectionOutlineColor = Bind(
            "Options",
            "ScriptEditorSelectionOutlineColor",
            new Color(0f, 0.5f, 1f, 0.8f),
            "The color of the outline of the selection box in the script editor"
        );

        ScriptEditorBlockSelectedLerpColor = Bind(
            "Options",
            "ScriptEditorSelect",
            Color.cyan,
            "The color used to tint the color of selected blocks"
        );
    }

    public class Keybind
    {
        public ConfigEntry<Key> Code;
        public PlayerAction Action;
        
        public Keybind(ConfigEntry<Key> code)
        {
            Code = code;
            ArchitectBinds.Keybinds.Add(this);
        }

        public bool IsPressed => Action?.IsPressed ?? false;
        public bool WasPressed => Action?.WasPressed ?? false;
    }

    public class ArchitectBinds : PlayerActionSet
    {
        public static readonly List<Keybind> Keybinds = [];
        
        public ArchitectBinds()
        {
            Init();
            foreach (var bind in Keybinds)
            {
                bind.Action = CreatePlayerAction(bind.Code.Key);
                bind.Action.AddDefaultBinding(bind.Code.Def);
            }
        }
    }
    
    public static ConfigEntry<T> Bind<T>(string group, string path, T def, string comment)
    {
        var key = $"{group}_{path}";
        var ce = new ConfigEntry<T>(def, key, path);

        if (ce is ConfigEntry<bool> ceb)
        {
            MiscConfig.Add(new HorizontalOption(path, comment, ["False", "True"],
                a => ceb.Value = a == 1, () => ceb.Value ? 1 : 0));
        }
        
        return ce;
    }
    
    public class ConfigEntry<T>(T def, string key, string name)
    {
        public readonly string Key = key;
        public readonly string Name = name;
        
        public T Value
        {
            get => GlobalArchitectData.Instance.ConfigData.TryGetValue(Key, out var s) ? 
                JsonConvert.DeserializeObject<T>(s, Converters.All) : Def;
            set => GlobalArchitectData.Instance.ConfigData[Key] = JsonConvert.SerializeObject(value, Converters.All);
        }

        public readonly T Def = def;
    }
}