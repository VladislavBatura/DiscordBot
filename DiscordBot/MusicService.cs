using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CliWrap;
using Discord.Audio;
using Discord.Interactions;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace DiscordBot
{
    public class MusicService
    {
        private readonly Storage _storage;
        private readonly YoutubeClient _client;

        public MusicService(Storage storage, YoutubeClient client)
        {
            _storage = storage;
            _client = client;
        }

        public async Task PlayMusicAsync(SocketInteractionContext context, IAudioClient audioClient)
        {
            _storage.AddChannel(context.User.Id, audioClient);

            StreamManifest streamManifest = new(null);

            try
            {
                streamManifest = await _client.Videos.Streams.GetManifestAsync(_storage.url);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                _ = context.Channel.SendMessageAsync("Something went wrong...");
            }

            if (!streamManifest.Streams.Any())
            {
                _ = context.Channel.SendMessageAsync("Can't find any music source");
                return;
            }

            var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
            if (_storage.inputStream == null)
            {
                var stream = await _client.Videos.Streams.GetAsync(streamInfo);
                _storage.inputStream = stream;
            }
            if (_storage.outputStream == null)
            {
                _storage.outputStream = new MemoryStream();
            }

            _ = await Cli.Wrap("ffmpeg")
                .WithArguments(" -hide_banner -loglevel panic -i pipe:0 -ac 2 -f s16le -ar 48000 pipe:1")
                .WithStandardInputPipe(PipeSource.FromStream(_storage.inputStream))
                .WithStandardOutputPipe(PipeTarget.ToStream(_storage.outputStream))
                .ExecuteAsync();

            using var discord = audioClient.CreatePCMStream(AudioApplication.Mixed);
            try
            {
                await discord.WriteAsync((_storage.outputStream as MemoryStream)
                    .ToArray()
                    .AsMemory(0, (int)(_storage.outputStream as MemoryStream).Length));
            }
            finally
            {
                await discord.FlushAsync();
            }
        }
    }
}
