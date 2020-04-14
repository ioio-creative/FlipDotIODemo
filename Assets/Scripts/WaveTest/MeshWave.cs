using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshWave : MonoBehaviour
{
    [Serializable]
    public class Harmonic
    {
        public float Amp;
        public float Lambda;
        public float Speed;
        public float PhaseOffset;
        /// <summary>
        /// Radian increment
        /// </summary>
        public float RadianStep
        {
            get; set;
        }
        public float TimeShift
        {
            get; set;
        }
    }

    [Range(0f, 4096f)]
    [SerializeField]
    private int samples = 1024;
    [SerializeField]
    private float length = 25;
    [SerializeField]
    private float heightOffset = 10;

    /// <summary>
    /// Domain dimension spacing
    /// </summary>
    private static float DomainIncrement;

    [SerializeField]
    private Harmonic[] harmonics;
    private Vector3[] superpositionPoints;

    [SerializeField]
    private MeshFilter mFilter;
    private Mesh mesh;

#if UNITY_EDITOR
    private void OnValidate()
    {
        foreach (var harmonic in harmonics)
        {
            harmonic.Lambda = Mathf.Max(harmonic.Lambda, 0);
        }
    }

    private void OnDrawGizmos()
    {
        mesh = new Mesh();
        UpdateMesh(mesh);
        GetComponent<MeshFilter>().mesh = mesh;
    }

    private void OnDrawGizmosSelected()
    {
        CalcPoints();
    }

#endif

    private void Awake()
    {
        mesh = new Mesh();

        if (mFilter == null)
        {
            mFilter = GetComponent<MeshFilter>();
        }
        mFilter.mesh = mesh;
        //superpositionPoints = new List<Vector3>(samples);
    }


    private void Start()
    {

    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetTimeShift();
        }

        CalcPoints();
        UpdateMesh(mesh);
    }

    private void CalcPoints()
    {
        superpositionPoints = new Vector3[samples];
        DomainIncrement = length / samples;

        foreach (var harmonic in harmonics)
        {
            harmonic.RadianStep = harmonic.Lambda == 0 ? float.NaN : Mathf.PI * 2 * (DomainIncrement / harmonic.Lambda);
            if (harmonic.Lambda != 0)
            {
                harmonic.TimeShift += Time.deltaTime * harmonic.Speed / harmonic.Lambda;
            }
            harmonic.TimeShift %= (2 * Mathf.PI);
        }

        //Increment theta with time
        //timeShift += (Time.deltaTime * speed);
        //timeShift %= (2 * Mathf.PI);

        //float x = timeShift;

        for (int i = 0; i < superpositionPoints.Length; i++)
        {
            float superposedValue = 0;
            foreach (var component in harmonics)
            {
                if (!float.IsNaN(component.RadianStep))
                {
                    superposedValue += Mathf.Sin(i * component.RadianStep + component.TimeShift + component.PhaseOffset * Mathf.PI / 180f) * component.Amp;
                }
            }

            superpositionPoints[i] = new Vector3(i * DomainIncrement, superposedValue);
        }
    }

    private void UpdateMesh(Mesh _mesh)
    {
        if (superpositionPoints == null) return;

        Vector3[] vertices = new Vector3[samples * 2];
        int[] triangles = new int[(vertices.Length - 2) * 3 / 2];

        for (int i = 0; i < superpositionPoints.Length; i++)
        {
            vertices[i * 2] = new Vector3(superpositionPoints[i].x, -heightOffset);
            vertices[i * 2 + 1] = superpositionPoints[i];
        }

        for (int i = 0; i < triangles.Length/6; i++)
        {
            int j = i * 6;
            triangles[j] = i * 2;
            triangles[j + 1] = i * 2 + 1;
            triangles[j + 2] = i * 2 + 2;
            triangles[j + 3] = i * 2 + 2;
            triangles[j + 4] = i * 2 + 1;
            triangles[j + 5] = i * 2 + 3;
        }

        _mesh.Clear();
        _mesh.vertices = vertices;
        _mesh.triangles = triangles;
        _mesh.RecalculateNormals();
        mFilter.mesh = _mesh;
    }

    private void RenderWave()
    {
        //line.positionCount = samples;
        //line.SetPositions(superpositionPoints);
    }

    public void ResetTimeShift()
    {
        foreach (var harmonic in harmonics)
        {
            harmonic.TimeShift = 0;
        }
    }
}
