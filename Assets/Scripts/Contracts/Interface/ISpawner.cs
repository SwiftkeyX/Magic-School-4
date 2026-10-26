namespace MagicSchool.Contracts
{
    // ISpawner answers: can this unit put new units on the board next to itself?
    // e.g.     Big Slime splits into Small Slimes when it dies
    public interface ISpawner
    {
        int SpawnUnitsNearby(int count);
    }
}
