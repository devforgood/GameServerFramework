using core;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class SCastle : Castle
    {
        protected SCastle()
        {
            //Log.Information($"create castle {0}", NetworkId));
        }

        public new static NetGameObject StaticCreate(byte worldId) { return NetworkManagerServer.sInstance.RegisterAndReturn(new SCastle(), worldId); }

        public override void HandleDying()
        {
            base.HandleDying();
            NetworkManagerServer.sInstance.UnregisterGameObject(this);
            Log.Information($"remove Castle {NetworkId}");
        }

        public override int OnExplode(int player_id, int parentNetworkId, int damage)
        {
            int applyDamage = _TakeDamage(player_id, parentNetworkId, damage);

            if (applyDamage != 0)
            {
                var damagingPlayer = (SActor)World.Instance(WorldId).GameMode.GetActor(player_id);
                if (damagingPlayer != null)
                {
                    InvokeClientRpcOnClient(NoticeHealth, damagingPlayer.GetPlayerId(), -1 * applyDamage);
                }
            }

            // 블러디 스킬중 받은 데미지로 hp를 흡수하는데, 기지는 예외
            return 0;
        }

        private int _TakeDamage(int player_id, int parentNetworkId, int damage)
        {
            // 기지는 기지 쟁탈전 모드에서만 사용하므로 모드별 처리를 하지 않는다.
            var entry = World.Instance(WorldId).GameMode.GetEntry(player_id);
            if (entry != null)
            {
                if (entry.GetTeam() == (core.Team)Team)
                {
                    return 0;
                }
            }

            var lastBuffHealth = 0;
            if (VaildShield())
            {
                lastBuffHealth = shield_hp;
                if (shield_hp >= (ushort)damage)
                {
                    shield_hp -= (ushort)damage;
                    Dirty((uint)ReplicationState.Buff);
                    return damage;
                }
                else
                {
                    // 버프 HP 0 및 actor HP 감소
                    damage -= shield_hp;
                    shield_hp = 0;
                    Dirty((uint)ReplicationState.Buff);
                }
            }

            var lastHealth = hp;
            if (hp > (ushort)damage)
            {
                hp -= (ushort)damage;
            }
            else
            {
                hp = 0;
                //kill yourself!
                //SetDoesWantToDie(true);
                World.Instance(WorldId).GameMode.EndGame(entry.GetTeam(), (core.Team)Team, false, CloseType.Clear);
            }

            var player = (SActor)World.Instance(WorldId).GameMode.GetActor(player_id);
            if (player != null)
            {
                World.Instance(WorldId).GameMode.OnTrigger(player.GetPlayerId(), PlayPointID.CastleAttack);
            }

            Dirty((uint)ReplicationState.Health);

            return lastHealth - hp + lastBuffHealth;
        }


        protected override void Dirty(uint state)
        {
            //Log.Information($"dirty castle {GetNetworkId()}");

            NetworkManagerServer.sInstance.SetStateDirty(GetNetworkId(), WorldId, state);
        }
    }


}

