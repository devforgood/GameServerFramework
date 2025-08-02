using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

internal class ReadWriteComponent : ReadOnlyComponent
{
    public override void Save() { /* 📌 이 라인에 컴파일 에러 */ }
}
