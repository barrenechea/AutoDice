using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace AutoDice.Sites
{
    class PrimeDiceAPI : DiceSite
    {
        private readonly WebClient client = new WebClient();
        private string _accessToken;
        public override GenericCheck Login(string username, string password, string twofactor)
        {
            CanTip = true;
            MinTipAmount = 0.00000001;
            TipAmountInterval = 0.00000001;
            CanLevel = false;
            CanJackpot = false;

            var loginParameters = new NameValueCollection
            {
                {"username", username},
                {"password", password},
                {"otp", twofactor}
            };
            try
            {
                var tipData = JsonConvert.DeserializeObject<PrimediceAPILogin>(Encoding.UTF8.GetString(client.UploadValues("https://api.primedice.com/api/login", loginParameters)));
                if (!tipData.access_token.Equals("")) _accessToken = tipData.access_token;
                return new GenericCheck {status = !tipData.access_token.Equals(string.Empty), username = username, error = tipData.access_token.Equals("") ? "Wrong User/Pass/2FA" : string.Empty};
            }
            catch (WebException e)
            {
                return new GenericCheck {status = false, error = e.Message.Contains("429") ? "Too many login attempts have been made" : "Unable to connect" };
            }
        }
        public override GenericBalance Balance()
        {
            try
            {
                string a = client.DownloadString($"https://api.primedice.com/api/users/1?access_token={_accessToken}");
                var balance = JsonConvert.DeserializeObject<PrimeDiceAPIUserdata>(a);
                return new GenericBalance {status = true, balance = balance.user.balance / 100000000};
            }
            catch (Exception e)
            {
                return new GenericBalance {status = false, error = "Unable to fetch balance"};
            }
        }
        public override GenericRoll Roll(double amount, double chance, bool overUnder)
        {
            var rollParameters = new NameValueCollection
            {
                {"amount", (amount*100000000).ToString(CultureInfo.InvariantCulture)},
                {"target", (overUnder ? 99.99 - chance : chance).ToString("0.00", CultureInfo.InvariantCulture)},
                {"condition", overUnder ? ">" : "<"}
            };
            try
            {
                var roll = JsonConvert.DeserializeObject<PrimeDiceAPIRoll>(Encoding.UTF8.GetString(client.UploadValues($"https://api.primedice.com/api/bet?access_token={_accessToken}", rollParameters)));
                return new GenericRoll
                {
                    status = true,
                    balance = roll.user.balance / 100000000,
                    data = new GenericRollData
                    {
                        amount = roll.bet.amount / 100000000,
                        bet = $"{(overUnder ? "Over" : "Under")} {roll.bet.target.ToString("0.00", CultureInfo.InvariantCulture)}",
                        chance = chance,
                        id = roll.bet.id.ToString(),
                        payout = roll.bet.win ? (roll.bet.profit / 100000000) + amount : 0,
                        profit = roll.bet.win ? (roll.bet.profit / 100000000) : -(roll.bet.amount / 100000000),
                        result = roll.bet.roll,
                        status = roll.bet.win ? "WIN" : "LOSS"
                    }
                };
            }
            catch (WebException e)
            {
                return e.Message.Contains("429") ? new GenericRoll {status = false, error = "Betting too fast. Slow down a bit?" } : new GenericRoll {status = false, error = "Error trying to bet"};
            }
        }
        public override GenericBalance Tip(string payee, double amount)
        {
            var tipParameters = new NameValueCollection
            {
                {"username", payee},
                {"amount", (amount*100000000).ToString(CultureInfo.InvariantCulture)}
            };
            try
            {
                var response = Encoding.UTF8.GetString(client.UploadValues($"https://api.primedice.com/api/tip?access_token={_accessToken}", tipParameters));
                return new GenericBalance {status = true, balance = Balance().balance};
            }
            catch
            {
                return new GenericBalance {status = false, error = "Error!"};
            }
        }
        public override GenericCheck Seed(bool ChangeSeed, string seed)
        {
            return new GenericCheck {status = false, error = "Not implemented"};
        }
        public override void Disconnect()
        {
            try
            {
                client.DownloadString($"https://api.primedice.com/api/logout?access_token={_accessToken}");
            }
            catch
            {
                // ignored
            }
        }
    }
}
