﻿using MiniLauncher.Data;
using MiniLauncher.Helper;
using MiniLauncher.Network;
using MiniLauncher.Network.Packets;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace MiniLauncher
{
    public partial class Main : Form
    {
        private NetworkClient networkClient;
        public Main()
        {
            InitializeComponent();
            InitializeNetwork();
        }

        private void InitializeNetwork()
        {
            var serverCfg = LauncherConfig.GetInstance.ServerConfig;
            networkClient = new NetworkClient(serverCfg.LogginAddress.Split(':')[0], int.Parse(serverCfg.LogginAddress.Split(':')[1]));
            networkClient.OnError += NetworkClient_OnError;
            networkClient.OnConnected += NetworkClient_OnConnected;
            networkClient.ClientEvents += NetworkClient_ClientEvents;
            (new Thread(() => {
                networkClient.StartClient();
            })).Start();
        }

        private void NetworkClient_OnError(object sender, EventArgs e)
        {
            ChangeStatus(false);
        }

        private void NetworkClient_OnConnected(object sender, EventArgs e)
        {
            ChangeStatus(true);
        }

        private void NetworkClient_ClientEvents(object sender, NetworkClientEventArgs e)
        {
            switch (e.CState)
            {
                case NetworkClientEventArgs.Callback.CRYPTO_KEY_INFORM:
                    EnableLoginBtn(true);
                    break;
                case NetworkClientEventArgs.Callback.LOGIN_ACCOUNT_WRONG_LOGIN:
                    EnableLoginBtn(true);
                    MessageBox.Show("Неправильный логин!", "Ошибка!");
                    break;
                case NetworkClientEventArgs.Callback.LOGIN_ACCOUNT_WRONG_PW:
                    EnableLoginBtn(true);
                    MessageBox.Show("Неправильный пароль!", "Ошибка!");
                    break;
                case NetworkClientEventArgs.Callback.LOGIN_ACCOUNT_SERVER_CLOSED:
                    EnableLoginBtn(true);
                    MessageBox.Show("На сервере проводятся технические работы. Повторите попытку позже!", "Ошибка!");
                    break;
                case NetworkClientEventArgs.Callback.LOGIN_ACCOUNT_BANNED:
                    MessageBox.Show("Аккаунт заблокирован!", "Ошибка!");
                    break;
                case NetworkClientEventArgs.Callback.SERVER_LIST_INFORM:
                    FillServerList(e.Servers);
                    break;
                case NetworkClientEventArgs.Callback.SERVER_SESSION_RESULT:
                    RunGame(e.DefaultSet);
                    break;
            }
        }
        private void login_btn_Click(object sender, EventArgs e)
        {
            if(!string.IsNullOrEmpty(login_input.Text) && !string.IsNullOrEmpty(password_input.Text) ||
               login_input.Text.Length <= 13 && password_input.Text.Length <= 13)
            {
                networkClient.DoLogin(login_input.Text, password_input.Text);
                EnableLoginBtn(false);
            }
            else
            {
                MessageBox.Show("Поля логин и пароль должны быть заполнены и не превышать 13 символов!", "Ошибка!");
            }
        }

        private void ChangeStatus(bool ok)
        {
            Invoke(new Action(() => {
                status_label.Text = ok ? "ПОДКЛЮЧЕН" : "ОТКЛЮЧЕН";
            }));
        }
        private void EnableLoginBtn(bool state)
        {
            Invoke(new Action(() => {
                login_btn.Enabled = state;
            }));
        }
        private void FillServerList(List<ServerState> _serverList)
        {
            Invoke(new Action(() => {
                server_list.Enabled = true;
                foreach (var server in _serverList)
                {
                    string serverStatus = server.b_ServerState == 1 ? "Открыт" : "Закрыт";
                    server_list.Items.Add(new ListViewItem(new[] { server.s_ServerName, serverStatus }));
                }
            }));
        }

        private void server_list_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (server_list.SelectedItems.Count > 0)
            {
                var item = server_list.SelectedItems[0];
                networkClient.SelectWordlRequest((short)item.Index);
                server_list.Enabled = false;
            }
        }

        private void RunGame(Default_Set defaultSet)
        {
            var clientCfg = LauncherConfig.GetInstance.ClientConfig;
            ClientRunHelper.WriteTmp(clientCfg.DefaultSetTmpPath, defaultSet);
            ClientRunHelper.RunClient(clientCfg.ClientBinaryPath);
            networkClient.StopListen();
            Environment.Exit(0);
        }

        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            networkClient.StopListen();
        }
    }
}
