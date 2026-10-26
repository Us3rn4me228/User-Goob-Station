using Content.Server._RIM.Projectiles.Components;
using Content.Server.Body.Components;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Projectiles;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.Server._RIM.Projectiles.Systems;

public sealed class LiquidPulseProjectileSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly IEntityLookup _lookup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LiquidPulseProjectileComponent, ProjectileHitEvent>(OnProjectileHit);
    }

    private void OnProjectileHit(EntityUid uid, LiquidPulseProjectileComponent comp, ref ProjectileHitEvent args)
    {
        if (!_solution.TryGetSolution(uid, comp.Solution, out var sol, out _))
            return;

        var currentVolume = sol.Value.Comp.Solution.Volume;
        if (currentVolume <= FixedPoint2.Zero)
            return;

        var volumeToInject = FixedPoint2.Min(comp.InjectVolume, currentVolume);

        var xform = Transform(uid);

        var lookup = _lookup.GetEntitiesInRange(xform.Coordinates, comp.InjectRadius);

        var targets = 0;
        foreach (var target in lookup)
        {
            if (HasComp<BloodstreamComponent>(target) || HasComp<InjectableSolutionComponent>(target))
                targets++;
        }

        if (targets == 0)
            return;

        var volumePerTarget = volumeToInject / targets;

        foreach (var target in lookup)
        {
            if (!HasComp<BloodstreamComponent>(target) && !HasComp<InjectableSolutionComponent>(target))
                continue;

            var injectSol = _solution.SplitSolution(sol.Value, volumePerTarget);
            if (injectSol.Volume <= FixedPoint2.Zero)
                continue;

            if (TryComp<BloodstreamComponent>(target, out var bloodstream) &&
                _solution.TryGetSolution(target, bloodstream.BloodSolutionName, out var bloodSol, out _))
            {
                _solution.TryAddSolution(bloodSol.Value, injectSol);
            }
            else if (TryComp<InjectableSolutionComponent>(target, out var injectable) &&
                     _solution.TryGetSolution(target, injectable.Solution, out var injectSolContainer, out _))
            {
                _solution.TryAddSolution(injectSolContainer.Value, injectSol);
            }
        }
    }
}
