using System.Globalization;

namespace AutoDice.Sites
{
    public abstract class DiceSite
    {
        public bool CanTip = true;
        public double MinTipAmount = 0.0;
        public double TipAmountInterval = 0.00000001;
        public bool CanLevel = true;
        public bool CanJackpot = true;
        public double MaxMultiplier = 98;
        public double MinMultiplier = 0.01;
        public abstract GenericCheck Login(string username, string password, string twofactor);
        public abstract GenericBalance Balance();
        public abstract GenericRoll Roll(double amount, double chance, bool overUnder);
        public abstract GenericBalance Tip(string payee, double amount);
        public abstract GenericCheck Seed(string seed);

        protected static string ToServerString(double value, bool mode)
        {
            return mode ? value.ToString("0.00000000", CultureInfo.InvariantCulture) : value.ToString("0.00").Replace(",", ".");
        }
    }
}
