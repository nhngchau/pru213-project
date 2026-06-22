using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Grid + A* pathfinding over the 2D play area (Enemy AI). At Start it bakes which cells are walkable
/// from the Environment colliders (inflated by agentRadius for clearance), then serves paths so Bugs
/// route AROUND furniture/walls to reach the Server instead of ramming into them and getting stuck.
/// One instance lives in the scene; enemies query it via PathfindingGrid.Instance.
/// </summary>
public class PathfindingGrid : MonoBehaviour
{
    public static PathfindingGrid Instance { get; private set; }

    [Header("Grid")]
    [Tooltip("Same Environment layer(s) the furniture / walls live on.")]
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private float cellSize = 0.5f;
    [Tooltip("Clearance kept from obstacles - cells within this distance of a collider are blocked.")]
    [SerializeField] private float agentRadius = 0.35f;
    [SerializeField] private Vector2 areaPadding = new Vector2(1f, 1f);
    [Tooltip("Used only if no Environment colliders are found to size the grid.")]
    [SerializeField] private Vector2 fallbackSize = new Vector2(30f, 20f);

    [Header("Debug")]
    [SerializeField] private bool drawGizmos = false;

    private bool[,] walkable;
    private int cols, rows;
    private Vector2 origin; // world position of the grid's bottom-left corner

    public bool IsReady => walkable != null;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        Bake();
    }

    /// <summary>Re-scan the Environment colliders. Call again if the layout changes at runtime.</summary>
    public void Bake()
    {
        Bounds bounds = ComputeBounds();
        origin = bounds.min;
        cols = Mathf.Max(1, Mathf.CeilToInt(bounds.size.x / cellSize));
        rows = Mathf.Max(1, Mathf.CeilToInt(bounds.size.y / cellSize));
        walkable = new bool[cols, rows];

        float checkRadius = cellSize * 0.5f + agentRadius;
        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                walkable[x, y] = Physics2D.OverlapCircle(CellCenter(x, y), checkRadius, obstacleMask) == null;
            }
        }
    }

    private Bounds ComputeBounds()
    {
        bool any = false;
        Bounds b = new Bounds();
        foreach (Collider2D col in FindObjectsByType<Collider2D>(FindObjectsSortMode.None))
        {
            if ((obstacleMask.value & (1 << col.gameObject.layer)) == 0)
            {
                continue; // only Environment colliders define the play area
            }

            if (!any) { b = col.bounds; any = true; }
            else { b.Encapsulate(col.bounds); }
        }

        if (!any)
        {
            b = new Bounds(transform.position, fallbackSize);
        }

        b.Expand(new Vector3(areaPadding.x * 2f, areaPadding.y * 2f, 0f));
        return b;
    }

    // --- Path query --------------------------------------------------------

    /// <summary>A* from start to goal. Fills 'result' with world-space waypoints; true if a path exists.</summary>
    public bool FindPath(Vector2 startWorld, Vector2 goalWorld, List<Vector2> result)
    {
        result.Clear();
        if (walkable == null)
        {
            return false;
        }

        Vector2Int start = NearestWalkable(WorldToCell(startWorld));
        Vector2Int goal = NearestWalkable(WorldToCell(goalWorld));
        if (start == goal)
        {
            result.Add(goalWorld);
            return true;
        }

        int n = cols * rows;
        int[] came = new int[n];
        float[] g = new float[n];
        float[] f = new float[n];
        bool[] closed = new bool[n];
        bool[] inOpen = new bool[n];
        for (int i = 0; i < n; i++) { came[i] = -1; g[i] = float.MaxValue; f[i] = float.MaxValue; }

        List<int> open = new List<int>();
        int startI = start.x + start.y * cols;
        int goalI = goal.x + goal.y * cols;
        g[startI] = 0f;
        f[startI] = Heuristic(start, goal);
        open.Add(startI);
        inOpen[startI] = true;

        int[] dxs = { 1, -1, 0, 0, 1, 1, -1, -1 };
        int[] dys = { 0, 0, 1, -1, 1, -1, 1, -1 };

        while (open.Count > 0)
        {
            // Extract the open node with the smallest f.
            int bestSlot = 0;
            for (int i = 1; i < open.Count; i++)
            {
                if (f[open[i]] < f[open[bestSlot]]) bestSlot = i;
            }
            int cur = open[bestSlot];

            if (cur == goalI)
            {
                Reconstruct(came, cur, result);
                return true;
            }

            open[bestSlot] = open[open.Count - 1];
            open.RemoveAt(open.Count - 1);
            inOpen[cur] = false;
            closed[cur] = true;

            int cx = cur % cols, cy = cur / cols;
            for (int d = 0; d < 8; d++)
            {
                int nx = cx + dxs[d], ny = cy + dys[d];
                if (nx < 0 || nx >= cols || ny < 0 || ny >= rows || !walkable[nx, ny]) continue;
                // Don't cut across the corner of a blocked cell on diagonals.
                if (d >= 4 && (!walkable[cx + dxs[d], cy] || !walkable[cx, cy + dys[d]])) continue;

                int ni = nx + ny * cols;
                if (closed[ni]) continue;

                float tentative = g[cur] + ((d >= 4) ? 1.4142135f : 1f);
                if (tentative < g[ni])
                {
                    came[ni] = cur;
                    g[ni] = tentative;
                    f[ni] = tentative + Heuristic(new Vector2Int(nx, ny), goal);
                    if (!inOpen[ni]) { open.Add(ni); inOpen[ni] = true; }
                }
            }
        }

        return false; // unreachable
    }

    private void Reconstruct(int[] came, int cur, List<Vector2> result)
    {
        List<int> chain = new List<int>();
        while (cur != -1) { chain.Add(cur); cur = came[cur]; }
        for (int i = chain.Count - 1; i >= 0; i--)
        {
            result.Add(CellCenter(chain[i] % cols, chain[i] / cols));
        }
    }

    private static float Heuristic(Vector2Int a, Vector2Int b)
    {
        int dx = Mathf.Abs(a.x - b.x), dy = Mathf.Abs(a.y - b.y);
        return (dx + dy) + (1.4142135f - 2f) * Mathf.Min(dx, dy); // octile distance
    }

    // --- Cell helpers ------------------------------------------------------

    private Vector2 CellCenter(int x, int y) =>
        origin + new Vector2((x + 0.5f) * cellSize, (y + 0.5f) * cellSize);

    private bool InGrid(int x, int y) => x >= 0 && x < cols && y >= 0 && y < rows;

    private Vector2Int WorldToCell(Vector2 p)
    {
        int x = Mathf.Clamp(Mathf.FloorToInt((p.x - origin.x) / cellSize), 0, cols - 1);
        int y = Mathf.Clamp(Mathf.FloorToInt((p.y - origin.y) / cellSize), 0, rows - 1);
        return new Vector2Int(x, y);
    }

    // Nearest walkable cell via expanding rings - lets a blocked start/goal (e.g. the Server tucked
    // against a wall, or a Bug spawned on an edge) still resolve to a usable cell.
    private Vector2Int NearestWalkable(Vector2Int c)
    {
        if (InGrid(c.x, c.y) && walkable[c.x, c.y]) return c;

        int maxR = Mathf.Max(cols, rows);
        for (int r = 1; r <= maxR; r++)
        {
            for (int dx = -r; dx <= r; dx++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    if (Mathf.Abs(dx) != r && Mathf.Abs(dy) != r) continue; // ring perimeter only
                    int nx = c.x + dx, ny = c.y + dy;
                    if (InGrid(nx, ny) && walkable[nx, ny]) return new Vector2Int(nx, ny);
                }
            }
        }
        return c; // grid fully blocked (shouldn't happen)
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos || walkable == null) return;
        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                Gizmos.color = walkable[x, y] ? new Color(0f, 1f, 0f, 0.12f) : new Color(1f, 0f, 0f, 0.30f);
                Gizmos.DrawCube(CellCenter(x, y), Vector3.one * cellSize * 0.9f);
            }
        }
    }
#endif
}
