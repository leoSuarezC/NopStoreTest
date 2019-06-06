using System;
using Microsoft.AspNetCore.Http;

namespace Omikron.FactFinder.Util
{
    /// <summary>
    /// Abstraction of HttpContext.Current to make testing of the web service easier.
    /// Code mostly taken from http://stackoverflow.com/a/4053620/1633117.
    /// </summary>
    public class HttpContextFactory
    {
        private static HttpContext _context;
        
        /// <summary>
        /// Set this property to hand out a certain HttpContextBase object (e.g. a mock).
        /// If this is not set, the factory will attempt to supply HttpContext.Current.
        /// </summary>
        public static HttpContext Current
        {
            get
            {
                if (_context != null )
                {
                    return _context;
                }

                throw new InvalidOperationException("HttpContext not available.");
            }
            set
            {
                _context = value;
            }
        }
    }
}
