using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


public class CProjectile : core.Projectile
{
    public GameObject mTarget = null;

    public static new core.NetGameObject StaticCreate(byte worldId) { return new CProjectile(); }

    public override bool HandleCollisionWithActor(core.Actor inActor)
    {
        if (GetPlayerId() != inActor.GetPlayerId())
        {
            //RenderManager.sInstance.RemoveComponent(mSpriteComponent.get());
        }
        return false;
    }

    public override void CompleteCreate()
    {
        mStartLocation = GetLocation();

        core.LogHelper.LogInfo($"start x{mStartLocation.x}, y{mStartLocation.y}, z{mStartLocation.z}");




        GameObject go = GameObject.Find("ProjectileBomb");
        if (go == null)
            return;

        GameObject bomb = GameObject.Instantiate(go, GetLocation(), go.transform.rotation);
        mTarget = bomb;
        //var script = mTarget.AddComponent<ProjectileBehaviour>();
        //script.projectile = this;
    }

    public override void HandleDying()
    {
        base.HandleDying();
        if (mTarget != null)
            GameObject.Destroy(mTarget, 0.3f);
    }


}
