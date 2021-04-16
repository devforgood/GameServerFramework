using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


public interface INetCharacter
{
    Vector3 GetPosition();
    Vector3 GetVelocity();
    void OnCreate(CActor actor, bool is_local_character);

}
