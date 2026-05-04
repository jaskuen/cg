namespace Lab6.SeaBattle.Game;

internal static class SoundEffects
{
    public static void Shot() => Beep(720, 55);
    public static void Explosion() => Beep(180, 120);
    public static void Miss() => Beep(260, 80);
    public static void Cooldown() => Beep(460, 35);

    private static void Beep(int frequency, int durationMs)
    {
        Task.Run(() =>
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    Console.Beep(frequency, durationMs);
                }
            }
            catch
            {
                // Sound is a bonus feature; gameplay should continue if the host cannot beep.
            }
        });
    }
}
