using System.Media;
using NAudio.Wave;
using Plugin.SimpleAudioPlayer;

namespace Lab6.SeaBattle.Game;

internal static class SoundEffects
{
    private static string _shot = "torpedo_start.wav";
    private static string _explosion = "hit.wav";
    private static string _miss = "damage.wav";
    private static string _cooldown = "cooldown.wav";
    private static string _gameOver = "game_over.wav";

    public static void Shot() => PlaySound(_shot);
    public static void Explosion() => PlaySound(_explosion);
    public static void Miss() => PlaySound(_miss);
    public static void Cooldown() => PlaySound(_cooldown);
    public static void GameOver() => PlaySound(_gameOver);

    private static void PlaySound(string path)
    {
        Task.Run(() =>
        {
            try
            {
                WaveStream sound = new AudioFileReader($"Sounds/{path}");
                WaveOutEvent output = new WaveOutEvent();
                
                output.Init(sound);
                output.Play();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        });
    }
}