using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CliWrap;
using Discord.Audio;
using Discord.Interactions;
using Discord.WebSocket;
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

        public async Task PlayMusicAsync(SocketInteractionContext context,
            IAudioClient audioClient)
        {
            //_storage.AddChannel(context.User.Id, audioClient);

            StreamManifest streamManifest = new(null);

            try
            {
                streamManifest = await _client.Videos.Streams.GetManifestAsync(_storage.Url);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                _ = await context.Channel.SendMessageAsync("Something went wrong...");
                return;
            }

            if (!streamManifest.Streams.Any())
            {
                _ = await context.Channel.SendMessageAsync("Can't find any music source");
                return;
            }

            var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
            if (_storage.InputStream == null)
            {
                var stream = await _client.Videos.Streams.GetAsync(streamInfo);
                _storage.InputStream = stream;
            }
            if (_storage.OutputStream == null)
            {
                _storage.OutputStream = new MemoryStream();
            }

            _ = await Cli.Wrap("ffmpeg")
                .WithArguments("-hide_banner -loglevel panic -i pipe:0 -ac 2 -f s16le -ar 48000 pipe:1")
                .WithStandardInputPipe(PipeSource.FromStream(_storage.InputStream))
                .WithStandardOutputPipe(PipeTarget.ToStream(_storage.OutputStream))
                .ExecuteAsync();

            using var discord = audioClient.CreatePCMStream(AudioApplication.Mixed);
            try
            {
                await discord.WriteAsync((_storage.OutputStream as MemoryStream)
                    .ToArray()
                    .AsMemory(0, (int)(_storage.OutputStream as MemoryStream).Length));
            }
            finally
            {
                await discord.FlushAsync();
            }
        }

        public async Task PlayMusicAsync(SocketMessageComponent context, string url)
        {
            if (!_storage.ChannelExist(context.User.Id))
            {
                _ = await context.Channel.SendMessageAsync("Зайди в голосовой канал");
                return;
            }

            var audioClient = _storage.GetChannel(context.User.Id);

            StreamManifest streamManifest = new(null);

            try
            {
                streamManifest = await _client.Videos.Streams.GetManifestAsync(url);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                _ = await context.Channel.SendMessageAsync("Something went wrong...");
                return;
            }

            if (!streamManifest.Streams.Any())
            {
                _ = await context.Channel.SendMessageAsync("Can't find any music source");
                return;
            }

            var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
            if (_storage.InputStream == null)
            {
                var stream = await _client.Videos.Streams.GetAsync(streamInfo);
                _storage.InputStream = stream;
            }
            if (_storage.OutputStream == null)
            {
                _storage.OutputStream = new MemoryStream();
            }

            _ = await Cli.Wrap("ffmpeg")
                .WithArguments("-hide_banner -loglevel panic -i pipe:0 -ac 2 -f s16le -ar 48000 pipe:1")
                .WithStandardInputPipe(PipeSource.FromStream(_storage.InputStream))
                .WithStandardOutputPipe(PipeTarget.ToStream(_storage.OutputStream))
                .ExecuteAsync();

            using var discord = audioClient.CreatePCMStream(AudioApplication.Mixed);
            try
            {
                await discord.WriteAsync((_storage.OutputStream as MemoryStream)
                    .ToArray()
                    .AsMemory(0, (int)(_storage.OutputStream as MemoryStream).Length));
            }
            finally
            {
                await discord.FlushAsync();
            }
        }
    }
}
