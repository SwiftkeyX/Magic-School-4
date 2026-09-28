namespace MagicSchool.Contracts
{
    // Oneliner that will be shown in IScoreboard: 
    // e.g. hero's name, team, damage dealt, damage taken, etc...
    public readonly struct ScoreRow
    {
        public readonly string Name;
        public readonly TeamEnum Team;
        public readonly bool IsAlive;   // FLAGGING: I don't like it here, let leave it for now.
        public readonly ICombatRecord Record;

        public ScoreRow(string name, TeamEnum team, bool isAlive, ICombatRecord record)
        {
            Name = name;
            Team = team;
            IsAlive = isAlive;
            Record = record;
        }
    }
}
