using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Media;
using AutoDice.Sites;
using ControlzEx.Theming;
using MahApps.Metro.Controls.Dialogs;

namespace AutoDice
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class Login
    {
        private readonly IniParser _parser;

        private DiceSite currentSite;
        private string username, password, twofactor;

        private GenericCheck checkLogin;
        private GenericBalance checkBalance;

        readonly BackgroundWorker _login = new BackgroundWorker();

        public Login()
        {
            InitializeComponent();
            FillComboBox();
            IniRead();
            _parser = new IniParser("config.ini");
            _login.DoWork += worker_DoWork;
            _login.RunWorkerCompleted += worker_RunWorkerCompleted;
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            if (txtUsername.Text == string.Empty)
            {
                lblStatus.Content = "Please enter your Username";
                return;
            }
            if (txtPassword.Password == string.Empty)
            {
                lblStatus.Content = "Please enter your Password";
                return;
            }
            currentSite = ((SiteList)cmbSite.SelectedItem).site;
            username = txtUsername.Text;
            password = txtPassword.Password;
            twofactor = txt2fa.Text;

            prgLoginProgress.IsIndeterminate = true;

            btnLogin.IsEnabled = false;
            chkLogin.IsEnabled = false;
            txtUsername.IsEnabled = false;
            txtPassword.IsEnabled = false;
            lblStatus.Content = "Attempting to login...";
            lblStatus.Foreground = Brushes.Orange;
            _login.RunWorkerAsync();

        }

        #region LoginWorker
        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            checkLogin = currentSite.Login(username, password, twofactor);
            if (checkLogin.status)
            {
                checkBalance = currentSite.Balance();
            }
        }
        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            prgLoginProgress.IsIndeterminate = false;

            if (checkLogin.status)
            {
                if (checkBalance.status)
                {
                    if ((bool)chkLogin.IsChecked)
                    {
                        _parser.AddSetting("AUTODICE", "USERNAME", txtUsername.Text);
                        _parser.AddSetting("AUTODICE", "PASSWORD", txtPassword.Password);
                        _parser.AddSetting("AUTODICE", "2FA", txt2fa.Text);
                        _parser.SaveSettings();
                    }
                    _parser.AddSetting("AUTODICE", "WEBSITE", cmbSite.SelectedIndex.ToString());
                    _parser.SaveSettings();

                    new ModeNormal(currentSite, checkLogin.username, checkBalance.balance) { Visibility = Visibility.Visible };
                    Close();
                }
                else
                {
                    lblStatus.Content = checkBalance.error;
                }
            }
            else
            {
                lblStatus.Content = checkLogin.error;
            }
            lblStatus.Foreground = Brushes.Black;
            btnLogin.IsEnabled = true;
            chkLogin.IsEnabled = true;
            txtUsername.IsEnabled = true;
            txtPassword.IsEnabled = true;
        }
        #endregion

        private void IniRead()
        {
            while (true)
            {
                if (File.Exists("config.ini"))
                {
                    using (var reader = new StreamReader("config.ini"))
                    {
                        if (!reader.ReadToEnd().Contains("DELAYENABLED"))
                        {
                            reader.Close();
                            var parserFix = new IniParser("config.ini");
                            parserFix.AddSetting("AUTODICE", "DELAYENABLED", "False");
                            parserFix.AddSetting("AUTODICE", "DELAYTIME", "10");
                            parserFix.SaveSettings();
                        }
                    }
                    var parserStart = new IniParser("config.ini");
                    
                    chkLogin.IsChecked = bool.Parse(parserStart.GetSetting("AUTODICE", "SAVELOGINDATA"));
                    if ((bool) chkLogin.IsChecked)
                    {
                        txtUsername.Text = parserStart.GetSetting("AUTODICE", "USERNAME");
                        txtPassword.Password = parserStart.GetSetting("AUTODICE", "PASSWORD");
                        txt2fa.Text = parserStart.GetSetting("AUTODICE", "2FA");
                    }
                    cmbSite.SelectedIndex = int.Parse(parserStart.GetSetting("AUTODICE", "WEBSITE"));
                    ChangeAppStyle(int.Parse(parserStart.GetSetting("AUTODICE", "WINDOWTHEME")), int.Parse(parserStart.GetSetting("AUTODICE", "WINDOWCOLOR")));
                }
                else
                {
                    using (var writer = new StreamWriter("config.ini"))
                    {
                        writer.WriteLine("[AUTODICE]");
                        writer.WriteLine("SAVELOGINDATA=False");
                        writer.WriteLine("USERNAME=");
                        writer.WriteLine("PASSWORD=");
                        writer.WriteLine("2FA=");
                        writer.WriteLine("WINDOWTHEME=0");
                        writer.WriteLine("WINDOWCOLOR=1");
                        writer.WriteLine("WEBSITE=0");
                        writer.WriteLine("DELAYENABLED=False");
                        writer.WriteLine("DELAYTIME=10");
                    }
                    cmbSite.SelectedIndex = 0;
                    continue;
                }
                break;
            }
        }

        #region Change App Style
        private static void ChangeAppStyle(int a, int b)
        {
            string accent;
            var theme = a == 0 ? "Light" : "Dark";
            switch (b)
            {
                case 0:
                    accent = "Blue";
                    break;
                case 1:
                    accent = "Red";
                    break;
                case 2:
                    accent = "Green";
                    break;
                case 3:
                    accent = "Purple";
                    break;
                case 4:
                    accent = "Orange";
                    break;
                case 5:
                    accent = "Lime";
                    break;
                case 6:
                    accent = "Emerald";
                    break;
                case 7:
                    accent = "Teal";
                    break;
                case 8:
                    accent = "Cyan";
                    break;
                case 9:
                    accent = "Cobalt";
                    break;
                case 10:
                    accent = "Indigo";
                    break;
                case 11:
                    accent = "Violet";
                    break;
                case 12:
                    accent = "Pink";
                    break;
                case 13:
                    accent = "Magenta";
                    break;
                case 14:
                    accent = "Crimson";
                    break;
                case 15:
                    accent = "Amber";
                    break;
                case 16:
                    accent = "Yellow";
                    break;
                case 17:
                    accent = "Brown";
                    break;
                case 18:
                    accent = "Olive";
                    break;
                case 19:
                    accent = "Steel";
                    break;
                case 20:
                    accent = "Mauve";
                    break;
                case 21:
                    accent = "Taupe";
                    break;
                case 22:
                    accent = "Sienna";
                    break;
                default:
                    accent = "Blue";
                    break;
            }

            ThemeManager.Current.ChangeTheme(Application.Current, $"{theme}.{accent}");
        }
        #endregion
        #region Event if user click on Save Login Info
        private void chkLogin_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)chkLogin.IsChecked)
            {
                _parser.AddSetting("AUTODICE", "SAVELOGINDATA", "True");
                _parser.SaveSettings();
            }
            else
            {
                _parser.AddSetting("AUTODICE", "SAVELOGINDATA", "False");
                _parser.AddSetting("AUTODICE", "USERNAME", "");
                _parser.AddSetting("AUTODICE", "PASSWORD", "");
                _parser.SaveSettings();
            }
        }
        #endregion

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            RenamePasswordLabel();
            AutoDiceServer();
        }

        private static string GetSHAHashFromFile(string fileName)
        {
            using (var sha = new SHA256Managed())
            {
                using (var stream = File.OpenRead(fileName))
                {
                    return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
                }
            }
        }
        private void FillComboBox()
        {
            var SitesList = new List<SiteList>
            {
                
                new SiteList
                {
                    name = "BetterBets - API Mode",
                    userlabel = "Username",
                    passlabel = "API Token",
                    site = new BetterBetsAPI()
                },
                new SiteList
                {
                    name = "Da Dice - Ajax Mode",
                    userlabel = "Username",
                    passlabel = "Password",
                    site = new DaDiceAjax()
                },
                new SiteList
                {
                    name = "Da Dice - API Mode",
                    userlabel = "Username",
                    passlabel = "API Key",
                    site = new DaDiceAPI()
                },
                new SiteList
                {
                    name = "PrimeDice - API Mode",
                    userlabel = "Username",
                    passlabel = "Password",
                    site = new PrimeDiceAPI()
                }
            };

            cmbSite.ItemsSource = SitesList;
        }
        private void RenamePasswordLabel()
        {
            lblUsername.Content = $"{((SiteList) cmbSite.SelectedItem).userlabel}:";
            lblPassword.Content = $"{((SiteList) cmbSite.SelectedItem).passlabel}:";
        }

        private void cmbSite_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            RenamePasswordLabel();
        }

        #region AutoDice Server Checks
        private readonly BackgroundWorker _workerSharpServer = new BackgroundWorker();
        private async void AutoDiceServer()
        {
            _workerSharpServer.DoWork += SharpServerDoWork;
            _workerSharpServer.RunWorkerCompleted += SharpServerCompleted;
            _controller = await this.ShowProgressAsync("Please wait", "Syncing with AutoDice server...");
            _workerSharpServer.RunWorkerAsync();
        }

        // AutoDice Server Validation
        private ProgressDialogController _controller;
        private string _currentVersion;
        private string _sha256;

        #region DoWork
        private void SharpServerDoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                using (var web = new WebClient())
                {
                    _currentVersion = web.DownloadString("http://www.autodice.net/internal/request?getcurrentversion");
                    _sha256 = web.DownloadString(
                        $"http://www.autodice.net/internal/request?checksha256&version={Assembly.GetExecutingAssembly().GetName().Version}");
                }
            }
            catch
            {
                _currentVersion = "1.0.0.0";
                _sha256 = null;
            }
        }
        #endregion
        #region WorkerCompleted
        private async void SharpServerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            await _controller.CloseAsync();

            if (Assembly.GetExecutingAssembly().GetName().Version.CompareTo(new Version(_currentVersion)) < 0)
            {
                ShowUpdateMessage();
            }
            if (_sha256 == null)
            {
                lblStatus.Content = "Unable to verify app!";
                lblStatus.ToolTip = "The app was unable to verify this version at AutoDice server";
                lblStatus.Foreground = Brushes.Orange;
            }
            else if (!_sha256.ToLower().Equals(GetSHAHashFromFile(AppDomain.CurrentDomain.FriendlyName).ToLower()))
            {
                lblStatus.Content = "This is a recompiled version!";
                lblStatus.ToolTip = "This version is not compiled by me. Use at your own risk!";
                lblStatus.Foreground = Brushes.Red;
            }
        }
        private async void ShowUpdateMessage()
        {
            var mySettings = new MetroDialogSettings()
            {
                AffirmativeButtonText = "GET IT NOW",
                ColorScheme = MetroDialogOptions.ColorScheme
            };

            var result = await this.ShowMessageAsync("Update available", "There's a new version available!",
            MessageDialogStyle.Affirmative, mySettings);

            if (result == MessageDialogResult.Affirmative)
            {
                Process.Start("http://www.autodice.net/");
            }
        }
        #endregion
        #endregion

    }
    internal class SiteList
    {
        public string name { private get; set; }
        public DiceSite site { get; set; }
        public string userlabel { get; set; }
        public string passlabel { get; set; }
        public override string ToString() { return name; }
    }
}