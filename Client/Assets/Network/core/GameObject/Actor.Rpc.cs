using System.Collections.Generic;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif


namespace core
{
    public partial class Actor 
    {
        [ServerRPC(RequireOwnership = false)]
        public virtual void PingServer(int number)
        {
        }

        [ClientRPC]
        public virtual void PingClient(int number)
        {
        }


        [ServerRPC(RequireOwnership = false)]
        public virtual void JumpServer(int power)
        {
        }

        [ClientRPC]
        public virtual void JumpClient(int power)
        {
        }


        [ServerRPC(RequireOwnership = false)]
        public virtual void TeleportServer(int map_uid)
        {

        }


        [ClientRPC]
        public virtual void TeleportClient(Vector3 pos, float power, float duration)
        {
        }

        [ServerRPC(RequireOwnership = false)]
        public virtual void ShootServer(byte actionToken, bool IsBasicAttack, Vector3 targetPos, float fDistance, float fForce, float fFireAngle, float fHeightAngle)
        {
        }

        [ClientRPC]
        public virtual void ShootClient(byte actionToken, bool IsBasicAttack, Vector3 targetPos, float fDistance, float fForce, float fFireAngle, float fHeightAngle)
        {
        }

        [ServerRPC(RequireOwnership = false)]
        public virtual void ShootPushServer(byte actionToken, bool IsBasicAttack, Vector3 targetPos)
        {

        }

        [ClientRPC]
        public virtual void ShootPushClient(byte actionToken, bool IsBasicAttack, Vector3 targetPos)
        {

        }

        [ServerRPC(RequireOwnership = false)]
        public virtual void ActionResult(byte actionToken, List<int> objectList, Vector3 reachedPosition)
        {

        }

        [ServerRPC(RequireOwnership = false)]
        public virtual void ActionResult2(byte actionToken, List<int> objectList, int hitProjectileIdx)
        {

        }

        [ClientRPC]
        public virtual void GetItemClient(int item_id)
        {

        }

        [ServerRPC(RequireOwnership = false)]
        public virtual void ShootSkillServer(byte actionToken, bool IsBasicAttack, Vector3 origin, Vector3 dir)
        {

        }


        [ClientRPC]
        public virtual void ShootSkillClient(byte actionToken, bool IsBasicAttack, Vector3 origin, Vector3 dir)
        {

        }

        [ServerRPC(RequireOwnership = false)]
        public virtual void Ready()
        {

        }

        [ServerRPC(RequireOwnership = false)]
        public virtual void ShootDashSkillServer(byte actionToken, bool IsBasicAttack, Vector3 targetPos, Vector3 dir)
        {

        }

        [ClientRPC]
        public virtual void ShootDashRamClient(byte actionToken, bool IsBasicAttack, Vector3 targetPos, Vector3 dir)
        {
        }

        [ClientRPC]
        public virtual void OnStartPlay(List<int> objectList)
        {
        }

        [ClientRPC]
        public virtual void OnTimeoutInit(int player_count)
        {
        }


        [ServerRPC(RequireOwnership = false)]
        public virtual void DebugCommand(string cmd, string param1, string param2, string param3, string param4)
        {

        }

        [ClientRPC]
        public virtual void OnNoticeCreateItem()
        {
        }

        [ServerRPC(RequireOwnership = false)]
        public virtual void KnockbackServer(Vector3 impact, int network_id)
        {

        }

        [ClientRPC]
        public virtual void KnockbackClient(Vector3 impact)
        {
        }

        [ServerRPC(RequireOwnership = false)]
        public virtual void ApplicationQuit(bool pause)
        {

        }

        [ClientRPC]
        public virtual void NoticeKillDeath(int killPlayerId, int deathPlayerId, bool isKingKiller, bool isKing)
        {

        }

        [ClientRPC]
        public virtual void NoticeHealth(int health)
        {

        }

        [ServerRPC(RequireOwnership = false)]
        public virtual void Hide(bool isHide, ushort map_object_uid)
		{
		}

        [ClientRPC]
        public virtual void JoinDebugUser(byte selectedCharacter, byte team, int playerId, string userId, int spawnIndex)
        {

        }

        [ClientRPC]
        public virtual void Disconnected()
        {

        }

    }
}
