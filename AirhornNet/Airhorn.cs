using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;
using AirhornNet.Services;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using log4net;
using log4net.Repository.Hierarchy;
using Microsoft.Extensions.DependencyInjection;


namespace AirhornNet
{
    class Airhorn
    {
        static readonly string BotUserToken = @"<your bot token>";

        DiscordSocketClient discordSocketClient;
        ILog log = LogManager.GetLogger(typeof(Airhorn));

        readonly CommandService commandService = new CommandService();
        IServiceProvider serviceProvider;

        public async Task RunAndBlockAsync()
        {
            discordSocketClient = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info,
            });

            InitLogger();

            try
            {
                await InitCommands();

                // ボットとしてログインする
                await discordSocketClient.LoginAsync(TokenType.Bot, BotUserToken);
                await discordSocketClient.StartAsync();

                // ずっと待つ
                await Task.Delay(-1);
            }
            catch (Exception e)
            {
                log.Error(e.Message, e);
            }
        }

        private void InitLogger()
        {
            XmlDocument log4netConfig = new XmlDocument();
            log4netConfig.Load(File.OpenRead("log4net.config"));
            var repo = LogManager.CreateRepository(Assembly.GetEntryAssembly(), typeof(Hierarchy));
            log4net.Config.XmlConfigurator.Configure(repo, log4netConfig["log4net"]);

            discordSocketClient.Log += msg =>
            {
                switch (msg.Severity)
                {
                    case LogSeverity.Critical:
                        log.Fatal(msg.Message, msg.Exception);
                        break;
                    case LogSeverity.Error:
                        log.Error(msg.Message, msg.Exception);
                        break;
                    case LogSeverity.Warning:
                        log.Warn(msg.Message, msg.Exception);
                        break;
                    case LogSeverity.Info:
                        log.Info(msg.Message, msg.Exception);
                        break;
                    case LogSeverity.Verbose:
                    case LogSeverity.Debug:
                        log.Debug(msg.Message, msg.Exception);
                        break;
                    default:
                        log.Info(msg.Message, msg.Exception);
                        break;
                }
                return Task.CompletedTask;
            };
        }

        private async Task InitCommands()
        {
            log.Info("InitCommands");

            // DIコンテナに音声再生用のサービスを登録する
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(new AudioService(log));
            serviceCollection.AddSingleton(log);

            // ModuleBaseを継承しているpublicなモジュールクラスを追加する
            await commandService.AddModulesAsync(Assembly.GetEntryAssembly());

            serviceProvider = serviceCollection.BuildServiceProvider();
            discordSocketClient.MessageReceived += HandleCommandAsync;
        }

        private async Task HandleCommandAsync(SocketMessage arg)
        {
            var msg = arg as SocketUserMessage;

            // メッセージが「!」から始まらない場合、処理しない
            if (msg?.Content?.StartsWith("!") != true) return;

            var context = new SocketCommandContext(discordSocketClient, msg);
            var result = await commandService.ExecuteAsync(context, 0, serviceProvider);

            // コマンドの実行に失敗したらエラーメッセージをチャンネルに流す
            if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
            {
                await msg.Channel.SendMessageAsync(result.ErrorReason);
            }
        }
    }
}
