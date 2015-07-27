using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace AutoDice.Sites
{
    public class DaDiceAjax : DiceSite
    {
        private HttpClient client;
        private string authKey, username, password, twofactor;

        public override GenericCheck Login(string _username, string _password, string _twofactor)
        {
            CanTip = true;
            MinTipAmount = 0.00050000;
            TipAmountInterval = 0.00050000;
            var handler = new HttpClientHandler
                {
                    CookieContainer = new CookieContainer()
                };
            client = new HttpClient(handler);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/43.0.2357.130 Safari/537.36");
            client.DefaultRequestHeaders.Add("x-requested-with", "XMLHttpRequest");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
            client.DefaultRequestHeaders.Referrer = new Uri("https://dadice.com/play");
            client.DefaultRequestHeaders.Host = "dadice.com";

            var contentLogin = new FormUrlEncodedContent(new[] 
                {
                    new KeyValuePair<string, string>("command", "auth"),
                    new KeyValuePair<string, string>("signin", "true"),
                    new KeyValuePair<string, string>("username", _username),
                    new KeyValuePair<string, string>("password", _password),
                    new KeyValuePair<string, string>("totp_code", _twofactor)
                });
            try
            {
                var resultContent =
                                client.PostAsync("https://dadice.com/auth?ajax", contentLogin)
                                .Result.Content.ReadAsStringAsync()
                                .Result;
                if (!resultContent.Contains("Redirecting") && !resultContent.Contains("account_deposit"))
                    return new GenericCheck { status = false, error = "Wrong Username/Password/2FA" };
                var web = client.GetAsync("https://dadice.com/play?modal=faucet")
                    .Result
                    .Content
                    .ReadAsStringAsync()
                    .Result
                    .Split('\r', '\n');
                foreach (var a in web.Where(a => a.Contains("auth_key")))
                {
                    authKey = a;
                    authKey = authKey.Substring(44);
                    authKey = authKey.Remove(authKey.IndexOf('"', 2));
                }
                username = _username;
                password = _password;
                twofactor = _twofactor;
                return new GenericCheck { status = true, username = _username};
            }
            catch
            {
                return new GenericCheck { status = false, error = "Unable to connect" };
            }
        }
        public override GenericBalance Balance()
        {
            try
            {
                var contentBalance = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("auth_key", authKey),
                    new KeyValuePair<string, string>("command", "balance_check"),
                });
                var resultContent =
                        client.PostAsync("https://dadice.com/play?ajax", contentBalance)
                        .Result.Content.ReadAsStringAsync()
                        .Result;
                if (resultContent.Contains("Welcome to Da Dice") || (resultContent.Equals(string.Empty))) return new GenericBalance { status = false, error = "You need to renew Cookie" };
                if (resultContent.Contains("true"))
                {
                    resultContent = resultContent.Replace("[true,{", "{\"status\":true,\"balance\":{");
                    resultContent = resultContent.Replace("]", "}");
                }
                else if (resultContent.Contains("false"))
                {
                    resultContent = resultContent.Replace("[false,[", "{\"status\":false,\"message\":");
                    resultContent = resultContent.Remove(resultContent.Length - 2);
                    resultContent = resultContent + "}";
                }
                var newBalance = JsonConvert.DeserializeObject<DaDiceAjaxRoll>(resultContent);
                return new GenericBalance { status = newBalance.status, balance = newBalance.balance.btc, error = newBalance.message != null ? newBalance.message[1] : string.Empty };
            }
            catch
            {
                return new GenericBalance { status = false, error = "Unable to fetch balance" };
            }
        }
        public override GenericRoll Roll(double amount, double chance, bool overUnder)
        {
            var contentBet = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("auth_key", authKey),
                    new KeyValuePair<string, string>("amount", ToServerString(amount, true)),
                    new KeyValuePair<string, string>("command", "roll"),
                    new KeyValuePair<string, string>("chance", ToServerString(chance, false)),
                    new KeyValuePair<string, string>("bet", overUnder ? "over" : "under"),
                    new KeyValuePair<string, string>("auto_bot", "true")
                });
            try
            {
                var resultContent = client.PostAsync("https://dadice.com/play?ajax", contentBet).Result;
                if (!resultContent.IsSuccessStatusCode) return new GenericRoll { status = false, error = "Server issue" };
                var aux = resultContent.Content.ReadAsStringAsync().Result;


                if (aux.Contains("Welcome to Da Dice") || (aux.Equals(string.Empty)))
                {
                    var relogin = Login(username, password, twofactor);
                    return relogin.status ? new GenericRoll {status = false, error = "New cookie fetched"} : new GenericRoll { status = false, error = relogin.error};
                }

                if (aux.Contains("CloudFlare")) return new GenericRoll { status = false, error = "CloudFlare error" };
                if (aux.ToLower().Contains("too many connections")) return new GenericRoll { status = false, error = "Too many connections" };
                if (aux.Contains("true"))
                {
                    aux = aux.Replace("[true,{", "{\"status\":true,\"roll\":{");
                    aux = aux.Replace("{\"stx\"", "\"balance\":{\"stx\"");
                    aux = aux.Replace("]", "}");
                }
                else if (aux.Contains("false"))
                {
                    aux = aux.Replace("[false,[", "{\"status\":false,\"message\":");
                    aux = aux.Remove(aux.Length - 2);
                    aux = aux + "}";
                    if (aux.Contains("\"]]"))
                    {
                        aux = aux.Replace("{\"stx", "\"balance\":{\"stx");
                        aux = aux.Replace("\"]]", "\"]");
                        aux = aux + "}";
                    }
                }
                var roll = JsonConvert.DeserializeObject<DaDiceAjaxRoll>(aux);
                if (roll.status)
                {
                    return new GenericRoll
                    {
                        status = roll.status,
                        error = roll.message != null ? roll.message[1] : string.Empty,
                        balance = roll.balance.btc,
                        data = new GenericRollData
                        {
                            amount = roll.roll.amount,
                            bet = roll.roll.roll_bet,
                            chance = roll.roll.roll_chance,
                            id = roll.roll.id,
                            payout = roll.roll.status_message.Equals("WON") ? roll.roll.payout : 0,
                            profit = (roll.roll.status_message.Equals("WON") ? roll.roll.payout : 0) - roll.roll.amount,
                            result = roll.roll.roll_result,
                            status = roll.roll.status_message.Equals("WON") ? "WIN" : "LOSS"
                        }
                    };
                }
                return new GenericRoll { status = roll.status, error = roll.message != null ? roll.message[1] : string.Empty };
            }
            catch
            {
                return new GenericRoll { status = false, error = "Unable to connect" };
            }
        }
        public override GenericBalance Tip(string payee, double amount)
        {
            var contentBet = new FormUrlEncodedContent(new[]
                        {
                            new KeyValuePair<string, string>("auth_key", authKey),
                            new KeyValuePair<string, string>("command", "tip"),
                            new KeyValuePair<string, string>("payee", payee),
                            new KeyValuePair<string, string>("amount", ToServerString(amount, true))
                        });
            try
            {
                var resultContent =
                        client.PostAsync("https://dadice.com/play?ajax", contentBet)
                        .Result.Content.ReadAsStringAsync()
                        .Result;
                if (resultContent.Contains("Welcome to Da Dice") || (resultContent.Equals(string.Empty))) return new GenericBalance { status = false, error = "You need to renew Cookie" };
                if (resultContent.Contains("true"))
                {
                    return new GenericBalance { status = true, balance = Balance().balance };
                }
                if (resultContent.Contains("false"))
                {
                    resultContent = resultContent.Replace("[false,[", "{\"status\":false,\"message\":");
                    resultContent = resultContent.Remove(resultContent.Length - 2);
                    resultContent = resultContent + "}";
                }
                var tipData = JsonConvert.DeserializeObject<DaDiceTip>(resultContent);
                return new GenericBalance { status = tipData.status, error = tipData.message != null ? tipData.message[1] : string.Empty };
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
