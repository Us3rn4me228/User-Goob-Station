// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Whitelist;
using Robust.Shared.Audio;

namespace Content.Server._RIM.Botany;

/// <summary>
/// Structure that accepts produce like the kitchen reagent grinder, then extracts
/// seeds from all stored produce via a do-after when activated with an empty hand.
/// Exactly one seed packet is spawned per piece of produce, like a seed extractor
/// rolling a 1.
/// </summary>
[RegisterComponent]
public sealed partial class SeedPressComponent : Component
{
    /// <summary>
    /// Id of the container holding the inserted produce.
    /// </summary>
    [DataField]
    public string ContainerId = "storage";

    /// <summary>
    /// Maximum amount of produce that can be inserted at once.
    /// </summary>
    [DataField]
    public int MaxEntities = 10;

    /// <summary>
    /// Duration of the extraction do-after.
    /// </summary>
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(2.5);

    /// <summary>
    /// Only entities passing this whitelist can be inserted. Defaults to produce.
    /// </summary>
    [DataField]
    public EntityWhitelist Whitelist = new() { Components = new[] { "Produce" } };

    /// <summary>
    /// Whether produce without viable seeds is consumed anyway.
    /// If false, such produce is left inside the press.
    /// </summary>
    [DataField]
    public bool DeleteSeedless = true;

    /// <summary>
    /// Sound played when produce is inserted.
    /// </summary>
    [DataField]
    public SoundSpecifier InsertSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

    /// <summary>
    /// Sound played when the extraction do-after starts.
    /// </summary>
    [DataField]
    public SoundSpecifier ExtractSound = new SoundPathSpecifier("/Audio/Machines/juicer.ogg");

    /// <summary>
    /// True while the extraction do-after is running. Not serialized or networked.
    /// </summary>
    [ViewVariables]
    public bool IsProcessing;
}
