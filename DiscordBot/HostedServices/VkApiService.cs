using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Addons.Hosting;
using Discord.Addons.Hosting.Util;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VkNet;
using VkNet.Enums.Filters;
using VkNet.Model;

namespace DiscordBot.HostedServices
{
    public class VkApiService
    {
        private readonly ILogger<VkApiService> _logger;
        private readonly VkApi _vk;
        private readonly IConfiguration _config;
        private readonly Storage _storage;

        public VkApiService(ILogger<VkApiService> logger,
                            VkApi vk,
                            IConfiguration config,
                            Storage storage)
        {
            _logger = logger;
            _vk = vk;
            _config = config;
            _storage = storage;
        }

        public async Task<bool> EnableVk(string twoFactorCode)
        {
            try
            {
                var login = _config["VkLogin"];
                var password = _config["VkPassword"];
                await _vk.AuthorizeAsync(new ApiAuthParams
                {
                    ApplicationId = 1998,
                    TwoFactorSupported = true,
                    Login = login,
                    Password = password,
                    Settings = Settings.All,
                    TwoFactorAuthorization = () =>
                    {
                        return twoFactorCode;
                    }
                });
                _storage.IsVkEnabled = true;
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                _storage.IsVkEnabled = false;
            }

            return false;
        }
    }
}
