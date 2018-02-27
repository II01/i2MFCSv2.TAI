using Infralution.Localization.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Resources;

namespace UserInterface.Services
{
    class LocalizedEnumConverter : ResourceEnumConverter
    {
        public LocalizedEnumConverter(Type type) : base(type, Properties.Resources.ResourceManager)
        {
        }
    }
    public static class EnumUtil
    {
        public static IEnumerable<T> GetValues<T>()
        {
            return Enum.GetValues(typeof(T)).Cast<T>();
        }
    }
    public class ResourceReader
    {
        private static ResourceManager _rm = Properties.Resources.ResourceManager;
        private static ResourceSet _rs = null;

        public static void ChangeResourceSet(CultureInfo culture)
        {
            try
            {
                _rs = _rm.GetResourceSet(culture, true, true);
            }
            catch(Exception e)
            {
                throw new UIException(string.Format("{0}.{1}: {2}", "ResourceReader", (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }

        }
        public static string GetString(string resID)
        {
            try
            {
                if (_rs == null)
                    ChangeResourceSet(CultureManager.UICulture);

                if (_rs != null)
                {
                    string value = _rs.GetString(resID);
                    if (value == null)
                        value = "#" + resID;
                    return value;
                }
                return "NULL";
            }
            catch (Exception e)
            {
                throw new UIException(string.Format("{0}.{1}: {2}", "ResourceReader", (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
    }

}
