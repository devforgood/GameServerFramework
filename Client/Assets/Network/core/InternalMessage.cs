using System;
using System.Collections.Generic;
using System.Text;

namespace ServerCommon
{
    public enum InternalMessageType : byte
    {
        Participant = 0,
        DebugCommand = 1,
        SuspendChannel = 2,
        ResumeChannel = 3,
    }

    public class Session
    {
        public string session_id;
        public string remote_endpoint;
        public long user_no;
        public string user_name;
        public string player_id; // 플랫폼(IDP)에서 제공하는 아이디
        public long member_no;
        public long rating;
        public long character_no;
        public byte character_type;
        public int map_id;
        public int rank; // 서버 사이드 랭크(케릭터 배틀 스코어에서 치환된 값)
        public int min_rank;
        public int max_rank;
        public DateTime first_request_time;
        public string device_uid;
    }

    public class UserInfo
    {
        public long user_no;
        public string user_id;
        public string session_id;
    }

    public class PlayerInfo
    {
        public int player_id;
        public long user_no;
        public string user_id;
        public byte character_type;
        public byte count; // 배틀서버에 접속 되었는지 확인 용도
        public byte team;
        public bool is_ai;
        public int spawn_index;
        public int character_level;
    }

    public class InternalMessage
    {
        public byte message_type;
        public byte world_id;
        public string channel_id;
        public long match_id;
        public long season_no;
        public int map_id;
        public Dictionary<string, PlayerInfo> players;
        public DebugCommand debug_command;
        public KillTheKingInfo kill_the_king_info;
    }

    public class KillTheKingInfo
    {
        public int king_player_id_A;
        public int king_player_id_B;
    }

    public class PlayerResult
    {
        public int play_point;
        public int battle_point;
        public bool IsWin;
        public bool IsLose;
        public bool IsMvp;
        public bool IsLeave;
        public Dictionary<int, int> missions;
    }

    public class GameResult
    {
        public int win_team;
        public bool is_draw;
        public long match_id;
        public long season_no;
        public core.Statistics statistics;
        public Dictionary<string, PlayerResult> player_result;
    }

    public class DebugCommand
    {
        public int ingame_player_id;
        public long msg_id;
        public string player_id;
        public string cmd;
        public string param1;
        public string param2;
        public string param3;
        public string param4;
    }

    public class PlayerLocation
    {
        // 메시지를 어느 채널로 보낼지
        public string channel_id;

        // 배틀 서버에서 플레이어를 찾기 위한 용도
        public int player_id;
        public byte world_id;
    }
}
