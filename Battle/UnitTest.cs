using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using core;
using Newtonsoft.Json;

public class Base
{
    public string Name;
}
public class Derived : Base
{
    public string Something;
}

public class Derived2 : Base
{
    public string Something2;
}


public class UnitTest
{

    public static void TestAsync()
    {
        var task1 = Task<int>.Run(() => {
            Task.Delay(1000);
            Console.WriteLine("Executing task {0}, thread{1}",
                              Task.CurrentId, Thread.CurrentThread.ManagedThreadId);
            return 54;
        });

        try
        {
            var continuation = task1.ContinueWith((antecedent) =>
            {
                Console.WriteLine("Executing continuation task {0}, thread{1}",
                                  Task.CurrentId, Thread.CurrentThread.ManagedThreadId);
                Console.WriteLine("Value from antecedent: {0}",
                                  antecedent.Result);
                throw new InvalidOperationException();
            });
        }
        catch(Exception ex)
        {
            Console.WriteLine("main5 task {0}, thread{1}, ex{2}", Task.CurrentId, Thread.CurrentThread.ManagedThreadId, ex.ToString());

        }

        Console.WriteLine("main1 task {0}, thread{1}",Task.CurrentId, Thread.CurrentThread.ManagedThreadId);
        Task.Delay(500);
        Console.WriteLine("main2 task {0}, thread{1}", Task.CurrentId, Thread.CurrentThread.ManagedThreadId);
        Task.Delay(1000);
        Console.WriteLine("main3 task {0}, thread{1}", Task.CurrentId, Thread.CurrentThread.ManagedThreadId);



    }
    public static void Test()
    {

        {
            // 클라이언트에서 Newtonsoft.Json 를 사용할 수 없다.
            // ios빌드시 System.Reflection.Emit 관련 에러가 발생
            // 클래스 상속 기능은 포기

            Base object1 = new Base() { Name = "Object1" };
            Derived object2 = new Derived() { Something = "Some other thing" };
            Derived2 object3 = new Derived2() { Something2 = "Some other thing2" };
            List<Base> inheritanceList = new List<Base>() { object1, object2, object3 };

            JsonSerializerSettings settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
            string Serialized = JsonConvert.SerializeObject(inheritanceList, settings);
            List<Base> deserializedList = JsonConvert.DeserializeObject<List<Base>>(Serialized, settings);



        }
        {
            Vector3 v = MathHelpers.DegreeToVector3Cached(90);
            Vector2 v2 = new Vector2(v.x, v.z);
            v2.Angle();

            v = MathHelpers.DegreeToVector3Cached(90);
            v2 = new Vector2(v.x, v.z);
            v2.Angle();

            v = MathHelpers.DegreeToVector3Cached(95);
            v2 = new Vector2(v.x, v.z);
            v2.Angle();

            v = MathHelpers.DegreeToVector3Cached(135);
            v2 = new Vector2(v.x, v.z);
            v2.Angle();


            v = MathHelpers.DegreeToVector3Cached(180);
            v2 = new Vector2(v.x, v.z);
            v2.Angle();

            v = MathHelpers.DegreeToVector3Cached(225);
            v2 = new Vector2(v.x, v.z);
            v2.Angle();

            v = MathHelpers.DegreeToVector3Cached(270);
            v2 = new Vector2(v.x, v.z);
            v2.Angle();

            v = MathHelpers.DegreeToVector3Cached(315);
            v2 = new Vector2(v.x, v.z);
            v2.Angle();

            v = MathHelpers.DegreeToVector3Cached(0);
            v2 = new Vector2(v.x, v.z);
            v2.Angle();

            v = MathHelpers.DegreeToVector3Cached(95);
            v2 = new Vector2(v.x, v.z);
            v2.Angle();
        }

        {
            var v = new Vector2(1, 0);
            v.Angle();

            v = new Vector2(1, 1);
            v.Angle();

            v = new Vector2(0, 1);
            v.Angle();

            v = new Vector2(-1, 0);
            v.Angle();

            v = new Vector2(0, -1);
            v.Angle();

            v = new Vector2(-1, -1);
            v.Angle();

            v = new Vector2(0, -1);
            v.Angle();

            v = new Vector2(1, -1);
            v.Angle();


        }

        {
            Vector3 v = MathHelpers.DegreeToVector3Cached(90);
            Vector2 v2 = new Vector2(v.x, v.z);


            var angle = v2.Angle();




            Quaternion q = Quaternion.LookRotation(v);

            Vector3 axis;
            float a = 0f;
            Quaternion.ToAxisAngleRad(q, out axis, out a);
        }

        {
            Vector3 v = new Vector3(0, 1, 0);
            var q = Quaternion.AngleAxis(90f, v);

            Vector3 axis;
            float a = 0f;
            q.ToAngleAxis(out a, out axis);





        }
        {
            Vector3 v = new Vector3(1, 0, 1);

            Quaternion q = Quaternion.LookRotation(v);

            Vector3 axis;
            float a = 0f;
            q.ToAngleAxis(out a, out axis);
        }





    }
}
