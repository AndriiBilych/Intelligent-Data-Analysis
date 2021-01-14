/*TODO:
 * K-nn algorithm
 * trait correlation between tags
 * trait list for each article not in the tag
 * Regex https://www.youtube.com/watch?v=sa-TUpSx1JA
 * Check traits 1-6
 * Simplify CollectConsecutiveSymbols()
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using static System.Net.Mime.MediaTypeNames;
using Porter2StemmerStandard;
using System.Linq;

namespace IntelligentAnalysis
{
    class Program
    {
        static List<string> placeTags;

        static void Main(string[] args)
        {
            //Viktor Kolesnichenko & Bogdan Artemov###################################################################################
            placeTags = new List<string> { "west - germany", "usa", "france", "uk", "canada", "japan" };
            DirectoryInfo rootDirProj = new DirectoryInfo(@"E:\Downloads\reuters21578");

            //Find files in directory
            FileInfo[] Files = rootDirProj.GetFiles("*.sgm");

            List<Sample> samples = new List<Sample>();

            //Reading samples from files
            Console.WriteLine("Sampling provided data...");
            foreach (FileInfo file in Files)
            {
                string contentFile = File.ReadAllText(Files[0].FullName);
                root myRootedXml = (root)XmlDeserializeFromString(
                    "<root>" + ReplaceHexadecimalSymbols(contentFile) + "</root>", typeof(root));

                foreach (rootREUTERS rootReuters in myRootedXml.REUTERS)
                {
                    if (rootReuters.PLACES.Length != 1)
                    {
                        continue;
                    }
                    else if (!placeTags.Contains(rootReuters.PLACES[0]))
                    {
                        continue;
                    }
                    else
                    {
                        samples.Add(new Sample(rootReuters.PLACES[0], rootReuters.TEXT.TITLE, rootReuters.TEXT.BODY));
                    }
                }
            }
            //#######################################################################################################################

            //Remove punctuation and split words in dictionaries
            for (int i = 0; i < samples.Count; i++)
            {
                if (samples[i].body != null)
                {
                    //Alexander Golovin###########################################################################################
                    //Break down words separated by special characters
                    samples[i].body = Regex.Replace(samples[i].body, "[\n/;]", " ");

                    //Remove punctuation
                    samples[i].body = Regex.Replace(samples[i].body, "[\\[\\]().,!?\"\"<>:\\-\']", "");

                    //Remove additional structures
                    samples[i].body = Regex.Replace(samples[i].body, "--", "");

                    //Make all samples lowercase
                    samples[i].body = samples[i].body.ToLower();

                    //Assign words to text holders, increment word count if the word is already in dictionary
                    foreach (var word in samples[i].body.Split(' '))
                        if (samples[i].words.ContainsKey(word))
                        {
                            var valueHolder = samples[i].words.GetValueOrDefault(word);
                            samples[i].words.Remove(word);
                            samples[i].words.Add(word, ++valueHolder);
                        }
                        else
                            samples[i].words.Add(word, 1);

                    //Remove numbers
                    foreach (var word in samples[i].words)
                        if (Regex.IsMatch(word.Key, "[0-9]") || word.Key.Length < 2)
                            samples[i].words.Remove(word.Key);

                    //Stem words
                    Stem(samples[i]);
                    //#######################################################################################################

                    //Traits vector
                    //Bogdan Artemov
                    //1-2 Longer than 5 or shorter than 5
                    CollectLength(samples[i]);

                    //Viktor kolesnichenko#####################################################################################
                    //3-4 How many words start with vowels and with syllables
                    CollectFirstLetterCounts(samples[i]);
                    //5-6 How many words end with vowels and with syllables
                    CollectLastLetterCounts(samples[i]);
                    //#########################################################################################################

                    //Alexander Golovin
                    //7-8 How many sequences of consecutive vowels or consecutive syllables are found in words
                    CollectConsecutiveSymbols(samples[i]);

                    //Andrii Bilych############################################################################################
                    //Vector normalization
                    double vectorLength = 0;

                    foreach (var trait in samples[i].traits)
                        vectorLength += Math.Pow(trait, 2);

                    vectorLength = Math.Sqrt(vectorLength);

                    for (int v = 0; v < samples[i].traits.Length; v++)
                        samples[i].traits[v] /= vectorLength;
                    //##########################################################################################################
                }
            }

            //Andrii Bilych############################################################################################
            //KNN Distance
            Console.WriteLine("Looking for nearest neighbours...");
            for (int i = 0; i < samples.Count; i++)
            {
                for (int j = 0; j < samples.Count; j++)
                {
                    //Prevents calculating distance against itself and against samples with another tag
                    if (i != j)
                    {
                        double sum = 0;
                        for (int t = 0; t < samples[i].traits.Length; t++)
                            sum += Math.Pow(samples[i].traits[t] - samples[j].traits[t], 2);

                        var temp = Math.Sqrt((double)sum);

                        if (samples[i].distance == -1)
                            samples[i].distance = temp;

                        if (samples[i].distance > temp)
                        {
                            samples[i].distance = temp;
                            samples[i].nnIndex = j;
                        }
                    }
                }
                
                Console.WriteLine((i + 1) + " ( " + samples[i].place + " ): " + samples[i].distance.ToString() + " to " + (samples[i].nnIndex + 1));
            }
            //###############################################################################################################

            Console.ReadLine();
        }

        static void Stem (Sample sample)
        {
            EnglishPorter2Stemmer stemmer = new EnglishPorter2Stemmer();
            Dictionary<string, int> stemmedWords = new Dictionary<string, int>();
            foreach (var word in sample.words)
            {
                var value = word.Value;
                var key = word.Key;
                var stemmedKey = stemmer.Stem(key).Value;

                if (stemmedWords.ContainsKey(stemmedKey))
                {
                    var valueHolder = stemmedWords.GetValueOrDefault(stemmedKey);
                    stemmedWords.Remove(stemmedKey);
                    stemmedWords.Add(stemmedKey, value + valueHolder);
                }
                else
                    stemmedWords.Add(stemmedKey, value);
            }
            sample.words.Clear();
            sample.words = stemmedWords;
        }

        static void CollectLength(Sample sample)
        {
            foreach (var word in sample.words)
                if (word.Key.Length > 5)
                    sample.traits[0] += word.Value;
                else
                    sample.traits[1] += word.Value;
        }
        
        static void CollectFirstLetterCounts(Sample sample)
        {
            foreach (var word in sample.words)
            {
                if (LetterList._vowels.Contains(word.Key[0]))
                    sample.traits[2] += word.Value;
                else
                    sample.traits[3] += word.Value;
            }
        }

        static void CollectLastLetterCounts(Sample sample)
        {
            foreach (var word in sample.words)
                if (LetterList._vowels.Contains(word.Key[word.Key.Length - 1]))
                    sample.traits[4] += word.Value;
                else
                    sample.traits[5] += word.Value;
        }

        static void CollectConsecutiveSymbols(Sample sample)
        {
            foreach (var word in sample.words)
            {
                string sequence = "";

                for (int it = 0; it < word.Key.Length; it++)
                {
                    if (LetterList._vowels.Contains(word.Key[it]))
                    {
                        sequence += word.Key[it];
                        if (it == word.Key.Length - 1 && sequence.Length > 1)
                            sample.traits[6]++;
                    }
                    else if (sequence.Length > 1)
                    {
                        sample.traits[6]++;
                        sequence = "";
                    }
                    else
                        sequence = "";
                }

                sequence = "";

                for (int it = 0; it < word.Key.Length; it++)
                {
                    if (!LetterList._vowels.Contains(word.Key[it]))
                    {
                        sequence += word.Key[it];
                        if (it == word.Key.Length - 1 && sequence.Length > 1)
                        {
                            sample.traits[7]++;
                        }
                    }
                    else if (sequence.Length > 1)
                    {
                        sample.traits[7]++;
                        sequence = "";
                    }
                    else
                        sequence = "";
                }
            }
        }

        static string ReplaceHexadecimalSymbols(string txt)
        {
            string r = "[\x00-\x08\x0B\x0C\x0E-\x1F\x26]";
            return Regex.Replace(txt, r, "", RegexOptions.Compiled);
        }

        private static object XmlDeserializeFromString(string v, Type type)
        {

            object result;

            using (TextReader reader = new StringReader(v))
            {
                result = new XmlSerializer(type).Deserialize(reader);
            }

            return result;

        }
    }
}