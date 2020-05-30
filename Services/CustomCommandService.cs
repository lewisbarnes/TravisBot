using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using LiteDB;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TravisBot
{
    public class CustomCommandService
    {
        private CommandService _commandService;
        public CustomCommandService(IConfiguration configuration, IServiceProvider services)
        {
            _commandService = services.GetRequiredService<CommandService>();
        }

        public async Task<bool> ExecuteCustomCommand(ICommandContext context, IServiceProvider services)
        {
            string commandText = context.Message.Content[1..];

            CustomCommandObject command = FindCommand(commandText, ((SocketCommandContext)context).Guild);
            if (command == null) return false;

            await context.Channel.SendMessageAsync(command.Output);
            return true;
        }

        public CustomCommandObject FindCommand(string command, SocketGuild guild)
        {
            CustomCommandObject ret = null;
            using (LiteDatabase db = new LiteDatabase(@$"db\{guild.Id.ToString()}.db"))
            {
                ILiteCollection<CustomCommandObject> collection = db.GetCollection<CustomCommandObject>("customCommands");

                if(collection.Count() > 0)
                {
                    if (collection.Query().Where(x => x.Command == command).Exists())
                    {
                        ret = collection.Query().Where(x => x.Command == command).First();
                    }
                }
            }

            return ret;
        }

        public bool RegisterCommand(string command, string output, SocketGuild guild)
        {
            if (_commandService.Commands.Any(x => x.Name == command)) return false;

            using (LiteDatabase db = new LiteDatabase(@$"db\{guild.Id.ToString()}.db"))
            {
                ILiteCollection<CustomCommandObject> collection = db.GetCollection<CustomCommandObject>("customCommands");

                if (collection.Query().Where(x => x.Command == command).Exists()) return false;

                CustomCommandObject commandObject = new CustomCommandObject { Command = command, Output = output };

                collection.Insert(commandObject);

                collection.EnsureIndex(x => x.Command);

            }

            return true;
        }
    }

    public class CustomCommandObject
    {
        public string Command { get; set; }
        public string Output { get; set; }
    }
}
