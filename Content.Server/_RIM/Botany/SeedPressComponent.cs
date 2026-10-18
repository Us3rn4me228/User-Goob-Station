// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Whitelist;
using Robust.Shared.Audio;

namespace Content.Server._RIM.Botany;

[RegisterComponent]
public sealed partial class SeedPressComponent : Component
{
    [DataField]
    public string ContainerId = "seeds";

    [DataField]
    public int MaxEntities = 10;

    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(2.5);

    [DataField]
    public EntityWhitelist Whitelist = new() { Components = new[] { "Produce" } };

    [DataField]
    public bool DeleteSeedless = true;

    [DataField]
    public SoundSpecifier InsertSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

    [DataField]
    public SoundSpecifier ExtractSound = new SoundPathSpecifier("/Audio/Machines/juicer.ogg");

    [ViewVariables]
    public bool IsProcessing;
}
