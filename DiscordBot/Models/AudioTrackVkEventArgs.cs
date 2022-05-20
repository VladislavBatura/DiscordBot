namespace DiscordBot.Models
{
    public class AudioTrackVkEventArgs : EventArgs, IDisposable
    {
        public AudioTrackVk? Audio { get; set; }

        public void Dispose()
        {
            Audio?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
