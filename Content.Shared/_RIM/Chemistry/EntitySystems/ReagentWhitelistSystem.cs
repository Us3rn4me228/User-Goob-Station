using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared._RIM.Chemistry.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._RIM.Chemistry.EntitySystems;

/// <summary>
/// Prevents transferring solutions containing non-whitelisted reagents
/// into entities with ReagentWhitelistComponent.
/// </summary>
public sealed partial class ReagentWhitelistSystem : EntitySystem
{
    [Dependency] private SharedSolutionContainerSystem _solution = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReagentWhitelistComponent, SolutionTransferAttemptEvent>(
            OnSolutionTransferAttempt);
    }

    private void OnSolutionTransferAttempt(
        Entity<ReagentWhitelistComponent> ent,
        ref SolutionTransferAttemptEvent args)
    {
        // We only care when this entity is the destination.
        if (args.To != ent.Owner)
            return;

        if (ent.Comp.Whitelist is null)
            return;

        // Find the solution being transferred from the source entity.
        if (!_solution.TryGetDrainableSolution(
                args.From,
                out var sourceSolution,
                out var solution))
        {
            return;
        }

        // An empty solution is handled by SolutionTransferSystem itself.
        if (solution.Volume <= 0)
            return;

        // Every reagent in the source solution must be whitelisted.
        foreach (var reagent in solution.Contents)
        {
            if (ent.Comp.Whitelist.Contains(reagent.Reagent.Prototype))
                continue;

            args.Cancel(Loc.GetString(
                "reagent-whitelist-component-invalid-reagent",
                ("reagent", _proto.Index(reagent.Reagent.Prototype).LocalizedName)));

            return;
        }
    }
}
