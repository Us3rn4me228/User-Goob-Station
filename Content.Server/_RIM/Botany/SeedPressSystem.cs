// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared._RIM.Botany;
using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Server.Popups;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;

namespace Content.Server._RIM.Botany;

public sealed class SeedPressSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly BotanySystem _botany = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SeedPressComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SeedPressComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<SeedPressComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<SeedPressComponent, SeedPressDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<SeedPressComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<SeedPressComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
    }

    private void OnInit(EntityUid uid, SeedPressComponent comp, ComponentInit args)
    {
        _container.EnsureContainer<Container>(uid, comp.ContainerId);
    }

    private Container GetContainer(EntityUid uid, SeedPressComponent comp)
    {
        return _container.EnsureContainer<Container>(uid, comp.ContainerId);
    }

    private void OnInteractUsing(EntityUid uid, SeedPressComponent comp, InteractUsingEvent args)
    {
        if (args.Handled || comp.IsProcessing)
            return;

        if (_whitelist.IsWhitelistFail(comp.Whitelist, args.Used))
        {
            _popup.PopupEntity(Loc.GetString("seed-press-cannot-insert", ("name", args.Used)), uid, args.User);
            return;
        }

        var container = GetContainer(uid, comp);
        if (container.ContainedEntities.Count >= comp.MaxEntities)
        {
            _popup.PopupEntity(Loc.GetString("seed-press-full", ("name", uid)), uid, args.User);
            return;
        }

        if (!_container.Insert(args.Used, container))
            return;

        _audio.PlayPvs(comp.InsertSound, uid);
        args.Handled = true;
    }

    private void OnActivate(EntityUid uid, SeedPressComponent comp, ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        if (comp.IsProcessing)
        {
            _popup.PopupEntity(Loc.GetString("seed-press-busy", ("name", uid)), uid, args.User);
            args.Handled = true;
            return;
        }

        var container = GetContainer(uid, comp);
        if (container.ContainedEntities.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("seed-press-empty", ("name", uid)), uid, args.User);
            args.Handled = true;
            return;
        }

        var started = _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
            args.User,
            comp.Delay,
            new SeedPressDoAfterEvent(),
            uid,
            target: uid)
        {
            BreakOnMove = true,
            NeedHand = true,
        });

        if (!started)
            return;

        comp.IsProcessing = true;
        _audio.PlayPvs(comp.ExtractSound, uid);
        _popup.PopupEntity(Loc.GetString("seed-press-start"), uid, args.User);
        args.Handled = true;
    }

    private void OnDoAfter(EntityUid uid, SeedPressComponent comp, SeedPressDoAfterEvent args)
    {
        comp.IsProcessing = false;

        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        var container = GetContainer(uid, comp);
        var user = args.Args.User;
        var coords = Transform(uid).Coordinates;

        var extracted = 0;
        var seedless = 0;

        foreach (var entity in container.ContainedEntities.ToList())
        {
            if (!TryComp<ProduceComponent>(entity, out var produce))
                continue;

            if (!_botany.TryGetSeed(produce, out var seed) || seed.Seedless)
            {
                seedless++;
                if (comp.DeleteSeedless)
                    QueueDel(entity);
                continue;
            }

            _botany.SpawnSeedPacket(seed, coords, user);
            QueueDel(entity);
            extracted++;
        }

        if (extracted > 0)
            _popup.PopupEntity(Loc.GetString("seed-press-extracted", ("count", extracted)), uid, user, PopupType.Medium);

        if (seedless > 0)
            _popup.PopupEntity(Loc.GetString("seed-press-seedless", ("count", seedless)), uid, user, PopupType.MediumCaution);
    }

    private void OnExamined(EntityUid uid, SeedPressComponent comp, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var container = GetContainer(uid, comp);
        args.PushMarkup(Loc.GetString("seed-press-examine", ("count", container.ContainedEntities.Count)));
    }

    private void OnGetVerbs(EntityUid uid, SeedPressComponent comp, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || comp.IsProcessing)
            return;

        var container = GetContainer(uid, comp);
        if (container.ContainedEntities.Count == 0)
            return;

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("seed-press-eject"),
            Act = () => EjectAll((uid, comp), args.User),
        });
    }

    private void EjectAll(Entity<SeedPressComponent> ent, EntityUid user)
    {
        if (ent.Comp.IsProcessing)
            return;

        var container = GetContainer(ent.Owner, ent.Comp);
        foreach (var entity in container.ContainedEntities.ToList())
        {
            _container.Remove(entity, container);
        }

        _popup.PopupEntity(Loc.GetString("seed-press-ejected", ("name", ent.Owner)), ent.Owner, user);
    }
}
