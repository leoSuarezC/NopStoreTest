using System;
using System.Collections.Specialized;
using log4net;
using Omikron.FactFinder.Util;

namespace Omikron.FactFinder.Core.Client
{
    public class RequestParser
    {
        private static ILog log;

        private ParametersConverter ParametersConverter;

        private NameValueCollection _serverRequestParameters;
        public NameValueCollection RequestParameters
        {
            get
            {
                if (_serverRequestParameters == null)
                {
                    _serverRequestParameters = ParametersConverter.ClientToServerRequestParameters(ClientRequestParameters);
                }
                return _serverRequestParameters;
            }
        }

        private NameValueCollection _clientRequestParameters;
        public NameValueCollection ClientRequestParameters
        {
            get
            {
                if (_clientRequestParameters == null)
                {
                    var request = HttpContextFactory.Current.Request;
                    _clientRequestParameters = System.Web.HttpUtility.ParseQueryString(HttpContextFactory.Current.Request.QueryString.ToString());
                    //_clientRequestParameters.Add((NameValueCollection)HttpContextFactory.Current.Request.Form);
                }
                return _clientRequestParameters;
            }
        }

        private string _requestTarget;
        public string RequestTarget
        {
            get
            {
                if (_requestTarget == null)
                {
                    var url = HttpContextFactory.Current.Request.Host + HttpContextFactory.Current.Request.Path;
                    _requestTarget = String.Format("{0}://{1}{2}", HttpContextFactory.Current.Request.Scheme, 
                        HttpContextFactory.Current.Request.Host,
                        HttpContextFactory.Current.Request.Path);
                }
                return _requestTarget;
            }
        }

        static RequestParser()
        {
            log = LogManager.GetLogger(typeof(RequestParser));
        }

        public RequestParser()
        {
            log.Debug("Initialize new RequestParser.");
            ParametersConverter = new ParametersConverter();
        }
    }
}
