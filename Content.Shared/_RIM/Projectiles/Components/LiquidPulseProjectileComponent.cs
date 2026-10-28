using Robust.Shared.GameStates;

namespace Content.Shared._RIM.Projectiles.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class LiquidPulseProjectileComponent : Component
{
    [DataField]
    public string Solution = "vapor";

    [DataField]
    public float InjectVolume = 10f;

    [DataField]
    public float InjectRadius = 1.5f;
}
