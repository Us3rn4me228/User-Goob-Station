// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Tools;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RIM.Tiles;

/// <summary>
/// Allows this entity to dig up diggable tiles, e.g. a shovel removing grass or snow.
/// The tiles themselves are configured with the digging data fields of <see cref="Content.Shared.Maps.ContentTileDefinition"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class TileDiggingComponent : Component
{
    /// <summary>
    /// Base time it takes to dig a tile, before the tile's own time multiplier and the tool's speed are applied.
    /// </summary>
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Tool quality required to dig. Set to null to allow digging with anything that has this component.
    /// </summary>
    [DataField]
    public ProtoId<ToolQualityPrototype>? RequiredQuality = "Digging";

    /// <summary>
    /// Whether the tile must be unobstructed (no walls, mobs, etc.) to be dug up.
    /// </summary>
    [DataField]
    public bool RequiresUnobstructed = true;

    /// <summary>
    /// Sound played when digging starts.
    /// </summary>
    [DataField]
    public SoundSpecifier? DigSound = new SoundPathSpecifier("/Audio/Items/shovel_dig.ogg");
}
