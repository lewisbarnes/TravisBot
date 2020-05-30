using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace TravisBot
{
    public class CommandHandlingService
    {
        private readonly IServiceProvider _services;
        private readonly IConfiguration _configuration;
        private readonly CommandService _commandService;
        private readonly DiscordSocketClient _client;
        private readonly CustomCommandService _customCommandService;
        private string _commandPrefix;
        private ILogger _logger;

        public CommandHandlingService(IServiceProvider services)
        {
            _services = services;
            _logger = _services.GetRequiredService<ILogger<CommandHandlingService>>();

            _configuration = _services.GetRequiredService<IConfiguration>();
            _customCommandService = _services.GetRequiredService<CustomCommandService>();
            
            _commandService = _services.GetRequiredService<CommandService>();

            _commandPrefix = _configuration.GetValue<string>("prefix");

            _commandService.AddModulesAsync(Assembly.GetEntryAssembly(), _services).Wait();
            _commandService.CommandExecuted += CommandExecutedAsync;
        }

        public async Task MessageReceivedAsync(SocketMessage rawMessage)
        {
            try
            {
                if (!(rawMessage is SocketUserMessage message)) return;
                if (message.Author.IsBot) return;

                int argPos = 0;

                if (!message.HasCharPrefix(_commandPrefix[0], ref argPos)) return;

                var context = new SocketCommandContext(_client, message);

                await _commandService.ExecuteAsync(context, argPos, _services);

            } catch (Exception e)
            {
                _logger.LogError(e.Message);
            }
        }

        public async Task CommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {

            if (!command.IsSpecified)
                if (await _customCommandService.ExecuteCustomCommand(context, _services)) return;

            if (result.IsSuccess)
                return;

            _logger.LogWarning($"{context.User.Id}/{context.User.Username}: {result.ToString()}");

            await context.Channel.SendMessageAsync($"error: {result}");
        }
    }
}
