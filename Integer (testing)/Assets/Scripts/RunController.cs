public static class RunState
{
    public static int Seed { get; private set; }
    public static bool IsSeededRun { get; private set; }

    static System.Random rng;

    public static void Init(int seed, bool seededRun)
    {
        Seed = seed;
        IsSeededRun = seededRun;

        rng = new System.Random(seed);
    }

    public static int Range(int min, int max)
    {
        return rng.Next(min, max);
    }

    public static float Value()
    {
        return (float)rng.NextDouble();
    }
}