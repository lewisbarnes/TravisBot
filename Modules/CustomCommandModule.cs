using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace TravisBot
{
    public class CustomCommandModule : ModuleBase<SocketCommandContext>
    {
        private readonly IServiceProvider _services;
        private readonly CustomCommandService _customCommandService;
        public CustomCommandModule(IServiceProvider services)
        {
            _services = services;
            _customCommandService = _services.GetRequiredService<CustomCommandService>();
        }

        [Command("registercommand")]
        [Alias("rc")]
        public async Task RegisterCommandAsync(string command, params string[] output)
        {
            CommandServiceResponse response = _customCommandService.RegisterCommand(command, string.Join(' ', output), Context);

            Emoji reaction = response switch
            {
                CommandServiceResponse.Success => new Emoji("✅"),
                _ => new Emoji("❌")
            };

            await Context.Message.AddReactionAsync(reaction);
        }


        [Command("editcommand")]
        [Alias("ec")]
        public async Task EditCommandAsync(string command, params string[] output)
        {
            CommandServiceResponse response = _customCommandService.EditCommand(command, string.Join(' ', output), Context);

            Emoji reaction = response switch
            {
                CommandServiceResponse.Success => new Emoji("✅"),
                _ => new Emoji("❌")
            };

            await Context.Message.AddReactionAsync(reaction);
        }

        [Command("deletecommand")]
        [Alias("dc")]
        public async Task DeleteCommandAsync(string command)
        {
            CommandServiceResponse response = _customCommandService.DeleteCommand(command, Context);

            Emoji reaction = response switch
            {
                CommandServiceResponse.Success => new Emoji("✅"),
                _ => new Emoji("❌")
            };

            await Context.Message.AddReactionAsync(reaction);
        }
    }
}
