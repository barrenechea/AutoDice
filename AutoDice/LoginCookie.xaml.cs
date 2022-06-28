using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows;
using MahApps.Metro;
using MahApps.Metro.Controls.Dialogs;
using Newtonsoft.Json;

namespace DaDice
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class Login
    {
        private readonly IniParser _parser;

        public Login()
        {
            InitializeComponent();
            IniRead();
            _parser = new IniParser("config.ini");
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            if (txtUsername.Text == string.Empty)
            {
                MostrarDialogo("Error", "Please enter an Username");
                return;
            }
            else if (txtApiKey.Text == string.Empty)
            {
                MostrarDialogo("Error", "Please enter an API Key");
                return;
            }
            else if (txtApiKey.Text.Length < 64)
            {
                MostrarDialogo("Error", "Please enter a valid API Key");
                txtApiKey.Text = string.Empty;
                return;
            }
            string url = string.Format("https://dadice.com/api/balance?username={0}&key={1}", txtUsername.Text, txtApiKey.Text);
            Balance balance = _download_serialized_json_data<Balance>(url);
            if (balance.status)
            {
                if (chkLogin.IsChecked != null && (bool)chkLogin.IsChecked)
                {
                    _parser.AddSetting("DADICE", "USERNAME", txtUsername.Text);
                    _parser.AddSetting("DADICE", "APIKEY", txtApiKey.Text);
                    _parser.SaveSettings();
                }
                DaDiceMain main = new DaDiceMain(new LoginData(txtUsername.Text, txtApiKey.Text), balance.balance);
                main.Show();
                Close();
            }
            else
            {
                MostrarDialogo("Error", balance.error);
            }
        }

        public async void MostrarDialogo(string titulo, string mensaje)
        {
            await this.ShowMessageAsync(titulo, mensaje);
        }

        private static T _download_serialized_json_data<T>(string url) where T : new()
        {
            using (var w = new WebClient())
            {
                var jsonData = string.Empty;
                // attempt to download JSON data as a string
                try
                {
                    jsonData = w.DownloadString(url);
                }
                catch (Exception)
                {
                    // ignored
                }
                // if string with JSON data is not empty, deserialize it to class and return its instance
                return !string.IsNullOrEmpty(jsonData) ? JsonConvert.DeserializeObject<T>(jsonData) : new T();
            }
        }

        private void btnRegister_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://www.dadice.com/?referrer=sbarrenechea");
        }

        private void btnApiKey_Click(object sender, RoutedEventArgs e)
        {
            MostrarDialogo("API Key", "To get your API Key, log into dadice.com and go to Account->Settings.\nAlso set 'Enable API for this account' there.");
        }

        private void IniRead()
        {
            if (File.Exists("config.ini"))
            {
                IniParser parserStart = new IniParser("config.ini");
                chkLogin.IsChecked = bool.Parse(parserStart.GetSetting("DADICE", "AUTOLOGIN").ToLower());
                if ((bool)chkLogin.IsChecked)
                {
                    txtUsername.Text = parserStart.GetSetting("DADICE", "USERNAME").ToLower();
                    txtApiKey.Text = parserStart.GetSetting("DADICE", "APIKEY").ToLower();
                }
                ChangeAppStyle(int.Parse(parserStart.GetSetting("DADICE", "WINDOWTHEME")), int.Parse(parserStart.GetSetting("DADICE", "WINDOWCOLOR")));
            }
            else
            {
                using (StreamWriter writer = new StreamWriter("config.ini"))
                {
                    writer.WriteLine("[DADICE]");
                    writer.WriteLine("AUTOLOGIN=False");
                    writer.WriteLine("USERNAME=");
                    writer.WriteLine("APIKEY=");
                    writer.WriteLine("WINDOWTHEME=0");
                    writer.WriteLine("WINDOWCOLOR=0");
                }
            }
        }

        private void ChangeAppStyle(int a, int b)
        {
            string theme, accent;
            if (a == 0) { theme = "BaseLight"; }
            else { theme = "BaseDark"; }
            if (b == 0) { accent = "Blue"; }
            else if (b == 1) { accent = "Red"; }
            else if (b == 2) { accent = "Green"; }
            else if (b == 3) { accent = "Purple"; }
            else { accent = "Orange"; }

            ThemeManager.ChangeAppStyle(Application.Current,
                                    ThemeManager.GetAccent(accent),
                                    ThemeManager.GetAppTheme(theme));
        }

        private void chkLogin_Click(object sender, RoutedEventArgs e)
        {
            if (chkLogin.IsChecked != null && (bool)chkLogin.IsChecked)
            {
                _parser.AddSetting("DADICE", "AUTOLOGIN", "TRUE");
                _parser.SaveSettings();
            }
            else
            {
                _parser.AddSetting("DADICE", "AUTOLOGIN", "FALSE");
                _parser.AddSetting("DADICE", "USERNAME", "");
                _parser.AddSetting("DADICE", "APIKEY", "");
                _parser.SaveSettings();
            }
        }
    }
}