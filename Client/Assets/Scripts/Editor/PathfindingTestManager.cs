using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace GameServerFramework.Editor
{
    public class PathfindingTestManager : EditorWindow
    {
        private int agentCount = 5;
        private float agentSpeed = 5f;
        private float agentRotationSpeed = 180f;
        private bool showAgentPaths = true;
        private bool showAgentTargets = true;
        private Color agentPathColor = Color.green;
        private Color agentTargetColor = Color.red;
        private bool enableDebugLogs = false;
        
        private List<GameObject> spawnedAgents = new List<GameObject>();
        private GameObject agentParent;

        [MenuItem("Tools/Pathfinding Test Manager")]
        public static void ShowWindow()
        {
            GetWindow<PathfindingTestManager>("Pathfinding Test Manager");
        }

        private void OnEnable()
        {
            // Find existing agent parent or create new one
            agentParent = GameObject.Find("PathfindingAgents");
            if (agentParent == null)
            {
                agentParent = new GameObject("PathfindingAgents");
            }
            
            // Find existing agents
            spawnedAgents.Clear();
            PathfindingAgent[] agents = FindObjectsOfType<PathfindingAgent>();
            foreach (PathfindingAgent agent in agents)
            {
                spawnedAgents.Add(agent.gameObject);
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Pathfinding Test Manager", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Agent settings
            EditorGUILayout.LabelField("Agent Settings", EditorStyles.boldLabel);
            agentCount = EditorGUILayout.IntField("Agent Count:", agentCount);
            agentSpeed = EditorGUILayout.FloatField("Agent Speed:", agentSpeed);
            agentRotationSpeed = EditorGUILayout.FloatField("Agent Rotation Speed:", agentRotationSpeed);
            
            EditorGUILayout.Space();

            // Visualization settings
            EditorGUILayout.LabelField("Visualization Settings", EditorStyles.boldLabel);
            showAgentPaths = EditorGUILayout.Toggle("Show Agent Paths:", showAgentPaths);
            showAgentTargets = EditorGUILayout.Toggle("Show Agent Targets:", showAgentTargets);
            agentPathColor = EditorGUILayout.ColorField("Agent Path Color:", agentPathColor);
            agentTargetColor = EditorGUILayout.ColorField("Agent Target Color:", agentTargetColor);
            
            EditorGUILayout.Space();

            // Debug settings
            EditorGUILayout.LabelField("Debug Settings", EditorStyles.boldLabel);
            enableDebugLogs = EditorGUILayout.Toggle("Enable Debug Logs:", enableDebugLogs);
            
            EditorGUILayout.Space();

            // Control buttons
            EditorGUILayout.LabelField("Agent Management", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Spawn Agents", GUILayout.Height(30)))
            {
                SpawnAgents();
            }
            if (GUILayout.Button("Clear All Agents", GUILayout.Height(30)))
            {
                ClearAllAgents();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Randomize Targets", GUILayout.Height(30)))
            {
                RandomizeAllTargets();
            }
            if (GUILayout.Button("Stop All Agents", GUILayout.Height(30)))
            {
                StopAllAgents();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();

            // Agent list
            EditorGUILayout.LabelField("Active Agents", EditorStyles.boldLabel);
            if (spawnedAgents.Count > 0)
            {
                EditorGUILayout.LabelField($"Total Agents: {spawnedAgents.Count}");
                
                for (int i = 0; i < spawnedAgents.Count; i++)
                {
                    if (spawnedAgents[i] != null)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.ObjectField($"Agent {i + 1}:", spawnedAgents[i], typeof(GameObject), true);
                        if (GUILayout.Button("Select", GUILayout.Width(60)))
                        {
                            Selection.activeGameObject = spawnedAgents[i];
                        }
                        if (GUILayout.Button("Remove", GUILayout.Width(60)))
                        {
                            RemoveAgent(i);
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }
            else
            {
                EditorGUILayout.LabelField("No agents spawned");
            }
        }

        private void SpawnAgents()
        {
            // Clear existing agents if requested
            if (spawnedAgents.Count > 0)
            {
                bool clearExisting = EditorUtility.DisplayDialog(
                    "Clear Existing Agents", 
                    "Do you want to clear existing agents before spawning new ones?", 
                    "Yes", "No"
                );
                
                if (clearExisting)
                {
                    ClearAllAgents();
                }
            }

            // Spawn new agents
            for (int i = 0; i < agentCount; i++)
            {
                GameObject agent = CreateAgent($"PathfindingAgent_{i + 1}");
                spawnedAgents.Add(agent);
            }

            Debug.Log($"Spawned {agentCount} pathfinding agents");
        }

        private GameObject CreateAgent(string name)
        {
            // Create agent GameObject
            GameObject agent = new GameObject(name);
            agent.transform.SetParent(agentParent.transform);
            
            // Set random position
            Vector3 randomPosition = new Vector3(
                Random.Range(-40f, 40f),
                1f,
                Random.Range(-40f, 40f)
            );
            agent.transform.position = randomPosition;
            
            // Add visual representation
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.SetParent(agent.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = new Vector3(1f, 2f, 1f);
            
            // Set material
            Renderer renderer = visual.GetComponent<Renderer>();
            Material agentMaterial = new Material(Shader.Find("Standard"));
            agentMaterial.color = new Color(
                Random.Range(0.5f, 1f),
                Random.Range(0.5f, 1f),
                Random.Range(0.5f, 1f)
            );
            renderer.material = agentMaterial;
            
            // Add PathfindingAgent component
            PathfindingAgent pathfindingAgent = agent.AddComponent<PathfindingAgent>();
            pathfindingAgent.moveSpeed = agentSpeed;
            pathfindingAgent.rotationSpeed = agentRotationSpeed;
            pathfindingAgent.showPath = showAgentPaths;
            pathfindingAgent.showTarget = showAgentTargets;
            pathfindingAgent.pathColor = agentPathColor;
            pathfindingAgent.targetColor = agentTargetColor;
            pathfindingAgent.enableDebugLogs = enableDebugLogs;
            
            return agent;
        }

        private void ClearAllAgents()
        {
            foreach (GameObject agent in spawnedAgents)
            {
                if (agent != null)
                {
                    DestroyImmediate(agent);
                }
            }
            
            spawnedAgents.Clear();
            Debug.Log("Cleared all pathfinding agents");
        }

        private void RandomizeAllTargets()
        {
            foreach (GameObject agent in spawnedAgents)
            {
                if (agent != null)
                {
                    PathfindingAgent pathfindingAgent = agent.GetComponent<PathfindingAgent>();
                    if (pathfindingAgent != null)
                    {
                        Vector3 randomTarget = new Vector3(
                            Random.Range(-40f, 40f),
                            0f,
                            Random.Range(-40f, 40f)
                        );
                        pathfindingAgent.SetTarget(randomTarget);
                    }
                }
            }
            
            Debug.Log("Randomized targets for all agents");
        }

        private void StopAllAgents()
        {
            foreach (GameObject agent in spawnedAgents)
            {
                if (agent != null)
                {
                    PathfindingAgent pathfindingAgent = agent.GetComponent<PathfindingAgent>();
                    if (pathfindingAgent != null)
                    {
                        pathfindingAgent.SetTarget(agent.transform.position);
                    }
                }
            }
            
            Debug.Log("Stopped all agents");
        }

        private void RemoveAgent(int index)
        {
            if (index >= 0 && index < spawnedAgents.Count)
            {
                GameObject agent = spawnedAgents[index];
                if (agent != null)
                {
                    DestroyImmediate(agent);
                }
                spawnedAgents.RemoveAt(index);
                Debug.Log($"Removed agent at index {index}");
            }
        }

        private void OnDestroy()
        {
            // Clean up when window is closed
            spawnedAgents.Clear();
        }
    }
} 