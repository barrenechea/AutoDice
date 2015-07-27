using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace AutoDice.Sites
{
    public class BetterBetsAPI : DiceSite
    {
        private HttpClient client;
        private string username, accessToken;

        public override GenericCheck Login(string _username, string _password, string _twofactor)
        {
            CanTip = true;
            MinTipAmount = 0.00000001;
            TipAmountInterval = 0.00000001;
            CanLevel = false;
            CanJackpot = false;
            MaxMultiplier = 97.05;

            username = _username;
            accessToken = _password;
            client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("AutoDice Bot");
            client.DefaultRequestHeaders.Add("x-requested-with", "XMLHttpRequest");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
            client.DefaultRequestHeaders.AcceptLanguage.Add(StringWithQualityHeaderValue.Parse("en-US"));
            client.DefaultRequestHeaders.AcceptLanguage.Add(StringWithQualityHeaderValue.Parse("en"));
            client.DefaultRequestHeaders.AcceptEncoding.Add(StringWithQualityHeaderValue.Parse("gzip"));
            client.DefaultRequestHeaders.AcceptEncoding.Add(StringWithQualityHeaderValue.Parse("deflate"));
            client.DefaultRequestHeaders.AcceptEncoding.Add(StringWithQualityHeaderValue.Parse("sdch"));
            client.DefaultRequestHeaders.Connection.Add("keep-alive");
            
            return new GenericCheck{status = true, username = _username};
        }
        public override GenericBalance Balance()
        {
            try
            {
                var balance = JsonConvert.DeserializeObject<BetterBetsAPIBalance>(client.GetAsync(string.Format("https://betterbets.io/api/user/?accessToken={0}", accessToken)).Result.Content.ReadAsStringAsync().Result);

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
            var contentBet = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("accessToken", accessToken),
                    new KeyValuePair<string, string>("wager", ToServerString(amount, true)),
                    new KeyValuePair<string, string>("chance", ToServerString(chance, false)),
                    new KeyValuePair<string, string>("direction", overUnder ? "1" : "0")
                });
            try
            {
                var resultContent = client.PostAsync("https://betterbets.io/api/betDice/", contentBet).Result;
                
                var roll = JsonConvert.DeserializeObject<BetterBetsAPIRoll>(resultContent.Content.ReadAsStringAsync().Result);
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
            var tipContent = new FormUrlEncodedContent(new[]
                        {
                            new KeyValuePair<string, string>("accessToken", accessToken),
                            new KeyValuePair<string, string>("uname", payee),
                            new KeyValuePair<string, string>("amount", ToServerString(amount, true))
                        });
            try
            {
                var tipData = JsonConvert.DeserializeObject<BetterBetsAPITip>(client.PostAsync("https://betterbets.io/api/tip/", tipContent).Result.Content.ReadAsStringAsync().Result);
                return tipData.success == 1 ? new GenericBalance { status = true, balance = double.Parse(tipData.balance, CultureInfo.InvariantCulture)} : new GenericBalance { status = false, error = tipData.errorMsg };
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