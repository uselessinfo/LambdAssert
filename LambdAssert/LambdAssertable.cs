using System;
using System.Collections.Generic;
using System.Linq;
using WatiN.Core;
using System.Configuration;
using System.Threading;

namespace LambdAssert
{
    public abstract class LambdAssertable
    {
        public LambdAssert LambdAssert = new LambdAssert(null);
    }

    public abstract class LambdAssertableWithWatin : LambdAssertable
    {
        #region Properties and Caching Helpers

        public bool UseQuickTyping = true;
        public bool ThrowOnFailedGet = true;
        public bool CaptureScreenOnFailedAssert = false;
        public string ScreenShotPath = @"C:\Temp\";

		public bool UseDemoMode = bool.Parse(ConfigurationManager.AppSettings["UseDemoMode"] ?? "false");

        public LambdAssertableWithWatin()
        { LambdAssert = new LambdAssert(this); }

        private Dictionary<string, WatinElement> ElementCache = new Dictionary<string, WatinElement>();

        public LambdAssert GoToUrlInFreshBrowser(string url)
        {
            using (IE ie = WebBrowser as IE)
            {
                ie.ClearCookies();
                ie.ClearCache();
                ie.Close();
            }

            WebBrowser.Reopen();
            WebBrowser.GoTo(url);
            WebBrowser.WaitForComplete();
            return LambdAssert;
        }

        public LambdAssert GoTo(string url)
        {
            WebBrowser.GoTo(url);
            WebBrowser.WaitForComplete();

            return LambdAssert;
        }

        internal void SetElement(WatinElement ele)
        {
            ElementToSelectFrom = ele;
        }

		private WatinElement ElementToSelectFrom = null;
		private Element ActiveElement
        {
            get
            {
                if (ElementToSelectFrom == null)
                    return WebBrowser.Body;
                else
                    if (!ElementToSelectFrom.Element.Exists)
                        LambdAssert.GetFrom(LambdAssert.LastGetFromSelector);
                    return ElementToSelectFrom.Element;
            }
        }

		private Browser _webBrowser;
        public Browser WebBrowser
        {
            get
            {				
				if (_webBrowser == null) 
				{
					InitWebBrowser();					
				}
				return _webBrowser;				
            }
            set { _webBrowser = value; }
        }

		private Stack<Browser> BrowserStack = new Stack<Browser>();
        public void AttachToDifferentWindow(string title, int ms)
        {
            BrowserStack.Push(WebBrowser);
			WebBrowser = Browser.AttachTo<IE>(Find.ByTitle(title), ms);			
        }
        public void UnAttachToWindow()
        {
            if (BrowserStack.Count > 0)
                WebBrowser = BrowserStack.Pop();
            else
                throw new ApplicationException("Could not Unattach to IE:  At root level");
        }

        public void ClearCache()
        {
            ElementCache = new Dictionary<string, WatinElement>();

			if (ElementToSelectFrom != null)
			{
				ElementToSelectFrom.Refresh();
			}
        }

        private WatinElement SetInCache(string selector, WatinElement ele)
        {
            if (ele == null || ele.Element == null)
            {
                if (ThrowOnFailedGet)
                    throw new ApplicationException("Get failed for selector '" + selector + "', could not find element for assertion.");
                return new WatinElement(null, this);;
            }

            if(ElementCache.ContainsKey(selector))
                ElementCache[selector] = ele;
            else
                ElementCache.Add(selector, ele);

            return ele;
        }

		private void InitWebBrowser()
		{
			Settings.AutoMoveMousePointerToTopLeft = false;
			_webBrowser = new IE();
			//_webBrowser = new FireFox();
		}


		private string currentUrl = String.Empty;
		private List<Action> pageChanged = new List<Action>();
		
		public void AddPageChangedEvent(Action callback)
		{
			pageChanged.Add(callback);
		}	
	
		public void CheckPageChanged()
		{
			return; 
			// Not really ready yet.
			if (!String.Equals(WebBrowser.Url, currentUrl)){
				currentUrl = WebBrowser.Url;
				foreach (var c in pageChanged) { c(); }
			}			
		}					

        #endregion

        /*
            Selector options (in addition to sizzle):
         
                #name           = Name or Id
                'text           = Link which contains text
                _name           = ASP.NET id particle
                =attr:value     = Attribute/Value selection
        */
        
        #region Get

        //Empty Get
        public WatinElement Get()
        {
			CheckPageChanged();
            return new WatinElement(ActiveElement, this);
        }

        //Get that does something
		private WatinElement GetInternal<TElement, TCollection>(BaseElementCollection<TElement, TCollection> ec, Func<Element, bool> check)
			where TElement : global::WatiN.Core.Element
			where TCollection : global::WatiN.Core.BaseElementCollection<TElement, TCollection>
        {
			CheckPageChanged();
            Element found = ec.Where(ele => check(ele)).FirstOrDefault();
            if (found != null && found is TextField)
                return new WatinElement(found, found as TextField, this);
            else
                return new WatinElement(found, this);
        }

        //Gets that reference other Gets (or GetList)
		private WatinElement GetInternal<TElement, TCollection>(BaseElementCollection<TElement, TCollection> elements, string selectorCode)
			where TElement : global::WatiN.Core.Element
			where TCollection : global::WatiN.Core.BaseElementCollection<TElement, TCollection>
        {
			CheckPageChanged();
			if (ElementCache.ContainsKey(selectorCode) && ElementCache[selectorCode] != null)
                return ElementCache[selectorCode];

            return SetInCache(selectorCode, GetList(elements, selectorCode).FirstOrDefault);
        }

        public WatinElement Get(string selectorCode)
        {
            return Get(ActiveElement, selectorCode);
        }

        public WatinElement Get(Func<Element, bool> check)
        {
            return Get(ActiveElement, check);
        }

		public WatinElement Get(Element el, Func<Element, bool> check)			
		{
			if (el is IElementContainer)
			{
				return GetInternal((el as IElementContainer).Elements, check);
			}
			else
			{
				return new WatinElement(null, this);
			}
		}

		public WatinElement Get(Element el, string selectorCode)			
		{
			if (el is IElementContainer)
			{
				return GetInternal((el as IElementContainer).Elements, selectorCode);
			}
			else
			{
				return new WatinElement(null, this);
			}
		}

        #endregion

        #region GetList

        //GetList that does stuff
        private WatinElementCollection GetList<TElement, TCollection>(BaseElementCollection<TElement, TCollection> elements, Func<Element, bool> check, Element parent = null)
			where TElement : global::WatiN.Core.Element
			where TCollection : global::WatiN.Core.BaseElementCollection<TElement, TCollection>
        {
			
            var retval = new List<WatinElement>();
            var eles = elements.Where(ele => check(ele)).ToList();

            foreach(Element found in eles)
                if (found != null && found is TextField)
                    retval.Add(new WatinElement(found, found as TextField, this));
                else
                    retval.Add(new WatinElement(found, this));

            return new WatinElementCollection(retval, parent);
        }

        private WatinElementCollection GetList<TElement, TCollection>(BaseElementCollection<TElement, TCollection> elements, string selectorCode, Element parent = null)
			where TElement : global::WatiN.Core.Element
			where TCollection : global::WatiN.Core.BaseElementCollection<TElement, TCollection>
        {
            if (selectorCode.StartsWith("#"))
            {
                string name = selectorCode.TrimStart('#');
                return GetList(elements, ele => (ele.TagName ?? "").ToLower() != "form" && ((ele.Id ?? "") == name || (ele.Name ?? "") == name));
            }
            else if (selectorCode.StartsWith("'"))
            {
                string txt = selectorCode.TrimStart('\'');
                return GetList(elements, ele => (ele.TagName ?? "").ToLower() == "a" && (ele.Text ?? "").StartsWith(txt));
            }
            else if (selectorCode.StartsWith("_"))
            {
                return GetList(elements, ele => (ele.Id ?? "").EndsWith(selectorCode) || (ele.Id ?? "") == selectorCode.TrimStart('_'));
            }
            else if (selectorCode.StartsWith("."))
            {
                string cssClass = selectorCode.TrimStart('.').ToLower();
                return GetList(elements, ele => (ele.ClassName ?? "").ToLower().Contains(cssClass));
            }
            else if (selectorCode.StartsWith("~"))
            {
				string txt = selectorCode.TrimStart('~');
                return GetList(elements, ele => (ele.Name ?? "").StartsWith(txt) || (ele.Name ?? "").EndsWith(txt));
            }
            else if (selectorCode.StartsWith("="))
            {
                string[] pair = selectorCode.TrimStart('=').Split(':');
                return GetList(elements, ele => (ele.GetAttributeValue(pair[0]) ?? "") == pair[1]);
            }
            else
            {
                List<WatinElement> eles = new List<WatinElement>();
                foreach (Element ele in elements.Filter(Find.BySelector(selectorCode)))
                    if (ele != null && ele is TextField)
                        eles.Add(new WatinElement(ele, ele as TextField, this));
                    else
                        eles.Add(new WatinElement(ele, this));

                return new WatinElementCollection(eles, parent);
            }
        }

        public WatinElementCollection GetList(string selectorCode)
        {
            return GetList(ActiveElement, selectorCode);
        }
        public WatinElementCollection GetList(Func<Element, bool> check)
        {
            return GetList(ActiveElement, check);
        }

		public WatinElementCollection GetList(Element el, Func<Element, bool> check)
		{
			if (el is IElementContainer)
			{
				return GetList((el as ElementContainer<Element>).Elements, check);
			}
			else
			{
				return new WatinElementCollection(new List<WatinElement>(), el);
			}
		}

		public WatinElementCollection GetList(Element el, string selectorCode)
		{
			if (el is IElementContainer)
			{
				return GetList((el as IElementContainer).Elements, selectorCode);
			}
			else
			{
				return new WatinElementCollection(new List<WatinElement>(), el);
			}
		}

        #endregion
    }


	

}
