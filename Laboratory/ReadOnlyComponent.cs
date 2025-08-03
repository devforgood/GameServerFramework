
using Common;
using Laboratory;

[ReadOnly]
public class ReadOnlyComponent : BaseComponent
{
    DBHandler db = new DBHandler();

   // public override void Save() { /* 📌 이 라인에 컴파일 에러 */ }
    //public void LikeSave() { /* 📌 이 라인에 컴파일 에러 */ }


    public void Load() {
        /* OK */ 
        db.Load();
    }

    public void DoSomething()
    {
        //this.Save(); // 📌 이 호출에도 에러
        //db.Save();
    }

    //public void Update()   {  }

    //public void LikeUpdate() { }

    //public void Insert()   { }

    //public void LikeInsert() {  }
}
