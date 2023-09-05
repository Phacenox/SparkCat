
namespace SparkCat
{
    public static class Enums
    {
        public static SoundID QuickZap { get; private set; }
        public static SoundID NoDischarge { get; private set; }
        public static SoundID Recharge { get; private set; }
        

        internal static void Initialize()
        {
            QuickZap = new SoundID("QuickZap", true);
            NoDischarge = new SoundID("NoDischarge", true);
            Recharge = new SoundID("Recharge", true);
        }
    }
}
