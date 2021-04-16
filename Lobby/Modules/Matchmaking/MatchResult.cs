using GameService;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Serilog;

namespace Lobby
{
    public class MatchResult
    {
        public StartPlayReply replyToClient = new StartPlayReply();
        public ServerCommon.InternalMessage replyToBattleServer = new ServerCommon.InternalMessage();
        public List<WaitingUser> waitingUsers = new List<WaitingUser>();

        public MatchResult( long match_id, int map_id)
        {
            replyToBattleServer.players = new Dictionary<string, ServerCommon.PlayerInfo>();

            replyToBattleServer.message_type = (byte)ServerCommon.InternalMessageType.Participant;
            //replyToBattleServer.world_id = (byte)world_id;
            //replyToBattleServer.channel_id = channel_id;
            replyToBattleServer.match_id = match_id;
            replyToBattleServer.map_id = map_id;

            replyToClient.Code = ErrorCode.Success;
            replyToClient.IsStart = true;
            //replyToClient.BattleServerAddr = server_addr;
            //replyToClient.WorldId = world_id;
            replyToClient.MapId = map_id;
        }

        public async Task Finish(byte world_id, string server_addr, string channel_id, JGameModeData game_mode)
        {
            replyToBattleServer.world_id = world_id;
            replyToBattleServer.channel_id = channel_id;

            replyToClient.BattleServerAddr = server_addr;
            replyToClient.WorldId = world_id;

            Complete();

            foreach(var player in replyToBattleServer.players)
            {
                var msg = new ServerCommon.PlayerLocation()
                {
                    channel_id = channel_id,
                    world_id = world_id,
                    player_id = player.Value.player_id,
                };

                await Cache.Instance.GetDatabase().StringSetAsync($"player:{player.Key}", JsonConvert.SerializeObject(msg), new TimeSpan(0, 15, 0));
            }
            SetModeData(game_mode);
        }

        void Complete()
        {
            // 플레이어 아이디를 다른 월드간에 겹치지 않도록 수정
            foreach(var player in replyToBattleServer.players)
            {
                player.Value.player_id = core.MathHelpers.MakeDWord(replyToBattleServer.world_id, (ushort)player.Value.player_id);
            }

            foreach(var player in replyToClient.CharacterList)
            {
                player.PlayerId = core.MathHelpers.MakeDWord(replyToBattleServer.world_id, (ushort)player.PlayerId);
            }
        }


        public async Task<bool> AddPlayer(Session session, JMapData map_data, JGameModeData game_mode)
        {
            if(replyToBattleServer.players.ContainsKey(session.session_id))
            {
                Log.Error($"AddPlayer duplicate session_id:{session.session_id}");
                return false;
            }

            List<JSpawnData> spawn;
            if (JsonData.Instance.SpawnPositions.TryGetValue(map_data.ID, out spawn)==false)
            {
                Log.Error($"AddPlayer cannot find map_id:{map_data.ID}");
                return false;
            }
            if(spawn.Count <= replyToBattleServer.players.Count)
            {
                Log.Error($"AddPlayer cannot find spawn index:{replyToBattleServer.players.Count}, size:{spawn.Count}");
                return false;
            }
            var team = (byte)spawn[replyToBattleServer.players.Count].spawnTeam;

            int characterLevel = 0;
            var character = await CharacterCache.Instance.GetEntity(session.member_no, session.character_no, true, false, false);
            if (character != null && character != default(Models.Character))
            {
                characterLevel = character.character_level;
            }

            var player = new ServerCommon.PlayerInfo()
            {
                user_no = session.user_no,
                character_type = session.character_type,
                user_id = session.user_name,
                team = (byte)team,
                player_id = replyToBattleServer.players.Count +1,
                spawn_index = replyToBattleServer.players.Count,
                character_level = characterLevel,
            };

            replyToBattleServer.players.Add(session.session_id, player);
            replyToClient.CharacterList.Add(new StartPlayCharacterInfo() { SelectedCharacter = session.character_type, UserId = session.user_name, Team = player.team, PlayerId = player.player_id, SpawnIndex = player.spawn_index });
            return true;
        }

        public void AddAI(Session session, JMapData map_data, JGameModeData game_mode)
        {
            var spawn = JsonData.Instance.SpawnPositions[map_data.ID];

            string session_id = Session.GetNewSessionId();
            // todo : AI 닉네임 설정이 필요. 
            // 현재 응답에서 중복만 되지 않으면 문제가 없을 것으로 보임
            string user_id = session_id;

            var player = new ServerCommon.PlayerInfo()
            {
                user_no = session.user_no,
                character_type = session.character_type,
                user_id = user_id,
                team = (byte)spawn[replyToBattleServer.players.Count].spawnTeam,
                is_ai = true,
                player_id = replyToBattleServer.players.Count + 1,
                spawn_index = replyToBattleServer.players.Count,
                character_level = 1,
            };

            replyToBattleServer.players.Add(session_id, player);
            replyToClient.CharacterList.Add(new StartPlayCharacterInfo() { SelectedCharacter = session.character_type, UserId = user_id, Team = player.team, PlayerId = player.player_id, SpawnIndex = player.spawn_index });

        }


        public void SetModeData(JGameModeData game_mode)
        {
            switch (game_mode.ID)
            {
                case (int)core.GameModeType.KillTheKing:
                    int playerId_A = core.KillTheKing.GetNextKingPlayerId(replyToBattleServer.players.Values.Where(x => x.team == (byte)core.Team.TeamA).Select(x => x.player_id).ToList());
                    int playerId_B = core.KillTheKing.GetNextKingPlayerId(replyToBattleServer.players.Values.Where(x => x.team == (byte)core.Team.TeamB).Select(x => x.player_id).ToList());
                    replyToBattleServer.kill_the_king_info = new ServerCommon.KillTheKingInfo()
                    {
                        king_player_id_A = playerId_A,
                        king_player_id_B = playerId_B
                    };

                    replyToClient.KillTheKingInfo = new KillTheKingInfo();
                    replyToClient.KillTheKingInfo.KingPlayerIdForTeamA = playerId_A;
                    replyToClient.KillTheKingInfo.KingPlayerIdForTeamB = playerId_B;

                    break;
            }
        }
    }
}
