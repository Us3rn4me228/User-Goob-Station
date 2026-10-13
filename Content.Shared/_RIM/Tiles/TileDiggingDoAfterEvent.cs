// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RIM.Tiles;

/// <summary>
/// Raised on the digging tool when the digging do-after finishes or gets cancelled.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class TileDiggingDoAfterEvent : DoAfterEvent
{
    public NetEntity Grid;
    public Vector2i GridTile;

    public TileDiggingDoAfterEvent(NetEntity grid, Vector2i gridTile)
    {
        Grid = grid;
        GridTile = gridTile;
    }

    public override DoAfterEvent Clone()
    {
        return this;
    }

    public override bool IsDuplicate(DoAfterEvent other)
    {
        return other is TileDiggingDoAfterEvent otherDigging
               && Grid == otherDigging.Grid
               && GridTile == otherDigging.GridTile;
    }
}
