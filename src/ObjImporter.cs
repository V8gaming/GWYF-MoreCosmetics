using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace GWYF_NewClothing;

internal static class ObjImporter
{
    public static Mesh Import(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogError($"[GWYF] OBJ file not found: {path}");
            return null!;
        }

        var lines = File.ReadAllLines(path);
        var positions = new List<Vector3>();
        var uvs = new List<Vector2>();
        var normals = new List<Vector3>();
        var vertices = new List<VertexTriplet>();
        var faces = new List<(int a, int b, int c)>();

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line[0] == '#') continue;

            var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) continue;

            switch (parts[0])
            {
                case "v":
                    positions.Add(ParseVector3(parts, 1));
                    break;
                case "vt":
                    uvs.Add(ParseVector2(parts, 1));
                    break;
                case "vn":
                    normals.Add(ParseVector3(parts, 1));
                    break;
                case "f":
                    ParseFace(parts, vertices, faces);
                    break;
            }
        }

        // Deduplicate and build mesh arrays
        var uniqueVerts = new Dictionary<VertexTriplet, int>();
        var meshVerts = new List<Vector3>();
        var meshUvs = new List<Vector2>();
        var meshNormals = new List<Vector3>();
        var meshTriangles = new List<int>();

        foreach (var (a, b, c) in faces)
        {
            var t0 = vertices[a];
            var t1 = vertices[b];
            var t2 = vertices[c];

            meshTriangles.Add(GetOrAdd(t0, uniqueVerts, meshVerts, meshUvs, meshNormals, positions, uvs, normals));
            meshTriangles.Add(GetOrAdd(t1, uniqueVerts, meshVerts, meshUvs, meshNormals, positions, uvs, normals));
            meshTriangles.Add(GetOrAdd(t2, uniqueVerts, meshVerts, meshUvs, meshNormals, positions, uvs, normals));
        }

        var mesh = new Mesh();
        mesh.name = Path.GetFileNameWithoutExtension(path);
        mesh.indexFormat = meshVerts.Count > 65535
            ? UnityEngine.Rendering.IndexFormat.UInt32
            : UnityEngine.Rendering.IndexFormat.UInt16;
        mesh.SetVertices(meshVerts);
        mesh.SetTriangles(meshTriangles, 0);

        if (meshUvs.Count == meshVerts.Count)
            mesh.SetUVs(0, meshUvs);

        if (meshNormals.Count == meshVerts.Count)
            mesh.SetNormals(meshNormals);
        else
            mesh.RecalculateNormals();

        mesh.RecalculateBounds();
        mesh.RecalculateTangents();

        Debug.Log($"[GWYF] Imported OBJ '{Path.GetFileName(path)}': {meshVerts.Count} verts, {meshTriangles.Count / 3} tris.");

        return mesh;
    }

    private static int GetOrAdd(VertexTriplet vt,
        Dictionary<VertexTriplet, int> dict,
        List<Vector3> verts, List<Vector2> uvs, List<Vector3> norms,
        List<Vector3> posSrc, List<Vector2> uvSrc, List<Vector3> nrmSrc)
    {
        if (dict.TryGetValue(vt, out var idx))
            return idx;

        var newIdx = verts.Count;
        dict[vt] = newIdx;

        verts.Add(vt.posIndex > 0 && vt.posIndex <= posSrc.Count
            ? posSrc[vt.posIndex - 1] : Vector3.zero);

        if (vt.uvIndex > 0 && vt.uvIndex <= uvSrc.Count)
            uvs.Add(uvSrc[vt.uvIndex - 1]);

        if (vt.nrmIndex > 0 && vt.nrmIndex <= nrmSrc.Count)
            norms.Add(nrmSrc[vt.nrmIndex - 1]);

        return newIdx;
    }

    private static Vector3 ParseVector3(string[] parts, int start)
    {
        float x = 0, y = 0, z = 0;
        if (parts.Length > start) float.TryParse(parts[start], NumberStyles.Float, CultureInfo.InvariantCulture, out x);
        if (parts.Length > start + 1) float.TryParse(parts[start + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out y);
        if (parts.Length > start + 2) float.TryParse(parts[start + 2], NumberStyles.Float, CultureInfo.InvariantCulture, out z);
        // Import raw coordinates — user must export from Blender with Forward:-Z, Up:Y
        return new Vector3(x, y, z);
    }

    private static Vector2 ParseVector2(string[] parts, int start)
    {
        float x = 0, y = 0;
        if (parts.Length > start) float.TryParse(parts[start], NumberStyles.Float, CultureInfo.InvariantCulture, out x);
        if (parts.Length > start + 1) float.TryParse(parts[start + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out y);
        return new Vector2(x, y);
    }

    private static void ParseFace(string[] parts, List<VertexTriplet> vertices, List<(int, int, int)> faces)
    {
        var indices = new List<int>();
        for (int i = 1; i < parts.Length; i++)
        {
            indices.Add(vertices.Count);
            vertices.Add(ParseVertexTriplet(parts[i]));
        }

        // Triangulate fan
        for (int i = 1; i < indices.Count - 1; i++)
        {
            faces.Add((indices[0], indices[i], indices[i + 1]));
        }
    }

    private static VertexTriplet ParseVertexTriplet(string part)
    {
        var splits = part.Split('/');
        int pos = 0, uv = 0, nrm = 0;

        if (splits.Length > 0) int.TryParse(splits[0], out pos);
        if (splits.Length > 1 && !string.IsNullOrEmpty(splits[1])) int.TryParse(splits[1], out uv);
        if (splits.Length > 2) int.TryParse(splits[2], out nrm);

        return new VertexTriplet(pos, uv, nrm);
    }

    private readonly struct VertexTriplet
    {
        public readonly int posIndex;
        public readonly int uvIndex;
        public readonly int nrmIndex;

        public VertexTriplet(int pos, int uv, int nrm)
        {
            posIndex = pos;
            uvIndex = uv;
            nrmIndex = nrm;
        }

        public override bool Equals(object obj) =>
            obj is VertexTriplet other &&
            posIndex == other.posIndex &&
            uvIndex == other.uvIndex &&
            nrmIndex == other.nrmIndex;

        public override int GetHashCode() =>
            HashCode.Combine(posIndex, uvIndex, nrmIndex);
    }
}
