using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FakePhysicsUnit : MonoBehaviour
{
    Vector3 velocity;
    public float gravity = 9.8f;
    public float floorY = 0f; // 초기 Y 위치

    void Start()
    {
        floorY = transform.position.y;
    }

    void Update()
    {
        // 중력 적용
        velocity.y -= gravity * Time.deltaTime;

        // 위치 갱신
        transform.position += velocity * Time.deltaTime;

        // 바닥 도달 시 멈춤 처리 등
        if (transform.position.y < floorY)
        {
            transform.position = new Vector3(transform.position.x, floorY, transform.position.z);
            velocity = Vector3.zero;
        }
    }

    public void AddVelocity(Vector3 v)
    {
        velocity += v;
    }
}