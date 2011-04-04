using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace LambdAssert
{
    public static class LambdAssertExtensions
    {
        private static string IntranetLoginUrl = ConfigurationManager.AppSettings["IntranetLogin"];

        #region Login Helpers

        public static LambdAssert LoginAsCSR(this LambdAssert la)
        {
            Login(la, "testcsr", "testcsr");
            return la;
        }

        public static LambdAssert LoginAsSupervisor(this LambdAssert la)
        {
            Login(la, "testsup", "testsup");
            return la;
        }
        public static LambdAssert LoginAsAdmin(this LambdAssert la)
        {
            Login(la, "dave", "test11");
            return la;
        }
        
        public static LambdAssert Login(this LambdAssert la, string user, string pwd)
        {
			if (la.ParentLAWW.WebBrowser is WatiN.Core.IE) {
				(la.ParentLAWW.WebBrowser as WatiN.Core.IE).ClearCookies();
			}
            
            la.ParentLAWW.WebBrowser.GoTo(IntranetLoginUrl);
            la.ParentLAWW.WebBrowser.WaitForComplete();

            la.Wait(() => la.ParentLAWW.Get().HasText("Please fill in your username and password to gain access"), 60000)
               .WaitFor(15000)
               .WaitForInterval(100);

            la.ParentLAWW.Get("#user_id").TypeText(user);
            la.ParentLAWW.Get("#password").TypeText(pwd);
			la.ParentLAWW.Get("#login").Click();            

            return la;
        }
        #endregion

        #region Customer Search Helpers

        public static LambdAssert GoToCustomerSearch(this LambdAssert la)
        {
            if (la.ParentLAWW.Get().HasText("Prospect Management"))
            {
                la.ParentLAWW.Get("'Existing Customer").Click();
                la.IsTrue(() => la.ParentLAWW.Get().HasText("Logged in as: "));
            }
            else
            {
                la.ParentLAWW.Get("'Find Customer").Click();
                la.IsTrue(() => la.ParentLAWW.Get().HasText("Customer Number:"));
            }

            return la;
        }
        public static LambdAssert SearchForCustomerByCustomerNumber(this LambdAssert la, string customerNumber)
        {
            la.ParentLAWW.Get("_atbCustomerNumber").TypeText(customerNumber);
            la.ParentLAWW.Get("_btnSearch").Click();

            la.Wait(() => la.ParentLAWW.Get().HasText("Customer #"))
                .IsTrue(() => la.ParentLAWW.Get().HasText("Customer #"));

            return la;
        }

        #endregion

        #region NavigationHelpers

        public static LambdAssert GoToTab(this LambdAssert la, string thisTab)
        {
            la.ParentLAWW.Get(".contentTabList").Get("'" + thisTab).Click();
            return la;
        }

        #endregion
    }
}