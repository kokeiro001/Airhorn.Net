namespace AirhornNet
{
    class Program
    {
        static void Main(string[] args)
            => new Airhorn().RunAndBlockAsync().GetAwaiter().GetResult();
    }
}
