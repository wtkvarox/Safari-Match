using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Board : MonoBehaviour
{
    public int width;
    public int height;
    public GameObject tileObject;
    public float cameraSizeOffset;
    public float cameraVerticalOffset;
    public GameObject[] availablePieces;
    public Tile[,] Tiles;
    public Piece[,] Pieces;
    public Tile startTile;
    public Tile endTile;
    public AudioSource audioSource;
    public float timeBetweenPieces = 0.01F;
    private Boolean swappingPieces;

    void Start()
    {
        Tiles = new Tile[width, height];
        Pieces = new Piece[width, height];

        SetupBoard();
        PositionCamera();
        StartCoroutine(SetupPieces());
    }

    private void SetupBoard()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var obj = Instantiate(tileObject, new Vector3(x, y, -5), Quaternion.identity);
                obj.transform.parent = transform;
                Tiles[x, y] = obj.GetComponent<Tile>();
                Tiles[x, y]?.Setup(x, y, this);
            }
        }
    }

    private void PositionCamera()
    {
        float newPosX = (float)width / 2F;
        float newPosY = (float)height / 2F;

        Camera.main.transform.position = new Vector3(newPosX - 0.5F, newPosY - 0.5F + cameraVerticalOffset, -10);

        float horizontal = width + 1;
        float vertical = (height / 2) + 1;

        Camera.main.orthographicSize = horizontal > vertical ? horizontal + cameraSizeOffset : vertical + cameraVerticalOffset;
    }

    private IEnumerator SetupPieces()
    {
        int maxIterations = int.MaxValue;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                yield return new WaitForSeconds(timeBetweenPieces);

                if (Pieces[x, y] == null)
                {
                    SetupPieceAt(x, y, maxIterations);

                }
            }
        }

        yield return null;
    }

    private void SetupPieceAt(int x, int y, int maxIterations)
    {
        int currentIteration = 0;

        do
        {
            ClearPieceAt(x, y);
            CreatePieceAt(x, y);
            currentIteration++;
        } while (currentIteration < maxIterations && HasPreviousMatches(x, y));
    }

    private void ClearPieceAt(int x, int y)
    {
        Pieces[x, y]?.Remove(true);
        Pieces[x, y] = null;
    }

    private Piece CreatePieceAt(int x, int y)
    {
        var selectedPiece = availablePieces[UnityEngine.Random.Range(0, availablePieces.Length)];
        var obj = Instantiate(selectedPiece, new Vector3(x, y + 2, -5), Quaternion.identity);
        obj.transform.parent = transform;
        Pieces[x, y] = obj.GetComponent<Piece>();
        Pieces[x, y]?.Setup(x, y, this);
        Pieces[x, y]?.Move(x, y);

        return Pieces[x, y];
    }

    public void TileDown(Tile tile_)
    {
        if (!swappingPieces)
        {
            startTile = tile_;
        }
    }

    public void TileOver(Tile tile_)
    {
        if (!swappingPieces) { 
            endTile = tile_;
    }
    }

    public void TileUp(Tile tile_)
    {
        if (!swappingPieces && startTile != null && endTile != null && IsCloseTo(startTile, endTile))
        {
            StartCoroutine(SwapTiles());
        }
    }

    public bool IsCloseTo(Tile start, Tile end)
    {
        return Math.Abs(start.x - end.x) == 1 && start.y == end.y ||
               Math.Abs(start.y - end.y) == 1 && start.x == end.x;
    }

    IEnumerator SwapTiles()
    {
        swappingPieces = true;

        var StartPiece = Pieces[startTile.x, startTile.y];
        var EndPiece = Pieces[endTile.x, endTile.y];

        StartPiece.Move(endTile.x, endTile.y);
        EndPiece.Move(startTile.x, startTile.y);

        Pieces[startTile.x, startTile.y] = EndPiece;
        Pieces[endTile.x, endTile.y] = StartPiece;

        yield return new WaitForSeconds(0.5F);

        var startMatches = GetMatchByPiece(endTile.x, endTile.y, 3);
        var endMatches = GetMatchByPiece(startTile.x, startTile.y, 3);

        var allMatches = startMatches.Union(endMatches).ToList();

        if (allMatches.Count == 0)
        {
            StartPiece.Move(startTile.x, startTile.y);
            EndPiece.Move(endTile.x, endTile.y);
            Pieces[startTile.x, startTile.y] = StartPiece;
            Pieces[endTile.x, endTile.y] = EndPiece;
        }
        else
        {
            ClearPieces(allMatches);
            audioSource.Play();
        }

        startTile = null;
        endTile = null;

        yield return null;
    }

    private void ClearPieces(List<Piece> piecesToClear)
    {
        piecesToClear.ForEach(piece =>
        {
            ClearPieceAt(piece.x, piece.y);
        });

        List<int> columns = GetColumns(piecesToClear);
        List<Piece> collapsePieces = CollapseColumns(columns, 0.3F);
        FindMatchsRecursively(collapsePieces);
    }

    private void FindMatchsRecursively(List<Piece> collapsePieces)
    {
        StartCoroutine(FindMatchsRecursivelyCoroutine(collapsePieces));
    }

    private IEnumerator FindMatchsRecursivelyCoroutine(List<Piece> collapsePieces)
    {
        yield return new WaitForSeconds(0.8F);

        var newMatches = collapsePieces
            .SelectMany(piece => GetMatchByPiece(piece.x, piece.y, 3) ?? Enumerable.Empty<Piece>())
            .ToList();

        if (newMatches.Any())
        {
            ClearPieces(newMatches);
            var newCollapsedPieces = CollapseColumns(GetColumns(newMatches), 0.3F);
            FindMatchsRecursively(newCollapsedPieces);
            audioSource.Play();
        }
        else
        {
            yield return new WaitForSeconds(0.1F);
            StartCoroutine(SetupPieces());
        }

        swappingPieces = false;
        yield return null;
    }

    private List<Piece> CollapseColumns(List<int> columns, float timeToCollapse)
    {
        List<Piece> movingPieces = new List<Piece>();

        foreach (var column in columns)
        {
            int emptySlotIndex = 0;

            while (emptySlotIndex < height && Pieces[column, emptySlotIndex] != null)
            {
                emptySlotIndex++;
            }

            if (emptySlotIndex == height)
            {
                continue;
            }

            for (int yPlus = emptySlotIndex + 1; yPlus < height; yPlus++)
            {
                if (Pieces[column, yPlus] != null)
                {
                    Pieces[column, yPlus].Move(column, emptySlotIndex);
                    Pieces[column, emptySlotIndex] = Pieces[column, yPlus];

                    if (!movingPieces.Contains(Pieces[column, emptySlotIndex]))
                    {
                        movingPieces.Add(Pieces[column, emptySlotIndex]);
                    }

                    Pieces[column, yPlus] = null;
                    emptySlotIndex++;
                }
            }
        }

        return movingPieces;
    }

    private List<int> GetColumns(List<Piece> piecesToClear)
    {
        var result = new List<int>();

        piecesToClear.ForEach(piece =>
        {
            if (!result.Contains(piece.x))
            {
                result.Add(piece.x);
            }
        });

        return result;
    }

    public List<Piece> GetMatchByPiece(int xPos, int yPos, int minPieces = 3)
    {
        var upMatch = GetMatchByDirection(xPos, yPos, new Vector2(0, 1), 2) ?? new List<Piece>();
        var downMatch = GetMatchByDirection(xPos, yPos, new Vector2(0, -1), 2) ?? new List<Piece>();
        var rightMatch = GetMatchByDirection(xPos, yPos, new Vector2(1, 0), 2) ?? new List<Piece>();
        var leftMatch = GetMatchByDirection(xPos, yPos, new Vector2(-1, 0), 2) ?? new List<Piece>();

        var verticalMatches = upMatch.Union(downMatch).ToList();
        var horizontalMatches = leftMatch.Union(rightMatch).ToList();

        var foundMatches = verticalMatches.Count >= minPieces
            ? verticalMatches
            : new List<Piece>();

        if (horizontalMatches.Count >= minPieces)
        {
            foundMatches = foundMatches.Union(horizontalMatches).ToList();
        }

        return foundMatches;
    }

    private bool HasPreviousMatches(int xPos, int yPos)
    {
        var downMatches = GetMatchByDirection(xPos, yPos, new Vector2(0, -1), 2) ?? new List<Piece>();
        var leftMatches = GetMatchByDirection(xPos, yPos, new Vector2(-1, 0), 2) ?? new List<Piece>();

        return (downMatches.Count > 0 || leftMatches.Count > 0);
    }

    public List<Piece> GetMatchByDirection(int xPos, int yPos, Vector2 direction, int minPieces = 3)
    {
        List<Piece> matches = new List<Piece>();

        if (IsValidPosition(xPos, yPos) && Pieces[xPos, yPos] != null)
        {
            matches.Add(Pieces[xPos, yPos]);
        }

        for (int i = 1; i < Mathf.Max(width, height); i++)
        {
            int nextX = xPos + (int)direction.x * i;
            int nextY = yPos + (int)direction.y * i;

            if (!IsValidPosition(nextX, nextY))
            {
                break;
            }

            var nextPiece = Pieces[nextX, nextY];

            if (nextPiece == null || matches.Count > 0 && nextPiece.type != matches[0].type)
            {
                break;
            }

            matches.Add(nextPiece);
        }

        return matches.Count >= minPieces ? matches : null;
    }

    private bool IsValidPosition(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }
}