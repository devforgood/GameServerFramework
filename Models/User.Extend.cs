using System;
using System.Collections.Generic;
using System.Text;


namespace Lobby.Models
{
    public partial class User : DirtyUpdate
    {
        public void Copy(User other)
        {
            character_no = other.character_no;
            play_point = other.play_point;
            user_grade = other.user_grade;
            battle_score = other.battle_score;
            gem = other.gem;
            coin = other.coin;
            battle_coin = other.battle_coin;
            medal = other.medal;
            upgrade_stone = other.upgrade_stone;
            medal_charge = other.medal_charge;
            medal_charge_time = other.medal_charge_time;
            map_id = other.map_id;
        }

        public int this[int index]
        {
            get
            {
                //switch ((GameItemId)index)
                //{
                //    case GameItemId.Coin: return coin;
                //    case GameItemId.Medal: return medal;
                //    case GameItemId.UpgradeStone: return upgrade_stone;
                //    case GameItemId.Gem: return gem;
                //    case GameItemId.BattleCoin: return battle_coin;
                //    default: return 0;
                //}

                return 0;
            }
            set
            {
                //switch ((GameItemId)index)
                //{
                //    case GameItemId.Coin: coin = value; break;
                //    case GameItemId.Medal: medal = value; break;
                //    case GameItemId.UpgradeStone: upgrade_stone = value; break;
                //    case GameItemId.Gem: gem = value; break;
                //    case GameItemId.BattleCoin: battle_coin = value; break;
                //}
            }
        }
    }
}
