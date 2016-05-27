using UnityEngine;
using System.Collections;
using System;
using UnityRTS;
using System.Collections.Generic;

public class PathingQuery {

    public class QueryChunk {
        public readonly PathingChunk Chunk;
        public Point GridPosition { get { return Chunk.GridPosition; } }
        public float[,] Field;
        public List<Point> Points = new List<Point>();
        public float MinEstimatedCost = 10000;
        public QueryChunk(PathingChunk chunk) {
            Chunk = chunk;
            Field = new float[chunk.Size, chunk.Size];
        }
    }

    public readonly PathingMap Map;
    public readonly Rect Destination;
    public readonly float Range;

    public int ChunkSize { get { return Map.ChunkSize; } }

    Expandable2DGrid<QueryChunk> chunks = new Expandable2DGrid<QueryChunk>();
    List<Point> grids = new List<Point>();

    struct Direction {
        public Point Dir;
        public float Cost;
    }
    static Direction[] dirs = new[] {
        new Direction() { Dir = new Point(-1, 0), Cost = 1, },
        new Direction() { Dir = new Point(1, 0), Cost = 1, },
        new Direction() { Dir = new Point(0, -1), Cost = 1, },
        new Direction() { Dir = new Point(0, 1), Cost = 1, },
    };
    static Point[] axes = new[] { new Point(1, 0), new Point(0, 1), };
    static Point[] directions = new[] { new Point(-1, 0), new Point(0, 1), new Point(1, 0), new Point(0, -1), };

    public PathingQuery(PathingMap map, Rect dest, float range) {
        Map = map;
        Destination = dest;
        Range = range;
        Initialize();
    }

    public QueryChunk GetChunk(Point grid) {
        return chunks.Get(grid);
    }
    public QueryChunk RequireChunk(Point grid) {
        var chunk = chunks.Get(grid);
        if (chunk == null) {
            chunk = new QueryChunk(Map.RequireChunk(grid));
            for (int x = 0; x < chunk.Field.GetLength(0); ++x) {
                for (int y = 0; y < chunk.Field.GetLength(1); ++y) {
                    chunk.Field[x, y] = float.NaN;
                }
            }
            chunks.Add(grid, chunk);
            grids.Add(grid);
        }
        return chunk;
    }

    public void Initialize() {
        /*Point otlg, otll, obrg, obrl;
        otlg = Map.WorldToChunk(new Vector2(Destination.xMin - Range, Destination.yMin + Range), out otll);
        obrg = Map.WorldToChunk(new Vector2(Destination.xMax - Range, Destination.yMax + Range), out obrl);

        Point tlg, tll, brg, brl;
        tlg = Map.WorldToChunk(new Vector2(Destination.xMin, Destination.yMin), out tll);
        brg = Map.WorldToChunk(new Vector2(Destination.xMax, Destination.yMax), out brl);

        for (int cy = tlg.Y; cy <= brg.Y; ++cy) {
            for (int cx = tlg.X; cx <= brg.X; ++cx) {
                var chunk = RequireChunk(new Point(cx, cy));
                var tl = new Point(cx == tlg.X ? tll.X : 0, cy == tlg.Y ? tll.Y : 0);
                var br = new Point(cx == brg.X ? brl.X : ChunkSize - 1, cy == brg.Y ? brl.Y : ChunkSize - 1);
                for (int lx = tl.X; lx <= br.X; ++lx) {
                    for (int ly = tl.Y; ly <= br.Y; ++ly) {
                        var world = new Point(cx * ChunkSize + lx, cy * ChunkSize + ly);
                        var clamped = new Point(Mathf.Clamp(world.X, tll.X, brl.X), Mathf.Clamp(world.Y, tll.Y, brl.Y));
                        if (Point.DistanceSquared(world, clamped) >= Range * Range) continue;
                        chunk.Field[lx, ly] = Mathf.Sqrt(Point.DistanceSquared(world, clamped));
                        if (Point.DistanceSquared(world, clamped) >= (Range - 1) * (Range - 1)) {
                            chunk.Points.Add(new Point(lx, ly));
                        }
                    }
                }
            }
        }*/
        Point dloc;
        var dgrid = Map.WorldToChunk(Destination.center, out dloc);
        var dchunk = RequireChunk(dgrid);
        dchunk.Field[dloc.X, dloc.Y] = 0;
        dchunk.Points.Add(dloc);
    }

    private Point _target;
    private QueryChunk[] dirChunks = new QueryChunk[4];
    private Point[] dirPoints = new Point[4];
    public float RequireFieldAt(Point pnt) {
        _target = pnt;
        Point ltarget;
        var gtarget = Map.WorldToChunk(pnt, out ltarget);
        var ctarget = RequireChunk(gtarget);
        if (ctarget.Chunk.Costs[ltarget.X, ltarget.Y] > 0) return ctarget.Field[ltarget.X, ltarget.Y];
        int executions = 0;
        for (int t = 0; t < 100 && executions < 2000 && float.IsNaN(ctarget.Field[ltarget.X, ltarget.Y]); ++t) {
            float minCost = float.MaxValue;
            for (int g = 0; g < grids.Count; ++g) {
                var chunk = chunks.Get(grids[g]);
                if (chunk.Points.Count <= 0) continue;
                var p0 = chunk.Points[0];
                var cost0 = chunk.Field[p0.X, p0.Y];
                minCost = Mathf.Min(minCost, cost0);
            }
            float executeUntil = minCost + 1;
            for (int g = 0; g < grids.Count; ++g) {
                var chunk = chunks.Get(grids[g]);
                var chunkP = chunk.GridPosition * chunk.Chunk.Size;
                for (int p = 0; p < chunk.Points.Count; ++p) {
                    var pp = chunk.Points[p];
                    if (chunk.Field[pp.X, pp.Y] > executeUntil) break;
                    ++executions;
                    var myField = chunk.Field[pp.X, pp.Y];
                    for (int d = 0; d < directions.Length; ++d) {
                        var dir = directions[d];
                        var pn = pp + dir;
                        int size = chunk.Chunk.Size;
                        var cnext = chunk;
                        if (pn.X < 0) {
                            cnext = RequireChunk(chunk.GridPosition + new Point(-1, 0));
                            pn.X += size;
                        } else if (pn.Y < 0) {
                            cnext = RequireChunk(chunk.GridPosition + new Point(0, -1));
                            pn.Y += size;
                        } else if (pn.X >= size) {
                            cnext = RequireChunk(chunk.GridPosition + new Point(1, 0));
                            pn.X -= size;
                        } else if (pn.Y >= size) {
                            cnext = RequireChunk(chunk.GridPosition + new Point(0, 1));
                            pn.Y -= size;
                        }
                        dirPoints[d] = pn;
                        dirChunks[d] = cnext;
                    }
                    for (int d = 0; d < directions.Length; ++d) {
                        var pn = dirPoints[d];
                        var cnext = dirChunks[d];
                        if (cnext.Chunk.Costs[pn.X, pn.Y] > 0) continue;
                        var nextField = myField;
                        for (int o = -1; o <= 1; o += 2) {
                            int od = (d + o + 4) % 4;
                            var op = dirPoints[od];
                            var oc = dirChunks[od];
                            if (oc.Field[op.X, op.Y] < nextField) {
                                nextField = (oc.Field[op.X, op.Y] + nextField) / 2;
                            }
                        }
                        ++nextField;
                        if (!(nextField >= cnext.Field[pn.X, pn.Y])) {
                            cnext.Field[pn.X, pn.Y] = nextField;
                            if (!cnext.Points.Contains(pn))
                                cnext.Points.Add(pn);
                        }
                    }
                    chunk.Points.RemoveAt(p--);
                }
            }
        }
        /*if (float.IsNaN(ctarget.Field[ltarget.X, ltarget.Y]))
            Debug.Log("NaN when required at " + pnt + "! Executed " + executions);*/
        return ctarget.Field[ltarget.X, ltarget.Y];
    }
    private void CalculateMinCost(QueryChunk chunk) {
        chunk.MinEstimatedCost = float.MaxValue;
        var cpos = chunk.GridPosition * chunk.Chunk.Size;
        for (int p = 0; p < chunk.Points.Count; ++p) {
            var lp = chunk.Points[p];
            var delta = lp + cpos - _target;
            //var totCost = chunk.Field[lp.X, lp.Y] + Mathf.Abs(delta.X) + Mathf.Abs(delta.Y);
            chunk.MinEstimatedCost = Mathf.Min(chunk.MinEstimatedCost, chunk.Field[lp.X, lp.Y]);
        }
    }

    public float GetCostAt(Point pnt) {
        Point ltarget;
        var gtarget = Map.WorldToChunk(pnt, out ltarget);
        var ctarget = GetChunk(gtarget);
        if (ctarget == null) return float.NaN;
        return ctarget.Field[ltarget.X, ltarget.Y];
    }

    private Point[] _getdir_corners = new[] { new Point(0, 0), new Point(1, 0), new Point(1, 1), new Point(0, 1), };
    private float[] _getdir_values = new float[4];
    public Vector2 GetDirectionFrom(Vector2 pos, bool drawGizmos = false) {
        if (drawGizmos) {
            Gizmos.DrawWireSphere(Destination.center.Y(0), 0.1f);
        }

        pos -= Vector2.one * 0.5f;
        var pnt = new Point(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y));
        var over = new Vector2(pos.x - pnt.X, pos.y - pnt.Y);
        pos += Vector2.one * 0.5f;

        var corners = _getdir_corners;
        var values = _getdir_values;

        float value = 0, count = 0;
        float[] weights = new [] {
            (1 - over.x) * (1 - over.y),
            (over.x) * (1 - over.y),
            (over.x) * (over.y),
            (1 - over.x) * (over.y),
        };
        for (int c = 0; c < corners.Length; ++c) {
            values[c] = RequireFieldAt(pnt + corners[c]);
            if (!float.IsNaN(values[c])) {
                var weight = weights[c];
                value += values[c] * weight;
                count += weight;
            }
        }
        value /= count;

        var gizCol = Gizmos.color;

        Vector2 sumDir = Vector2.zero;
        int firstClear = -1;
        for (int c = 0; c < corners.Length; ++c) {
            var corner = corners[c];

            var deltaV = values[c] - value;
            var deltaP = new Vector2(over.x - corner.X, over.y - corner.Y);
            Vector2 cdir = Vector2.zero;
            if (float.IsNaN(deltaV)) {
                continue;
                cdir = deltaP.normalized * 2;
            } else {
                var amnt = deltaV / deltaP.magnitude;
                cdir = deltaP * Mathf.Sign(amnt) / Mathf.Max(1.0f - Mathf.Abs(amnt), 0.01f);
            }

            if (!float.IsNaN(deltaV)) {
                sumDir += cdir;
                if (firstClear == -1) firstClear = c;
            }

            if (drawGizmos) {
                var cpos = ((Vector2)(pnt + corner) + Vector2.one * 0.5f).Y(0.05f);
                Gizmos.color = float.IsNaN(values[c]) ? Color.red : Color.blue;
                Gizmos.DrawLine(cpos, cpos + cdir.Y(0) / 4);
            }
        }

        if (drawGizmos) {
            Gizmos.color = new Color(1, 0.5f, 0, 1);
            Gizmos.DrawLine(pos.Y(0), pos.Y(0) + sumDir.normalized.Y(0.1f));
        }

        if (firstClear >= 0) {
            int firstBlock = (firstClear + 1) % 4;
            while (firstBlock < 4 && !float.IsNaN(values[firstBlock])) ++firstBlock;
            if (firstBlock < 4) {
                int endBlock = firstBlock;
                while (float.IsNaN(values[(endBlock + 1) % 4])) ++endBlock;
                Vector2 normal = Vector2.zero;
                bool twoWay = false;
                if (endBlock == firstBlock && float.IsNaN(values[(firstBlock + 2) % 4])) {
                    endBlock = firstBlock + 2;
                    twoWay = true;
                }
                if (endBlock > firstBlock) {
                    var delta = corners[endBlock % 4] - corners[firstBlock];
                    normal = new Vector2(-delta.Y, delta.X);
                } else {
                    normal = over - corners[endBlock % 4];
                }
                if (twoWay && Vector2.Dot(normal, over - corners[endBlock % 4]) < 0) normal = -normal;
                if (drawGizmos) {
                    Gizmos.color = new Color(1, 1, 0, 1);
                    Gizmos.DrawLine(pos.Y(0), pos.Y(0) + normal.normalized.Y(0.1f) * 0.5f);
                }
                var dp = Vector2.Dot(normal, sumDir);
                if (dp < 0) {
                    sumDir += normal * -dp / normal.sqrMagnitude * (twoWay ? 2 : 1);
                }
            }
        }

        sumDir.Normalize();

        if (drawGizmos) {
            Gizmos.color = gizCol;
            Gizmos.DrawLine(pos.Y(0), pos.Y(0) + sumDir.Y(0.1f));
            Gizmos.DrawWireSphere(pos.Y(0), 0.05f);
        }

        return sumDir;
    }

}
