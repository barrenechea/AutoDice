using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shell;
using AutoDice.Sites;
using ControlzEx.Theming;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using SharpLua;
using System.Windows.Input;

namespace AutoDice
{
    /// <summary>
    /// Lógica de interacción para ModeNormal.xaml
    /// </summary>
    public partial class Programmer
    {
        #region Variables
        // Readonly variables
        private readonly IniParser _parser = new IniParser("config.ini");
        private readonly List<GenericDataGrid> _datosRoll = new List<GenericDataGrid>();
        private readonly string _username;

        private readonly BackgroundWorker _rollWorker = new BackgroundWorker();
        private readonly BackgroundWorker _tipWorker = new BackgroundWorker();
        private readonly BackgroundWorker _balanceWorker = new BackgroundWorker();

        // Variables for tipping
        private GenericBalance _tip;
        private string _payee;
        private double _tipAmount;

        // Current roll data
        private GenericRoll _roll;

        // Aux variables
        private DateTime _initialDateTime;
        private GenericBalance _getBalance;

        double _initialBalance, _finalBalance, _balance;
        double _baseAmount, _currentAmount, _chance, _delay;

        int _amountIncWin, _amountIncLose;
        int _won, _lose, _jp, _contador;
        int _amountMaximumBeforeReturn, _amountEarnedToQuit;
        int _winAmounts, _loseAmounts;
        int _maximumWin, _maximumLose;
        int _cantidad;

        bool _switchBetting, _switchBettingLost, _isRandom;
        bool _stopWin, _stopLose, _returnWin, _returnLose, _incDecWin, _incDecLose;
        bool _returnBaseIfAmount, _manuallyStopped, _stopIfEarned;

        bool _bet;

        private readonly DiceSite _CurrentSite;

        #endregion
        #region Constructor

        /// <summary>
        /// Constructor for General Sites
        /// </summary>
        /// <param name="site">Current site</param>
        /// <param name="username">Current username</param>
        /// <param name="balance">Current balance in BTC</param>
        public Programmer(DiceSite site, string username, double balance)
        {
            InitializeComponent();
            _CurrentSite = site;
            Title = $"AutoDice: {username}";
            _balance = balance;
            _username = username;

            cmbTheme.ItemsSource = Enum.GetValues(typeof (Theme));
            cmbTheme.SelectedIndex = int.Parse(_parser.GetSetting("AUTODICE", "WINDOWTHEME"));

            cmbAccent.ItemsSource = Enum.GetValues(typeof (Accent));
            cmbAccent.SelectedIndex = int.Parse(_parser.GetSetting("AUTODICE", "WINDOWCOLOR"));

            dataBets.ItemsSource = _datosRoll;

            _rollWorker.WorkerReportsProgress = true;
            _rollWorker.WorkerSupportsCancellation = true;
            _rollWorker.DoWork += worker_DoWork;
            _rollWorker.ProgressChanged += worker_ProgressChanged;
            _rollWorker.RunWorkerCompleted += worker_RunWorkerCompleted;

            _tipWorker.DoWork += tipWorker_DoWork;
            _tipWorker.RunWorkerCompleted += tipWorker_RunWorkerCompleted;

            _balanceWorker.DoWork += BalanceWorkerOnDoWork;
            _balanceWorker.RunWorkerCompleted += BalanceWorkerOnRunWorkerCompleted;
            txtScript.Text = "function dobet()\r\n\r\nend";

            Lua.RegisterFunction("stop", this, new dStop(Stop).Method);
            Lua.RegisterFunction("tip", this, new dtip(luatip).Method);
            Lua.RegisterFunction("print", this, new dWriteConsole(WriteConsole).Method);

            txtVariables.Text = @"balance:double, RO
win:bool, RO
profit:double, RO
currentprofit:double, RO
lastBet:Bet, RO
previousbet:double, RO
nextbet:double, RW
chance:double, RW
bethigh:bool, RW
";

            txtFunctions.Text = @"print(messagetoprint:string)
tip(username/userid:string, amount:double)";

        }

        #region Lua Methods and delegates
        delegate void dStop();
        void Stop()
        {
            _manuallyStopped = true;
        }

        delegate void dtip(string username, double amount);
        void luatip(string username, double amount)
        {
            //process tips here
        }
        delegate void dWriteConsole(string Message);
        void WriteConsole(string Message)
        {
            
            if (txtConsole.Text.Split('\n').Length > 1000)
            {
                List<string> lines = new List<string>(txtConsole.Text.Split('\n').Length);
                while (lines.Count > 950)
                {
                    lines.RemoveAt(0);
                }
                txtConsole.Text = "";
                foreach (string s in lines)
                {
                    txtConsole.Text += s + "\r\n";
                }
            }
            txtConsole.Text +=(Message + "\r\n");
            
        }
        #endregion

        #endregion
        #region Window Loaded
        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            lblVersion.Content =
                $"Version {Assembly.GetExecutingAssembly().GetName().Version.ToString().Remove(5)} by @sbarrenechea";
            BalanceUpdate(_balance);
            //radLeveling.IsEnabled = _CurrentSite.CanLevel;
            chkOnlyJackpot.IsEnabled = _CurrentSite.CanJackpot;
            //numWinningChance.Maximum = _CurrentSite.MaxMultiplier;
            //numWinningChance.Minimum = _CurrentSite.MinMultiplier;
            chkDelay.IsChecked = DelayEnabled = bool.Parse(_parser.GetSetting("AUTODICE", "DELAYENABLED"));
            numDelay.Value = _delay = double.Parse(_parser.GetSetting("AUTODICE", "DELAYTIME"), CultureInfo.InvariantCulture);
            TabTip.IsEnabled = _CurrentSite.CanTip;
            numAmountTip.Minimum = _CurrentSite.MinTipAmount;
            numAmountTip.Interval = _CurrentSite.TipAmountInterval;
        }
        #endregion

        #region Methods

        #region Method: BalanceUpdate()
        /// <summary>
        /// Updates the Balance label
        /// </summary>
        /// <param name="balance">Balance in BTC</param>
        private void BalanceUpdate(double balance)
        {
            VerifyTip(balance);
            //numBet.Maximum = Math.Truncate(balance * 100000000);
            lblBalance.Content = $"Balance: {balance.ToString("0.00000000").Replace(",", ".")} BTC";
        }
        #endregion
        #region Method: CalcSatoshis(double number)
        /// <summary>
        /// Return the amount in Satoshi
        /// </summary>
        /// <param name="number">Amount in BTC</param>
        /// <returns>Amount in Satoshi</returns>
        private static double CalcSatoshis(double number)
        {
            return number * 0.00000001;
        }
        #endregion
        
        #region Method: ShowNormalDialog(string title, string message)
        /// <summary>
        /// This will show a normal async dialog with MahApps library
        /// </summary>
        /// <param name="title">The title of the dialog</param>
        /// <param name="message">The message of the dialog</param>
        private async void ShowNormalDialog(string title, string message)
        {
            await this.ShowMessageAsync(title, message);
        }
        #endregion
        #region Method: VerifyAndCalcProfit()
        private void VerifyAndCalcProfit()
        {
           /* if (numBet.Value != null && numWinningChance.Value != null)
            {
                txtNextBetProfit.Text =
                    $"{Math.Round(int.Parse(txtNextBetAmount.Text)*CalculatePayout((double) numWinningChance.Value)) - int.Parse(txtNextBetAmount.Text)}";
            }*/
        }
        #endregion
        #region Method: CalculatePayout(double chance)
        private static double CalculatePayout(double chance)
        {
            chance = Math.Round(chance, 2);

            var calculated = (100 / chance);
            calculated = calculated - (calculated / 100);
            calculated = Math.Round(calculated, 4);

            return calculated;
        }
        #endregion
        #region Method: VerifyTip(double balance)
        /// <summary>
        /// This will check if the user can tip or not, and will hide everything if he can't
        /// </summary>
        /// <param name="balance">Current balance of the user</param>
        private void VerifyTip(double balance)
        {
            if (balance >= _CurrentSite.MinTipAmount)
            {
                lblTipMessage.Visibility = Visibility.Hidden;
                numAmountTip.Visibility = Visibility.Visible;
                numAmountTip.Maximum = (Math.Truncate((balance * 100000000) / (_CurrentSite.TipAmountInterval * 100000000)) * _CurrentSite.TipAmountInterval);
                lblBtc.Visibility = Visibility.Visible;
                lblTipAmount.Visibility = Visibility.Visible;
                lblTipPayee.Visibility = Visibility.Visible;
                txtPayee.Visibility = Visibility.Visible;
                lblOrDonateMe.Visibility = Visibility.Visible;
                btnDonateMe.Visibility = Visibility.Visible;
                btnSendTip.Visibility = Visibility.Visible;
            }
            else
            {
                lblTipMessage.Visibility = Visibility.Visible;
                numAmountTip.Visibility = Visibility.Hidden;
                lblTipAmount.Visibility = Visibility.Hidden;
                lblBtc.Visibility = Visibility.Hidden;
                lblTipPayee.Visibility = Visibility.Hidden;
                txtPayee.Visibility = Visibility.Hidden;
                lblOrDonateMe.Visibility = Visibility.Hidden;
                btnDonateMe.Visibility = Visibility.Hidden;
                btnSendTip.Visibility = Visibility.Hidden;
            }
        }
        #endregion
        #region Methods to avoid closing window if betting

        private bool avoidClosing = true;

        protected override void OnClosing(CancelEventArgs e)
        {
            if (_rollWorker.IsBusy && avoidClosing)
            {
                e.Cancel = true;
                ShowCloseMessage();
            }
            else
            {
                _CurrentSite.Disconnect();
                e.Cancel = false;
            }
        }
        private async void ShowCloseMessage()
        {
            var mySettings = new MetroDialogSettings()
            {
                AffirmativeButtonText = "Yes",
                NegativeButtonText = "No",
                ColorScheme = MetroDialogOptions.ColorScheme
            };

            var result = await this.ShowMessageAsync("Betting", "You are betting right now. ¿Are you sure do you want to quit?",
            MessageDialogStyle.AffirmativeAndNegative, mySettings);

            if (result != MessageDialogResult.Affirmative) return;
            avoidClosing = false;
            _CurrentSite.Disconnect();
            Close();
        }
        #endregion
        #region Event: Click Pro Mode button
        private void btnProMode_Click(object sender, RoutedEventArgs e)
        {
            new ModePro(_CurrentSite, _username, _balance)
            {
                Visibility = Visibility.Visible
            };
            Close();
        }
        #endregion

        #region Selection changes
        #region Style stuff

        #region Method: ChangeAppStyle()
        /// <summary>
        /// Reads config.ini and changes the current Window colours based on the saved data.
        /// </summary>
        private void ChangeAppStyle()
        {
            var a = int.Parse(_parser.GetSetting("AUTODICE", "WINDOWTHEME"));
            var b = int.Parse(_parser.GetSetting("AUTODICE", "WINDOWCOLOR"));
            string theme, accent;
            if (a == 0) { theme = "Light"; logoBlack.Visibility = Visibility.Hidden; logoWhite.Visibility = Visibility.Visible; }
            else { theme = "Dark"; logoWhite.Visibility = Visibility.Hidden; logoBlack.Visibility = Visibility.Visible; }
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
        private void cmbTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _parser.AddSetting("AUTODICE", "WINDOWTHEME", cmbTheme.SelectedIndex.ToString());
            _parser.SaveSettings();
            ChangeAppStyle();
        }

        private void cmbAccent_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _parser.AddSetting("AUTODICE", "WINDOWCOLOR", cmbAccent.SelectedIndex.ToString());
            _parser.SaveSettings();
            ChangeAppStyle();
        }
        #endregion
        #region Big shit 
        //lol, nice

        private void chkWinningStop_Checked(object sender, RoutedEventArgs e)
        {
            _stopWin = true;
            numWinningStop.IsEnabled = true;
        }
        private void chkWinningStop_Unchecked(object sender, RoutedEventArgs e)
        {
            _stopWin = false;
            numWinningStop.IsEnabled = false;
        }
        private void radWinningReturn_Checked(object sender, RoutedEventArgs e)
        {
            _returnWin = true;
        }
        private void radWinningReturn_Unchecked(object sender, RoutedEventArgs e)
        {
            _returnWin = false;
        }
        private void radWinningIncDecBet_Checked(object sender, RoutedEventArgs e)
        {
            numIncDecWinning.IsEnabled = true;
            _incDecWin = true;
        }
        private void radWinningIncDecBet_Unchecked(object sender, RoutedEventArgs e)
        {
            numIncDecWinning.IsEnabled = false;
            _incDecWin = false;
        }
        private void chkLosingStop_Checked(object sender, RoutedEventArgs e)
        {
            numLosingStop.IsEnabled = true;
            _stopLose = true;
        }
        private void chkLosingStop_Unchecked(object sender, RoutedEventArgs e)
        {
            numLosingStop.IsEnabled = false;
            _stopLose = false;
        }
        private void radLosingReturn_Checked(object sender, RoutedEventArgs e)
        {
            _returnLose = true;
        }
        private void radLosingReturn_Unchecked(object sender, RoutedEventArgs e)
        {
            _returnLose = false;
        }
        private void radLosingIncDecBet_Checked(object sender, RoutedEventArgs e)
        {
            numIncDecLosing.IsEnabled = true;
            _incDecLose = true;
        }
        private void radLosingIncDecBet_Unchecked(object sender, RoutedEventArgs e)
        {
            numIncDecLosing.IsEnabled = false;
            _incDecLose = false;
        }
        
        #endregion
        #region More big shit
        private void dataBets_PreviewMouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var row = UIHelpers.TryFindFromPoint<DataGridRow>((UIElement)sender, e.GetPosition(dataBets));
            if (row != null)
            {
                (sender as DataGrid).SelectedIndex = row.GetIndex();
            }
            else
            {
                (sender as DataGrid).SelectedIndex = -1;
            }
        }
        private void CopyID_Click(object sender, RoutedEventArgs e)
        {
            if (dataBets.SelectedIndex != -1)
            {
                Clipboard.SetText($"!#{((GenericDataGrid) dataBets.SelectedItem).id}");
            }
            else
            {
                ShowNormalDialog("Error", "You must select an item!");
            }
        }
        private void numDelay_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            try
            {
                _delay = (double)numDelay.Value;
                _parser.AddSetting("AUTODICE", "DELAYTIME", ((double)numDelay.Value).ToString("0.00", CultureInfo.InvariantCulture));
                _parser.SaveSettings();
            }
            catch
            {
                // ignored
            }
        }

        private bool DelayEnabled;
        private void chkDelay_CheckedChanged(object sender, RoutedEventArgs e)
        {
            try
            {
                DelayEnabled = (bool) chkDelay.IsChecked;
                _parser.AddSetting("AUTODICE", "DELAYENABLED", chkDelay.IsChecked.ToString());
                _parser.SaveSettings();
            }
            catch
            {
                // ignored
            }
        }

        private void txtNextBetAmount_TextChanged(object sender, TextChangedEventArgs e)
        {
            VerifyAndCalcProfit();
            txtNextBetLost.Text = $"-{txtNextBetAmount.Text}";
        }
        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            flyoutSettings.IsOpen = true;
        }

        private void btnOnWinningEvents_Click(object sender, RoutedEventArgs e)
        {
            flyoutWinning.IsOpen = true;
        }

        private void btnOnLosingEvents_Click(object sender, RoutedEventArgs e)
        {
            flyoutLosing.IsOpen = true;
        }

        private void chkOnlyJackpot_Checked(object sender, RoutedEventArgs e)
        {
            chkShowWon.IsChecked = false;
            chkShowLost.IsChecked = false;
            chkShowWon.Visibility = Visibility.Hidden;
            chkShowLost.Visibility = Visibility.Hidden;
            radJackpot0.Visibility = Visibility.Visible;
            radJackpot99.Visibility = Visibility.Visible;
            lblStatsJp.Visibility = Visibility.Visible;
            lblJackpot.Visibility = Visibility.Visible;

            var labelWon = lblStatsWon.Margin;
            var labelWonAmount = lblWin.Margin;
            var labelLose = lblStatsLose.Margin;
            var labelLoseAmount = lblLose.Margin;

            labelWon.Left = 10;
            labelWonAmount.Left = 65;
            labelLose.Right = 195;
            labelLoseAmount.Right = 135;

            lblStatsWon.Margin = labelWon;
            lblWin.Margin = labelWonAmount;
            lblStatsLose.Margin = labelLose;
            lblLose.Margin = labelLoseAmount;
        }

        private void chkOnlyJackpot_Unchecked(object sender, RoutedEventArgs e)
        {
            chkShowWon.IsEnabled = true;
            chkShowLost.IsEnabled = true;
            chkShowWon.Visibility = Visibility.Visible;
            chkShowLost.Visibility = Visibility.Visible;
            radJackpot0.Visibility = Visibility.Hidden;
            radJackpot99.Visibility = Visibility.Hidden;
            lblStatsJp.Visibility = Visibility.Hidden;
            lblJackpot.Visibility = Visibility.Hidden;

            var labelWon = lblStatsWon.Margin;
            var labelWonAmount = lblWin.Margin;
            var labelLose = lblStatsLose.Margin;
            var labelLoseAmount = lblLose.Margin;

            labelWon.Left = 68;
            labelWonAmount.Left = 123;
            labelLose.Right = 137;
            labelLoseAmount.Right = 77;

            lblStatsWon.Margin = labelWon;
            lblWin.Margin = labelWonAmount;
            lblStatsLose.Margin = labelLose;
            lblLose.Margin = labelLoseAmount;
        }
        #endregion
        #region Random buttons
        private void btnClearData_Click(object sender, RoutedEventArgs e)
        {
            _won = 0;
            _lose = 0;
            _jp = 0;
            lblWin.Content = _won;
            lblLose.Content = _lose;
            lblJackpot.Content = _jp;
            _datosRoll.Clear();
            dataBets.ItemsSource = null;
            dataBets.ItemsSource = _datosRoll;
        }
        private void btnAbout_Click(object sender, RoutedEventArgs e)
        {
            flyoutAbout.IsOpen = true;
        }
        private void btnSaveData_Click(object sender, RoutedEventArgs e)
        {
            if (_datosRoll.Count == 0)
            {
                ShowNormalDialog("Error", "You don't have any data to save!");
            }
            else
            {
                var savePlace = new SaveFileDialog
                {
                    FileName = "Bets.txt",
                    Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*"
                };

                if (savePlace.ShowDialog() != true) return;

                var chatAppend = new List<string>();
                using (var sw = new StreamWriter(savePlace.FileName, false, System.Text.Encoding.Unicode))
                {
                    foreach (var dato in _datosRoll)
                    {
                        sw.WriteLine("[{0}] Amount: {1}, Payout: {2}. Result: {3}, Winning Chance: {4}. Bet: {5}. ID: {6}", dato.status, dato.amount, dato.payout, dato.result, dato.chance, dato.bet, dato.id);
                        chatAppend.Add($"Bet Info: !#{dato.id}");
                    }
                    sw.WriteLine("");
                    sw.WriteLine("ID's to be pasted on Chat:");
                    foreach (var datoChat in chatAppend)
                    {
                        sw.WriteLine(datoChat);
                    }
                }
                ShowNormalDialog("Success", "Data saved succesfully!");
            }
        }
        private void btnDonateMe_Click(object sender, RoutedEventArgs e)
        {
            txtPayee.Text = "sbarrenechea";
        }
        #endregion

        #endregion

        #endregion

        
        #region RollWorker

        #region DoWork
        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            _initialDateTime = DateTime.Now;
            _manuallyStopped = false;
            var timelapse = new Stopwatch();

            
            while (!_manuallyStopped)
            {
                if ((sender as BackgroundWorker).CancellationPending)
                {
                    e.Cancel = true;
                    break;
                }
                Dispatcher.BeginInvoke(new dWriteConsole(WriteConsole), System.Windows.Threading.DispatcherPriority.DataBind, string.Format("Betting {0} at {1} {2}", _currentAmount, _chance, _bet?"over":"under"));
                _roll = _CurrentSite.Roll(_currentAmount, _chance, _bet);

                if (_roll.status)
                {
                    _balance = _roll.balance;
                    parseScript(_roll);
                    _contador++;
                }
                (sender as BackgroundWorker).ReportProgress(_contador);
            }
            
        }
        #endregion
        #region ProgressChanged

        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            var betsPerSecond = Math.Round((e.ProgressPercentage / (DateTime.Now - _initialDateTime).TotalSeconds), 2, MidpointRounding.AwayFromZero);
            lblBetsPerSecond.Content = $"{betsPerSecond} Bet{(betsPerSecond <= 1 ? string.Empty : "s")}/s";

            #region Status update
            if (_roll.status)
            {
                if (_bet.Equals("over"))
                {
                    //radOver.IsChecked = true;
                }
                else
                {
                    //radUnder.IsChecked = true;
                }
                
                txtNextBetAmount.Text = $"{Math.Round(_currentAmount*100000000)}";

                #region Add to DataGrid at Won
                if ((bool)chkShowWon.IsChecked && _roll.data.status.Equals("WIN"))
                {
                    var data = new GenericDataGrid
                    {
                        status = _roll.data.status,
                        amount = $"-{Math.Round(_roll.data.amount*100000000)} Satoshi",
                        payout = $"{Math.Round(_roll.data.payout*100000000)} Satoshi",
                        profit = $"{Math.Round(_roll.data.profit*100000000)} Satoshi",
                        result = _roll.data.result,
                        bet = _roll.data.bet,
                        chance = $"{_roll.data.chance}%",
                        id = _roll.data.id,
                        hour = DateTime.Now.ToString("HH:mm:ss", DateTimeFormatInfo.InvariantInfo)
                    };
                    var selected = dataBets.SelectedItem;
                    _datosRoll.Add(data);
                    dataBets.Items.Refresh();
                    dataBets.SelectedItem = selected;
                }
                #endregion
                #region Add to DataGrid at Jackpot
                if ((bool)radJackpot0.IsChecked && (bool)chkOnlyJackpot.IsChecked && _roll.data.status.Equals("WIN") && _roll.data.result.Equals(0))
                {
                    var data = new GenericDataGrid
                    {
                        status = _roll.data.status,
                        amount = $"-{Math.Round(_roll.data.amount*100000000)} Satoshi",
                        payout = $"{Math.Round(_roll.data.payout*100000000)} Satoshi",
                        profit = $"{Math.Round(_roll.data.profit*100000000)} Satoshi",
                        result = _roll.data.result,
                        bet = _roll.data.bet,
                        chance = $"{_roll.data.chance}%",
                        id = _roll.data.id,
                        hour = DateTime.Now.ToString("HH:mm:ss", DateTimeFormatInfo.InvariantInfo)
                    };
                    var selected = dataBets.SelectedItem;
                    _datosRoll.Add(data);
                    dataBets.Items.Refresh();
                    dataBets.SelectedItem = selected;
                }
                if ((bool)radJackpot99.IsChecked && (bool)chkOnlyJackpot.IsChecked && _roll.data.status.Equals("WIN") && _roll.data.result.Equals(99.99))
                {
                    var data = new GenericDataGrid
                    {
                        status = _roll.data.status,
                        amount = $"-{Math.Round(_roll.data.amount*100000000)} Satoshi",
                        payout = $"{Math.Round(_roll.data.payout*100000000)} Satoshi",
                        profit = $"{Math.Round(_roll.data.profit*100000000)} Satoshi",
                        result = _roll.data.result,
                        bet = _roll.data.bet,
                        chance = $"{_roll.data.chance}%",
                        id = _roll.data.id,
                        hour = DateTime.Now.ToString("HH:mm:ss", DateTimeFormatInfo.InvariantInfo)
                    };
                    var selected = dataBets.SelectedItem;
                    _datosRoll.Add(data);
                    dataBets.Items.Refresh();
                    dataBets.SelectedItem = selected;
                }
                #endregion
                #region Add to DataGrid at Lost
                if ((bool)chkShowLost.IsChecked && _roll.data.status.Equals("LOSS"))
                {
                    var data = new GenericDataGrid
                    {
                        status = _roll.data.status,
                        amount = $"-{Math.Round(_roll.data.amount*100000000)} Satoshi",
                        payout = "0 Satoshi",
                        profit = $"{Math.Round(_roll.data.profit*100000000)} Satoshi",
                        result = _roll.data.result,
                        bet = _roll.data.bet,
                        chance = $"{_roll.data.chance}%",
                        id = _roll.data.id,
                        hour = DateTime.Now.ToString("HH:mm:ss", DateTimeFormatInfo.InvariantInfo)
                    };
                    var selected = dataBets.SelectedItem;
                    _datosRoll.Add(data);
                    dataBets.Items.Refresh();
                    dataBets.SelectedItem = selected;
                }
                #endregion

                if ((bool)chkAutoScroll.IsChecked && dataBets.Items.Count > 0)
                {
                    dataBets.ScrollIntoView(dataBets.Items[dataBets.Items.Count - 1]);
                }

                BalanceUpdate(_balance);

                if (_cantidad == 0)
                {
                    if (_manuallyStopped)
                    {
                        //if (_roll.data != null)
                            //lblStatus.Content = $"Bet {e.ProgressPercentage}: {_roll.data.status}. Bets stopped.";
                    }
                    else
                    {
                        //if (_roll.data != null)
                            //lblStatus.Content = $"Bet {e.ProgressPercentage}: {_roll.data.status}";
                    }
                }
                else
                {
                    if (_manuallyStopped)
                    {
                        //if (_roll.data != null)
                            //lblStatus.Content = $"Last bet: {_roll.data.status}. Stopped.";
                    }
                    else
                    {
                        //if (_roll.data != null)
                            //lblStatus.Content = $"Bet {e.ProgressPercentage}/{_cantidad}: {_roll.data.status}";
                    }
                }
                if (_roll.data != null && (_roll.data.status != null && _roll.data.status.Equals("WIN")))
                {
                    if (((bool)radJackpot0.IsChecked && _roll.data.result.Equals(0)) || ((bool)radJackpot99.IsChecked && _roll.data.result.Equals(99.99)))
                    {
                        _jp += 1;
                        lblJackpot.Content = _jp;
                    }
                    _won += 1;
                    lblWin.Content = _won;
                    //lblStatus.Foreground = Brushes.Green;
                }
                else if (_roll.data != null && (_roll.data.status != null && _roll.data.status.Equals("LOSS")))
                {
                    _lose += 1;
                    lblLose.Content = _lose;
                    //lblStatus.Foreground = Brushes.Red;
                }
                else
                {
                   // lblStatus.Foreground = Brushes.Orange;
                }
            }
            else if (!_roll.error.Equals("Betting too fast. Slow down a bit?"))
            {
                //lblStatus.Content = $"Bet {e.ProgressPercentage + 1}: {_roll.error}. Retrying...";
                //lblStatus.Foreground = Brushes.Orange;
            }
            #endregion

        }

        #endregion
        #region RunWorkerCompleted
        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
            //btnStart.Content = "Start";
            Topmost = true;
            Topmost = false;

            //txtNextBetAmount.Text = $"{numBet.Value}";
            _manuallyStopped = false;

            if (!(bool) chkShowResults.IsChecked) return;

            _finalBalance = double.Parse(lblBalance.Content.ToString().Substring(9, 10),
                CultureInfo.InvariantCulture);
            if ((_finalBalance * 100000000) - (_initialBalance * 100000000) >= 0)
            {
                lblResultsWonLose.Content = "You have won";
                lblResultsAmount.Content =
                    $"{Math.Round((_finalBalance*100000000) - (_initialBalance*100000000))} Satoshi";
            }
            else
            {
                lblResultsWonLose.Content = "You have lost";
                lblResultsAmount.Content =
                    $"{Math.Round((_finalBalance*100000000) - (_initialBalance*100000000))*-1} Satoshi";
            }
            _winAmounts = 0;
            _loseAmounts = 0;
            flyoutResults.IsOpen = true;
        }
        #endregion

        #endregion

        #region Tip button
        private void btnSendTip_Click(object sender, RoutedEventArgs e)
        {
            if (txtPayee.Text.Equals(string.Empty))
            {
                ShowNormalDialog("Error", "You should enter a payee!");
            }
            else
            {
                numAmountTip.IsEnabled = false;
                txtPayee.IsEnabled = false;
                btnDonateMe.IsEnabled = false;
                btnSendTip.IsEnabled = false;

                _payee = txtPayee.Text;
                if (numAmountTip.Value != null) _tipAmount = (double)numAmountTip.Value;

                _tipWorker.RunWorkerAsync();
                prgTipRing.IsActive = true;
            }
        }
        #endregion
        #region TipWorker

        #region DoWork
        private void tipWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            _tip = null;
            _tip = _CurrentSite.Tip(_payee, _tipAmount);
            if (_tip.status)
            {
                _balance = _tip.balance;
            }
        }
        #endregion
        #region WorkerCompleted
        private void tipWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            prgTipRing.IsActive = false;
            numAmountTip.IsEnabled = true;
            txtPayee.IsEnabled = true;
            btnDonateMe.IsEnabled = true;
            btnSendTip.IsEnabled = true;
            if (_tip != null && _tip.status)
            {
                BalanceUpdate(_balance);
                ShowNormalDialog("Success", "Tip successfully sent!");
            }
            else
            {
                ShowNormalDialog("Error", _tip.error);
            }
        }
        #endregion
        #endregion

        #region Balance Update
        private void btnRefreshBalance_Click(object sender, RoutedEventArgs e)
        {
            _getBalance = null;
            btnRefreshBalance.IsEnabled = false;
            prgProgreso.IsIndeterminate = true;
            _balanceWorker.RunWorkerAsync();
        }
        #region BalanceWorker
        private void BalanceWorkerOnDoWork(object sender, DoWorkEventArgs doWorkEventArgs)
        {
            _getBalance = _CurrentSite.Balance();
        }
        private void BalanceWorkerOnRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs runWorkerCompletedEventArgs)
        {
            btnRefreshBalance.IsEnabled = true;
            prgProgreso.IsIndeterminate = false;
            if (_getBalance.status)
            {
                _balance = _getBalance.balance;
                BalanceUpdate(_balance);
                //lblStatus.Content = "Balance updated successfully";
                //lblStatus.Foreground = Brushes.Green;
            }
            else
            {
                //lblStatus.Content = _getBalance.error;
                //lblStatus.Foreground = Brushes.Red;
            }
        }
        #endregion
        #endregion
        #region Programmer Mode Process
        LuaInterface Lua = LuaRuntime.GetLua();
        List<string> LastCommands = new List<string>();
        int LCindex = 0;
        void Start()
        {
            bool valid = true;
            if (_currentAmount < 0)
            {
                WriteConsole("Please set starting bet using nextbet = x.xxxxxxxx");
                valid = false;
            }
            if (_chance == 0)
            {

                WriteConsole("Please set starting chance using chance = yy.yyyy");
                valid = false;
            }
            if (valid)
            {
                flyoutResults.IsOpen = false;
                _contador = 0;
                TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Indeterminate;
                _rollWorker.RunWorkerAsync();
                prgProgreso.IsIndeterminate = true;
            }
        }
        void ConsoleIn(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {

                SetLuaVars();
                LCindex = 0;
                LastCommands.Add(txtConsoleIn.Text);
                if (LastCommands.Count > 26)
                { LastCommands.RemoveAt(0); }
                txtConsole.Text += (txtConsoleIn.Text)+"\r\n";
                if (txtConsoleIn.Text.ToLower() == "start()")
                {
                    LuaRuntime.SetLua(Lua);
                    try
                    {
                        LuaRuntime.Run(txtScript.Text);
                        Start();
                    }
                    catch (Exception ex)
                    {
                        txtConsole.Text += ("LUA ERROR!!") + "\r\n";
                        txtConsole.Text += (ex.Message) + "\r\n";
                    }

                }

                else
                {
                    try
                    {
                        LuaRuntime.SetLua(Lua);
                        LuaRuntime.Run(txtConsoleIn.Text);
                    }
                    catch (Exception ex)
                    {
                        txtConsole.Text += ("LUA ERROR!!") + "\r\n";
                        txtConsole.Text += (ex.Message) + "\r\n";
                    }
                }

                txtConsoleIn.Text = "";
                GetLuaVars();
            }
            if (e.Key == Key.Up)
            {
                if (LCindex < LastCommands.Count)
                    LCindex++;
                if (LastCommands.Count > 0)
                    txtConsoleIn.Text = LastCommands[LastCommands.Count - LCindex];

            }
            if (e.Key == Key.Down)
            {
                if (LCindex > 0)
                    LCindex--;
                if (LCindex <= 0)
                {
                    txtConsoleIn.Text = "";
                }
                else if (LastCommands.Count > 0)
                    txtConsoleIn.Text = LastCommands[LastCommands.Count - LCindex];


            }
        }
        private void txtConsoleIn_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                txtConsoleIn.Text = "";
            }
        }
        void GetLuaVars()
        {

            try
            {
                _currentAmount = (double)Lua["nextbet"];
                _chance = (double)Lua["chance"];
                _bet = (bool)Lua["bethigh"];
            }
            catch
            {

            }
        }
        void SetLuaVars()
        {
            try
            {
                //Lua.clear();
                Lua["balance"] = _balance;
                Lua["profit"] = _initialBalance - _balance;
                
                //Lua["currentstreak"] = (Winstreak > 0) ? Winstreak : -Losestreak;
                Lua["previousbet"] = _currentAmount;
                Lua["nextbet"] = _currentAmount;
                Lua["chance"] = _chance;
                Lua["bethigh"] = _bet;
                //Lua["bets"] = Wins + Losses;
                //Lua["wins"] = Wins;
                //Lua["losses"] = Losses;
                //Lua["currencies"] = CurrentSite.Currencies;
                //Lua["currency"] = CurrentSite.Currency;
                //Lua["enablersc"] = EnableReset;
                //Lua["enablezz"] = EnableProgZigZag;
            }
            catch (Exception e)
            {
                _manuallyStopped= true;
                txtConsole.Text += ("LUA ERROR!!") + "\r\n";
                txtConsole.Text += (e.Message) + "\r\n";
            }
        }
        private void parseScript(GenericRoll bet)
        {

            try
            {
                
                bool Win = bet.data.status.Equals("WIN");
                SetLuaVars();
                Lua["win"] = Win;
                Lua["currentprofit"] = bet.data.profit;
                Lua["lastBet"] = bet;
                LuaRuntime.SetLua(Lua);
                LuaRuntime.Run("dobet()");
                GetLuaVars();
            }
            catch
            {

            }

        }

        #endregion
    }
}
