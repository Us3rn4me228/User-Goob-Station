// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using Content.Shared.Administration.Logs;
using Content.Shared.Burial.Components;
using Content.Shared.Database;
using Content.Shared.Decals;
using Content.Shared.DoAfter;
using Content.Shared.EntityTable;
using Content.Shared.Fluids.Components;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RIM.Tiles;

/// <summary>
/// Allows tools with <see cref="TileDiggingComponent"/> (e.g. shovels) to dig up natural planet tiles.
/// Digging turns the tile into another one (grass into dirt, snow into dug up snow, ...) and can spawn
/// loot. Everything is configured with the digging data fields on <see cref="ContentTileDefinition"/>.
/// </summary>
public sealed class TileDiggingSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefs = default!;
    [Dependency] private readonly EntityTableSystem _entityTable = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDecalSystem _decals = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedMapSystem _maps = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedToolSystem _tools = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TileDiggingComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<TileDiggingComponent, TileDiggingDoAfterEvent>(OnDigComplete);
    }

    private void OnAfterInteract(Entity<TileDiggingComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        // Only dig when clicking a tile, not when using the tool on an entity. Same as tile prying.
        if (args.Target != null && !HasComp<PuddleComponent>(args.Target))
            return;

        if (!HasRequiredQuality(ent.Comp, ent.Owner))
            return;

        if (!TryGetTile(args.ClickLocation, out var gridUid, out var grid, out var tileRef))
            return;

        if (!TryGetDiggable(tileRef, out var tileDef, out _))
            return;

        args.Handled = true;

        if (IsObstructed(ent.Comp, gridUid, grid, tileRef, args.User))
        {
            if (_net.IsServer)
                _popup.PopupClient(Loc.GetString("tile-digging-blocked"), args.User, args.User);

            return;
        }

        var doAfterArgs = new DoAfterArgs(EntityManager,
            args.User,
            GetDigDelay(ent, tileDef),
            new TileDiggingDoAfterEvent(GetNetEntity(gridUid), tileRef.GridIndices),
            ent.Owner,
            used: ent.Owner)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            BreakOnWeightlessMove = false,
            NeedHand = ent.Owner != args.User,
        };

        if (!_doAfter.TryStartDoAfter(doAfterArgs))
            return;

        PlayDigSound(ent, args.User);
    }

    private void OnDigComplete(Entity<TileDiggingComponent> ent, ref TileDiggingDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (!HasRequiredQuality(ent.Comp, ent.Owner))
            return;

        var gridUid = GetEntity(args.Grid);
        if (!TryComp<MapGridComponent>(gridUid, out var grid))
            return;

        var tileRef = _maps.GetTileRef(gridUid, grid, args.GridTile);

        if (!TryGetDiggable(tileRef, out var tileDef, out var resultDef))
            return;

        if (IsObstructed(ent.Comp, gridUid, grid, tileRef, args.User))
            return;

        args.Handled = true;

        // Tiles and spawned entities are server-authoritative. The do-after itself is predicted, so the
        // progress bar is shown without any latency.
        if (_net.IsClient)
            return;

        var tileCoordinates = _maps.GridTileToLocal(gridUid, grid, tileRef.GridIndices);

        // Turn the tile into the dug version and get rid of whatever was painted on top of it.
        _maps.SetTile(gridUid, grid, tileRef.GridIndices, new Tile(resultDef.TileId));
        RemoveDecals(gridUid, tileCoordinates);

        SpawnLoot(tileDef, tileCoordinates);

        _popup.PopupClient(Loc.GetString("tile-digging-finished"), args.User, args.User);

        _adminLogger.Add(LogType.Tile,
            LogImpact.Low,
            $"{ToPrettyString(args.User):player} dug up {tileDef.ID} at {tileCoordinates}");
    }

    /// <summary>
    /// Resolves the grid and tile at the given coordinates.
    /// </summary>
    private bool TryGetTile(EntityCoordinates coordinates,
        out EntityUid gridUid,
        out MapGridComponent grid,
        out TileRef tileRef)
    {
        gridUid = default;
        grid = default!;
        tileRef = default;

        if (!_mapManager.TryFindGridAt(_transform.ToMapCoordinates(coordinates), out var foundGridUid, out var foundGrid))
            return false;

        gridUid = foundGridUid;
        grid = foundGrid;
        tileRef = _maps.GetTileRef(gridUid, grid, coordinates);
        return true;
    }

    /// <summary>
    /// Checks whether the tile can be dug up and resolves which tile it turns into.
    /// </summary>
    private bool TryGetDiggable(TileRef tileRef,
        [NotNullWhen(true)] out ContentTileDefinition? tileDef,
        [NotNullWhen(true)] out ContentTileDefinition? resultDef)
    {
        tileDef = null;
        resultDef = null;

        if (tileRef.Tile.IsEmpty)
            return false;

        tileDef = _tileDefs[tileRef.Tile.TypeId] as ContentTileDefinition;
        if (tileDef == null || !tileDef.Diggable)
            return false;

        var resultId = tileDef.DigResultTile ?? tileDef.BaseTurf;
        if (resultId == null || !_prototypes.TryIndex(resultId.Value, out resultDef))
            return false;

        return true;
    }

    private bool IsObstructed(TileDiggingComponent component,
        EntityUid gridUid,
        MapGridComponent grid,
        TileRef tileRef,
        EntityUid user)
    {
        // Nothing in reach, e.g. the user walked away or a wall got in the way.
        var tileCoordinates = _maps.GridTileToLocal(gridUid, grid, tileRef.GridIndices);
        if (!_interaction.InRangeUnobstructed(user, tileCoordinates, popup: false))
            return true;

        return component.RequiresUnobstructed
               && _turf.IsTileBlocked(gridUid, tileRef.GridIndices, CollisionGroup.MobMask);
    }

    private TimeSpan GetDigDelay(Entity<TileDiggingComponent> ent, ContentTileDefinition tileDef)
    {
        var speed = 1f;

        // ToolComponent's modifier is a divisor, matching how tile prying works. Tools that lack it can
        // still use their grave digging modifier.
        if (TryComp<ToolComponent>(ent, out var tool))
            speed = tool.SpeedModifier;
        else if (TryComp<ShovelComponent>(ent, out var shovel))
            speed = shovel.SpeedModifier;

        if (speed <= 0f)
            speed = 1f;

        return ent.Comp.Delay * tileDef.DigTimeMultiplier / speed;
    }

    private bool HasRequiredQuality(TileDiggingComponent component, EntityUid tool)
    {
        if (component.RequiredQuality == null)
            return true;

        return _tools.HasQuality(tool, component.RequiredQuality.Value);
    }

    private void PlayDigSound(Entity<TileDiggingComponent> ent, EntityUid user)
    {
        if (ent.Comp.DigSound != null)
            _audio.PlayPredicted(ent.Comp.DigSound, ent.Owner, user);
    }

    private void RemoveDecals(EntityUid gridUid, EntityCoordinates coordinates)
    {
        var decals = _decals.GetDecalsInRange(gridUid, coordinates.Position, 0.5f);
        foreach (var (id, _) in decals)
        {
            _decals.RemoveDecal(gridUid, id);
        }
    }

    private void SpawnLoot(ContentTileDefinition tileDef, EntityCoordinates coordinates)
    {
        if (tileDef.DigSpawnsItem)
            SpawnLoot(tileDef.ItemDropPrototypeName, coordinates);

        if (tileDef.DigLoot == null)
            return;

        foreach (var prototype in _entityTable.GetSpawns(tileDef.DigLoot))
        {
            SpawnLoot(prototype.Id, coordinates);
        }
    }

    private void SpawnLoot(string prototype, EntityCoordinates coordinates)
    {
        if (string.IsNullOrEmpty(prototype))
            return;

        // Scatter the loot a bit so it doesn't all end up in the exact middle of the tile.
        var loot = Spawn(prototype, coordinates.Offset(_random.NextVector2(0.3f)));

        Transform(loot).LocalRotation = _random.NextDouble() * Math.Tau;
    }
}
