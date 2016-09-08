using System;
using System.Reflection;

namespace LogViewer.Helpers
{
    public static class ApplicationAttributes
    {
        private static readonly Assembly _Assembly = null;

        private static readonly AssemblyTitleAttribute _Title = null;
        private static readonly AssemblyCompanyAttribute _Company = null;
        private static readonly AssemblyCopyrightAttribute _Copyright = null;
        private static readonly AssemblyProductAttribute _Product = null;

        public static string Title { get; private set; }
        public static string CompanyName { get; private set; }
        public static string Copyright { get; private set; }
        public static string ProductName { get; private set; }

        private static Version _Version = null;
        public static string Version { get; private set; }

        static ApplicationAttributes()
        {
            try
            {
                Title = String.Empty;
                CompanyName = String.Empty;
                Copyright = String.Empty;
                ProductName = String.Empty;
                Version = String.Empty;

                _Assembly = Assembly.GetEntryAssembly();

                if (_Assembly != null)
                {
                    object[] attributes = _Assembly.GetCustomAttributes(false);

                    foreach (object attribute in attributes)
                    {
                        Type type = attribute.GetType();

                        if (type == typeof (AssemblyTitleAttribute)) _Title = (AssemblyTitleAttribute) attribute;
                        if (type == typeof (AssemblyCompanyAttribute)) _Company = (AssemblyCompanyAttribute) attribute;
                        if (type == typeof (AssemblyCopyrightAttribute))
                            _Copyright = (AssemblyCopyrightAttribute) attribute;
                        if (type == typeof (AssemblyProductAttribute)) _Product = (AssemblyProductAttribute) attribute;
                    }

                    _Version = _Assembly.GetName().Version;
                }

                if (_Title != null) Title = _Title.Title;
                if (_Company != null) CompanyName = _Company.Company;
                if (_Copyright != null) Copyright = _Copyright.Copyright;
                if (_Product != null) ProductName = _Product.Product;
                if (_Version != null) Version = _Version.ToString();
            }
            catch
            {
            }
        }
    }
}
