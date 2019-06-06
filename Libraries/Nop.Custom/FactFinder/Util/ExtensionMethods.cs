using Nop.Custom.Domain;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace Omikron.FactFinder.Util
{
    public static class ExtensionMethods
    {
        public static string ToUriQueryString(this IDictionary<string, string> dictionary)
        {
            StringBuilder queryString = new StringBuilder();
            foreach (KeyValuePair<string, string> pair in dictionary)
            {
                if (!String.IsNullOrEmpty(pair.Key) && !String.IsNullOrEmpty(pair.Value))
                {
                    if (queryString.Length > 0)
                    { queryString.Append("&"); }
                    queryString.Append(HttpUtility.UrlEncode(pair.Key)).Append("=").Append(HttpUtility.UrlEncode(pair.Value));
                }
            }

            return queryString.ToString();
        }

        public static string ToUriQueryString(this NameValueCollection nvc)
        {
            StringBuilder queryString = new StringBuilder();
            // Copy all keys over and make sure to handle multi-value keys properly
            foreach (string key in nvc)
                if (!String.IsNullOrEmpty(key))
                {
                    string encodedKey = HttpUtility.UrlEncode(key);
                    foreach (string value in nvc.GetValues(key))
                        if (!String.IsNullOrEmpty(value))
                        {
                            if (queryString.Length > 0)
                            { queryString.Append("&"); }
                            queryString.Append(encodedKey).Append("=").Append(HttpUtility.UrlEncode(value));
                        }
                }
            return queryString.ToString();
        }

        public static IDictionary<string, string> ToDictionary(this NameValueCollection source)
        {
            return source.Cast<string>()
                         .Select(s => new { Key = s, Value = source[s] })
                         .ToDictionary(p => p.Key, p => p.Value);
        }

        public static string ToMD5(this string input)
        {
            if ((input == null) || (input.Length == 0))
            {
                return string.Empty;
            }

            byte[] password = Encoding.Default.GetBytes(input);
            byte[] result = new MD5CryptoServiceProvider().ComputeHash(password);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < result.Length; i++)
            {
                sb.Append(result[i].ToString("x2"));
            }
            return sb.ToString();
        }

        // We can't add a static extension method NameValueCollection.FromParameters(), so
        // we'll add a conversion method to String instead.
        public static NameValueCollection ToParameters(this string queryString)
        {
            char[] querySeparator = { '?' };
            return HttpUtility.ParseQueryString(queryString.Split(querySeparator).Last());
        }
        public static string RemoveUnwantedParameters(this string queryString)
        {
            if (queryString.IndexOf("?") >= 0)
            {
                var queryParamsCollection = queryString.ToParameters();
                queryParamsCollection.Remove("followSearch");
                if(!string.IsNullOrEmpty(queryParamsCollection["keywords"]) && queryParamsCollection["keywords"] != "*")
                {
                    queryParamsCollection.Add("query", queryParamsCollection["keywords"]);
                }
                queryParamsCollection.Remove("keywords");
                queryParamsCollection.Remove("Seite");
                if (!string.IsNullOrEmpty(queryParamsCollection["page"]))
                {                    
                    queryParamsCollection.Add("Seite", queryParamsCollection["page"]);
                }
                queryParamsCollection.Remove("page");
                queryParamsCollection.Remove("filteFilterFabre");
                queryParamsCollection.Remove("filterKategorie1");
                queryParamsCollection.Remove("filterKategorie2");
                queryParamsCollection.Remove("filterKategorie3");
                queryParamsCollection.Remove("filterKategorie4");
                queryParamsCollection.Remove("filterKategorie5");
                queryParamsCollection.Remove("filterMarke");
                queryParamsCollection.Remove("filterAngebotArt");
                return "?" + queryParamsCollection.ToUriQueryString();
            }
            else
                return queryString;
        }
        public static string GetFactFinderUrl(this string queryString, IList<SeoMap> seoMapList = null)
        {
            var queryParamsCollection = queryString.ToParameters();
            string make = "", farbe = "", angebotArt = "", query = "";
            string filter1 = "", filter2 = "", filter3 = "", url = "";
            foreach (var key in queryParamsCollection.Keys)
            {
                string keyName = key.ToString();
                if (keyName == "filterKategorie1")
                {
                    filter1 = queryParamsCollection.GetValues(keyName).First();
                }
                else if (keyName == "filterKategorie2")
                {
                    filter2 = queryParamsCollection.GetValues(keyName).First();
                }
                else if (keyName == "filterKategorie3")
                {
                    filter3 = queryParamsCollection.GetValues(keyName).First();
                }
                else if (keyName == "filterMarke")
                {
                    make = queryParamsCollection.GetValues(keyName).First();
                }
                else if (keyName == "filterFilterFarbe")
                {
                    farbe = queryParamsCollection.GetValues(keyName).First();
                }
                else if (keyName == "filterAngebotArt")
                {
                    angebotArt = queryParamsCollection.GetValues(keyName).First();
                }
                else if (keyName == "query" || keyName == "keywords")
                {
                    query = queryParamsCollection.GetValues(keyName).First();
                }
            }
            if (!string.IsNullOrEmpty(filter1) && seoMapList != null)
            {
                SeoMap item = seoMapList.Where(t => t.text_orig == filter1).FirstOrDefault();
                if (item != null)
                    filter1 = item.text_seo;
            }
            if (!string.IsNullOrEmpty(filter2) && seoMapList != null)
            {
                SeoMap item = seoMapList.Where(t => t.text_orig == filter2).FirstOrDefault();
                if (item != null)
                    filter2 = item.text_seo;
            }
            if (!string.IsNullOrEmpty(filter3) && seoMapList != null)
            {
                SeoMap item = seoMapList.Where(t => t.text_orig == filter3).FirstOrDefault();
                if (item != null)
                    filter3 = item.text_seo;
            }
            if (!string.IsNullOrEmpty(make) && seoMapList != null)
            {
                SeoMap item = seoMapList.Where(t => t.text_orig == make).FirstOrDefault();
                if (item != null)
                    make = item.text_seo;
            }
            bool filterPresent = false;
            if (!string.IsNullOrEmpty(make))
            {
                url += "/marke/" + make;
                filterPresent = true;

                if (!string.IsNullOrEmpty(filter1))
                {
                    url += (url.IndexOf("?") >= 0 ? "&" : "?") + "ProduktOberGruppeName=" + filter1;
                    filterPresent = true;
                }
                if (!string.IsNullOrEmpty(filter2))
                {
                    url += (url.IndexOf("?") >= 0 ? "&" : "?") + "ProduktGruppeName=" + filter2;
                    filterPresent = true;
                }
                if (!string.IsNullOrEmpty(filter3))
                {
                    url += (url.IndexOf("?") >= 0 ? "&" : "?") + "ProduktUnterGruppeName=" + filter3;
                    filterPresent = true;
                }
                if (!string.IsNullOrEmpty(farbe))
                {
                    url += (url.IndexOf("?") >= 0 ? "&" : "?") + "Farbe=" + farbe;
                    filterPresent = true;
                }
                if (!string.IsNullOrEmpty(angebotArt))
                {
                    url += (url.IndexOf("?") >= 0 ? "&" : "?") + "AngebotArt=" + angebotArt;
                    filterPresent = true;
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(filter1))
                {
                    url += "/kategorie/" + filter1;
                    filterPresent = true;
                }
                if (!string.IsNullOrEmpty(filter2))
                {
                    url += "/" + filter2;
                    filterPresent = true;
                }
                if (!string.IsNullOrEmpty(filter3))
                {
                    url += "/" + filter3;
                    filterPresent = true;
                }
                if (!string.IsNullOrEmpty(farbe))
                {
                    url += (url.IndexOf("?") >= 0 ? "&" : "?") + "Farbe=" + farbe;
                    filterPresent = true;
                }
                if (!string.IsNullOrEmpty(angebotArt))
                {
                    url += (url.IndexOf("?") >= 0 ? "&" : "?") + "AngebotArt=" + angebotArt;
                    filterPresent = true;
                }
            }
            if (!string.IsNullOrEmpty(query) && query != "*")
            {
                filterPresent = true;
                url += (url.IndexOf("?") >= 0 ? "&" : "?") + "query=" + query;
            }
            if (!filterPresent)
            {
                url = "/onlineshop";
            }
           
            return url;
        }
    }
}