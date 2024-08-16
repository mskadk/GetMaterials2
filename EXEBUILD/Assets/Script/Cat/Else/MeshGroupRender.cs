using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

/// <summary>
/// render +　mesh
/// </summary>



public class MeshGroupRender : MeshGroup
{

    private MeshRenderer meshRender;
    private MeshFilter meshFilter;
    
    private Material[] sharedMaterials = new Material[0];
  

    private Mesh mesh;

    public MeshGroupRender() : base()
    {
        mesh = newMesh();
    }

    public MeshGroupRender(MeshRenderer meshRender, MeshFilter meshFilter) : base()
    {
        this.meshRender = meshRender;
        this.meshFilter = meshFilter;


        meshRender.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRender.receiveShadows = false;

        meshRender.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;

        mesh = newMesh();
    }


    private Mesh newMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "FairyPlayer Mesh";
        mesh.hideFlags = HideFlags.HideAndDontSave;
        mesh.MarkDynamic();
        return mesh;
    }
    

    public override void endDrawed()
    {
        if (isDrawElements == false)
        {
            meshRender.enabled = false;
            mesh.Clear();
            return;
        }else
        {
            meshRender.enabled = true;
        }


        flashMaterial(lastMaterial);
        meshBody.endDrawBody();

        isDrawElements = false;

        //顶点颜
       // Mesh mesh = mesh1;// useMesh1 ? mesh1 : mesh2;
        mesh.Clear();
        mesh.SetVertices(meshBody.vertices);
        mesh.SetUVs(0, meshBody.uvs);
        mesh.SetNormals( meshBody.normals);
        mesh.SetColors(meshBody.colors);

        //子网格
        mesh.subMeshCount = meshSub.Count;
        for (int i = 0; i < meshSub.Count; i++)
        {
            mesh.SetTriangles(meshSub[i], i);
        }
        mesh.MarkDynamic();
        mesh.RecalculateBounds();

        // Set materials.
        if (materialList.Count == sharedMaterials.Length)
            materialList.CopyTo(sharedMaterials);
        else
            sharedMaterials = materialList.ToArray();

        setMeshTextures(mesh, sharedMaterials);
    }

    #region render + filter

    public void setBaseRender(MeshRenderer meshRender, MeshFilter meshFilter)
    {
        this.meshRender = meshRender;
        this.meshFilter = meshFilter;
    }

    public Material[] getMaterials()
    {
        return sharedMaterials;
    }

    public void setMesh(Mesh mesh, Material material)
    {
        meshFilter.sharedMesh = mesh;
        meshRender.sharedMaterial = material;
    }

    public void setMeshTextures(Mesh mesh, Material[] materials)
    {
        if(meshFilter != null)
            meshFilter.sharedMesh = mesh;
        if(meshRender != null)
            meshRender.sharedMaterials = materials;
    }

    public MeshRenderer getMeshRenderer()
    {
        return meshRender;
    }

    public MeshFilter getMeshFilter()
    {
        return meshFilter;
    }


    #endregion

    public void clearDraw()
    {
        meshRender.enabled = false;
    }

    public void destroy()
    {
        if(mesh != null)
        {
            //PlatformTools.DestroyMonoRef(ref mesh);
            mesh = null;
        }
    }

}