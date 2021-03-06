﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TravisBot
{

    public class DiscordSocketService : IHostedService
    {
        private readonly ILogger _logger;
        private readonly IHostApplicationLifetime _lifetime;
        private DiscordSocketClient _discordClient;
        private readonly string _botToken;
        private readonly IServiceProvider _services;
        private readonly CommandHandlingService _commandHandlingService;

        public DiscordSocketService(ILogger<DiscordSocketService> logger, IHostApplicationLifetime lifetime, IConfiguration configuration, IServiceProvider services)
        {
            _logger = logger;
            _lifetime = lifetime;
            _botToken = configuration.GetValue<string>("Token");
            _services = services;
            _commandHandlingService = _services.GetRequiredService<CommandHandlingService>();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _lifetime.ApplicationStarted.Register(OnStarted);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private void OnStarted()
        {
            _discordClient = _services.GetRequiredService<DiscordSocketClient>();

            _discordClient.Log += OnClientLog;
            _discordClient.MessageReceived += _commandHandlingService.MessageReceivedAsync;

            _discordClient.LoginAsync(Discord.TokenType.Bot, _botToken).Wait();
            _discordClient.StartAsync().Wait();
        }

        private Task OnClientLog(Discord.LogMessage logMessage)
        {
            LogLevel severity = logMessage.Severity.ToString() switch {
                "Critical" => LogLevel.Critical,
                "Warning" => LogLevel.Warning,
                "Info" => LogLevel.Information,
                "Verbose" => LogLevel.Information,
                "Debug" => LogLevel.Debug,
                "Error" => LogLevel.Error,
                _ => LogLevel.Information
            };

            _logger.Log(severity, $"{logMessage.Source.PadRight(15)}{logMessage.Message}");
            return Task.CompletedTask;
        }
    }
}
