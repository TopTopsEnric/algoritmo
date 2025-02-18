using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Unity.VisualScripting;
using UnityEngine;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public int Size;
    public BoxCollider2D Panel;
    public GameObject token;
    //private int[,] GameMatrix; //0 not chosen, 1 player, 2 enemy de momento no hago nada con esto
    private Node[,] NodeMatrix;
    private int startPosx, startPosy;
    private int endPosx, endPosy;
    private List<Node> finalPath;
    void Awake()
    {
        Instance = this;
        //GameMatrix = new int[Size, Size];
        Calculs.CalculateDistances(Panel, Size);
    }
    private void Start()
    {
        /*for(int i = 0; i<Size; i++)
        {
            for (int j = 0; j< Size; j++)
            {
                GameMatrix[i, j] = 0;
            }
        }*/

        startPosx = Random.Range(0, Size);
        startPosy = Random.Range(0, Size);
        do
        {
            endPosx = Random.Range(0, Size);
            endPosy = Random.Range(0, Size);
        } while (endPosx == startPosx || endPosy == startPosy);

        //GameMatrix[startPosx, startPosy] = 2;
        //GameMatrix[startPosx, startPosy] = 1;
        NodeMatrix = new Node[Size, Size];
        CreateNodes();
        PaintWay(NodeMatrix[startPosx, startPosy], NodeMatrix[endPosx, endPosy]);
    }
    public void CreateNodes()
    {
        for (int i = 0; i < Size; i++)
        {
            for (int j = 0; j < Size; j++)
            {
                NodeMatrix[i, j] = new Node(i, j, Calculs.CalculatePoint(i, j));
                NodeMatrix[i, j].Heuristic = Calculs.CalculateHeuristic(NodeMatrix[i, j], endPosx, endPosy);
            }
        }
        for (int i = 0; i < Size; i++)
        {
            for (int j = 0; j < Size; j++)
            {
                SetWays(NodeMatrix[i, j], i, j);
            }
        }
        DebugMatrix();
    }
    public void DebugMatrix()
    {
        for (int i = 0; i < Size; i++)
        {
            for (int j = 0; j < Size; j++)
            {
                Instantiate(token, NodeMatrix[i, j].RealPosition, Quaternion.identity);
                Debug.Log("Element (" + j + ", " + i + ")");
                Debug.Log("Position " + NodeMatrix[i, j].RealPosition);
                Debug.Log("Heuristic " + NodeMatrix[i, j].Heuristic);
                Debug.Log("Ways: ");
                foreach (var way in NodeMatrix[i, j].WayList)
                {
                    Debug.Log(" (" + way.NodeDestiny.PositionX + ", " + way.NodeDestiny.PositionY + ")");
                }
            }
        }
    }
    public void SetWays(Node node, int x, int y)
    {
        node.WayList = new List<Way>();
        if (x > 0)
        {
            node.WayList.Add(new Way(NodeMatrix[x - 1, y], Calculs.LinearDistance));
            if (y > 0)
            {
                node.WayList.Add(new Way(NodeMatrix[x - 1, y - 1], Calculs.DiagonalDistance));
            }
        }
        if (x < Size - 1)
        {
            node.WayList.Add(new Way(NodeMatrix[x + 1, y], Calculs.LinearDistance));
            if (y > 0)
            {
                node.WayList.Add(new Way(NodeMatrix[x + 1, y - 1], Calculs.DiagonalDistance));
            }
        }
        if (y > 0)
        {
            node.WayList.Add(new Way(NodeMatrix[x, y - 1], Calculs.LinearDistance));
        }
        if (y < Size - 1)
        {
            node.WayList.Add(new Way(NodeMatrix[x, y + 1], Calculs.LinearDistance));
            if (x > 0)
            {
                node.WayList.Add(new Way(NodeMatrix[x - 1, y + 1], Calculs.DiagonalDistance));
            }
            if (x < Size - 1)
            {
                node.WayList.Add(new Way(NodeMatrix[x + 1, y + 1], Calculs.DiagonalDistance));
            }
        }
    }

    public void PaintWay(Node startNode, Node endNode)
    {
        finalPath = new List<Node>();
        List<Node> closedList = new List<Node>();
        Dictionary<Node, float> openList = new Dictionary<Node, float>();
        Dictionary<Node, float> gScores = new Dictionary<Node, float>();

        openList.Add(startNode, startNode.Heuristic);
        gScores[startNode] = 0;

        while (openList.Count > 0)
        {
            Node currentNode = openList.OrderBy(kvp => kvp.Value).First().Key;

            if (currentNode.PositionX == endNode.PositionX && currentNode.PositionY == endNode.PositionY)
            {
                Debug.Log("¡Camino encontrado!");
                ReconstructPath(currentNode);
                StartCoroutine(VisualizePathCoroutine());
                return;
            }

            openList.Remove(currentNode);
            closedList.Add(currentNode);

            foreach (Way way in currentNode.WayList)
            {
                Node neighbor = way.NodeDestiny;

                if (closedList.Contains(neighbor))
                    continue;

                float tentativeGScore = gScores[currentNode] + way.Cost;

                if (!openList.ContainsKey(neighbor))
                {
                    neighbor.NodeParent = currentNode;
                    gScores[neighbor] = tentativeGScore;
                    float fScore = tentativeGScore + neighbor.Heuristic;
                    openList.Add(neighbor, fScore);
                }
                else if (tentativeGScore < gScores[neighbor])
                {
                    neighbor.NodeParent = currentNode;
                    gScores[neighbor] = tentativeGScore;
                    openList[neighbor] = tentativeGScore + neighbor.Heuristic;
                }
            }
        }

        Debug.Log("No se encontró camino");
    }

    private void ReconstructPath(Node endNode)
    {
        Node currentNode = endNode;
        while (currentNode != null)
        {
            finalPath.Insert(0, currentNode);
            currentNode = currentNode.NodeParent;
        }
    }

    private IEnumerator VisualizePathCoroutine()
    {
        // Esperar 20 segundos antes de empezar a visualizar
        yield return new WaitForSeconds(10f);

        // Visualizar cada nodo del camino
        foreach (Node node in finalPath)
        {
            // Encontrar el Circle en la posición del nodo
            GameObject[] circles = GameObject.FindObjectsOfType<GameObject>()
                .Where(obj => obj.name.Contains("Circle") &&
                       Vector2.Distance(obj.transform.position, node.RealPosition) < 0.1f)
                .ToArray();

            if (circles.Length > 0)
            {
                SpriteRenderer spriteRenderer = circles[0].GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = Color.yellow;
                }
            }

            // Esperar un pequeño tiempo entre cada visualización
            yield return new WaitForSeconds(0.2f);
        }
    }

}
