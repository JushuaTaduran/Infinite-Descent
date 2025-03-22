using System.Collections.Generic;
using UnityEngine;

public class GraphEditDistance : MonoBehaviour
{
    public static int CalculateGED(List<Vector2Int> layout1, List<Vector2Int> layout2)
    {
        // Simple GED calculation: sum of distances between corresponding nodes
        int distance = 0;
        int minCount = Mathf.Min(layout1.Count, layout2.Count);

        for (int i = 0; i < minCount; i++)
        {
            distance += (int)Vector2Int.Distance(layout1[i], layout2[i]);
        }

        // Add the difference in the number of nodes
        distance += Mathf.Abs(layout1.Count - layout2.Count);

        return distance;
    }
}