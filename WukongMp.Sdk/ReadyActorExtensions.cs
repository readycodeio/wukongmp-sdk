using b1;
using WukongMp.Api.ECS.Entities;

namespace WukongMp.Sdk;

public static class ReadyActorExtensions
{
    extension<TSelf>(TSelf obj)
        where TSelf : struct, IReadyEntity<TSelf>, IReadyConvertable<TSelf, ReadyActor> 
    {
        public BGUCharacterCS? Pawn
        {
            get
            {
                default(TSelf).Deconstruct(obj, out _, out var entity);
                if (MainCharacterEntity.TryGetMainCharacter(entity, out var mainCharacter))
                {
                    return mainCharacter.Value.Pawn;
                }
                else if (TamerEntity.TryGetTamer(entity, out var tamer))
                {
                    return tamer.Value.Pawn;
                }
                else
                {
                    return null;
                }
            }
        }
    }
}