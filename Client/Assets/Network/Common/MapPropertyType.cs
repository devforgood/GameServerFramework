using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public enum MapPropertyType
{
    None,
    Tile,                   //1 바닥.
    Ramp,                   //2 경사로.
    Spawn,                  //3 리젠.
    Respawn,                //4 사망 후 리젠.
    MapDestroy,             //5 오브젝트 파괴.
    MapRegenerate,          //6 오브젝트 재생.
    PropertyDestroy,        //7 속성 파괴.
    PropertyRegenerate,     //8 속성 재생.
    MapItem,                //9 파괴 시 아이템 생성.
    Castle,                 //10 기지.
    Gravity,                //11 오브젝트 중력 적용.
    WarpZone,               //12 워프존.
    WarpZoneEnd,            //13 워프존 착지점.
    Jump,                   //14 점프.
    JumpEnd,                //15 점프 착지점.
    AutoCreateBomb,         //16 폭탄 자동 생성.
    AutoCreateItem,         //17 아이템 자동 생성.
    Foothold,               //18 거점.
    CreateWall,             //19 벽 생성.
    CreatePoison,           //20 독구름 생성.
    TileOut,                //21 바닥 사라짐.
    IceFloor,               //22 빙판.
    SlowFloor,              //23 느려지는 바닥.
    Health,                 //24 HP.
    MovePathStart,          //25 이동하는 오브젝트의 이동 시작 위치.
    MovePathEnd,            //26 이동하는 오브젝트의 이동 끝 위치.
    MoveObject,             //27 이동하는 오브젝트.
    MapSwitch,              //28 스위치 기능.
    MapSpecialObject,       //29 스위치 스페셜 오브젝트.
    MapIcon,                //30 맵 아이콘.
    MapKingTemple,          //31 킬더킹 Temple.
}