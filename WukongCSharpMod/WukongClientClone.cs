using Photon.Client;
using Photon.Realtime;
using UnrealEngine.Runtime;
using WukongCSharpMod.State;

namespace WukongCSharpMod
{
    public class WukongClientClone : WukongClient
    {
        private static int _counter;
        private readonly FVector _locationOffset;

        public WukongClientClone() : base($"Clone_{_counter++}", () => { }, _ => { })
        {
            // spawn each clone at 6 positions (hexagon) starting from R = 400 with 6 clones on 1st circle, then the same at R = 600, R = 800 etc.
            var r = 400 + 200 * (_counter / 6);
            var angle = 60 * (_counter % 6);
            var even = _counter % 2 == 0;
            r += even ? 0 : 100;
            var x = r * FMath.Cos(FMath.DegreesToRadians(angle));
            var y = r * FMath.Sin(FMath.DegreesToRadians(angle));
            _locationOffset = new FVector(x, y, 0);
        }

        public override void CachePlayerProperty(string key, object value)
        {
            if (key == nameof(PlayerState.Location))
            {
                var val = (FVector)value;
                val += _locationOffset;
                base.CachePlayerProperty(key, val);
            }
            else
            {
                base.CachePlayerProperty(key, value);
            }
        }

        public override void OnJoinedRoom()
        {
            Helpers.Log("Clone joined room");

            var teamId = PhotonUtils.GetTeamIdForPlayer(PhotonId);
            LocalPlayerState = new PlayerState(PhotonId, GameUtils.GetControlledPawn(), teamId);
        }

        public override void OnPlayerPropertiesUpdate(Player targetPlayer, PhotonHashtable changedProps)
        {
            if (targetPlayer.IsLocal)
            {
                base.OnPlayerPropertiesUpdate(targetPlayer, changedProps);
            }
        }

        protected override void ApplyMonsterMove(PhotonHashtable props)
        {
            // do nothing
        }
    }
}