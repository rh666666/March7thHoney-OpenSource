using March7thHoney.Data;
using March7thHoney.GameServer.Game.GridFight.Sync;
using March7thHoney.GameServer.Game.GridFight;
using March7thHoney.GameServer.Server.Packet.Send.GridFight;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.GridFight;

[Opcode(CmdIds.GridFightEquipDressCsReq)]
public class HandlerGridFightEquipDressCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = GridFightEquipDressCsReq.Parser.ParseFrom(data);
        var player = connection.Player!;
        var inst = player.GridFightManager?.GridFightInstance;

        await connection.SendPacket(new PacketGridFightEquipDressScRsp());
        if (inst == null) return;

        var equipUid = req.DressEquipmentUniqueId;
        var roleUid = req.DressRoleUniqueId;
        if (!inst.RoleByUniqueId.TryGetValue(roleUid, out var roleId)) return;
        if (!inst.Equipments.Any(e => e.UniqueId == equipUid)) return;

        if (!inst.EquipUniqueIdsByRoleUniqueId.TryGetValue(roleUid, out var equipList))
            inst.EquipUniqueIdsByRoleUniqueId[roleUid] = equipList = new List<uint>();
        if (!equipList.Contains(equipUid)) equipList.Add(equipUid);

        
        
        var craftedAdvancedEquip = TryAutoMergeBasicsOnRole(inst, roleUid, equipList);

        uint pos = 0;
        foreach (var kv in inst.UniqueIdByPos)
            if (kv.Value == roleUid) { pos = kv.Key; break; }

        var roleInfo = new GridGameRoleInfo
        {
            Id = roleId,
            Pos = pos,
            UniqueId = roleUid,
            RoleStar = inst.RoleStarByUniqueId.GetValueOrDefault(roleUid, 1u)
        };
        foreach (var eid in equipList) roleInfo.UpdateEquipsComponent.Add(eid);
        if (GameData.GridFightRoleBasicInfoData.TryGetValue(roleId, out var roleExcel)
            && roleExcel.RoleSavedValueList.Count > 0)
        {
            roleInfo.GridFightValueInitComponent[roleExcel.RoleSavedValueList[0]] = 0;
        }

        var sync = new GridFightSyncUpdateResultScNotify();

        
        if (craftedAdvancedEquip != null)
        {
            var craftSec = new GridFightSyncResultData { GridUpdateSrc = GridFightUpdateSrcType.LnpfefkjdhpDgojihijlaf };
            foreach (var removed in craftedAdvancedEquip.RemovedBasics)
            {
                var item = new GridFightGameItemSyncInfo();
                item.GridFightEquipmentList.Add(removed);
                craftSec.UpdateDynamicList.Add(new GridFightSyncData { RemoveGameItemInfo = item });
            }
            var addItem = new GridFightGameItemSyncInfo();
            addItem.GridFightEquipmentList.Add(craftedAdvancedEquip.Added);
            craftSec.UpdateDynamicList.Add(new GridFightSyncData { AddGameItemInfo = addItem });
            GridFightInstance.AppendBattlefieldLayoutSync(craftSec, inst);
            sync.SyncResultDataList.Add(craftSec);
        }

        var sec = new GridFightSyncResultData();
        sec.UpdateDynamicList.Add(new GridFightSyncData { UpdateRoleInfo = roleInfo });
        GridFightInstance.AppendBattlefieldLayoutSync(sec, inst);
        sync.SyncResultDataList.Add(sec);
        await connection.SendPacket(new PacketGridFightSyncUpdateResultScNotify(sync));
    }

    private sealed class AutoMergeResult
    {
        public List<GridFightEquipmentInfo> RemovedBasics = new();
        public GridFightEquipmentInfo Added = null!;
    }

    private static AutoMergeResult? TryAutoMergeBasicsOnRole(GridFightInstance inst, uint roleUid, List<uint> equipList)
    {
        
        var basics = new List<GridFightEquipmentInfo>();
        foreach (var uid in equipList)
        {
            var info = inst.FindEquipment(uid);
            if (info == null) continue;
            if (!GameData.GridFightEquipmentData.TryGetValue(info.GridFightEquipmentId, out var excel)) continue;
            if (excel.EquipCategory != Enums.GridFight.GridFightEquipCategoryEnum.Basic) continue;
            basics.Add(info);
            if (basics.Count >= 2) break;
        }
        if (basics.Count < 2) return null;

        var a = basics[0].GridFightEquipmentId;
        var b = basics[1].GridFightEquipmentId;
        
        var ia = (int)(a % 10);
        var ib = (int)(b % 10);
        if (ia <= 0 || ib <= 0) return null;
        var lo = Math.Min(ia, ib);
        var hi = Math.Max(ia, ib);
        var advancedId = (uint)(35030000 + lo * 100 + hi);
        if (!GameData.GridFightEquipmentData.ContainsKey(advancedId)) return null;

        
        var removedSnapshots = basics.Select(b2 => b2.Clone()).ToList();
        foreach (var basic in basics)
            inst.RemoveEquipmentByUniqueId(basic.UniqueId);
        var added = inst.AddEquipment(advancedId, source: 5);

        
        equipList.RemoveAll(uid => basics.Any(b2 => b2.UniqueId == uid));
        equipList.Add(added.UniqueId);

        return new AutoMergeResult { RemovedBasics = removedSnapshots, Added = added };
    }
}

[Opcode(CmdIds.GridFightEquipCraftCsReq)]
public class HandlerGridFightEquipCraftCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = GridFightEquipCraftCsReq.Parser.ParseFrom(data);
        var player = connection.Player!;
        var inst = player.GridFightManager?.GridFightInstance;

        await connection.SendPacket(new PacketEPJJBNPIFLC());
        if (inst == null) return;

        var targetId = req.BGEKACPOAOP;
        var materials = req.CraftMaterials.ToList();
        if (targetId == 0 || materials.Count == 0) return;

        
        var matInfos = materials
            .Select(uid => inst.FindEquipment(uid))
            .Where(e => e != null)
            .Cast<GridFightEquipmentInfo>()
            .ToList();
        if (matInfos.Count != materials.Count) return;

        var sync = new GridFightSyncUpdateResultScNotify();
        var sec = new GridFightSyncResultData { GridUpdateSrc = GridFightUpdateSrcType.LnpfefkjdhpDgojihijlaf };

        
        foreach (var mat in matInfos)
        {
            var clone = mat.Clone();
            inst.RemoveEquipmentByUniqueId(mat.UniqueId);
            var item = new GridFightGameItemSyncInfo();
            item.GridFightEquipmentList.Add(clone);
            sec.UpdateDynamicList.Add(new GridFightSyncData { RemoveGameItemInfo = item });
        }

        
        var crafted = inst.AddEquipment(targetId, 5);
        var addItem = new GridFightGameItemSyncInfo();
        addItem.GridFightEquipmentList.Add(crafted);
        sec.UpdateDynamicList.Add(new GridFightSyncData { AddGameItemInfo = addItem });
        GridFightInstance.AppendBattlefieldLayoutSync(sec, inst);

        sync.SyncResultDataList.Add(sec);
        sync.SyncResultDataList.Add(new GridFightSyncResultData());
        await connection.SendPacket(new PacketGridFightSyncUpdateResultScNotify(sync));
    }
}

[Opcode(CmdIds.GridFightUseConsumableCsReq)]
public class HandlerGridFightUseConsumableCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = GridFightUseConsumableCsReq.Parser.ParseFrom(data);
        var player = connection.Player!;
        var inst = player.GridFightManager?.GridFightInstance;

        await connection.SendPacket(new PacketOLOGIALOJDP());
        if (inst == null) return;

        var itemId = req.ItemId;
        if (!GameData.GridFightConsumablesData.TryGetValue(itemId, out var consumable)) return;

        var sync = new GridFightSyncUpdateResultScNotify();
        var sec = new GridFightSyncResultData { GridUpdateSrc = GridFightUpdateSrcType.LnpfefkjdhpNefkflkampo };
        sec.SyncEffectParamList.Add(itemId);

        
        
        
        
        if (consumable.IfConsume)
        {
            var current = inst.Consumables.FirstOrDefault(c => c.ItemId == itemId);
            var remaining = current == null ? 0u : (current.Num > 0 ? current.Num - 1 : 0u);

            var payload = new GridFightGameItemSyncInfo();
            var update = new GridFightConsumableUpdateInfo
            {
                ItemId = itemId,
                ItemStackCount = -1
            };
            if (remaining > 0) update.Num = remaining;
            payload.UpdateGridFightConsumableList.Add(update);

            sec.UpdateDynamicList.Add(remaining > 0
                ? new GridFightSyncData { UpdateGameItemInfo = payload }
                : new GridFightSyncData { RemoveGameItemInfo = payload });
        }

        var target = req.DisplayValue;
        switch (consumable.ConsumableRule)
        {
            case Enums.GridFight.GridFightConsumeTypeEnum.Remove:
                HandleRemoveRule(inst, target, sec);
                break;
            case Enums.GridFight.GridFightConsumeTypeEnum.Roll:
                HandleRollRule(inst, target, sec);
                break;
            default:
                
                break;
        }

        if (consumable.IfConsume) inst.TryConsumeConsumable(itemId);

        sync.SyncResultDataList.Add(sec);
        sync.SyncResultDataList.Add(new GridFightSyncResultData());
        await connection.SendPacket(new PacketGridFightSyncUpdateResultScNotify(sync));
    }

    
    private static void HandleRemoveRule(GridFightInstance inst, GridFightConsumableTargetInfo target, GridFightSyncResultData sec)
    {
        if (target?.RemoveTypeTargetInfo == null) return;
        var roleUid = target.RemoveTypeTargetInfo.DressRoleUniqueId;
        if (!inst.RoleByUniqueId.TryGetValue(roleUid, out var roleId)) return;
        inst.UnequipAllFromRole(roleUid);
        sec.UpdateDynamicList.Add(new GridFightSyncData { UpdateRoleInfo = BuildRoleInfoSnapshot(inst, roleUid, roleId) });
    }

    
    private static void HandleRollRule(GridFightInstance inst, GridFightConsumableTargetInfo target, GridFightSyncResultData sec)
    {
        if (target?.RollTypeTargetInfo == null) return;
        var equipUid = target.RollTypeTargetInfo.DressEquipmentUniqueId;
        var roleUid = target.RollTypeTargetInfo.DressRoleUniqueId;

        if (equipUid != 0)
        {
            RollOneEquipment(inst, equipUid, sec);
            return;
        }

        if (roleUid != 0 && inst.RoleByUniqueId.TryGetValue(roleUid, out var roleId))
        {
            
            var equips = inst.EquipUniqueIdsByRoleUniqueId.TryGetValue(roleUid, out var list)
                ? list.ToList()
                : new List<uint>();
            foreach (var uid in equips)
                RollOneEquipment(inst, uid, sec);
            sec.UpdateDynamicList.Add(new GridFightSyncData { UpdateRoleInfo = BuildRoleInfoSnapshot(inst, roleUid, roleId) });
        }
    }

    private static void RollOneEquipment(GridFightInstance inst, uint equipUid, GridFightSyncResultData sec)
    {
        var old = inst.FindEquipment(equipUid);
        if (old == null) return;
        var newId = GridFightInstance.RollSameCategoryEquipment(old.GridFightEquipmentId);
        if (newId == 0) return;
        var oldClone = old.Clone();
        var added = inst.RollEquipment(equipUid, newId, source: 1);
        if (added == null) return;

        var removeItem = new GridFightGameItemSyncInfo();
        removeItem.GridFightEquipmentList.Add(oldClone);
        sec.UpdateDynamicList.Add(new GridFightSyncData { RemoveGameItemInfo = removeItem });

        var addItem = new GridFightGameItemSyncInfo();
        addItem.GridFightEquipmentList.Add(added);
        sec.UpdateDynamicList.Add(new GridFightSyncData { AddGameItemInfo = addItem });
        GridFightInstance.AppendBattlefieldLayoutSync(sec, inst);
    }

    private static GridGameRoleInfo BuildRoleInfoSnapshot(GridFightInstance inst, uint roleUid, uint roleId)
    {
        uint pos = 0;
        foreach (var kv in inst.UniqueIdByPos)
            if (kv.Value == roleUid) { pos = kv.Key; break; }

        var roleInfo = new GridGameRoleInfo
        {
            Id = roleId,
            Pos = pos,
            UniqueId = roleUid,
            RoleStar = inst.RoleStarByUniqueId.GetValueOrDefault(roleUid, 1u)
        };
        if (inst.EquipUniqueIdsByRoleUniqueId.TryGetValue(roleUid, out var equipList))
            foreach (var eid in equipList) roleInfo.UpdateEquipsComponent.Add(eid);
        if (GameData.GridFightRoleBasicInfoData.TryGetValue(roleId, out var roleExcel)
            && roleExcel.RoleSavedValueList.Count > 0)
            roleInfo.GridFightValueInitComponent[roleExcel.RoleSavedValueList[0]] = 0;
        return roleInfo;
    }
}

[Opcode(CmdIds.GridFightUseForgeCsReq)]
public class HandlerGridFightUseForgeCsReq : Handler { public override async Task OnHandle(Connection connection, byte[] header, byte[] data) { _ = GridFightUseForgeCsReq.Parser.ParseFrom(data); await connection.SendPacket(new PacketEEBMIAFNJMC()); await connection.SendPacket(new PacketGridFightSyncUpdateResultScNotify(connection.Player!, GridFightSyncKind.NoOp)); } }

[Opcode(CmdIds.GridFightBuyExpCsReq)]
public class HandlerGridFightBuyExpCsReq : Handler { public override async Task OnHandle(Connection connection, byte[] header, byte[] data) { _ = GridFightBuyExpCsReq.Parser.ParseFrom(data); var player = connection.Player!; _ = new GridFightService(player).BuyExp(); await connection.SendPacket(new PacketDJCHCHCAJPB()); await connection.SendPacket(new PacketGridFightSyncUpdateResultScNotify(player, GridFightSyncKind.BuyExp)); } }

[Opcode(CmdIds.GridFightLockShopCsReq)]
public class HandlerGridFightLockShopCsReq : Handler { public override async Task OnHandle(Connection connection, byte[] header, byte[] data) { _ = GridFightLockShopCsReq.Parser.ParseFrom(data); await connection.SendPacket(new PacketHGOIBDBMDBG()); await connection.SendPacket(new PacketGridFightSyncUpdateResultScNotify(connection.Player!, GridFightSyncKind.NoOp)); } }

[Opcode(CmdIds.GJMIIBDEAAJ)]
public class HandlerGridFightUseOrbCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = GJMIIBDEAAJ.Parser.ParseFrom(data);
        var player = connection.Player!;
        var inst = player.GridFightManager?.GridFightInstance;

        await connection.SendPacket(new PacketCEFIMADBIBH());
        if (inst == null) return;

        var results = inst.TryUseOrbsDetailed(req.HDFJAINBKJG, req.IsGetAll);

        var sync = new GridFightSyncUpdateResultScNotify();
        var levelChanged = false;

        
        
        
        foreach (var r in results)
        {
            if (r.LevelChanged) levelChanged = true;

            var sec = new GridFightSyncResultData
            {
                GridUpdateSrc = GridFightUpdateSrcType.LnpfefkjdhpOopnbcijhmp
            };
            sec.SyncEffectParamList.Add(r.UniqueId);
            sec.SyncEffectParamList.Add(r.ItemId);
            sec.UpdateDynamicList.Add(new GridFightSyncData { RemoveOrbUniqueId = r.UniqueId });

            if (r.AddEquipmentId.HasValue && r.AddEquipmentUniqueId.HasValue)
            {
                var item = new GridFightGameItemSyncInfo();
                item.GridFightEquipmentList.Add(new GridFightEquipmentInfo
                {
                    GridFightEquipmentId = r.AddEquipmentId.Value,
                    Source = 1,
                    UniqueId = r.AddEquipmentUniqueId.Value
                });
                sec.UpdateDynamicList.Add(new GridFightSyncData { AddGameItemInfo = item });
                GridFightInstance.AppendBattlefieldLayoutSync(sec, inst);
            }

            if (r.AddConsumableItemId.HasValue)
            {
                
                var item = new GridFightGameItemSyncInfo();
                item.UpdateGridFightConsumableList.Add(new GridFightConsumableUpdateInfo
                {
                    ItemId = r.AddConsumableItemId.Value,
                    ItemStackCount = 1,
                    Num = r.AddConsumableNewTotal
                });
                sec.UpdateDynamicList.Add(r.AddConsumableNewTotal <= 1
                    ? new GridFightSyncData { AddGameItemInfo = item }
                    : new GridFightSyncData { UpdateGameItemInfo = item });
            }

            if (r.GoldChanged)
                sec.UpdateDynamicList.Add(new GridFightSyncData { ItemValue = r.GoldAfter });

            if (r.AddRoleId.HasValue && r.AddRoleUniqueId.HasValue && r.AddRolePos.HasValue)
            {
                var roleInfo = new GridGameRoleInfo
                {
                    Id = r.AddRoleId.Value,
                    Pos = r.AddRolePos.Value,
                    RoleStar = 1,
                    UniqueId = r.AddRoleUniqueId.Value
                };
                if (!string.IsNullOrEmpty(r.AddRoleComponent))
                    roleInfo.GridFightValueInitComponent[r.AddRoleComponent] = 0;
                sec.UpdateDynamicList.Add(new GridFightSyncData { AddRoleInfo = roleInfo });
            }

            sync.SyncResultDataList.Add(sec);
        }

        if (levelChanged)
        {
            var lvlSec = new GridFightSyncResultData();
            lvlSec.UpdateDynamicList.Add(new GridFightSyncData
            {
                PlayerLevel = new GridFightPlayerLevelSyncInfo
                {
                    Level = inst.PlayerLevel,
                    Exp = inst.PlayerExp,
                    MaxLevel = inst.PlayerMaxLevel
                }
            });
            lvlSec.UpdateDynamicList.Add(new GridFightSyncData { GridFightBuyExpCost = inst.GetBuyExpCost() });
            GridFightInstance.AppendBattlefieldLayoutSync(lvlSec, inst);
            lvlSec.UpdateDynamicList.Add(new GridFightSyncData
            {
                ShopSyncInfo = new GridFightShopSyncInfo { LDEDGOOKHFL = inst.BuildShopRarityDisplayInfo() }
            });
            sync.SyncResultDataList.Add(lvlSec);
        }

        
        if (sync.SyncResultDataList.Count == 0)
        {
            var noop = new GridFightSyncResultData();
            noop.UpdateDynamicList.Add(new GridFightSyncData { ItemValue = inst.Gold });
            sync.SyncResultDataList.Add(noop);
        }

        await connection.SendPacket(new PacketGridFightSyncUpdateResultScNotify(sync));
    }
}

[Opcode(CmdIds.GridFightRecycleRoleCsReq)]
public class HandlerGridFightRecycleRoleCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = GridFightRecycleRoleCsReq.Parser.ParseFrom(data);
        var player = connection.Player!;
        var refund = new GridFightService(player).RecycleRole(req.UniqueId);
        await connection.SendPacket(new PacketGridFightRecycleRoleScRsp());
        await connection.SendPacket(new PacketGridFightSyncUpdateResultScNotify(player, GridFightSyncKind.RecycleRole, (req.UniqueId, (int)refund)));
    }
}

[Opcode(CmdIds.GridFightUpdateEquipTrackCsReq)]
public class HandlerGridFightUpdateEquipTrackCsReq : Handler { public override async Task OnHandle(Connection connection, byte[] header, byte[] data) { _ = GridFightUpdateEquipTrackCsReq.Parser.ParseFrom(data); await connection.SendPacket(new PacketGridFightUpdateEquipTrackScRsp()); } }
