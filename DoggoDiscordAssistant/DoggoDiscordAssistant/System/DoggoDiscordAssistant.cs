﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace DoggoDiscordAssistant
{
    class DoggoDiscordAssistant : DiscordClient
    {
        string BotToken = null;
        public bool Debug { get; set; } = true;
        bool ServerConnection = false;
        public List<Server> servers { get; } = new List<Server>();

        public DoggoDiscordAssistant(string token, Action<DiscordConfigBuilder> configFunc) : base (configFunc)
        {
            Logging.consoleLog("Retrieving Token...", Logging.logType.System);
            BotToken = token;
            Logging.consoleBreak();
            Logging.consoleLog("Connecting to server...", Logging.logType.System);
            Connect(3, 1000);
            LoadServers();
            UserJoined += DoggoDiscordAssistant_UserJoined;
            MessageReceived += DoggoDiscordAssistant_MessageReceived;
            Logging.consoleBreak();
            Logging.consoleLog("DogGoDiscordAssistant is now online!", Logging.logType.System);
            Console.WriteLine();
            while (true)
            {
                AdminConsole.parseAdminInput(Console.ReadLine(), this);
            }
        }

        /// <summary>
        /// Attempts to connect the bot to Discords Server.
        /// </summary>
        /// <param name="maxTries">The amount of tries the bot will attempt to connect to the server</param>
        /// <param name="timeout">The amount of time in milliseconds the bot will make in between each attempt</param>
        private void Connect(int maxTries, int timeout)
        {
            for (int tries = 0; tries < maxTries; tries++)
            {
                try
                {
                    Logging.consoleLog("attempting connection...[" + (tries + 1) + "/3]", Logging.logType.System);
                    Task task = Task.Run(async () => await Connect(BotToken, TokenType.Bot));
                    task.Wait();
                    Logging.consoleLog("Connected to server!", Logging.logType.System);
                    break;
                }
                catch (AggregateException e)
                {
                    Logging.consoleLog("Failed!", Logging.logType.Error);
                    foreach (Exception exception in e.InnerExceptions) Logging.consoleLog(exception.Message, Logging.logType.Error);
                    //If bot fails to connect to server, print error to screen and exit program
                    if (tries < (maxTries - 1))
                    {
                        System.Threading.Thread.Sleep(timeout);
                        Logging.consoleBreak();
                    }
                    else
                    {
                        Logging.consoleBreak();
                        Logging.consoleLog("Could not connect to server!", Logging.logType.Error);
                        Console.ReadLine();
                        Environment.Exit(0);
                    }
                }
            }
        }

        /// <summary>
        /// Sets up event handling for servers, then finds and loads all servers that the bot is currently connected to
        /// </summary>
        private void LoadServers()
        {
            Logging.consoleBreak();
            Logging.consoleLog("Setting up server handlers...", Logging.logType.System);
            ServerAvailable += ((s, e) => ServerConnection = true);
            ServerUnavailable += ((s, e) => ServerConnection = false);
            Logging.consoleLog("Sniffing out servers...", Logging.logType.System);
            System.Threading.SpinWait.SpinUntil(() => ServerConnection);
            foreach(Discord.Server discordserver in Servers)
            {
                Server server = new Server(discordserver);
                servers.Add(server);
                Logging.consoleLog("Server Found! Name: " + server.ServerAPI.Name + " ID: " + server.ServerAPI.Id, Logging.logType.System);
            }
            Logging.consoleLog("All servers loaded!", Logging.logType.System);
        }

        /// <summary>
        /// Finds the server from the list of servers the bot is tracking using the discord server ID
        /// </summary>
        /// <returns>returns the server matched with its ID</returns>
        private Server LocateServer(ulong ID)
        {
            Server returnedserver = null;
            if (Debug == true)
            {
                Logging.consoleBreak();
                Logging.consoleLog("locating server with ID: " + ID, Logging.logType.Debug);
            }
            foreach(Server server in servers)
            {
                if (server.ServerAPI.Id == ID)
                {
                    if (Debug == true) Logging.consoleLog("Located Server! Server Name: " + server.ServerAPI.Name, Logging.logType.Debug);
                    returnedserver = server;
                }
            }
            if (Debug == true && returnedserver == null) Logging.consoleLog("No server found! returning null", Logging.logType.Debug);
            return returnedserver;
        }

        private void DoggoDiscordAssistant_UserJoined(object sender, UserEventArgs e)
        {
            Server server = LocateServer(e.Server.Id);
            Task.Factory.StartNew(() => CommandEngine.GreetUser(server));
        }

        private void DoggoDiscordAssistant_MessageReceived(object sender, MessageEventArgs e)
        {
            if(e.Message.RawText != "" && e.Message.RawText[0] == '>' && !e.User.IsBot)
            {
                Server server = LocateServer(e.Server.Id);
                Task.Factory.StartNew(() => CommandEngine.parseInput(this, server, e.Channel, e.User, e.Message.Text));
            }
        }
    }
}
