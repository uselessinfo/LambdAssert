using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using WatiN.Core;
using System.Threading;

namespace LambdAssert
{
    public class WatinElement
    {
        private readonly Element _ele;
        private readonly TextField _tf;
        private readonly LambdAssertableWithWatin _laww;

        internal LambdAssertableWithWatin ParentLAWW
        {
            get
            {
                return _laww;
            }
        }

        public WatinElement(Element ele, LambdAssertableWithWatin la) : this (ele, null, la) { }

        public WatinElement(Element ele, TextField tf, LambdAssertableWithWatin la)
        {
            _ele = ele;
            _laww = la;
            _tf = tf;

			if (_isFrame && _ele != null && !String.IsNullOrEmpty(_ele.Name)) { _frameNameCached = _ele.Name.ToLower(); }
        }

        public Element Element
        {
            get
            {				
				if (_isFrame)
				{
					return _frameBody;
				}
				else {
					return _ele;
				}                
            }
        }

		private Element _frameBodyCached = null;
		private string _frameNameCached = null;

		private bool _isFrame
		{
			get { return (!String.IsNullOrEmpty(_frameNameCached) || (_ele != null && _ele.Exists && (_ele.TagName ?? "") == "IFRAME")); }
		}

		private Element _frameBody
		{
			get
			{
				if (_isFrame)
				{
					if (_frameBodyCached == null)
					{
						var frame = ParentLAWW.WebBrowser.Frames.Where(f => f.Name != null && _frameNameCached != null && f.Name.ToLower() == _frameNameCached).FirstOrDefault();
						if (frame != null)
						{
							_frameBodyCached = frame.Body;							
						}
						else
						{
							throw new ApplicationException("Could not find frame.");
						}
					}
					return _frameBodyCached;
				}
				else
				{
					throw new ApplicationException("Attemped to access framebody from a non frame element.");
				}
			}
		}

        public string Text
        {
			get { return Element.Text; }
        }
        
		public string CleanText
        {
            get { try { return (Text ?? String.Empty).Trim(); } catch { return String.Empty; } }
        }
        
		public Style Style
        {
			get { return Element.Style; }
        }
        
		public void Click()
        {
            _laww.ClearCache();
            _ele.Click();
			//_laww.LambdAssert.WaitForAjax();
			if(_ele is Link || _ele is Button)
                try { _laww.LambdAssert.WaitForASPNetPostback(); } catch (Exception ex) { }
        }
                
		public void Focus()
        {
            _ele.Focus();
        }

		public void Refresh()
		{
			_frameBodyCached = null;			
		}

        public bool HasText(string text)
        {			
            return CleanText.ToLower().Contains(text.ToLower());
        }

        public void TypeText(string text)
        {
			if (_laww.UseQuickTyping && !_laww.UseDemoMode)
			{
				TypeTextQuick(text);
				return;
			}
			else
			{
				TypeTextSlow(text);
				return;
			}
        }

        public bool Exists
        {
            get { return _ele != null; }
        }

        private void TypeTextQuick(string text)
        {
            if (_tf != null)
            {
                _tf.SetAttributeValue("value", text);
            }
            else
            {
                throw new ApplicationException("TypeText not supposed with tag " + _ele.TagName);
            }
        }

        public void TypeTextSlow(string text)
        {
            if (_tf != null)
            {
                _tf.TypeText(text);
            }
            else
            {
                throw new ApplicationException("TypeText not supposed with tag " + _ele.TagName);
            }
        }

		public void SelectValue(string value)
		{
			if (_ele is SelectList && ParentLAWW != null)
			{
				var _sl = (_ele as SelectList);
				_sl.Focus();
				_sl.SelectByValue(value);
				_sl.Change();
				ParentLAWW.Get().Element.Focus();
			}
			else
			{
				throw new ApplicationException("SelectValue not supposed with tag " + _ele.TagName);
			}
		}

        public void SetValue(string value)
        {
            try
            {
				_ele.SetAttributeValue("value", value);
            }
            catch (Exception ex)
            {
                throw new ApplicationException(string.Format("Failed to set value on element {0}: {1}", _ele.Name, ex.Message));
            }
        }

        public string Value
        {
            get
            {
                return _ele.GetAttributeValue("value");
            }
        }

        public string GetAttributeValue(string attribute)
        {
            return _ele.GetAttributeValue(attribute);
        }

        public WatinElement Get(string selector)
        {
            return _laww.Get(Element, selector);
        }
        public WatinElement Get(Func<Element, bool> check)
        {
            return _laww.Get(Element, check);
        }
        public WatinElementCollection GetList(string selector)
        {
            return _laww.GetList(Element, selector);
        }
        public WatinElementCollection GetList(Func<Element, bool> check)
        {
            return _laww.GetList(Element, check);
        }
    }

    public class WatinElementCollection : IEnumerable
    {
        public Element ParentElement;

        private List<WatinElement> list;
        private WatinElementCollection(List<WatinElement> l)
        {
            list = l;
        }
        public WatinElementCollection(List<WatinElement> l, Element parent)
        {
            list = l;
            ParentElement = parent;
        }

        private LambdAssertableWithWatin LAWW
        {
            get
            {
                if (list != null && list.Count > 0)
                    return list[0].ParentLAWW;
                else
                    return null;
            }
        }

        public WatinElementCollection Each(Action<WatinElement> action)
        {
            foreach (WatinElement ele in list)
                action(ele);

            return this;
        }
        public void Click()
        {
            Each(ele => ele.Click());
        }

        public WatinElement FirstOrDefault
        {
            get
            {
                return list.FirstOrDefault();
            }
        }

		public IEnumerator GetEnumerator()
		{
			return list.GetEnumerator();
		}

        public WatinElement Get(string selector)
        {
            if (LAWW == null)
                return new WatinElement(null, LAWW);

            return LAWW.Get(ParentElement, selector);
        }
        public WatinElement Get(Func<Element, bool> check)
        {
            if (LAWW == null)
                return new WatinElement(null, LAWW);

            return LAWW.Get(ParentElement, check);
        }
        public WatinElementCollection GetList(string selector)
        {
            if (LAWW == null)
                return new WatinElementCollection(new List<WatinElement>());

            return LAWW.GetList(ParentElement, selector);
        }
        public WatinElementCollection GetList(Func<Element, bool> check)
        {
            if (LAWW == null)
                return new WatinElementCollection(new List<WatinElement>());

            return LAWW.GetList(ParentElement, check);
        }

	}
}
