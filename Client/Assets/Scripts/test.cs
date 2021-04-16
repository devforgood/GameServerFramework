using Assets.Network.Lobby;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class test : MonoBehaviour
{
    void Start()
    {
        //NetworkController.Instance.Login();

        CActor.FuncCreateGameObject = () =>
        {
            var obj = (GameObject)Instantiate(Resources.Load("Character"), new Vector3(0, 0, 0), Quaternion.identity);
            return obj.GetComponent<ACBattleCharacter>();
        };

    }

}
