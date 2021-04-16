using core;
using Serilog;
using Server;
using System;
using System.Collections.Generic;
using System.Text;

namespace Battle
{
    public class DebugCommand
    {
        public static bool Execute(SActor actor, string Cmd, string Param1, string Param2, string Param3, string Param4)
        {
            return (bool)System.Reflection.Assembly.GetExecutingAssembly().GetType("Battle.DebugCommand").GetMethod(Cmd).Invoke(null, new object[] { actor, Param1, Param2, Param3, Param4 });
        }

        public static bool end(SActor actor, string Param1, string Param2, string Param3, string Param4)
        {
            var otherTeam = World.Instance(actor.WorldId).GameMode.GetOtherTeam(actor.Team);
            if (Param1 == "win")
                World.Instance(actor.WorldId).GameMode.EndGame(actor.Team, otherTeam, false, CloseType.Clear);
            else if (Param1 == "lose")
                World.Instance(actor.WorldId).GameMode.EndGame(otherTeam, actor.Team, false, CloseType.Clear);

            return true;
        }

        public static bool createAi(SActor actor, string Param1, string Param2, string Param3, string Param4)
        {
            byte character_type = byte.Parse(Param1);
            string user_id = Param2;
            byte world_id = byte.Parse(Param3);
            int team = int.Parse(Param4);

            AIController aiController = NetworkManagerServer.sInstance.CreateAI(character_type, user_id, world_id, System.Guid.NewGuid(), team, 0, -1, 1);
            aiController.OnStart();

            return true;
        }
        public static bool getinfo(SActor actor, string Param1, string Param2, string Param3, string Param4)
        {
            var networkId = int.Parse(Param1);
            var gameObject = NetworkManagerServer.Instance.GetGameObject(networkId, actor.WorldId);
            if (gameObject != null)
            {
                Log.Information($"DebugCommand networkID:{networkId}, gameObjectType:{(GameObjectClassId)gameObject.GetClassId()}");
            }
            return true;
        }
        public static bool addSpell(SActor actor, string Param1, string Param2, string Param3, string Param4)
        {
            var spellId = int.Parse(Param1);
            JSpellData spellData = ACDC.SpellData[spellId];
            actor.AddSpell(spellData, 0);
            return true;
        }
        public static bool SetHP(SActor actor, string Param1, string Param2, string Param3, string Param4)
        {
            var hp = int.Parse(Param1);
            actor.ResetHealth(hp, null);
            return true;
        }
        public static bool SetTrainPeriod(SActor actor, string Param1, string Param2, string Param3, string Param4)
        {
            var period = float.Parse(Param1);
            var train_list = World.Instance(actor.WorldId).GetGameObjects(GameObjectClassId.Train);
            foreach (var train in train_list)
            {
                ((STrain)train).mapData.jMapMovePathData[0].createTime = period;
            }
            return true;
        }
        public static bool SetTrainDamage(SActor actor, string Param1, string Param2, string Param3, string Param4)
        {
            var ObjectDamage = int.Parse(Param1);
            var train_list = World.Instance(actor.WorldId).GetGameObjects(GameObjectClassId.Train);
            foreach (var train in train_list)
            {
                ((STrain)train).mapData.jMapMovePathData[0].ObjectDamage = ObjectDamage;
            }
            return true;
        }

        public static bool InsertItemIngame(SActor actor, string Param1, string Param2, string Param3, string Param4)
        {
            var item_id = int.Parse(Param1);
            var item_count = int.Parse(Param2);

            for(int i=0;i<item_count;++i)
                actor.GetItem(item_id);

            return true;
        }
    }
}
