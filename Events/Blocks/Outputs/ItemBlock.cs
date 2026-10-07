using System;
using System.Collections.Generic;
using Architect.Content.Custom;
using Architect.Workshop.Items;
using ItemChanger;
using ItemChanger.Items;

namespace Architect.Events.Blocks.Outputs;

public class ItemBlock : ScriptBlock
{
    protected override IEnumerable<string> Inputs => ["Give", "GiveSilent", "Take"];
    protected override IEnumerable<(string, string)> OutputVars => [
        ("Obtained", "Boolean"),
        ("Amount", "Number")
    ];
    
    protected override string Name => "Item Control";

    public override void Reset()
    {
        ItemName = "";
    }

    public string ItemName;

    public override object GetValue(string id)
    {
        var item = Finder.GetItem(ItemName);
        
        var amount = item is CustomItem.IcCustomItem i &&
               ArchitectData.Instance.CustomItems.TryGetValue(i.Item.Id, out var value)
            ? value
            : 0;
        if (id == "Obtained")
            return item != null && (item.Redundant() || item.IsObtained())
                   || amount > 0;
        return amount;
    }

    protected override void Trigger(string trigger)
    {
        var item = Finder.GetItem(ItemName);
        if (item == null) return;
        if (trigger == "Take")
        {
            switch (item)
            {
                case BoolItem bi:
                    PlayerData.instance.SetBool(bi.fieldName, false);
                    switch (bi)
                    {
                        case IsmaItem:
                            PlayMakerFSM.BroadcastEvent("GET ACID ARMOUR");
                            break;
                        case LumaflyLanternItem:
                            AbilityObjects.RefreshLanternBinding();
                            break;
                    }

                    break;
                case MultiBoolItem mbi:
                    foreach (var field in mbi.fieldNames) PlayerData.instance.SetBool(field, false);
                    break;
                case MapItem mi:
                    PlayerData.instance.SetBool(mi.fieldName, false);
                    break;
                case MapMarkerItem mmi:
                    PlayerData.instance.SetBool(mmi.fieldName, false);
                    break;
                case MapPinItem mpi:
                    PlayerData.instance.SetBool(mpi.fieldName, false);
                    break;
                case SpellItem si:
                    PlayerData.instance.SetInt(si.fieldName, si.spellLevel - 1);
                    break;
                case ItemChanger.Items.CharmItem ci:
                    PlayerData.instance.SetBool(ci.gotBool, false);
                    break;
                case CustomSkillItem csi:
                    PlayerData.instance.SetBool(csi.boolName, false);
                    break;
                case DirtmouthStagItem:
                    PlayerData.instance.SetBool(nameof(PlayerData.openedTown), false);
                    PlayerData.instance.SetBool(nameof(PlayerData.openedTownBuilding), false);
                    break;
                case DreamerItem di:
                    TakeDreamer(di.dreamer);
                    break;
                case GrubItem:
                    PlayerData.instance.SetInt(nameof(PlayerData.grubsCollected),
                        Math.Max(0, PlayerData.instance.GetInt(nameof(PlayerData.grubsCollected)) - 1));
                    break;
                case StagItem si:
                    PlayerData.instance.SetBool(si.fieldName, false);
                    PlayerData.instance.SetInt(nameof(PlayerData.stationsOpened),
                        Math.Max(0, PlayerData.instance.GetInt(nameof(PlayerData.stationsOpened)) - 1));
                    break;
                case RelicItem ri:
                    PlayerData.instance.SetInt($"trinket{ri.trinketNum}", 
                        Math.Max(0, PlayerData.instance.GetInt($"trinket{ri.trinketNum}") - 1));
                    break;
                case IntItem ii:
                    PlayerData.instance.IntAdd(ii.fieldName, -ii.amount);
                    break;
                case CustomItem.IcCustomItem ici:
                    ici.Take();
                    break;
                default:
                    return;
            }
        }
        else
        {
            item.Give(null, new GiveInfo
            {
                MessageType = trigger == "Give" ? MessageType.Any : MessageType.None
            });
        }
    }

    private static void TakeDreamer(DreamerItem.DreamerType dreamer)
    {
        switch (dreamer)
        {
            case DreamerItem.DreamerType.Lurien:
                PlayerData.instance.SetBool(nameof(PlayerData.lurienDefeated), false);
                PlayerData.instance.SetBool(nameof(PlayerData.maskBrokenLurien), false);
                break;
            case DreamerItem.DreamerType.Monomon:
                PlayerData.instance.SetBool(nameof(PlayerData.monomonDefeated), false);
                PlayerData.instance.SetBool(nameof(PlayerData.maskBrokenMonomon), false);
                break;
            case DreamerItem.DreamerType.Herrah:
                PlayerData.instance.SetBool(nameof(PlayerData.hegemolDefeated), false);
                PlayerData.instance.SetBool(nameof(PlayerData.maskBrokenHegemol), false);
                break;
            case DreamerItem.DreamerType.None:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(dreamer), dreamer, null);
        }
        if (PlayerData.instance.GetInt(nameof(PlayerData.guardiansDefeated)) > 0)
        {
            PlayerData.instance.DecrementInt(nameof(PlayerData.guardiansDefeated));
        }
    }
}