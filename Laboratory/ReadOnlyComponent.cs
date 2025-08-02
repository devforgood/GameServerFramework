
using Common;

[ReadOnly]
public class ReadOnlyComponent : BaseComponent
{
    //public override void Save() { /* 📌 이 라인에 컴파일 에러 */ }

    public void Load() { /* OK */ }

    public void DoSomething()
    {
        //this.Save(); // 📌 이 호출에도 에러
    }

    //public void Update()   {  }
}
