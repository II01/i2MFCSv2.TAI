using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace UserInterface.Services
{
    public class PropertyValidator
    {
        #region members
        Dictionary<string, bool> _properties;
        #endregion

        public PropertyValidator()
        {
            _properties = new Dictionary<string, bool>();
        }

        public void AddOrUpdate(string prop, bool val)
        {
            try
            {
                if (_properties.ContainsKey(prop))
                    _properties[prop] = val;
                else
                    _properties.Add(prop, val);
            }
            catch (Exception e)
            {
                throw new UIException(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public bool IsValid(string prop)
        {
            try
            {
                if (_properties.ContainsKey(prop))
                    return _properties[prop];
                else
                    return false;
            }
            catch (Exception e)
            {
                throw new UIException(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public bool IsValid()
        {
            try
            {
                bool all = _properties.Count() > 0;

                foreach (var p in _properties)
                    all &= p.Value;

                return all;
            }
            catch (Exception e)
            {
                throw new UIException(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
    }
}
