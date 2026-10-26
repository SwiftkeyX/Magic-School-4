namespace MagicSchool.Core.Benchmark
{
    // speed: fixed steps, no frame cap, nothing drawn to increase render speed (See BenchmarkSpeed)
    public partial class HeroBenchmark
    {
        private readonly BenchmarkSpeed _speed = new BenchmarkSpeed();

        private void GoFast() => _speed.GoFast(TimeStep);
        private void GoNormal() => _speed.GoNormal();

        private void OnDestroy() => GoNormal();
    }
}
