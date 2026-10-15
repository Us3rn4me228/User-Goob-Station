// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Tools;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RIM.Tiles;

[RegisterComponent, NetworkedComponent]
public sealed partial class TileDiggingComponent : Component
{
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(2);

    [DataField]
    public ProtoId<ToolQualityPrototype>? RequiredQuality = "Digging";

    [DataField]
    public bool RequiresUnobstructed = true;

    [DataField]
    public SoundSpecifier? DigSound = new SoundPathSpecifier("/Audio/Items/shovel_dig.ogg");
}
