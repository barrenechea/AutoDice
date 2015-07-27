using System.Collections.Generic;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace AutoDice
{
    #region Da Dice Classes
    #region Da Dice Ajax Classes
    public class DaDiceAjaxRoll
    {
        #region Atributos
        public bool status { get; set; }
        public List<string> message { get; set; }
        public DaDiceAjaxRollData roll { get; set; }
        public DaDiceAjaxBalance balance { get; set; }
        #endregion
    }
    public class DaDiceAjaxRollData
    {
        #region Atributos
        public string id { get; set; }
        public string flag { get; set; }
        public string status { get; set; }
        public string status_message { get; set; }
        public string seed { get; set; }
        public long nonce { get; set; }
        public string user_a { get; set; }
        public double roll_chance { get; set; }
        public string roll_bet { get; set; }
        public double roll_result { get; set; }
        public double amount { get; set; }
        public double payout { get; set; }
        public long micro_stamp { get; set; }
        public string unix_stamp { get; set; }
        public long profit { get; set; }
        public string username { get; set; }
        public double roll_chance_payout { get; set; }
        public string hms_stamp { get; set; }
        public double roll_bet_number { get; set; }
        public long stx_amount { get; set; }
        public long stx_payout { get; set; }
        public string hash { get; set; }
        #endregion
    }
    public class DaDiceAjaxBalance
    {
        #region Atributos
        public long stx { get; set; }
        public double btc { get; set; }
        public int transaction { get; set; }
        #endregion
    }
    #endregion
    #region Da Dice API Classes
    public class DaDiceAPIBalance
    {
        #region Atributos
        public bool status { get; set; }
        public double balance { get; set; }
        public string error { get; set; }
        #endregion
    }
    public class DaDiceAPIRoll
    {
        #region Atributos
        public bool status { get; set; }
        public double balance { get; set; }
        public string error { get; set; }
        public DaDiceAPIRollData roll { get; set; }
        #endregion
    }
    public class DaDiceAPIRollData
    {
        #region Atributos
        public string id { get; set; }
        public string status { get; set; }
        public double amount { get; set; }
        public double payout { get; set; }
        public int nonce { get; set; }
        public double result { get; set; }
        public string bet { get; set; }
        public double chance { get; set; }
        public string timestamp { get; set; }
        #endregion
    }
    #endregion
    #region Da Dice Generic Classes
    public class DaDiceTip
    {
        #region Atributos
        public bool status { get; set; }
        public string error { get; set; }
        public List<string> message { get; set; }
        #endregion
    }
    #endregion
    #endregion
    #region BetterBets Classes
    #region BetterBets API Classes
    #region Balance
    public class BetterBetsAPIBalance
    {
        public string error { get; set; }
        public string errorCode { get; set; }
        public string errorMsg { get; set; }
        public string id { get; set; }
        public string balance { get; set; }
        public string alias { get; set; }
        public string vip_level { get; set; }
        public object vip_granted { get; set; }
        public string time_created { get; set; }
        public string time_last_active { get; set; }
        public string access_token_api { get; set; }
        public string client_seed { get; set; }
        public string client_seed_sequence { get; set; }
        public string client_seed_date { get; set; }
        public string server_seed { get; set; }
        public string last_server_seed { get; set; }
        public string weekly_bets { get; set; }
        public string weekly_wagered { get; set; }
        public string weekly_he_amount { get; set; }
        public string total_bets { get; set; }
        public string total_wagered { get; set; }
        public string total_wagered_lf { get; set; }
        public string total_wins { get; set; }
        public string total_profit { get; set; }
    }


    #endregion
    #region Roll
    public class BetterBetsAPIRoll
    {
        public string error { get; set; }
        public string errorCode { get; set; }
        public string errorMsg { get; set; }
        public string win { get; set; }
        public string balanceOrig { get; set; }
        public string balance { get; set; }
        public string wager { get; set; }
        public string profit { get; set; }
        public string lfNotified { get; set; }
        public string lfActive { get; set; }
        public string lfMaxBetAmt { get; set; }
        public string lfMaturityPercent { get; set; }
        public string lfActivePercent { get; set; }
        public string version { get; set; }
        public string maintenance { get; set; }
        public string happyHour { get; set; }
        public string direction { get; set; }
        public string target { get; set; }
        public string result { get; set; }
        public string clientSeed { get; set; }
        public string serverSeed { get; set; }
        public string nextServerSeed { get; set; }
        public string betId { get; set; }
        public string betIdMP { get; set; }
    }


    #endregion
    #region Tip
    public class BetterBetsAPITip
    {
        public int error { get; set; }
        public string errorCode { get; set; }
        public string errorMsg { get; set; }
        public int success { get; set; }
        public string balance { get; set; }
        public float version { get; set; }
        public int maintenance { get; set; }
        public int happyHour { get; set; }
    }
    #endregion
    #region Seed
    public class BetterBetsAPISeed
    {
        public string error { get; set; }
        public string errorCode { get; set; }
        public string errorMsg { get; set; }
        public string newSeed { get; set; }
    }
    #endregion
    #endregion
    #endregion
    #region Generic Classes
    public class GenericCheck
    {
        public bool status { get; set; }
        public string error { get; set; }
        public string username { get; set; }
    }
    public class GenericBalance
    {
        public bool status { get; set; }
        public string error { get; set; }
        public double balance { get; set; }
    }
    public class GenericRoll
    {
        public bool status { get; set; }
        public string error { get; set; }
        public double balance { get; set; }
        public GenericRollData data { get; set; }
    }
    public class GenericRollData
    {
        #region Atributos
        public string status { get; set; }
        public double amount { get; set; }
        public double payout { get; set; }
        public double profit { get; set; }
        public double result { get; set; }
        public string bet { get; set; }
        public double chance { get; set; }
        public string id { get; set; }

        #endregion
    }
    public class GenericDataGrid
    {
        #region Atributos
        public string status { get; set; }
        public string amount { get; set; }
        public string payout { get; set; }
        public string profit { get; set; }
        public double result { get; set; }
        public string bet { get; set; }
        public string chance { get; set; }
        public string id { get; set; }
        public string hour { get; set; }

        #endregion
    }
    #endregion


    #region UIHelper
    public static class UIHelpers
    {

        #region find parent

        /// <summary>
        /// Finds a parent of a given item on the visual tree.
        /// </summary>
        /// <typeparam name="T">The type of the queried item.</typeparam>
        /// <param name="child">A direct or indirect child of the
        /// queried item.</param>
        /// <returns>The first parent item that matches the submitted
        /// type parameter. If not matching item can be found, a null
        /// reference is being returned.</returns>
        public static T TryFindParent<T>(DependencyObject child)
          where T : DependencyObject
        {
            //get parent item
            DependencyObject parentObject = GetParentObject(child);

            //we've reached the end of the tree
            if (parentObject == null) return null;

            //check if the parent matches the type we're looking for
            T parent = parentObject as T;
            if (parent != null)
            {
                return parent;
            }
            else
            {
                //use recursion to proceed with next level
                return TryFindParent<T>(parentObject);
            }
        }


        /// <summary>
        /// This method is an alternative to WPF's
        /// <see cref="VisualTreeHelper.GetParent"/> method, which also
        /// supports content elements. Do note, that for content element,
        /// this method falls back to the logical tree of the element.
        /// </summary>
        /// <param name="child">The item to be processed.</param>
        /// <returns>The submitted item's parent, if available. Otherwise
        /// null.</returns>
        public static DependencyObject GetParentObject(DependencyObject child)
        {
            if (child == null) return null;
            ContentElement contentElement = child as ContentElement;

            if (contentElement != null)
            {
                DependencyObject parent = ContentOperations.GetParent(contentElement);
                if (parent != null) return parent;

                FrameworkContentElement fce = contentElement as FrameworkContentElement;
                return fce != null ? fce.Parent : null;
            }

            //if it's not a ContentElement, rely on VisualTreeHelper
            return VisualTreeHelper.GetParent(child);
        }

        #endregion
        #region update binding sources

        /// <summary>
        /// Recursively processes a given dependency object and all its
        /// children, and updates sources of all objects that use a
        /// binding expression on a given property.
        /// </summary>
        /// <param name="obj">The dependency object that marks a starting
        /// point. This could be a dialog window or a panel control that
        /// hosts bound controls.</param>
        /// <param name="properties">The properties to be updated if
        /// <paramref name="obj"/> or one of its childs provide it along
        /// with a binding expression.</param>
        public static void UpdateBindingSources(DependencyObject obj,
                                  params DependencyProperty[] properties)
        {
            foreach (DependencyProperty depProperty in properties)
            {
                //check whether the submitted object provides a bound property
                //that matches the property parameters
                BindingExpression be = BindingOperations.GetBindingExpression(obj, depProperty);
                if (be != null) be.UpdateSource();
            }

            int count = VisualTreeHelper.GetChildrenCount(obj);
            for (int i = 0; i < count; i++)
            {
                //process child items recursively
                DependencyObject childObject = VisualTreeHelper.GetChild(obj, i);
                UpdateBindingSources(childObject, properties);
            }
        }

        #endregion


        /// <summary>
        /// Tries to locate a given item within the visual tree,
        /// starting with the dependency object at a given position. 
        /// </summary>
        /// <typeparam name="T">The type of the element to be found
        /// on the visual tree of the element at the given location.</typeparam>
        /// <param name="reference">The main element which is used to perform
        /// hit testing.</param>
        /// <param name="point">The position to be evaluated on the origin.</param>
        public static T TryFindFromPoint<T>(UIElement reference, Point point)
          where T : DependencyObject
        {
            DependencyObject element = reference.InputHitTest(point)
                                         as DependencyObject;
            if (element == null) return null;
            else if (element is T) return (T)element;
            else return TryFindParent<T>(element);
        }
    }
    #endregion

}