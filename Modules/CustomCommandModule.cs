using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace TravisBot
{
    public class CustomCommandModule : ModuleBase<SocketCommandContext>
    {
        private IServiceProvider _services;
        private CustomCommandService _customCommandService;
        public CustomCommandModule(IServiceProvider services)
        {
            _services = services;
            _customCommandService = _services.GetRequiredService<CustomCommandService>();
        }

        [Command("registercommand")]
        [Alias("rc")]
        public async Task RegisterCommandAsync(string command, string output)
        {
           if(_customCommandService.RegisterCommand(command, output, Context.Guild))
            {
                await Task.CompletedTask;
            } else
            {
                await Context.Channel.SendMessageAsync("Command Already Registered!");
            }
        }
    }
}
