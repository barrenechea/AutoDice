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
using MahApps.Metro;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;

namespace AutoDice
{
    /// <summary>
    /// Lógica de interacción para ModeNormal.xaml
    /// </summary>
    public partial class ModeNormal
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

        string _bet;

        private readonly DiceSite _CurrentSite;

        #endregion
        #region Constructor

        /// <summary>
        /// Constructor for General Sites
        /// </summary>
        /// <param name="site">Current site</param>
        /// <param name="username">Current username</param>
        /// <param name="balance">Current balance in BTC</param>
        public ModeNormal(DiceSite site, string username, double balance)
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

        }
        #endregion
        #region Window Loaded
        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            lblVersion.Content =
                $"Version {Assembly.GetExecutingAssembly().GetName().Version.ToString().Remove(5)} by @sbarrenechea";
            BalanceUpdate(_balance);
            radLeveling.IsEnabled = _CurrentSite.CanLevel;
            chkOnlyJackpot.IsEnabled = _CurrentSite.CanJackpot;
            numWinningChance.Maximum = _CurrentSite.MaxMultiplier;
            numWinningChance.Minimum = _CurrentSite.MinMultiplier;
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
            numBet.Maximum = Math.Truncate(balance * 100000000);
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
        #region Method: RandomizeBet()
        /// <summary>
        /// Will randomize over/under bet
        /// </summary>
        private void RandomizeBet()
        {
            var r = new Random();
            _bet = r.Next(0, 2) == 0 ? "over" : "under";
        }
        #endregion
        #region Method: CheckOverUnder()
        /// <summary>
        /// Will check the current Radius selected (Over or Under) and assign that value to _bet
        /// </summary>
        private void CheckOverUnder()
        {
            _bet = (bool)radOver.IsChecked ? "over" : "under";
            if (!(bool)radRandom.IsChecked) return;
            RandomizeBet();
        }
        #endregion
        #region Method: Lock all
        private void LockEverything()
        {
            // Main Grid, Roll Tab
            rad33.IsEnabled = false;
            rad90.IsEnabled = false;
            radLeveling.IsEnabled = false;
            numWinningChance.IsEnabled = false;
            radOver.IsEnabled = false;
            radUnder.IsEnabled = false;
            radRandom.IsEnabled = false;
            numAmountBets.IsEnabled = false;
            chkReturnBaseBet.IsEnabled = false;
            numGreaterBet.IsEnabled = false;
            txtNextBetPayout.IsEnabled = false;
            chkStopIfEarned.IsEnabled = false;
            numAmountEarned.IsEnabled = false;
            btnRefreshBalance.IsEnabled = false;
            btnProMode.IsEnabled = false;

            // Grid winning
            btnOnWinningEvents.IsEnabled = false;
            chkWinningStop.IsEnabled = false;
            radWinningReturn.IsEnabled = false;
            radWinningIncDecBet.IsEnabled = false;
            numIncDecWinning.IsEnabled = false;
            numWinningStop.IsEnabled = false;
            chkSwitchBetOnRoll.IsEnabled = false;

            // Grid losing
            btnOnLosingEvents.IsEnabled = false;
            chkLosingStop.IsEnabled = false;
            radLosingReturn.IsEnabled = false;
            radLosingIncDecBet.IsEnabled = false;
            numIncDecLosing.IsEnabled = false;
            numLosingStop.IsEnabled = false;
            chkSwitchBetOnRollLost.IsEnabled = false;
        }
        #endregion
        #region Methods: Unlock all
        private void MainControlUnlock()
        {
            prgProgreso.IsIndeterminate = false;
            btnStart.IsEnabled = true;
            UnlockEverything();
        }
        private void UnlockEverything()
        {
            // Main Grid, Roll Tab
            rad33.IsEnabled = true;
            rad90.IsEnabled = true;
            radLeveling.IsEnabled = true;
            numWinningChance.IsEnabled = true;
            radOver.IsEnabled = true;
            radUnder.IsEnabled = true;
            radRandom.IsEnabled = true;
            numAmountBets.IsEnabled = true;
            chkReturnBaseBet.IsEnabled = true;
            numGreaterBet.IsEnabled = true;
            txtNextBetPayout.IsEnabled = true;
            chkStopIfEarned.IsEnabled = true;
            numAmountEarned.IsEnabled = true;
            btnRefreshBalance.IsEnabled = true;
            btnProMode.IsEnabled = true;

            // Grid winning
            btnOnWinningEvents.IsEnabled = true;
            chkWinningStop.IsEnabled = true;
            radWinningReturn.IsEnabled = true;
            radWinningIncDecBet.IsEnabled = true;
            chkSwitchBetOnRoll.IsEnabled = true;
            if (radWinningIncDecBet.IsChecked != null && (bool)radWinningIncDecBet.IsChecked)
            {
                numIncDecWinning.IsEnabled = true;
            }
            if (chkWinningStop.IsChecked != null && (bool)chkWinningStop.IsChecked)
            {
                numWinningStop.IsEnabled = true;
            }
            // Grid losing
            btnOnLosingEvents.IsEnabled = true;
            chkLosingStop.IsEnabled = true;
            radLosingReturn.IsEnabled = true;
            radLosingIncDecBet.IsEnabled = true;
            chkSwitchBetOnRollLost.IsEnabled = true;
            if (radLosingIncDecBet.IsChecked != null && (bool)radLosingIncDecBet.IsChecked)
            {
                numIncDecLosing.IsEnabled = true;
            }
            if (chkLosingStop.IsChecked != null && (bool)chkLosingStop.IsChecked)
            {
                numLosingStop.IsEnabled = true;
            }
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
            if (numBet.Value != null && numWinningChance.Value != null)
            {
                txtNextBetProfit.Text =
                    $"{Math.Round(int.Parse(txtNextBetAmount.Text)*CalculatePayout((double) numWinningChance.Value)) - int.Parse(txtNextBetAmount.Text)}";
            }
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
            if (a == 0) { theme = "BaseLight"; logoBlack.Visibility = Visibility.Hidden; logoWhite.Visibility = Visibility.Visible; }
            else { theme = "BaseDark"; logoWhite.Visibility = Visibility.Hidden; logoBlack.Visibility = Visibility.Visible; }
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

            ThemeManager.ChangeAppStyle(Application.Current,
                                        ThemeManager.GetAccent(accent),
                                        ThemeManager.GetAppTheme(theme));
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

        private void rad33_Checked(object sender, RoutedEventArgs e)
        {
            numBet.Value = 3;
            numWinningChance.Value = 33;
            radOver.IsChecked = true;
            chkSwitchBetOnRoll.IsChecked = true;
            radWinningReturn.IsChecked = true;
            radLosingIncDecBet.IsChecked = true;
            chkWinningStop.IsChecked = false;
            chkLosingStop.IsChecked = false;
            numIncDecLosing.Value = 65;
            numAmountBets.Value = 0;
            chkReturnBaseBet.IsChecked = false;
        }
        private void rad90_Checked(object sender, RoutedEventArgs e)
        {
            numBet.Value = 10;
            numWinningChance.Value = 90;
            radOver.IsChecked = true;
            chkSwitchBetOnRoll.IsChecked = true;
            radWinningReturn.IsChecked = true;
            radLosingIncDecBet.IsChecked = true;
            chkWinningStop.IsChecked = false;
            chkLosingStop.IsChecked = false;
            numIncDecLosing.Value = 1000;
            numAmountBets.Value = 50;
            chkReturnBaseBet.IsChecked = true;
            numGreaterBet.Value = 1000;
        }
        private void radLeveling_Checked(object sender, RoutedEventArgs e)
        {
            numBet.Value = 11;
            numWinningChance.Value = 98;
            radUnder.IsChecked = true;
            chkWinningStop.IsChecked = false;
            chkLosingStop.IsChecked = false;
            chkSwitchBetOnRoll.IsChecked = false;
            radWinningReturn.IsChecked = true;
            radLosingReturn.IsChecked = true;
            numAmountBets.Value = 0;
            chkReturnBaseBet.IsChecked = false;
        }
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
        private void numBet_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            try
            {
                _baseAmount = CalcSatoshis((double)numBet.Value);
                if (!_rollWorker.IsBusy)
                {
                    txtNextBetAmount.Text = $"{numBet.Value}";
                }
            }
            catch
            {
                // ignored
            }
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

        private void numWinningChance_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (numWinningChance.Value != null)
            {
                numWinningChance.Value = Math.Round((double)numWinningChance.Value, 2);
                txtNextBetPayout.Text = $"{CalculatePayout((double) numWinningChance.Value)}x";
            }
            VerifyAndCalcProfit();
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

        #region Start button
        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            if (btnStart.Content.Equals("Start"))
            {
                if (numBet.Value == null)
                {
                    ShowNormalDialog("Error", "Please enter a Bet Amount");
                    return;
                }
                if (numWinningChance.Value == null)
                {
                    ShowNormalDialog("Error", "Please enter a Winning Chance value");
                    return;
                }
                if (!(bool)radOver.IsChecked && !(bool)radUnder.IsChecked && !(bool)radRandom.IsChecked)
                {
                    ShowNormalDialog("Error", "Please select a Bet on Roll");
                    return;
                }
                if (numAmountBets.Value == null)
                {
                    ShowNormalDialog("Error", "Please enter a number of Bets to do");
                    return;
                }

                _currentAmount = _baseAmount;
                _chance = (double)numWinningChance.Value;
                _cantidad = (int)numAmountBets.Value;

                CheckOverUnder();

                if (_incDecWin)
                {
                    if (numIncDecWinning.Value != null) _amountIncWin = (int)numIncDecWinning.Value;
                }
                if (_incDecLose)
                {
                    if (numIncDecLosing.Value != null) _amountIncLose = (int)numIncDecLosing.Value;
                }
                if (chkReturnBaseBet.IsChecked == null) return;
                _returnBaseIfAmount = (bool)chkReturnBaseBet.IsChecked;
                if ((bool)chkReturnBaseBet.IsChecked)
                {
                    if (numGreaterBet.Value != null) _amountMaximumBeforeReturn = (int)numGreaterBet.Value;
                }

                _maximumWin = (int)numWinningStop.Value;
                _maximumLose = (int)numLosingStop.Value;
                _stopIfEarned = (bool)chkStopIfEarned.IsChecked;
                _amountEarnedToQuit = (int)numAmountEarned.Value;
                _switchBetting = (bool)chkSwitchBetOnRoll.IsChecked;
                _switchBettingLost = (bool)chkSwitchBetOnRollLost.IsChecked;
                _isRandom = (bool)radRandom.IsChecked;
                _initialBalance = double.Parse(lblBalance.Content.ToString().Substring(9, 10), CultureInfo.InvariantCulture);
                flyoutResults.IsOpen = false;
                _contador = 0;
                TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Indeterminate;
                _rollWorker.RunWorkerAsync();
                prgProgreso.IsIndeterminate = true;
                btnStart.Content = "Stop";
                LockEverything();
            }
            else
            {
                btnStart.IsEnabled = false;
                _rollWorker.CancelAsync();
            }
        }
        #endregion
        #region RollWorker

        #region DoWork
        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            _initialDateTime = DateTime.Now;
            _manuallyStopped = false;
            var timelapse = new Stopwatch();

            #region Infinite bets
            if (_cantidad == 0)
            {
                while (!_manuallyStopped)
                {
                    if ((sender as BackgroundWorker).CancellationPending)
                    {
                        e.Cancel = true;
                        break;
                    }
                    if (DelayEnabled) timelapse.Start();
                    _roll = _CurrentSite.Roll(_currentAmount, _chance, _bet.Equals("over"));

                    if (_roll.status)
                    {
                        _balance = _roll.balance;
                        _contador++;
                        if (_roll.data.status.Equals("WIN"))
                        {
                            _winAmounts++;
                            if (_stopWin)
                            {
                                if (_winAmounts >= _maximumWin)
                                {
                                    _manuallyStopped = true;
                                }
                            }
                            if (_stopIfEarned &&
                                Math.Round((_roll.balance * 100000000) - (_initialBalance * 100000000)) >=
                                _amountEarnedToQuit)
                            {
                                _manuallyStopped = true;
                            }
                            if (_returnWin)
                            {
                                _currentAmount = _baseAmount;
                            }
                            else if (_incDecWin)
                            {
                                _currentAmount += (_currentAmount * ((double)_amountIncWin / 100));
                                _currentAmount = Math.Round(_currentAmount, 8, MidpointRounding.AwayFromZero);
                                if (_currentAmount < 0.00000000)
                                {
                                    _currentAmount = 0.00000001;
                                }
                            }
                            if (_switchBetting)
                            {
                                _bet = _bet.Equals("over") ? "under" : "over";
                            }
                            if (_isRandom)
                            {
                                RandomizeBet();
                            }
                        }
                        else if (_roll.data.status.Equals("LOSS"))
                        {
                            _loseAmounts++;
                            if (_stopLose)
                            {
                                if (_loseAmounts >= _maximumLose)
                                {
                                    _manuallyStopped = true;
                                }
                            }
                            if (_returnLose)
                            {
                                _currentAmount = _baseAmount;
                            }
                            else if (_incDecLose)
                            {
                                _currentAmount += (_currentAmount * ((double)_amountIncLose / 100));
                                _currentAmount = Math.Round(_currentAmount, 8, MidpointRounding.AwayFromZero);
                                if (_currentAmount < 0.00000000)
                                {
                                    _currentAmount = 0.00000001;
                                }

                            }
                            if (_switchBettingLost)
                            {
                                _bet = _bet.Equals("over") ? "under" : "over";
                            }
                            if (_isRandom)
                            {
                                RandomizeBet();
                            }
                        }
                        if (_returnBaseIfAmount)
                        {
                            if ((_currentAmount * 100000000) > _amountMaximumBeforeReturn)
                            {
                                _currentAmount = _baseAmount;
                            }
                        }
                    }
                    if (timelapse.IsRunning)
                    {
                        if (timelapse.ElapsedMilliseconds < (1000/_delay))
                        {
                            Thread.Sleep((int) (1000/_delay) - (int) (timelapse.ElapsedMilliseconds));
                        }
                        timelapse.Reset();
                    }

                    (sender as BackgroundWorker).ReportProgress(_contador);
                }
            }
            #endregion
            else
            #region Limited bets
            {
                for (var i = 1; i <= _cantidad; i++)
                {
                    if ((sender as BackgroundWorker).CancellationPending)
                    {
                        e.Cancel = true;
                        break;
                    }
                    if (DelayEnabled) timelapse.Start();
                    _roll = _CurrentSite.Roll(_currentAmount, _chance, _bet.Equals("over"));
                    if (_roll.status)
                    {
                        _balance = _roll.balance;
                        if (_roll.data.status.Equals("WIN"))
                        {
                            _winAmounts++;
                            if (_stopWin && _winAmounts >= _maximumWin)
                            {
                                i = _cantidad + 1;
                                _manuallyStopped = true;
                            }
                            if (_stopIfEarned && Math.Round((_roll.balance * 100000000) - (_initialBalance * 100000000)) >= _amountEarnedToQuit)
                            {
                                i = _cantidad + 1;
                                _manuallyStopped = true;
                            }
                            if (_returnWin)
                            {
                                _currentAmount = _baseAmount;
                            }
                            else if (_incDecWin)
                            {
                                _currentAmount += (_currentAmount * ((double)_amountIncWin / 100));
                                _currentAmount = Math.Round(_currentAmount, 8, MidpointRounding.AwayFromZero);
                                if (_currentAmount < 0.00000000)
                                {
                                    _currentAmount = 0.00000001;
                                }
                            }
                            if (_switchBetting)
                            {
                                _bet = _bet.Equals("over") ? "under" : "over";
                            }
                            if (_isRandom)
                            {
                                RandomizeBet();
                            }
                        }
                        else if (_roll.data.status.Equals("LOSS"))
                        {
                            _loseAmounts++;
                            if (_stopLose)
                            {
                                if (_loseAmounts >= _maximumLose)
                                {
                                    i = _cantidad + 1;
                                    _manuallyStopped = true;
                                }
                            }
                            if (_returnLose)
                            {
                                _currentAmount = _baseAmount;
                            }
                            else if (_incDecLose)
                            {
                                _currentAmount += (_currentAmount * ((double)_amountIncLose / 100));
                                _currentAmount = Math.Round(_currentAmount, 8, MidpointRounding.AwayFromZero);
                                if (_currentAmount < 0.00000000)
                                {
                                    _currentAmount = 0.00000001;
                                }
                            }
                            if (_switchBettingLost)
                            {
                                _bet = _bet.Equals("over") ? "under" : "over";
                            }
                            if (_isRandom)
                            {
                                RandomizeBet();
                            }
                        }
                        if (_returnBaseIfAmount)
                        {
                            if ((_currentAmount * 100000000) > _amountMaximumBeforeReturn)
                            {
                                _manuallyStopped = true;
                            }
                        }
                    }
                    else
                    {
                        i -= 1;
                    }
                    if (timelapse.IsRunning)
                    {
                        if (timelapse.ElapsedMilliseconds < (1000/_delay))
                        {
                            Thread.Sleep((int) (1000/_delay) - (int) (timelapse.ElapsedMilliseconds));
                        }
                        timelapse.Reset();
                    }

                    (sender as BackgroundWorker).ReportProgress(i);
                }
            }
            #endregion

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
                    radOver.IsChecked = true;
                }
                else
                {
                    radUnder.IsChecked = true;
                }
                CheckOverUnder();
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
                        if (_roll.data != null)
                            lblStatus.Content = $"Bet {e.ProgressPercentage}: {_roll.data.status}. Bets stopped.";
                    }
                    else
                    {
                        if (_roll.data != null)
                            lblStatus.Content = $"Bet {e.ProgressPercentage}: {_roll.data.status}";
                    }
                }
                else
                {
                    if (_manuallyStopped)
                    {
                        if (_roll.data != null)
                            lblStatus.Content = $"Last bet: {_roll.data.status}. Stopped.";
                    }
                    else
                    {
                        if (_roll.data != null)
                            lblStatus.Content = $"Bet {e.ProgressPercentage}/{_cantidad}: {_roll.data.status}";
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
                    lblStatus.Foreground = Brushes.Green;
                }
                else if (_roll.data != null && (_roll.data.status != null && _roll.data.status.Equals("LOSS")))
                {
                    _lose += 1;
                    lblLose.Content = _lose;
                    lblStatus.Foreground = Brushes.Red;
                }
                else
                {
                    lblStatus.Foreground = Brushes.Orange;
                }
            }
            else if (!_roll.error.Equals("Betting too fast. Slow down a bit?"))
            {
                lblStatus.Content = $"Bet {e.ProgressPercentage + 1}: {_roll.error}. Retrying...";
                lblStatus.Foreground = Brushes.Orange;
            }
            #endregion

        }

        #endregion
        #region RunWorkerCompleted
        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
            btnStart.Content = "Start";
            Topmost = true;
            Topmost = false;

            MainControlUnlock();
            txtNextBetAmount.Text = $"{numBet.Value}";
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
                lblStatus.Content = "Balance updated successfully";
                lblStatus.Foreground = Brushes.Green;
            }
            else
            {
                lblStatus.Content = _getBalance.error;
                lblStatus.Foreground = Brushes.Red;
            }
        }
        #endregion
        #endregion
    }
}
