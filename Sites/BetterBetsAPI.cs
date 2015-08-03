using System.Collections.Specialized;
using System.Globalization;
using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace AutoDice.Sites
{
    public class BetterBetsAPI : DiceSite
    {
        private WebClient client;
        private string username, accessToken;

        public override GenericCheck Login(string _username, string _password, string _twofactor)
        {
            CanTip = false;
            MinTipAmount = 0.00000001;
            TipAmountInterval = 0.00000001;
            CanLevel = false;
            CanJackpot = false;
            MaxMultiplier = 97.05;

            username = _username;
            accessToken = _password;
            client = new WebClient();

            return new GenericCheck{status = true, username = _username};
        }
        public override GenericBalance Balance()
        {
            try
            {
                var balance = JsonConvert.DeserializeObject<BetterBetsAPIBalance>(client.DownloadString($"https://betterbets.io/api/user/?accessToken={accessToken}"));

                if (balance.error != null) return new GenericBalance {status = false, error = balance.errorMsg};
                
                if (!username.ToLower().Equals(balance.alias.ToLower()))
                {
                    return new GenericBalance {status = false, error = "Username doesn't match!"};
                }
                return new GenericBalance
                {
                    status = true,
                    balance = double.Parse(balance.balance, CultureInfo.InvariantCulture)
                };
            }
            catch
            {
                return new GenericBalance { status = false, error = "Unable to connect" };
            }
        }
        public override GenericRoll Roll(double amount, double chance, bool overUnder)
        {
            var rollParameters = new NameValueCollection
            {
                {"accessToken", accessToken},
                {"wager", ToServerString(amount, true)},
                {"chance", ToServerString(chance, false)},
                {"direction", (overUnder ? "1" : "0")}
            };
            try
            {
                var roll = JsonConvert.DeserializeObject<BetterBetsAPIRoll>(Encoding.UTF8.GetString(client.UploadValues("https://betterbets.io/api/betDice/", rollParameters)));
                if (roll.error.Equals("0"))
                {
                    return new GenericRoll
                    {
                        status = true,
                        error = string.Empty,
                        balance = double.Parse(roll.balance, CultureInfo.InvariantCulture),
                        data = new GenericRollData
                        {
                            amount = double.Parse(roll.wager, CultureInfo.InvariantCulture),
                            bet = roll.direction.Equals("1") ? "Over" : "Under",
                            chance = chance,
                            id = roll.betId,
                            payout = roll.win.Equals("1") ? double.Parse(roll.profit, CultureInfo.InvariantCulture) + amount : 0,
                            profit = roll.win.Equals("1") ? double.Parse(roll.profit, CultureInfo.InvariantCulture) : -double.Parse(roll.wager, CultureInfo.InvariantCulture),
                            result = double.Parse(roll.result, CultureInfo.InvariantCulture),
                            status = roll.win.Equals("1") ? "WIN" : "LOSS"
                        }
                    };
                }
                return new GenericRoll{status = false, error = roll.errorMsg};
            }
            catch
            {
                return new GenericRoll { status = false, error = "Unable to connect" };
            }
        }
        public override GenericBalance Tip(string payee, double amount)
        {
            var tipParameters = new NameValueCollection
            {
                {"accessToken", accessToken},
                {"uname", payee},
                {"amount", ToServerString(amount, true)}
            };
            try
            {
                var tipData = JsonConvert.DeserializeObject<BetterBetsAPITip>(Encoding.UTF8.GetString(client.UploadValues("https://betterbets.io/api/tip/", tipParameters)));
                return tipData.success == 1 ? new GenericBalance { status = true, balance = double.Parse(tipData.balance, CultureInfo.InvariantCulture)} : new GenericBalance { status = false, error = tipData.errorMsg };
            }
            catch
            {
                return new GenericBalance { status = false, error = "Unable to connect" };
            }
        }
        public override GenericCheck Seed(bool ChangeSeed, string seed)
        {
            return new GenericCheck{status = false, error = "Not implemented"};
        }
        public override void Disconnect()
        {
            // ignored
        }
    }
}