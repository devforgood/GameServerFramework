using UnityEngine;
using System.Collections.Generic;

namespace GameServerFramework
{
    public class PathfindingAgent : MonoBehaviour
    {
        [Header("Agent Settings")]
        public float moveSpeed = 5f;
        public float rotationSpeed = 180f;
        public float stoppingDistance = 0.5f;
        public float pathfindingInterval = 0.1f;
        
        [Header("Visualization")]
        public bool showPath = true;
        public Color pathColor = Color.green;
        public float pathLineWidth = 0.1f;
        public bool showTarget = true;
        public Color targetColor = Color.red;
        
        [Header("Debug")]
        public bool enableDebugLogs = false;
        
        private Vector3 targetPosition;
        private List<Vector3> currentPath = new List<Vector3>();
        private int currentPathIndex = 0;
        private bool isMoving = false;
        private float lastPathfindingTime = 0f;
        
        // Line renderer for path visualization
        private LineRenderer pathRenderer;
        
        private void Start()
        {
            InitializePathRenderer();
            SetRandomTarget();
        }
        
        private void Update()
        {
            if (Time.time - lastPathfindingTime > pathfindingInterval)
            {
                UpdatePathfinding();
                lastPathfindingTime = Time.time;
            }
            
            MoveAlongPath();
            UpdateVisualization();
        }
        
        private void InitializePathRenderer()
        {
            pathRenderer = gameObject.AddComponent<LineRenderer>();
            pathRenderer.material = new Material(Shader.Find("Sprites/Default"));
            pathRenderer.startColor = pathColor;
            pathRenderer.endColor = pathColor;
            pathRenderer.startWidth = pathLineWidth;
            pathRenderer.endWidth = pathLineWidth;
            pathRenderer.positionCount = 0;
        }
        
        private void SetRandomTarget()
        {
            // Set a random target within the terrain bounds
            float x = Random.Range(-50f, 50f);
            float z = Random.Range(-50f, 50f);
            targetPosition = new Vector3(x, 0f, z);
            
            if (enableDebugLogs)
            {
                Debug.Log($"Agent {gameObject.name} set target to: {targetPosition}");
            }
        }
        
        private void UpdatePathfinding()
        {
            // Simple A* pathfinding implementation
            // In a real implementation, you would use Unity's NavMesh or a custom pathfinding system
            currentPath = CalculateSimplePath(transform.position, targetPosition);
            currentPathIndex = 0;
            isMoving = currentPath.Count > 0;
            
            if (enableDebugLogs && currentPath.Count > 0)
            {
                Debug.Log($"Agent {gameObject.name} calculated path with {currentPath.Count} waypoints");
            }
        }
        
        private List<Vector3> CalculateSimplePath(Vector3 start, Vector3 end)
        {
            List<Vector3> path = new List<Vector3>();
            
            // Simple direct path calculation
            // In a real implementation, this would use proper pathfinding algorithms
            float distance = Vector3.Distance(start, end);
            
            if (distance > stoppingDistance)
            {
                // Add intermediate waypoints for smoother movement
                int waypointCount = Mathf.Max(2, Mathf.FloorToInt(distance / 10f));
                
                for (int i = 0; i <= waypointCount; i++)
                {
                    float t = (float)i / waypointCount;
                    Vector3 waypoint = Vector3.Lerp(start, end, t);
                    
                    // Add some randomness to avoid obstacles
                    if (i > 0 && i < waypointCount)
                    {
                        waypoint += new Vector3(
                            Random.Range(-2f, 2f),
                            0f,
                            Random.Range(-2f, 2f)
                        );
                    }
                    
                    path.Add(waypoint);
                }
            }
            
            return path;
        }
        
        private void MoveAlongPath()
        {
            if (!isMoving || currentPathIndex >= currentPath.Count)
            {
                // Reached target or no path available
                if (Vector3.Distance(transform.position, targetPosition) < stoppingDistance)
                {
                    SetRandomTarget();
                }
                return;
            }
            
            Vector3 targetWaypoint = currentPath[currentPathIndex];
            Vector3 direction = (targetWaypoint - transform.position).normalized;
            
            // Move towards waypoint
            transform.position += direction * moveSpeed * Time.deltaTime;
            
            // Rotate towards movement direction
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, 
                    targetRotation, 
                    rotationSpeed * Time.deltaTime
                );
            }
            
            // Check if reached current waypoint
            if (Vector3.Distance(transform.position, targetWaypoint) < stoppingDistance)
            {
                currentPathIndex++;
            }
        }
        
        private void UpdateVisualization()
        {
            if (showPath && pathRenderer != null)
            {
                if (currentPath.Count > 0)
                {
                    pathRenderer.positionCount = currentPath.Count;
                    pathRenderer.SetPositions(currentPath.ToArray());
                }
                else
                {
                    pathRenderer.positionCount = 0;
                }
            }
            else if (pathRenderer != null)
            {
                pathRenderer.positionCount = 0;
            }
        }
        
        private void OnDrawGizmos()
        {
            if (showTarget)
            {
                Gizmos.color = targetColor;
                Gizmos.DrawWireSphere(targetPosition, 1f);
                Gizmos.DrawLine(transform.position, targetPosition);
            }
            
            // Draw path waypoints
            if (showPath && currentPath.Count > 0)
            {
                Gizmos.color = pathColor;
                for (int i = 0; i < currentPath.Count - 1; i++)
                {
                    Gizmos.DrawLine(currentPath[i], currentPath[i + 1]);
                }
                
                // Draw waypoint spheres
                for (int i = 0; i < currentPath.Count; i++)
                {
                    Gizmos.DrawWireSphere(currentPath[i], 0.3f);
                }
            }
        }
        
        // Public methods for external control
        public void SetTarget(Vector3 target)
        {
            targetPosition = target;
            if (enableDebugLogs)
            {
                Debug.Log($"Agent {gameObject.name} target set to: {target}");
            }
        }
        
        public void SetMoveSpeed(float speed)
        {
            moveSpeed = speed;
        }
        
        public void SetPathfindingInterval(float interval)
        {
            pathfindingInterval = interval;
        }
        
        public Vector3 GetCurrentTarget()
        {
            return targetPosition;
        }
        
        public bool IsMoving()
        {
            return isMoving;
        }
        
        public List<Vector3> GetCurrentPath()
        {
            return new List<Vector3>(currentPath);
        }
    }
} 