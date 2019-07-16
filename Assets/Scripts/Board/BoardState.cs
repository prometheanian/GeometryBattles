﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GeometryBattles.PlayerManager;

namespace GeometryBattles.BoardManager
{
    public class BoardState : MonoBehaviour
    {
        public Resource resource;
        List<GameObject> players;

        public float spreadRate = 0.1f;
        public int spreadAmount = 1;
        
        public int infMax = 200;
        public int infThreshold = 100;

        int cap = -1;
        List<List<TileState>> grid;
        List<List<TileState>> buffer;

        void Start()
        {
            StartCoroutine("CalcBuffer");
        }

        public void SetCap(int n)
        {
            players = new List<GameObject>();
            grid = new List<List<TileState>>(n);
            buffer = new List<List<TileState>>(n);
            for (int i = 0; i < n; i++)
            {
                grid.Add(new List<TileState>(n));
                buffer.Add(new List<TileState>(n));
                for (int j = 0; j < n; j++)
                {
                    grid[i].Add(new TileState(null, null, 0));
                    buffer[i].Add(new TileState(null, null, 0));
                }
            }
            cap = n;
        }

        public void AddPlayer(GameObject player)
        {
            players.Add(player);
        }

        public GameObject GetPlayer(int i)
        {
            return players[i];
        }

        public void InitNode(GameObject node, int q, int r)
        {
            if (cap > 0 && q < cap && r < cap)
            {
                grid[q][r].SetTile(node);
                buffer[q][r].SetTile(node);
            }
        }

        public GameObject GetNodeOwner(int q, int r)
        {
            return grid[q][r].GetOwner();
        }

        public int GetNodeInfluence(int q, int r)
        {
            return grid[q][r].GetInfluence();
        }

        public void SetNode(int q, int r, GameObject owner, bool target = true)
        {
            List<List<TileState>> gridbuffer = target ? grid : buffer;
            GameObject prevOwner = gridbuffer[q][r].GetOwner();
            int prevInfluence = gridbuffer[q][r].GetInfluence();
            if (resource.IsResourceTile(q, r))
            {
                if (prevOwner != owner && prevInfluence >= infThreshold)
                    prevOwner.GetComponent<PlayerPrefab>().AddMiningAmount(-resource.resourceAmount);
                if (prevOwner != owner || (prevOwner == owner && prevInfluence < infThreshold))
                    owner.GetComponent<PlayerPrefab>().AddMiningAmount(resource.resourceAmount);
            }
            gridbuffer[q][r].Set(owner, infThreshold);
        }

        public void SetNode(int q, int r, GameObject owner, int influence, bool target = true)
        {
            List<List<TileState>> gridbuffer = target ? grid : buffer;
            GameObject prevOwner = gridbuffer[q][r].GetOwner();
            int prevInfluence = gridbuffer[q][r].GetInfluence();
            if (resource.IsResourceTile(q, r))
            {
                if (prevOwner != owner && prevInfluence >= infThreshold)
                    prevOwner.GetComponent<PlayerPrefab>().AddMiningAmount(-resource.resourceAmount);
                if (influence >= infThreshold && (prevOwner != owner || (prevOwner == owner && prevInfluence < infThreshold)))
                    owner.GetComponent<PlayerPrefab>().AddMiningAmount(resource.resourceAmount);
            }
            gridbuffer[q][r].Set(owner, influence);
        }

        public void AddNode(int q, int r, GameObject player, int value, bool target = true)
        {
            List<List<TileState>> gridbuffer = target ? grid : buffer;
            GameObject owner = gridbuffer[q][r].GetOwner();
            int influence = gridbuffer[q][r].GetInfluence();
            if (owner == null || owner == player)
            {
                if (resource.IsResourceTile(q, r))
                    if (influence < infThreshold && influence + value >= infThreshold)
                        player.GetComponent<PlayerPrefab>().AddMiningAmount(resource.resourceAmount);
                gridbuffer[q][r].Set(player, Mathf.Min(influence + value, infMax), false);
            }
            else
            {
                if (resource.IsResourceTile(q, r))
                {
                    if (influence >= infThreshold && influence - value < infThreshold)
                        owner.GetComponent<PlayerPrefab>().AddMiningAmount(-resource.resourceAmount);
                    if (influence < value && value - influence >= infThreshold)
                        player.GetComponent<PlayerPrefab>().AddMiningAmount(resource.resourceAmount);
                }
                if (influence < value)
                    gridbuffer[q][r].Set(player, Mathf.Min(value - influence, infMax), false);
                else if (influence > value)
                    gridbuffer[q][r].Set(owner, Mathf.Min(influence - value, infMax), false);
                else
                    gridbuffer[q][r].Set(null, 0, false);
            }
        }

        public bool IsOwned(int q, int r)
        {
            return grid[q][r].GetInfluence() >= infThreshold;
        }

        public int ClosestOwned(int q, int r, GameObject player)
        {
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
            visited.Add(new Vector2Int(q, r));
            Queue<Vector3Int> queue = new Queue<Vector3Int>();
            queue.Enqueue(new Vector3Int(q, r, 0));
            while (queue.Count > 0)
            {
                Vector3Int curr = queue.Dequeue();
                if (grid[curr[0]][curr[1]].GetOwner() == player && grid[curr[0]][curr[1]].GetInfluence() >= infThreshold)
                    return curr[2];
                else
                {
                    List<Vector2Int> neighbors = GetNeighbors(curr[0], curr[1]);
                    foreach (var n in neighbors)
                    {
                        if (!visited.Contains(n))
                        {
                            visited.Add(n);
                            queue.Enqueue(new Vector3Int(n[0], n[1], curr[2] + 1));
                        }
                    }
                }
            }
            return -1;
        }

        public List<Vector2Int> GetNeighbors(int q, int r)
        {
            List<Vector2Int> neighbors = new List<Vector2Int>();
            bool qMin = q == 0, qMax = q == cap - 1, rMin = r == 0, rMax = r == cap - 1;
            if (!qMin)
                neighbors.Add(new Vector2Int(q - 1, r));
            if (!qMax)
                neighbors.Add(new Vector2Int(q + 1, r));
            if (!rMin)
                neighbors.Add(new Vector2Int(q, r - 1));
            if (!rMax)
                neighbors.Add(new Vector2Int(q, r + 1));
            if (!qMin && !rMax)
                neighbors.Add(new Vector2Int(q - 1, r + 1));
            if (!qMax && !rMin)
                neighbors.Add(new Vector2Int(q + 1, r - 1));
            return neighbors;
        }

        public void SwapBuffer()
        {
            List<List<TileState>> temp;
            temp = grid;
            grid = buffer;
            buffer = temp;
        }

        IEnumerator CalcBuffer()
        {
            while (true)
            {
                for (int i = 0; i < cap; i++)
                {
                    for (int j = 0; j < cap; j++)
                    {
                        buffer[i][j].Set(grid[i][j].GetOwner(), grid[i][j].GetInfluence(), false, false);
                        List<Vector2Int> neighbors = GetNeighbors(i, j);
                        foreach (var n in neighbors)
                            if (grid[n[0]][n[1]].GetInfluence() >= infThreshold)
                                AddNode(i, j, grid[n[0]][n[1]].GetOwner(), spreadAmount + grid[n[0]][n[1]].GetBuff(grid[n[0]][n[1]].GetOwner()), false);
                    }
                }
                SwapBuffer();
                yield return new WaitForSeconds(spreadRate);
            }
        }

        public void SetBuff(int q, int r, GameObject player, int buff)
        {
            grid[q][r].SetBuff(player, buff);
        }
    }
}