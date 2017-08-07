using System;
using System.Collections.Generic;

namespace OSBLEPlus.Services.Models
{
    [Serializable]
    public class WordStats
    {
        string AuthToken { get; set; }

        int Characters { get; set; }
        int Words { get; set; }
        int Sentences { get; set; }
        int Paragraphs { get; set; }
        int Tables { get; set; }
        int Windows { get; set; }

        int SpellingErrors { get; set; }
        bool SpellingChecked { get; set; }

        int GrammaticalErrors { get; set; }
        bool GrammarChecked { get; set; }
    }
}