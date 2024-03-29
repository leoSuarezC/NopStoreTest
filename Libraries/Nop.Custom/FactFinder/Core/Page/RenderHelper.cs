﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Text.RegularExpressions;
using Omikron.FactFinder.Adapter;
using Omikron.FactFinder.Data;
using Omikron.FactFinder.Util;
using Omikron.FactFinder.Core.Configuration;

namespace Omikron.FactFinder.Core.Page
{
    public class RenderHelper
    {
        protected Search SearchAdapter { get; set; }
        protected SearchParameters FFParameters { get; set; }

        public RenderHelper(SearchParameters ffParameters, Search searchAdapter)
        {
            SearchAdapter = searchAdapter;
            FFParameters = ffParameters;
        }

        public string CreateJavaScriptClickCode(Record record, string sid = null)
        {
            if (String.IsNullOrEmpty(sid))
                sid = HttpContextFactory.Current.Session.Id;

            string query = FFParameters.Query.Replace(@"'", @"\'");

            int position = record.Position;

            string clickCode = "";

            if (position != 0 && query != "")
            {
                string channel = FFParameters.Channel;

                int currentPageNumber = SearchAdapter.Paging.CurrentPage;
                string originalPageSize = SearchAdapter.ProductsPerPageOptions.DefaultOption.Label;

                int originalPosition = record.OriginalPosition;
                if (originalPosition == 0) originalPosition = position;

                string campaign = record.Campaign;
                bool instoreAds = record.InstoreAds;

                string id = record.ID;
                string masterId = (string)record.GetFieldValue(FieldsSection.GetInstance().MasterProduktID);

                sid = Regex.Replace(sid, "['\"\\\0]", @"\$&");

                clickCode = String.Format("tracking.click('{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}', '{8}', '{9}');",
                    sid, id, masterId, query, position, originalPosition, currentPageNumber, originalPageSize, originalPageSize, record.Similarity);
            }

            return clickCode;
        }
    }
}
