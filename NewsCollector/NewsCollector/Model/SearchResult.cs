using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace NewsCollector.Model
{
    public class SearchResult
    {
        public SearchResult()
        {
            
        }


        public string Date { get; set; }

        public string Category { get; set; }

        public string NewsName { get; set; }

        public string Content { get; set; }


        public string Title { get; set; }

        public string Link { get; set; }

        public string PublishedDate { get; set; }

        public ObservableCollection<RelatedKeywordCount> _KeywordFrequency = null;
        public ObservableCollection<RelatedKeywordCount> KeywordFrequency
        {
            get
            {
                _KeywordFrequency ??= new ObservableCollection<RelatedKeywordCount>();
                return _KeywordFrequency;
            }

        }

        public int TotalFrequency { get; set; }

    }
}
