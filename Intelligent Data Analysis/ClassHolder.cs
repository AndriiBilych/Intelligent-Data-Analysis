using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace IntelligentAnalysis
{
    class Sample
    {
        public Sample(string place, string title, string body)
        {
            this.place = place;
            this.title = title;
            this.body = body;
            words = new Dictionary<string, int>();
            for(int i = 0; i < traits.Length; i++)
                traits[i] = 0;
            distance = -1;
            nnIndex = -1;
        }

        public Dictionary<string, int> words {get; set; }

        public double[] traits = new double[8];

        public string place { get; set; }

        public string title { get; set; }

        public string body { get; set; }

        public double distance { get; set; }

        public int nnIndex { get; set; }
    }

    class LetterList
    {
        public static readonly string _vowels = "aoeuiy";
    };
}