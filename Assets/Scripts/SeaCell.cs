using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SeaCell : MonoBehaviour {

    public Mesh kMesh;

    public Camera kUnderWaterPassCam;

    [NonSerialized] public List<Vector3> vertices;
    [NonSerialized] public List<Color> colors;
    [NonSerialized] public List<Vector2> uvs;
    [NonSerialized] public List<int> triangles;

    private void Awake()
    {
        kMesh = GetComponent<MeshFilter>().mesh = new Mesh();
        Gen();
    }

    public void Apply()
    {
        kMesh.SetVertices(vertices);
        kMesh.SetColors(colors);
        kMesh.SetUVs(0, uvs);
        kMesh.SetTriangles(triangles, 0);

        kMesh.UploadMeshData(true);
    }

    public void Gen()
    {
        vertices = new List<Vector3>();
        colors = new List<Color>();
        uvs = new List<Vector2>();
        triangles = new List<int>();

        int halfwidth = ConfigParam.BLOCKMAXWIDTH / 2;
        int halfheight = ConfigParam.BLOCKMAXHEIGHT / 2;

        this.transform.localPosition = new Vector3( halfwidth, ConfigParam.Water_Height, halfheight);

        Vector3 leftdown = new Vector3(-halfwidth, 0, -halfheight);
        Vector3 leftup = new Vector3(-halfwidth, 0, halfheight);

        LC_Helper.Loop(4, (index) =>
        {
            bool left = index < 2;
            bool down = index == 0 || index == 3;

            int leftm = left ? -1 : 1;
            int downm = down ? -1 : 1;

            Vector3 a = new Vector3(leftm * halfwidth, 0 , downm * halfheight);
            vertices.Add(a);

        });

        triangles.Add(0);
        triangles.Add(1);
        triangles.Add(2);
        triangles.Add(2);
        triangles.Add(3);
        triangles.Add(0);

        Apply();
    }
}
