using System.Globalization;
using System.Net;
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
            var url = string.Format("https://dadice.com/api/balance?username={0}&key={1}", username, password);
            try
            {
                var aux = JsonConvert.DeserializeObject<DaDiceAPIBalance>(client.DownloadString(url));
                _username = username;
                _apikey = password;
                return new GenericCheck
                {
                    status = aux.status,
                    username = username,
                    error = aux.error
                };
            }
            catch
            {
                return new GenericCheck
                {
                    status = false,
                    error = "Unable to connect"
                };
            }
        }
        public override GenericBalance Balance()
        {
            try
            {
                var url = string.Format("https://dadice.com/api/balance?username={0}&key={1}", _username, _apikey);
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
            var url = string.Format("https://dadice.com/api/roll?username={0}&key={1}&amount={2}&chance={3}&bet={4}", _username, _apikey, ToServerString(amount, true), ToServerString(chance, false), overUnder ? "over" : "under");
            try
            {
                var roll = JsonConvert.DeserializeObject<DaDiceAPIRoll>(client.DownloadString(url));
                return new GenericRoll
                {
                    status = roll.status,
                    error = roll.error,
                    balance = roll.balance,
                    data = new GenericRollData
                    {
                        status = roll.roll.status.Equals("WON") ? "WIN" : "LOSS",
                        amount = roll.roll.amount,
                        bet = roll.roll.bet,
                        chance = double.Parse(roll.roll.bet.Replace(",", "."), CultureInfo.InvariantCulture),
                        id = roll.roll.id,
                        payout = roll.roll.status.Equals("WON") ? roll.roll.payout : 0,
                        profit = (roll.roll.status.Equals("WON") ? roll.roll.payout : 0) - roll.roll.amount,
                        result = roll.roll.result,
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
            try
            {
                var url = string.Format("https://dadice.com/api/tip?username={0}&key={1}&amount={2}&payee={3}",
                    _username, _apikey, ToServerString(amount, true), payee);
                var aux = JsonConvert.DeserializeObject<DaDiceTip>(client.DownloadString(url));
                return new GenericBalance { status = aux.status, error = aux.error, balance = Balance().balance };
            }
            catch
            {
                return new GenericBalance { status = false, error = "Unable to connect" };
            }
        }
        public override GenericCheck Seed(string seed)
        {
            return new GenericCheck{status = false, error = "Not implemented"};
        }
    }
}
