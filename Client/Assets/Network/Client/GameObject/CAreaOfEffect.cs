using UnityEngine;
using Lidgren.Network;

public class CAreaOfEffect : core.AreaOfEffect
{
    public static new core.NetGameObject StaticCreate(byte worldId) { return new CAreaOfEffect(); }

    public GameObject mTarget = null;
    //public DamageAreaOfEffect mDamageAreaOfEffect = null;

    protected CAreaOfEffect()
    {

    }



    public override void CompleteCreate()
    {
        Debug.Log($"CAreaOfEffect {GetNetworkId()}, {SkillId} {GetLocation().x}, {GetLocation().y}, {GetLocation().z}");

        var parent = NetworkManagerClient.sInstance.GetGameObject(mParentNetworkId, core.World.DefaultWorldIndex);
        if (parent != null)
        {
            //this.InitFrom((CActor)parent, this.SkillId, this.GetLocation());
            //mDamageAreaOfEffect.Init(this, this.GetLocation(), this.mDurationTime);
            //SkillManager.GetInstance().InstallAreaOfEffect(this, mPlayerId, SkillId, mDurationTime, mTeam);
        }
    }

    public override void HandleDying()
    {
        base.HandleDying();
        Debug.Log($"remove skill {GetNetworkId()}, {SkillId}");

        if (mTarget != null)
            GameObject.Destroy(mTarget);
    }
}
