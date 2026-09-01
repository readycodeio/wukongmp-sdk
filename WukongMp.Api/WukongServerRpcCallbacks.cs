using ReadyM.Api.Mapping.Events;
using ReadyM.Api.Mapping.Tags;
using ReadyM.Api.Multiplayer;
using ReadyM.Api.Multiplayer.RPC;
using ReadyM.Wukong.Common.Rpc;
using WukongMp.Api.ECS.GameEvents;

// ReSharper disable InconsistentNaming

namespace WukongMp.Api;

[ServerRpcFor(typeof(SdkRpcContracts))]
internal partial class WukongServerRpcCallbacks(IMappedEventManager mappedEvent) : ServerRpcClient
{
    private IMappedEventManager MappedEvent => mappedEvent;

    public override void OnScopeStart()
    {
        base.OnScopeStart();

        MappedEvent.RegisterEcsEventHandler<SkipMovieEvent, WukongServerRpcCallbacks>(static (ev, self) =>
        {
            self.SendSkipMovie(ev.SequenceId);
        }, this);
    }

    partial void OnSkipMovie(SkipMovieData data)
    {
        RunOnGameThread(() =>
        {
            MappedEvent.InvokeInGameIfApplicable(
                new SkipMovieEvent(
                    sequenceId: data.SequenceId,
                    waitingPlayers: data.WaitingPlayers,
                    allPlayers: data.AllPlayers
                ), default(EmptyContext)
            );
        });
    }
}