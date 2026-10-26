using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RIM.Chemistry.Components;

/// <summary>
/// Prevents solutions containing reagents outside the whitelist
/// from being transferred into this entity.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ReagentWhitelistComponent : Component
{
    /// <summary>
    /// Reagents that are allowed to be transferred into the entity.
    /// A null whitelist allows all reagents.
    /// An empty whitelist allows no reagents.
    /// </summary>
    [DataField]
    public List<ProtoId<ReagentPrototype>>? Whitelist;
}
