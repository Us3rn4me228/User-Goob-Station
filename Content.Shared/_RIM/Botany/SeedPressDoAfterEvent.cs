// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RIM.Botany;

/// <summary>
/// Raised on the seed press when its seed extraction do-after finishes or is cancelled.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class SeedPressDoAfterEvent : SimpleDoAfterEvent;
