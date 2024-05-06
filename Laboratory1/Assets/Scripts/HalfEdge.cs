using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HalfEdge
{
    public class HalfEdge
    {
        public int index;
        public Vertex sourceVertex;
        public Face face;
        public HalfEdge prevEdge;
        public HalfEdge nextEdge;
        public HalfEdge twinEdge;

        public HalfEdge(int index, Vertex sourceVertex, Face face, HalfEdge prevEdge = null, HalfEdge nextEdge = null,
            HalfEdge twinEdge = null)
        {
            this.index = index;
            this.sourceVertex = sourceVertex;
            this.face = face;
            this.prevEdge = prevEdge;
            this.nextEdge = nextEdge;
            this.twinEdge = twinEdge;
        }
    }

    public class Vertex
    {
        public int index;
        public Vector3 position;
        public HalfEdge outgoingEdge;

        public Vertex(int index, Vector3 pos) : this(index, pos, null)
        {
        }

        public Vertex(int index, Vector3 pos, HalfEdge outgoingEdge)
        {
            this.index = index;
            position = pos;
            this.outgoingEdge = outgoingEdge;
        }

        public List<HalfEdge> GetAdjacentEdges(List<HalfEdge> edges)
        {
            var adjacentEdges = new List<HalfEdge>();

            foreach (var edge in edges)
                if (edge.sourceVertex == this || edge.nextEdge.sourceVertex == this)
                    if (!adjacentEdges.Contains(edge))
                    {
                        adjacentEdges.Add(edge);
                        adjacentEdges.Add(edge.twinEdge);
                    }

            return adjacentEdges;
        }

        public List<HalfEdge> GetIncidentEdges(List<HalfEdge> edges)
        {
            var incidentEdges = new List<HalfEdge>();

            foreach (var edge in edges)
                if (edge.nextEdge.sourceVertex == this && outgoingEdge != edge)
                    if (!incidentEdges.Contains(edge))
                        incidentEdges.Add(edge);
            return incidentEdges;
        }

        public List<Face> GetAdjacentFaces(List<HalfEdge> edges)
        {
            var adjacentFaces = new Dictionary<int, Face>();

            var adjacentEdges = GetAdjacentEdges(edges);

            for (var i = 0; i < adjacentEdges.Count; i += 2)
            {
                var edge = adjacentEdges[i];
                var twinEdge = adjacentEdges[i + 1];
                adjacentFaces.TryAdd(edge.face.index, edge.face);

                if (twinEdge != null) adjacentFaces.TryAdd(twinEdge.face.index, twinEdge.face);
            }

            return adjacentFaces.Values.ToList();
        }
    }

    public class Face
    {
        public int index;
        public HalfEdge edge;

        public Face(int index)
        {
            this.index = index;
        }

        public Face(int index, HalfEdge edge)
        {
            this.index = index;
            this.edge = edge;
        }

        public void GetEdgesVertices(out List<Vertex> faceVertices, out List<HalfEdge> faceEdges)
        {
            faceVertices = new List<Vertex>();
            faceEdges = new List<HalfEdge>();

            var halfEdge = edge;
            do
            {
                faceVertices.Add(halfEdge.sourceVertex);
                faceEdges.Add(halfEdge);
                halfEdge = halfEdge.nextEdge;
            } while (edge != halfEdge);
        }

        public List<Vertex> GetVertices()
        {
            var faceVertices = new List<Vertex>();

            var halfEdge = edge;
            do
            {
                faceVertices.Add(halfEdge.sourceVertex);
                halfEdge = halfEdge.nextEdge;
            } while (edge != halfEdge);

            return faceVertices;
        }

        public List<HalfEdge> GetEdges()
        {
            var faceEdges = new List<HalfEdge>();

            var halfEdge = edge;
            do
            {
                faceEdges.Add(halfEdge);
                halfEdge = halfEdge.nextEdge;
            } while (edge != halfEdge);

            return faceEdges;
        }
    }

    public class HalfEdgeMesh
    {
        public List<Vertex> vertices;
        public List<HalfEdge> edges;
        public List<Face> faces;

        private int nVerticesForTopology;

        public HalfEdgeMesh(Mesh mesh)
        {
            vertices = new List<Vertex>();
            edges = new List<HalfEdge>();
            faces = new List<Face>();
            var meshVertices = mesh.vertices;

            var mapOfIndex = new Dictionary<string, int>();

            for (var i = 0; i < mesh.vertexCount; i++) vertices.Add(new Vertex(i, meshVertices[i]));

            var shapes = mesh.GetIndices(0);
            var meshTopology = mesh.GetTopology(0);
            nVerticesForTopology = meshTopology.Equals(MeshTopology.Triangles) ? 3 : 4;

            var indexVertex = 0;
            var indexHalfEdge = 0;
            var nbFaces = shapes.Length / nVerticesForTopology;

            var cmp = 0;
            for (var i = 0; i < nbFaces; i++)
            {
                var f = new Face(i);
                var tempHalfEdges = new List<HalfEdge>();

                for (var j = 0; j < nVerticesForTopology; j++)
                {
                    var v = vertices[shapes[indexVertex++]];
                    var halfEdge = new HalfEdge(indexHalfEdge++, v, f);
                    tempHalfEdges.Add(halfEdge);
                }

                var nbTempHalfEdge = tempHalfEdges.Count;
                for (var j = 0; j < nbTempHalfEdge; j++)
                {
                    var currentHalfEdge = tempHalfEdges[j];
                    var nextEdgeIndice = j == nbTempHalfEdge - 1 ? 0 : j + 1;
                    var previousEdgeIndice = j == 0 ? nbTempHalfEdge - 1 : j - 1;

                    currentHalfEdge.prevEdge = tempHalfEdges[previousEdgeIndice];
                    currentHalfEdge.nextEdge = tempHalfEdges[nextEdgeIndice];

                    currentHalfEdge.sourceVertex.outgoingEdge = currentHalfEdge;
                    edges.Add(currentHalfEdge);
                }

                for (var j = 0; j < nVerticesForTopology; j++)
                {
                    var startIndex = edges[cmp].sourceVertex.index;
                    var endIndex = edges[cmp].nextEdge.sourceVertex.index;

                    var newKey = startIndex + "|" + endIndex;
                    mapOfIndex.Add(newKey, edges[cmp].index);

                    cmp++;
                }

                f.edge = tempHalfEdges[0];
                faces.Add(f);
            }

            foreach (var kp in mapOfIndex)
            {
                var key = kp.Key;
                var value = kp.Value;

                var startIndex = int.Parse(key.Split("|")[0]);
                var endIndex = int.Parse(key.Split("|")[1]);
                var reversedKey = "" + endIndex + "|" + startIndex;

                if (mapOfIndex.TryGetValue(reversedKey, out var reversedValue))
                {
                    edges[reversedValue].twinEdge = edges[value];
                    edges[value].twinEdge = edges[reversedValue];
                }
            }
        }

        public Mesh ConvertToFaceVertexMesh()
        {
            var newMesh = new Mesh();

            var vertices = new Vector3[this.vertices.Count];
            var quads = new int[faces.Count * nVerticesForTopology];

            foreach (var v in this.vertices) vertices[v.index] = v.position;

            for (var i = 0; i < faces.Count; i++)
            {
                var f = faces[i];
                var halfEdge = f.edge.prevEdge;
                var offset = 0;
                var j = i * nVerticesForTopology;
                do
                {
                    quads[j + offset] = halfEdge.sourceVertex.index;
                    offset++;
                    halfEdge = halfEdge.nextEdge;
                } while (f.edge.prevEdge != halfEdge);
            }

            newMesh.vertices = vertices;
            newMesh.SetIndices(quads, MeshTopology.Quads, 0);
            newMesh.RecalculateBounds();
            newMesh.RecalculateNormals();
            return newMesh;
        }

        public string ConvertToCsvFormat(string separator = "\t")
        {
            var tabSize = Mathf.Max(edges.Count, faces.Count, vertices.Count);
            var strings = new List<string>(new string[tabSize]);

            for (var i = 0; i < edges.Count; i++)
            {
                var halfEdge = edges[i];
                strings[i] += halfEdge.index + separator;
                strings[i] += halfEdge.sourceVertex.index + "; " + halfEdge.nextEdge.sourceVertex.index + separator;
                strings[i] += halfEdge.prevEdge.index + "; " + halfEdge.nextEdge.index + "; ";
                strings[i] += halfEdge.twinEdge != null ? halfEdge.twinEdge.index : "null";
                strings[i] += separator + separator;
            }

            for (var i = edges.Count; i < tabSize; i++) strings[i] += separator + separator + separator + separator;

            for (var i = 0; i < faces.Count; i++)
            {
                var f = faces[i];
                strings[i] += f.index + separator + f.edge.index + separator + separator;
            }

            for (var i = faces.Count; i < tabSize; i++) strings[i] += separator + separator + separator;

            for (var i = 0; i < vertices.Count; i++)
            {
                var vertex = vertices[i];
                var pos = vertex.position;
                strings[i] += vertex.index + separator;
                strings[i] += pos.x.ToString("N02") + " ";
                strings[i] += pos.y.ToString("N02") + " ";
                strings[i] += pos.z.ToString("N02") + separator;
                strings[i] += vertex.outgoingEdge.index + separator + separator;
            }

            var header = "HalfEdges" + separator + separator + separator + separator + "Faces" + separator + separator +
                         separator + "Vertices\n" +
                         "Index" + separator + "Src Vertex Index,end" + separator +
                         "Prev + Next + Twin HalfEdge Index" + separator + separator +
                         "Index" + separator + "HalfEdge Index" + separator + separator +
                         "Index" + separator + "Position" + separator + "OutgoingEdgeIndex" + "\n";

            return header + string.Join("\n", strings);
        }

        public void DrawGizmos(bool drawVertices, bool drawEdges, bool drawFaces, Transform transform)
        {
            var style = new GUIStyle
            {
                fontSize = 15,
                normal =
                {
                    textColor = Color.red
                }
            };

            if (drawVertices)
                for (var i = 0; i < vertices.Count; i++)
                {
                    var worldPos = transform.TransformPoint(vertices[i].position);
                    Handles.Label(worldPos, i.ToString(), style);
                }

            style.normal.textColor = Color.black;
            if (drawEdges)
                for (var i = 0; i < edges.Count; i++)
                {
                    var worldPositionStart = transform.TransformPoint(edges[i].sourceVertex.position);
                    var worldPositionEnd = transform.TransformPoint(edges[i].nextEdge.sourceVertex.position);
                    Gizmos.DrawLine(worldPositionStart, worldPositionEnd);
                    Handles.Label((worldPositionEnd + worldPositionStart) / 2, "E : " + i, style);
                }

            style.normal.textColor = Color.blue;
            if (drawFaces)
                for (var i = 0; i < faces.Count; i++)
                {
                    var e = faces[i].edge;
                    var p0 = transform.TransformPoint(e.sourceVertex.position);
                    var p1 = transform.TransformPoint(e.nextEdge.sourceVertex.position);
                    var p2 = transform.TransformPoint(e.nextEdge.nextEdge.sourceVertex.position);
                    var p3 = nVerticesForTopology > 3
                        ? e.nextEdge.nextEdge.nextEdge.sourceVertex.position
                        : Vector3.zero;
                    p3 = transform.TransformPoint(p3);

                    var index1 = e.index;
                    var index2 = e.nextEdge.index;
                    var index3 = e.nextEdge.nextEdge.index;
                    var index4 = nVerticesForTopology > 3 ? e.nextEdge.nextEdge.nextEdge.index : -1;

                    var str = $"{i} ({index1},{index2},{index3},{index4})";
                    Handles.Label((p0 + p1 + p2 + p3) / nVerticesForTopology, str, style);
                }
        }
    }
}