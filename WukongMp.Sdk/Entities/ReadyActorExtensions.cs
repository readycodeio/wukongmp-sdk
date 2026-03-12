using b1;
using WukongMp.Api.ECS.Entities;

namespace WukongMp.Sdk.Entities;

public static class ReadyActorExtensions
{
    extension<TSelf>(TSelf obj)
        where TSelf : struct, IReadyEntity<TSelf>, IReadyConvertable<TSelf, ReadyActor>
    {
        public BGUCharacterCS? Pawn
        {
            get
            {
                obj.Deconstruct(out _, out var entity);

                if (MainCharacterEntity.TryGetMainCharacter(entity, out var mainCharacter))
                {
                    return mainCharacter.Value.Pawn;
                }

                if (TamerEntity.TryGetTamer(entity, out var tamer))
                {
                    return tamer.Value.Pawn;
                }

                return null;
            }
        }
    }
}