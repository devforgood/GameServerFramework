using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MaykerStudio.VFX
{
    [ExecuteAlways]
    public class SlashDecal : MonoBehaviour
    {
        [SerializeField]
        private float decalOffsetY = 0;

        [SerializeField]
        private float maxDistance = 0.5f;

        [SerializeField]
        private Vector3 rotation;

        [SerializeField]
        private ParticleSystem slash;

        public float capsuleHeight = 2f;
        public float capsuleRadius = 0.2f;

        private ParticleSystem decal;

        private CapsuleCollider slashCollider;

        void Start()
        {
            decal = GetComponent<ParticleSystem>();
            decal.Stop(false);

            slashCollider = slash.GetComponent<CapsuleCollider>();
        }

        void Update()
        {
            if (!slash || !decal || !slash.isPlaying)
                return;

            if (IsGrounded(out Vector3 hit))
            {
                transform.SetPositionAndRotation(hit + decalOffsetY * Vector3.up, Quaternion.Euler(rotation));
                if (!decal.isPlaying)
                {
                    decal.Play(true);
                }
            }
            else if (decal.isPlaying)
            {
                decal.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        bool IsGrounded(out Vector3 collisionPoint)
        {
            collisionPoint = Vector3.zero;

            Quaternion rotation = slash.transform.rotation;
            Vector3 up = rotation * Vector3.right;
            Vector3 position = slash.transform.position;

            Vector3 point1 = position + up * (capsuleHeight / 2 - capsuleRadius);
            Vector3 point2 = position - up * (capsuleHeight / 2 - capsuleRadius);

            if (Physics.CapsuleCast(point1, point2, capsuleRadius, -up, out RaycastHit hit, maxDistance))
            {
                collisionPoint = hit.point;
                return true;
            }
            else if (Physics.CapsuleCast(point1, point2, capsuleRadius, up, out hit, maxDistance))
            {
                collisionPoint = hit.point;
                return true;
            }

            return false;
        }

        void OnDrawGizmos()
        {
            if (!slash)
                return;

            if (IsGrounded(out Vector3 hit))
            {
                Gizmos.color = Color.green;
                Gizmos.DrawRay(slash.transform.position, Vector3.down * maxDistance);
                Gizmos.DrawWireSphere(hit, 0.1f);
            }
            else
            {
                Gizmos.color = Color.red;
                Gizmos.DrawRay(slash.transform.position, Vector3.down * maxDistance);
            }

            Gizmos.color = Color.green;
            Vector3 position = slash.transform.position;
            Quaternion rotation = slash.transform.rotation;
            Vector3 up = rotation * Vector3.right;

            Vector3 point1 = position + up * (capsuleHeight / 2 - capsuleRadius);
            Vector3 point2 = position - up * (capsuleHeight / 2 - capsuleRadius);

            Gizmos.DrawWireSphere(point1, capsuleRadius);
            Gizmos.DrawWireSphere(point2, capsuleRadius);
            Gizmos.DrawLine(point1 + Vector3.right * capsuleRadius, point2 + Vector3.right * capsuleRadius);
            Gizmos.DrawLine(point1 - Vector3.right * capsuleRadius, point2 - Vector3.right * capsuleRadius);
            Gizmos.DrawLine(point1 + Vector3.forward * capsuleRadius, point2 + Vector3.forward * capsuleRadius);
            Gizmos.DrawLine(point1 - Vector3.forward * capsuleRadius, point2 - Vector3.forward * capsuleRadius);
        }
    }

}