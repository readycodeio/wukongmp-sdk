namespace WukongApi;

public readonly struct RoomCreationOptions(
    int tournamentRounds,
    bool gourdAllowed,
    bool immobilizeAllowed,
    bool phantomRushAllowed
)
{
    public readonly int TournamentRounds = tournamentRounds;
    public readonly bool GourdAllowed = gourdAllowed;
    public readonly bool ImmobilizeAllowed = immobilizeAllowed;
    public readonly bool PhantomRushAllowed = phantomRushAllowed;
}