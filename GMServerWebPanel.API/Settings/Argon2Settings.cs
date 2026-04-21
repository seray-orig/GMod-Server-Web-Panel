namespace GMServerWebPanel.API.Settings
{
    public class Argon2Settings
    {
        public int MemorySize
        {
            get => field;
            // Ограничение Argon2 на минимум памяти >= 4.
            set => field = value > 3 ? value : 64 * 1024;
        }
        public int Iterations
        {
            get => field;
            set => field = value > 0 ? value : 4;
        }
        public int DegreeOfParallelism
        {
            get => field;
            set => field = value > 0 ? value : Environment.ProcessorCount;
        }
        public int HashLength { get; set; } = 32;
        public int SaltLength { get; set; } = 16;
    }

}
