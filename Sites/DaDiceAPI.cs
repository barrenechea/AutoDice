using System.Collections.Specialized;
using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace AutoDice.Sites
{
    public class DaDiceAPI : DiceSite
    {
        private readonly WebClient client = new WebClient();
        private string _username, _apikey;

        public override GenericCheck Login(string username, string password, string twofactor)
        {
            CanTip = true;
            MinTipAmount = 0.00050000;
            TipAmountInterval = 0.00050000;
            _username = username;
            _apikey = password;
            return new GenericCheck { status = true, username = username };
        }
        public override GenericBalance Balance()
        {
            try
            {
                var url = $"https://dadice.com/api/balance?username={_username}&key={_apikey}";
                var aux = JsonConvert.DeserializeObject<DaDiceAPIBalance>(client.DownloadString(url));
                return new GenericBalance { status = aux.status, balance = aux.balance, error = aux.error};
            }
            catch
            {
                return new GenericBalance{status = false, error = "Unable to connect"};
            }
        }
        public override GenericRoll Roll(double amount, double chance, bool overUnder)
        {
            var rollParameters = new NameValueCollection
            {
                {"username", _username},
                {"key", _apikey},
                {"amount", ToServerString(amount, true)},
                {"chance", ToServerString(chance, false)},
                {"bet", overUnder ? "over" : "under"}
            };
            try
            {
                var aux = Encoding.UTF8.GetString(client.UploadValues("https://dadice.com/api/roll", rollParameters));
                var roll = JsonConvert.DeserializeObject<DaDiceAPIRoll>(aux);
                return new GenericRoll
                {
                    status = roll.status,
                    error = roll.error,
                    balance = roll.balance,
                    data = new GenericRollData
                    {
                        status = roll.roll.status.Equals("WON") ? "WIN" : "LOSS",
                        amount = roll.roll.amount,
                        payout = roll.roll.status.Equals("WON") ? roll.roll.payout : 0,
                        profit = (roll.roll.status.Equals("WON") ? roll.roll.payout : 0) - roll.roll.amount,
                        result = roll.roll.result,
                        bet = roll.roll.bet,
                        chance = roll.roll.chance,
                        id = roll.roll.id
                    }
                };
            }
            catch
            {
                return new GenericRoll
                {
                    status = false,
                    error = "Unable to connect"
                };
            }
        }
        public override GenericBalance Tip(string payee, double amount)
        {
            var tipParameters = new NameValueCollection
            {
                {"username", _username},
                {"key", _apikey},
                {"amount", ToServerString(amount, true)},
                {"payee", payee}
            };
            try
            {
                var aux = JsonConvert.DeserializeObject<DaDiceTip>(Encoding.UTF8.GetString(client.UploadValues("https://dadice.com/api/tip", tipParameters)));
                return new GenericBalance { status = aux.status, error = aux.error, balance = Balance().balance };
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
