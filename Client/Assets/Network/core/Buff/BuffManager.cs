using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace core
{
    public class BuffManager
    {
        // 중복 가능 여부에 따른 버프
        // resetID, Buff
        public Dictionary<int, Buff> buffs = new Dictionary<int, Buff>();


        public void Clear()
        {
            buffs.Clear();
        }

        public float GetMaxRemainTime(BuffType buffType)
        {
            return buffs.Where(x => x.Value.GetBuffType() == buffType).Max(x => x.Value.RemainTime);
        }

        public bool AddBuff(Buff buff, JSpellData spellData, int parentNetworkId, float value)
        {
            buff.Set(spellData, parentNetworkId, value);
            return AddBuff(buff);
        }

        public bool AddBuff(Buff buff)
        {
            Buff last_buff = null;
            if (buffs.TryGetValue(buff.GetResetId(), out last_buff) == true)
            {
                last_buff.SetDoesWantToDie(true);
            }

            buffs[buff.GetResetId()] = buff;

            return true;
        }

        public (bool, float) GetBuff(BuffType buffType)
        {
            bool isAvailable = false;
            float status = 0f;

            foreach (var buff in buffs)
            {
                if (buff.Value.GetBuffType() == buffType && buff.Value.IsExpired() == false)
                {
                    status += buff.Value.mAddStatus;
                    isAvailable = true;
                }
            }
            return (isAvailable, status);
        }

        public Buff GetBuffFirstRef(BuffType buffType)
        {
            foreach (var buff in buffs)
            {
                if (buff.Value.GetBuffType() == buffType && buff.Value.IsExpired() == false)
                {
                    return buff.Value;
                }
            }
            return null;
        }

        public bool IsExist(BuffType buffType)
        {
            foreach (var buff in buffs)
            {
                if (buff.Value.GetBuffType() == buffType && buff.Value.IsExpired() == false)
                {
                    return true;
                }
            }
            return false;
        }

        public int HP
        {
            get
            {
                (var exist, var status) = GetBuff(BuffType.Shield);
                if (exist)
                {
                    return (int)status;
                }
                return 0;
            }
            set
            {
                var shield = GetBuffFirstRef(BuffType.Shield);
                if (shield != null)
                {
                    shield.SetAddStatus(value);

                }
            }
        }

        public float ShieldTime
        {
            get
            {
                var buff = GetBuffFirstRef(BuffType.Shield);
                if (buff != null)
                {
                    if (buff.RemainTime <= Timing.sInstance.GetFrameStartTime())
                        return 0.0f;

                    return buff.RemainTime - Timing.sInstance.GetFrameStartTime();
                }
                return 0.0f;
            }
        }

        public float AddItemMoveSpeed
        {
            get
            {
                (var exist, var status) = GetBuff(BuffType.SpeedUp);
                if (exist)
                {
                    return status;
                }
                return 0;
            }
        }
        public float AddSpeedSlowByPer
        {
            get
            {
                (var exist, var status) = GetBuff(BuffType.SpeedSlowByPer);
                if (exist)
                {
                    return status;
                }
                return 0;
            }
        }

        /// <summary>
        /// 기본공격 충전 쿨타임 버프
        /// </summary>
        public float AddChargerCoolTime
        {
            get
            {
                (var exist, var status) = GetBuff(BuffType.BasicAttack_ChargerTime);
                if(exist)
                {
                    return status;
                }
                return 0;
            }
        }
    }
}
