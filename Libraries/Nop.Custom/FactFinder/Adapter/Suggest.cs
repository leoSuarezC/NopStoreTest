using System;
using System.Collections;
using System.Collections.Generic;
using log4net;
using Newtonsoft.Json;
using Omikron.FactFinder.Core.Server;
using Omikron.FactFinder.Data;

namespace Omikron.FactFinder.Adapter
{
    public class Suggest : AbstractAdapter
    {
        private IList<SuggestQuery> _suggestions;
        public IList<SuggestQuery> Suggestions
        {
            get
            {
                if (_suggestions == null)
                    _suggestions = CreateSuggestions();
                return _suggestions;
            }
        }

        private string _rawSuggestions;
        public string RawSuggestions
        {
            get
            {
                if (_rawSuggestions == null)
                    _rawSuggestions = CreateRawSuggestions();
                return _rawSuggestions;
            }
        }

        protected dynamic JsonData { get { return ResponseContent; } }

        private static ILog log;

        static Suggest()
        {
            log = LogManager.GetLogger(typeof(Suggest));
        }

        public Suggest(Request request, Core.Client.UrlBuilder urlBuilder)
            : base(request, urlBuilder)
        {
            log.Debug("Initialize new Suggest adapter.");

            Request.Action = RequestType.Suggest;
        }
        class FFSuggestions
        {
            public FFSuggestion[] suggestions { get; set; }
        }
        class FFSuggestion
        {
            public FFAttributes attributes { get; set; }
            public int hitCount { get; set; }
            public string image { get; set; }
            public string name { get; set; }
            public int priority { get; set; }
            public string searchParams { get; set; }
            public string type { get; set; }

        }
        class FFAttributes
        {
            public string masterId { get; set; }
            public string articleNr { get; set; }
            public string deeplink { get; set; }
            public string id { get; set; }
            public string campaignProductNr { get; set; }
        }
        protected IList<SuggestQuery> CreateSuggestions()
        {
            UseJsonResponseContentProcessor();

            string oldFormat = null;
            if (Parameters["format"] != null)
                oldFormat = Parameters["format"];

            Parameters["format"] = "json";

            var suggestions = new List<SuggestQuery>();
            var suggestionsData = JsonConvert.DeserializeObject<FFSuggestions>(RawSuggestions);
            if (suggestionsData.suggestions.Length > 0)
            {                
                foreach (var suggestData in suggestionsData.suggestions)
                {
                    string query = (string)suggestData.name;
                    suggestions.Add(new SuggestQuery(
                        query,
                        //ConvertServerQueryToClientUrl((string)suggestData.searchParams),
                        new Uri(suggestData.attributes.deeplink, UriKind.Relative),
                        (int)suggestData.hitCount,
                        (string)suggestData.type,
                        new Uri((string)suggestData.image, UriKind.RelativeOrAbsolute)
                    ));
                }
            }
            if (oldFormat != null)
                Parameters["format"] = oldFormat;

            return suggestions;
        }


        /**
         * Get the suggestions from FACT-Finder as the string returned by the
         * server.
         *
         * The format parameter is optional. Either 'json' or 'jsonp'. Use to
         * overwrite the 'format' request parameter.
         * The callback parameter is optional. Pass in a (JavaScript) function
         * name to overwrite the 'callback' request parameter, which determines 
         * the name of the callback the response is wrapped in.
         */
        protected string CreateRawSuggestions(string format = null, string callback = null)
        {
            UsePassthroughResponseContentProcessor();

            if (format != null)
                Parameters["format"] = format;
            if (callback != null)
                Parameters["callback"] = callback;

            return (string)ResponseContent;
        }
    }
}
