using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lobby
{ 
    public class BannedWordFilter
    {
        static List<string> banned_word_list = null;

        public static async Task<bool> Check(string word)
        {
            if (banned_word_list == null)
                banned_word_list = await BannedWordQuery.GetBannedWord();

            foreach (var banned_word in banned_word_list)
            {
                if (word.Contains(banned_word) == true)
                {
                    return false;
                }
            }
            return true;
        }


        public static async Task<string> Replace(string word)
        {
            if (banned_word_list == null)
                banned_word_list = await BannedWordQuery.GetBannedWord();

            string resultWord = null;
            resultWord = word;
            string replaceWord = null;

            foreach (var banned_word in banned_word_list)
            {
                if (resultWord.Contains(banned_word) == true)
                {
                    for (int i = 0; i < banned_word.Length; i++)
                    {
                        replaceWord = replaceWord + "*";
                    }
                    resultWord = resultWord.Replace(banned_word, replaceWord);
                    replaceWord = null;
                }
            }

            return resultWord;
        }
    }
}
