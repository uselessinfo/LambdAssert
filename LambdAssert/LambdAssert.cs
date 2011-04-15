using System;
using System.Collections.Generic;
using MbUnit.Framework;
using Gallio.Framework.Assertions;
using System.Threading;
using System.Linq;

namespace LambdAssert
{
    public class LambdAssert
	{
		#region Properties

        public string LastGetFromSelector = null;

        private LambdAssertableWithWatin _parent = null;
        protected bool HasParentSet
        {
            get { return _parent != null; }
        }
        public LambdAssertableWithWatin ParentLAWW
        {
            get
            {
                if (_parent == null)
                    throw new ApplicationException("In order to use the LambdAssert extensions you need to call into LambdAssertableWithWatin's base constructor  (  public MyClass: base() { }  )");
                return _parent;
            }
        }

		public LambdAssert(LambdAssertableWithWatin laww)
        {
            _parent = laww;

			//if (HasParentSet) {
			//    ParentLAWW.AddPageChangedEvent(InitAjax);
			//    //ParentLAWW.AddPageChangedEvent(InitJqueryCallback);
			//}
        }

        private int _msWait = 2500;
        private int _msInterval = 500;
        private bool throwing;


		#endregion

		#region Private Helpers 
		
		private void DontThrow()
        {
            throwing = (!HasParentSet) ? true : ParentLAWW.ThrowOnFailedGet;
            if (HasParentSet)
                ParentLAWW.ThrowOnFailedGet = false;
        }

        private void MaybeThrow()
        {
            if (HasParentSet)
                ParentLAWW.ThrowOnFailedGet = throwing;
        }
		
		private void ExecuteAssert(Action assertBlock)
        {
            try
            {
                assertBlock();
            }
            catch (AssertionException ex)
            {
                if (HasParentSet && ParentLAWW.CaptureScreenOnFailedAssert)
                {
                    if(string.IsNullOrEmpty(ParentLAWW.ScreenShotPath))
                        throw new ApplicationException("ScreenShotPath must be set to a valid path if CaptureScreenOnFailedAssert is set to true");

                    string fileName = ParentLAWW.ScreenShotPath.TrimEnd('\\') + "\\" + DateTime.Now.ToString().Replace('/', '-').Replace(':', '|').Replace(' ', '_') + ".png";
                    ParentLAWW.WebBrowser.CaptureWebPageToFile(fileName);
                }
                
                throw ex;
            }
        }

        //private void ExecuteAssertOverride(Action assertBlock, string failMessage)
        //{
        //    try
        //    {
        //        AssertionContext.CurrentContext.CaptureFailures(() => assertBlock(), AssertionFailureBehavior.Throw, false);
        //    }
        //    catch (AssertionException)
        //    {
        //        Assert.Fail(failMessage);
        //    }
        //}

		#endregion

		#region Public - Settings
		
		public LambdAssert WaitFor(int ms)
        {
            _msWait = ms;
            return this;
        }

        public LambdAssert WaitForInterval(int ms)
        {
            _msInterval = ms;
            return this;
        }

		#endregion

        #region Protected - Assert Now

        protected LambdAssert IsTrueNow(bool condition)
        {
            ExecuteAssert(() => Assert.IsTrue(condition));
            return this;
        }

        protected LambdAssert IsFalseNow(bool condition)
        {
            ExecuteAssert(() => Assert.IsFalse(condition));
            return this;
        }

        protected LambdAssert EqualToNow<T>(T o1, T o2)
        {
            ExecuteAssert(() => Assert.AreEqual(o1, o2));
            return this;
        }

        protected LambdAssert NotEqualToNow<T>(T o1, T o2)
        {
            ExecuteAssert(() => Assert.AreNotEqual(o1, o2));
            return this;
        }

        protected LambdAssert LessThanNow<T>(T left, T right)
		{
			ExecuteAssert(() => Assert.LessThan<T>(left, right));
			return this;
		}

        protected LambdAssert LessThanOrEqualToNow<T>(T left, T right)
		{
			ExecuteAssert(() => Assert.LessThanOrEqualTo<T>(left, right));
			return this;
		}

        protected LambdAssert GreaterThanNow<T>(T left, T right)
		{
			ExecuteAssert(() => Assert.GreaterThan<T>(left, right));
			return this;
		}

        protected LambdAssert GreaterThanOrEqualToNow<T>(T left, T right)
		{
			ExecuteAssert(() => Assert.GreaterThanOrEqualTo<T>(left, right));
			return this;
		}

        protected LambdAssert NotEmptyNow(object obj)
        {
            if (obj is string)
				ExecuteAssert(() => Assert.IsFalse(string.IsNullOrWhiteSpace((string)obj)));
            else
                ExecuteAssert(() => Assert.IsNotNull(obj));

            return this;
        }

        protected LambdAssert HasTextNow(string text)
		{
			return IsTrue(() => ParentLAWW.Get().HasText(text));			
		}

        protected LambdAssert ExistsNow(WatinElement ele)
		{
			return NotEmptyNow(ele)
					.NotEmptyNow(ele.Element)
					.IsTrueNow(ele.Element.Exists);			
		}

        #endregion

        #region Public - Delayed Asserts

		public LambdAssert IsTrue(Func<bool> condition)
        {
			return ConditionalDelay(
				condition, 
				() => { IsTrueNow(condition()); }
			);
        }

        public LambdAssert IsFalse(Func<bool> condition)
        {
			return ConditionalDelay(
				() => { return !condition(); }, 
				() => { IsFalseNow(condition()); }
			);
        }

        public LambdAssert EqualTo<T>(T expectedValue, Func<T> call) where T : IComparable
        {
			return ConditionalDelay(
				() => { return expectedValue.Equals(call()); },
				() => { EqualToNow(expectedValue, call()); }
			);					
        }

        public LambdAssert NotEqualTo<T>(T expectedValue, Func<T> call) where T : IComparable
        {
			return ConditionalDelay(
				() => { return !expectedValue.Equals(call()); },
				() => { NotEqualToNow(expectedValue, call()); }
			);			
        }

        public LambdAssert NotEmpty<T>(Func<T> condition)
        {
			return ConditionalDelay(
				() => {
					if (typeof(T) == typeof(string))
						return !string.IsNullOrWhiteSpace(condition() as string);
					else
						return null != condition();
				},
				() => { NotEmptyNow(condition()); }
			);
        }

        public LambdAssert Exists(string selectorCode)
        {
			WatinElement ele = null;
			return ConditionalDelay(
				() => { return ((ele = ParentLAWW.Get(selectorCode)).Element != null); },
				() => { ExistsNow(ele); }
			);
       }

        public LambdAssert Exists(Func<WatiN.Core.Element, bool> check)
        {
			WatinElement ele = null;
			return ConditionalDelay(
				() => { return ((ele = ParentLAWW.Get(check)).Element != null); },
				() => { ExistsNow(ele); }
			);
        }

        public LambdAssert NotHasText(string text)
		{
            WatinElement el = null;
            return ConditionalDelay(
                () => { return !(el = ParentLAWW.Get()).HasText(text); },
                () =>
                {
                    ExecuteAssert(() =>
                    {
                        if (el.CleanText.ToLower().Contains(text.ToLower()))
                            Assert.Fail("Expected context would not contain string: {0}", text);
                    });
                }
            );
		}

        public LambdAssert HasText(string text, bool shouldHaveText)
        {
            return shouldHaveText? HasText(text) : NotHasText(text);
        }

        public LambdAssert HasText(string text)
		{
			WatinElement el = null;
            return ConditionalDelay(
                () => { return (el = ParentLAWW.Get()).HasText(text); },
                () =>
                {
                    ExecuteAssert(() =>
                    {
                        if (!el.CleanText.ToLower().Contains(text.ToLower()))
                            Assert.Fail("Expected context to contain string: {0}", text);
                    });
                }
            );
		}

		public LambdAssert HasTextWithFail(string shouldHave, string shouldNotHave)
		{
			return ConditionalDelay(
				() => { return ParentLAWW.Get().HasText(shouldHave) || ParentLAWW.Get().HasText(shouldNotHave); },
				() => { IsFalseNow(ParentLAWW.Get().HasText(shouldNotHave))
							.IsTrue(() => ParentLAWW.Get().HasText(shouldHave)); }
			);
		}

        public LambdAssert Click(string selectorCode, bool waitForPostBack)
        {
            return Click(selectorCode)
                .BranchIf(waitForPostBack, () => WaitForASPNetPostback());
        }

        public LambdAssert Click(string selectorCode)
        {
			ParentLAWW.Get(selectorCode).Click();
			return this;
			
			//WatinElement el = null;
			//return ConditionalDelay(
			//    () =>
			//        {
			//        el = ParentLAWW.Get(selectorCode);
			//        return Guard.GetSafeResult(() => el.Element.Exists, false);
			//        },
			//    () =>
			//        {
			//        ExecuteAssert(() =>
			//            {
			//            if (!Guard.GetSafeResult(() => el.Element.Exists, false))
			//                Assert.Fail("Could not find element to click.  Selector code = " + selectorCode);
			//            });
			//        el.Click();
			//        }
			//);
        }

        #endregion

        #region Public - Waits and Delays

        public LambdAssert Wait(Func<bool> waitFor)
        {
            return Wait(waitFor, _msWait);
        }

        public LambdAssert Wait(Func<bool> waitFor, int duration)
        {
			return Wait(waitFor, null, duration);
        }

        public LambdAssert Wait(Func<bool> waitFor, Action failAction)
        {
            return Wait(waitFor, failAction, _msWait);
        }

        public LambdAssert Wait(Func<bool> waitFor, Action failAction, int duration)
        {
			return ConditionalDelay(waitFor, null, failAction, _msWait);
        }

		public LambdAssert ConditionalDelay(Func<bool> waitFor, Action call)
		{
			return ConditionalDelay(waitFor, call, null, _msWait);
		}

		public LambdAssert ConditionalDelay(Func<bool> waitFor, Action call, Action failAction, int duration)
        {
			DateTime start = DateTime.Now;
			DontThrow();

            while (!waitFor() && DateTime.Now.Subtract(start).TotalMilliseconds < duration)
                Thread.Sleep(_msInterval);

            if (!waitFor() && failAction != null) { failAction(); }
            
            //bool res;
            //while (!(res = waitFor()) && DateTime.Now.Subtract(start).TotalMilliseconds < duration)
            //    Thread.Sleep(_msInterval);

            //if (!res && failAction != null) { failAction(); }

			MaybeThrow();

			if (call != null) { call(); }
			return this;
        }

		public LambdAssert PauseIfDemo()
		{
			return PauseIfDemo("Click to continue...");
		}

		public LambdAssert PauseIfDemo(string message)
		{
			if (ParentLAWW.UseDemoMode)
			{
				ParentLAWW.WebBrowser.DialogWatcher.CloseUnhandledDialogs = false;
                
                //string js = "alert('" + message.Replace("'", "\\\\'") + "');";
                //ParentLAWW.WebBrowser.Eval(js);

                ExecuteJS("alert('{0}')", message);

                //This is another way to approach this:
                //ParentLAWW.WebBrowser.Eval("alert('" + message.Replace("'", "'+\"'\"+'") + "');");
				
				ParentLAWW.WebBrowser.DialogWatcher.CloseUnhandledDialogs = true;							
			}
			return this;
		}


        #endregion

        #region Public - Control Flow

        public LambdAssert BranchIf(bool condition, Action actionIfTrue) {
            return Branch(condition, actionIfTrue, () => { });
        }

        public LambdAssert Each(WatinElementCollection list, Action<WatinElement> action)
        {
            list.Each(ele => action(ele));
            return this;
        }

        public LambdAssert Branch(bool condition, Action actionIfTrue, Action actionIfFalse)
        {
            if (condition)
            {
                actionIfTrue();
            }
            else
            {
                actionIfFalse();
            }
            return this;
        }
        #endregion

		#region Public - Utilities

		public LambdAssert InWindowNow(string windowTitle, Action action)
		{
			if (ParentLAWW == null)
				throw new ApplicationException("Cannot use window attachments without using LambdAssertableWithWatin and implementing a call to its CTOR");

			ParentLAWW.AttachToDifferentWindow(windowTitle, 0);
			action();
			ParentLAWW.UnAttachToWindow();

			return this;
		}

		public LambdAssert InWindow(string windowTitle, Action action)
		{
			return InWindow(windowTitle, action, true);
		}

		public LambdAssert InWindow(string windowTitle, Action action, bool closeWindowOnExit)
		{
			if (ParentLAWW == null)
				throw new ApplicationException("Cannot use window attachments without using LambdAssertableWithWatin and implementing a call to its CTOR");

			ParentLAWW.AttachToDifferentWindow(windowTitle, _msWait);
			var childBrowser = ParentLAWW.WebBrowser;

			try
			{
				action();
			}
			finally
			{
				ParentLAWW.UnAttachToWindow();

				if (closeWindowOnExit)
					childBrowser.Close();
			}

			return this;
		}

		public LambdAssert GetFrom(WatinElement ele)
		{
			if (ParentLAWW == null)
				throw new ApplicationException("Cannot use GetFrom() without using LambdAssertableWithWatin and implementing a call to its CTOR");

			ParentLAWW.ClearCache();
			ParentLAWW.SetElement(ele);
			return this;
		}

		public LambdAssert GetFrom(WatiN.Core.Element ele)
		{
			return GetFrom(new WatinElement(ele, ParentLAWW));
		}

		public LambdAssert GetFrom(string selectorCode)
		{
			//Technically we clear the cache twice, but it has to be done before we call Get below
			ParentLAWW.ClearCache();

            GetFrom();      //Bug fix -- clear before setting so we're not limited to calling GetFrom() on things inside the current GetFrom() call.
			DontThrow();
			WatinElement ele = ParentLAWW.Get(selectorCode);

			Wait(() => (ele = ParentLAWW.Get(selectorCode)).Element != null && ele.Element.Exists);

			MaybeThrow();

			if (ele.Element == null)
				throw new ApplicationException("Could not GetFrom '" + selectorCode + "', element was not found despite waiting.");

            LastGetFromSelector = selectorCode;

			return GetFrom(ele);
		}

		public LambdAssert GetFrom()
		{
			if (ParentLAWW == null)
				throw new ApplicationException("Cannot use GetFrom() without using LambdAssertableWithWatin and implementing a call to its CTOR");

			ParentLAWW.SetElement(null);
            LastGetFromSelector = null;
			return this;
		}

		public LambdAssert DoNow(Action act)
		{
			act();
			return this;
		}

        public LambdAssert ExecuteJS(string JS)
        {
            ParentLAWW.WebBrowser.Eval(JS);
            return this;
        }

        public LambdAssert ExecuteJS(string js, params string[] parms)
        {
            string jsAssembled = js;
            int parmId = 0;

            foreach (string parm in parms)
            {
                int pos;
                while ((pos = js.IndexOf("{" + parmId + "}")) > -1)
                {
                    bool isString = pos > 0 && js[pos - 1] == '\'' || js[pos - 1] == '"';
                    char stringDelm = isString ? js[pos - 1] : ' ';
                    string repWith = parm;

                    if (isString)
                    {
                        if (stringDelm == '"')
                            repWith = repWith.Replace("\"", "\\\\\"");
                        else
                            repWith = repWith.Replace("'", "\\\\'");
                    }

                    js = js.Replace("{" + parmId + "}", repWith);
                }
            }

            return ExecuteJS(js);
        }

		#endregion

		#region Public - Web Waits

		#region Init Methods
		
		private void InitAjax()
		{
			ParentLAWW.WebBrowser.RunScript(@"				
			(function () {
				var pendingRequests = 0;
				var _XMLHttpRequest;
				var _ActiveXObject;

				if (window.XMLHttpRequest) { _XMLHttpRequest = window.XMLHttpRequest; }
				if (window.ActiveXObject) { _ActiveXObject = window.ActiveXObject; }

				var trackableXHR = function (createXHR) {
					var realXHR = createXHR();

					this.readyState = null;
					this.responseBody = null;
					this.responseText = null;
					this.responseXML = null;
					this.status = null;
					this.statusText = null;
					this.timeout = null;

					this.onreadystatechange = function () { };
					this.ontimeout = function () { };

					this.updateStatus = function () {
						try {
							this.timeout = realXHR.timeout;
							this.readyState = realXHR.readyState;
							this.status = realXHR.status;
							this.statusText = realXHR.statusText;
							this.responseBody = realXHR.responseBody;
							this.responseText = realXHR.responseText;
							this.responseXML = realXHR.responseXML;
						} catch (ex) { }
					};

					this.open = function (method, url, isAsync, username, password) {
						var that = this;
						that.updateStatus = this.updateStatus;

						realXHR.open(method, url, isAsync, username, password);

						realXHR.onreadystatechange = function () {
							if (realXHR.readyState === 4) {
								pendingRequests = Math.max(pendingRequests - 1, 0);
							}
							that.updateStatus();
							that.onreadystatechange();
						};

						if (realXHR.ontimeout) {
							realXHR.ontimeout = function () {
								pendingRequests = Math.max(pendingRequests - 1, 0);
								that.updateStatus();
								that.ontimeout();
							};
						}
					};

					this.send = function (body) {
						if (realXHR) {
							pendingRequests += 1;
							realXHR.send(body);
							this.updateStatus();
						}
					};

					this.abort = function () {
						if (realXHR) {
							if (realXHR.readyState !== 4) { pendingRequests = Math.max(pendingRequests - 1, 0); }
							this.onreadystatechange = function () { };
							realXHR.abort();
							this.updateStatus();
						}
					};

					this.getAllResponseHeaders = function () {
						if (realXHR) {
							return realXHR.getAllResponseHeaders();
						}
					};

					this.setRequestHeader = function (name, value) {
						if (realXHR) {
							realXHR.setRequestHeader(name, value);
							this.updateStatus();
						}
					};

					// For the ActiveX version
					this.Open = this.open;
					this.Send = this.send;
					this.Abort = this.abort;
					this.GetAllResponseHeaders = this.getAllResponseHeaders;
					this.SetRequestHeader = this.setRequestHeader;
				};

				window.XMLHttpRequest = (function () {
					var ret = function () { return new trackableXHR(function () { return new _XMLHttpRequest(); }); };
					ret.HasPendingXHRRequests = function () { return (pendingRequests > 0); };
					return ret;
				} ());

				if (_ActiveXObject) {
					window.ActiveXObject = function (progid) {
						if (progid.toLowerCase().indexOf('xmlhttp') !== -1) {
							return new trackableXHR(function () { return new _ActiveXObject(progid); });
						} else {
							return new _ActiveXObject(progid);
						}
					};
				}
			} ());
			".Replace(Environment.NewLine, "").Replace("\t", String.Empty).Replace("  ", " "));
		}

		private void InitJqueryCallback()
		{
			ParentLAWW.WebBrowser.RunScript(@"
					var LambdAssert_PendingRequests = 0;
					try {
						$(document).ajaxStart(function(){ 							
							LambdAssert_PendingRequests += 1;							
						}).ajaxStop(function(){ 
							LambdAssert_PendingRequests = Math.max(0, LambdAssert_PendingRequests - 1);
						});
					} catch(ex) { }

					function LambdAssert_HasNoAjax(){
						return (LambdAssert_PendingRequests <= 0);
					}
				".Replace(Environment.NewLine, "").Replace("\t", String.Empty).Replace("  ", " "));
		}

		#endregion

        public LambdAssert WaitForComplete()
        {
            ParentLAWW.WebBrowser.WaitForComplete();
            return this;
        }
        public LambdAssert WaitForAjax()
		{			
			return Wait(() => { return !Convert.ToBoolean(ParentLAWW.WebBrowser.Eval("XMLHttpRequest.HasPendingXHRRequests();")); });			
		}

		public LambdAssert WaitForJQueryCallback()
		{
			return Wait(() =>
			{
				return Convert.ToBoolean(ParentLAWW.WebBrowser.Eval("LambdAssert_HasNoAjax();"));
			});
		}

		public LambdAssert WaitForASPNetPostback()
		{
			return Wait(() =>
			{
				return Convert.ToBoolean(ParentLAWW.WebBrowser.Eval(@"
                    var F=function() {
	                    try {
	                        var pendejo = Sys.WebForms.PageRequestManager.getInstance();
	                        return !pendejo.get_isInAsyncPostBack();	
                        } catch(ex)
                        { return false; }
                    }; 
                    F();
                    ".Replace(Environment.NewLine, "").Replace("\t", String.Empty).Replace("  ", " ")));
			});
		}

		#endregion

	}
}
