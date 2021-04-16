using System;
using System.Collections.Generic;
using System.Text;
using GameService;

namespace Lobby
{
    public static partial class AccountGoodsExtensions
    {
        public static void Set(this Goods accountGoods, Models.User user)
        {
            accountGoods.Gem = user.gem;
            accountGoods.Coin = user.coin;
            accountGoods.BattleCoin = user.battle_coin;
            accountGoods.Medal = user.medal;
            accountGoods.UpgradeStone = user.upgrade_stone;

            (user.medal_charge, user.medal_charge_time) = core.MathHelpers.GetMedalCharge(user.medal_charge, user.medal_charge_time, DateTime.UtcNow);
            accountGoods.MedalCharge = user.medal_charge;
            if (accountGoods.MedalCharge != (int)MedalChargeConst.MaxCharge)
            {
                accountGoods.MedalChargeRemainTime = ((int)MedalChargeConst.ChargePeriod * 60) - (int)(DateTime.UtcNow - (DateTime)user.medal_charge_time).TotalSeconds;
            }
            else
            {
                accountGoods.MedalChargeRemainTime = 0;
            }
        }
    }
}
