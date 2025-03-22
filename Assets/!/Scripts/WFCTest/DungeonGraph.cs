using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class DungeonGraph : MonoBehaviour
{
    public GEDRoomInitializer roomInitializer; // Reference to the GEDRoomInitializer
    public List<GEDRoomManager> roomSets;

    private void Start()
    {
    }

    private IEnumerator InitializeAfterDelay()
    {
        // Wait until the roomInitializer has completed its Start method
        yield return new WaitForEndOfFrame();

        if (roomInitializer == null)
        {
            Debug.LogError("[DungeonGraph] GEDRoomInitializer reference is missing.");
            yield break;
        }

        // Populate roomSets from the GEDRoomInitializer
        roomSets = roomInitializer.roomManagers;

        if (roomSets == null || roomSets.Count < 2)
        {
            Debug.LogError("[DungeonGraph] Insufficient room sets for GED calculation.");
            yield break;
        }

        StartCoroutine(WaitForGenerationCompletion());
    }

    private IEnumerator WaitForGenerationCompletion()
    {
        Debug.Log("[DungeonGraph] Waiting for all RoomManagers to complete generation...");

        // Wait for all room managers to finish generating
        foreach (var roomManager in roomSets)
        {
            yield return new WaitUntil(() => roomManager.generationComplete);
            Debug.Log($"[DungeonGraph] RoomManager {roomManager.name} generation complete.");
        }

        Debug.Log("[DungeonGraph] All RoomManagers have completed generation. Performing pairwise comparisons...");

        // Perform pairwise comparisons
        PerformPairwiseComparisons();

        yield break; // Explicit coroutine termination
    }

    private void PerformPairwiseComparisons()
    {
        string filePath = Application.dataPath + "/PairwiseGEDResults.txt";
        using (StreamWriter writer = new StreamWriter(filePath))
        {
            writer.WriteLine("Pairwise Graph Edit Distance (GED) Results");
            writer.WriteLine("==========================================");
            writer.WriteLine();

            int comparisonCount = 0;

            // Compare each layout against every other layout
            for (int i = 0; i < roomSets.Count; i++)
            {
                for (int j = i + 1; j < roomSets.Count; j++)
                {
                    comparisonCount++;

                    // Build graphs for the two layouts
                    DungeonGraphData graph1 = DungeonGraphData.FromRoomManager(roomSets[i]);
                    DungeonGraphData graph2 = DungeonGraphData.FromRoomManager(roomSets[j]);

                    // Calculate GED
                    int nodeAdditions, nodeDeletions, edgeAdditions, edgeDeletions;
                    int gedScore = DungeonGraphData.CalculateGED(graph1, graph2, out nodeAdditions, out nodeDeletions, out edgeAdditions, out edgeDeletions);

                    // Write results to the file
                    writer.WriteLine($"Comparison {comparisonCount}: Layout {i + 1} vs. Layout {j + 1}");
                    writer.WriteLine($"Total Nodes of Layout {i + 1}: {graph1.nodes.Count}");
                    writer.WriteLine($"Total Edges of Layout {i + 1}: {graph1.edges.Count}");
                    writer.WriteLine($"Total Nodes of Layout {j + 1}: {graph2.nodes.Count}");
                    writer.WriteLine($"Total Edges of Layout {j + 1}: {graph2.edges.Count}");
                    writer.WriteLine($"Node Insertions: {nodeAdditions}");
                    writer.WriteLine($"Node Deletions: {nodeDeletions}");
                    writer.WriteLine($"Edge Insertions: {edgeAdditions}");
                    writer.WriteLine($"Edge Deletions: {edgeDeletions}");
                    writer.WriteLine($"GED Calculation: {nodeAdditions} + {nodeDeletions} + {edgeAdditions} + {edgeDeletions} = {gedScore}");
                    writer.WriteLine($"Total GED Score: {gedScore}");
                    writer.WriteLine();

                    Debug.Log($"[DungeonGraph] Comparison {comparisonCount}: Layout {i + 1} vs. Layout {j + 1}, GED Score: {gedScore}");
                }
            }

            writer.WriteLine($"Total comparisons: {comparisonCount}");
        }

        Debug.Log($"[DungeonGraph] Pairwise GED results saved to {filePath}");
    }
}

// Graph Data Structure
public class DungeonGraphData
{
    public Dictionary<string, Vector2Int> nodes = new Dictionary<string, Vector2Int>();
    public HashSet<(string, string)> edges = new HashSet<(string, string)>();

    public static DungeonGraphData FromRoomManager(GEDRoomManager roomManager)
    {
        DungeonGraphData graph = new DungeonGraphData();

        foreach (var room in roomManager.roomPositions)
        {
            graph.nodes.Add(room.Key, room.Value);
            Debug.Log($"[Graph] Added Node: {room.Key} at {room.Value}");
        }

        foreach (var connection in roomManager.roomConnections)
        {
            graph.edges.Add(connection);
            Debug.Log($"[Graph] Added Edge: {connection.Item1} <--> {connection.Item2}");
        }

        return graph;
    }

    public static int CalculateGED(DungeonGraphData graph1, DungeonGraphData graph2, out int nodeAdditions, out int nodeDeletions, out int edgeAdditions, out int edgeDeletions)
    {
        nodeAdditions = 0;
        nodeDeletions = 0;
        edgeAdditions = 0;
        edgeDeletions = 0;

        // Node changes
        foreach (var node in graph1.nodes)
        {
            if (!graph2.nodes.ContainsKey(node.Key))
            {
                nodeDeletions++;
                Debug.Log($"[GED] Node removed: {node.Key}");
            }
        }

        foreach (var node in graph2.nodes)
        {
            if (!graph1.nodes.ContainsKey(node.Key))
            {
                nodeAdditions++;
                Debug.Log($"[GED] Node added: {node.Key}");
            }
        }

        // Edge changes
        foreach (var edge in graph1.edges)
        {
            if (!graph2.edges.Contains(edge) && !graph2.edges.Contains((edge.Item2, edge.Item1)))
            {
                edgeDeletions++;
                Debug.Log($"[GED] Edge removed: {edge.Item1} <--> {edge.Item2}");
            }
        }

        foreach (var edge in graph2.edges)
        {
            if (!graph1.edges.Contains(edge) && !graph1.edges.Contains((edge.Item2, edge.Item1)))
            {
                edgeAdditions++;
                Debug.Log($"[GED] Edge added: {edge.Item1} <--> {edge.Item2}");
            }
        }

        int totalGED = nodeAdditions + nodeDeletions + edgeAdditions + edgeDeletions;
        Debug.Log($"[GED] Total GED Score: {totalGED} (Nodes: +{nodeAdditions}, -{nodeDeletions} | Edges: +{edgeAdditions}, -{edgeDeletions})");

        return totalGED;
    }
}