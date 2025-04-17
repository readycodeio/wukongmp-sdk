namespace WukongApi;

public readonly struct RoomCreationOptions(
    int tournamentRounds,
    int enemiesNgPlusLevel,
    bool gourdAllowed,
    bool immobilizeAllowed,
    bool phantomRushAllowed,
    bool consumablesAllowed
)
{
    public readonly int TournamentRounds = tournamentRounds;
    public readonly int EnemiesNgPlusLevel = enemiesNgPlusLevel;
    public readonly bool GourdAllowed = gourdAllowed;
    public readonly bool ImmobilizeAllowed = immobilizeAllowed;
    public readonly bool PhantomRushAllowed = phantomRushAllowed;
    public readonly bool ConsumablesAllowed = consumablesAllowed;
}