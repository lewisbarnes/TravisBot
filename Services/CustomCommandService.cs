using System;
using System.IO;
using System.Linq;
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
        private readonly CommandService _commandService;
        public CustomCommandService(IServiceProvider services)
        {
            _commandService = services.GetRequiredService<CommandService>();
        }

        public async Task<bool> ExecuteCustomCommand(ICommandContext context, IServiceProvider services)
        {
            string commandText = context.Message.Content[1..];

            CustomCommandObject command = FindCommand(commandText, context);
            if (command == null) return false;

            await context.Channel.SendMessageAsync(command.Output);
            return true;
        }

        public CustomCommandObject FindCommand(string command, ICommandContext context)
        {
            CustomCommandObject ret = null;

            using LiteDatabase db = new LiteDatabase(Path.Combine("db", $"{context.Guild.Id}.db"));

            ILiteCollection<CustomCommandObject> collection = db.GetCollection<CustomCommandObject>("customCommands");

            if (collection.Count() == 0) return ret;

            if (collection.Query().Where(x => x.Command == command).Exists())
            {
                ret = collection.Query().Where(x => x.Command == command).First();
            }

            return ret;
        }

        public CommandServiceResponse RegisterCommand(string command, string output, ICommandContext context)
        {
            if (_commandService.Commands.Any(x => x.Name == command)) return CommandServiceResponse.AlreadyExists;

            using LiteDatabase db = new LiteDatabase(Path.Combine("db", $"{context.Guild.Id}.db"));

            ILiteCollection<CustomCommandObject> collection = db.GetCollection<CustomCommandObject>("customCommands");

            if (collection.Query().Where(x => x.Command == command).Exists()) return CommandServiceResponse.AlreadyExists;

            CustomCommandObject commandObject = new CustomCommandObject { Command = command, Output = output, RegisteredBy = context.User.Id };

            collection.Insert(commandObject);

            collection.EnsureIndex(x => x.Command);

            return CommandServiceResponse.Success;
        }

        public CommandServiceResponse EditCommand(string command, string output, ICommandContext context)
        {
            if (_commandService.Commands.Any(x => x.Name == command)) return CommandServiceResponse.NotOwner;

            using LiteDatabase db = new LiteDatabase(Path.Combine("db", $"{context.Guild.Id}.db"));

            ILiteCollection<CustomCommandObject> collection = db.GetCollection<CustomCommandObject>("customCommands");

            if (!collection.Query().Where(x => x.Command == command).Exists()) return CommandServiceResponse.NotFound;

            CustomCommandObject commandObject = collection.Query().Where(x => x.Command == command).First();

            if (commandObject.RegisteredBy != context.User.Id) return CommandServiceResponse.NotOwner;

            commandObject.Output = output;

            collection.Update(commandObject);

            collection.EnsureIndex(x => x.Command);

            return CommandServiceResponse.Success;
        }

        public CommandServiceResponse DeleteCommand(string command, ICommandContext context)
        {
            if (_commandService.Commands.Any(x => x.Name == command)) return CommandServiceResponse.NotOwner;

            using LiteDatabase db = new LiteDatabase(Path.Combine("db",$"{context.Guild.Id}.db"));

            ILiteCollection<CustomCommandObject> collection = db.GetCollection<CustomCommandObject>("customCommands");

            if (!collection.Query().Where(x => x.Command == command).Exists()) return CommandServiceResponse.NotFound;

            CustomCommandObject commandObject = collection.Query().Where(x => x.Command == command).First();

            if (commandObject.RegisteredBy != context.User.Id) return CommandServiceResponse.NotOwner;

            collection.Delete(commandObject.ID);

            collection.EnsureIndex(x => x.Command);

            return CommandServiceResponse.Success;
        }
    }

    public class CustomCommandObject
    {
        public int ID { get; set; }
        public string Command { get; set; }
        public string Output { get; set; }
        public ulong RegisteredBy { get; set; }
    }

    public enum CommandServiceResponse
    {
        NotFound = 0,
        Found = 1,
        NotOwner = 2,
        AlreadyExists = 3,
        Success = 4
    }
}
