using System;
using System.Collections.Generic;
using System.Text;

namespace Lobby
{
    /// <summary>
    /// 서칭 유저의 랭크와 근접한 상대 랭크 얻기
    /// </summary>
    public class RankSequencer
    {
        public int rank;
        public int min_rank;
        public int max_rank;
        bool incre = true;


        public int Count
        {
            get { return max_rank - min_rank; }
        }

        public IEnumerator<int> GetEnumerator()
        {
            bool is_min = false;
            bool is_max = false;
            int cur_min_rank = rank-1;
            int cur_max_rank = rank;
            for (int i = 0; i < this.Count+1; i++)
            {
                if (cur_max_rank > max_rank)
                    is_max = true;
                if (cur_min_rank < min_rank)
                    is_min = true;

                if (is_max && is_min)
                    break;

                if(incre && is_max )
                {
                    incre = false;
                }

                if (!incre && is_min)
                {
                    incre = true;
                }

                if (incre)
                    yield return cur_max_rank++;
                else
                    yield return cur_min_rank--;

                incre = !incre;
            }
        }
    }
}
