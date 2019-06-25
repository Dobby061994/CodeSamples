using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshCombiner : MonoBehaviour
{
    // When placed on a GameObject, this script takes the meshes of all that object's children and will combine them
    // into a single mesh.
    // The script also tries to account for materials. When using multiple textures/materials, the parent  object's mesh
    // renderer must have those materials listed in the order the loop would encounter them, otherwise the resulting
    // mesh's textures will be incorrectly applied.

    // While this brings some performance benefits since fewer total meshes results in fewer draw calls, combining the meshes
    // was necessary for this project in order for the more advanced room prefabs to function with my existing code.

    // Also, there does appear to be a limit to how many meshes can be combined at once. If the final combined mesh exceeds
    // roughly 60,000 vertices its geometry may be skewed and unpredictable, so the number of objects to be combined should
    // ideally be kept as low as possible.

    public void AdvancedMerge()
    {
        // Save the old rotation and position of the mesh:
        Quaternion oldRot = transform.rotation;
        Vector3 oldPos = transform.position;

        // Set the rotation and position of the mesh to zero:
        transform.rotation = Quaternion.identity;
        transform.position = Vector3.zero;

        MeshFilter[] filters = GetComponentsInChildren<MeshFilter>(false);

        List<Material> materials = new List<Material>();
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>(false);

        // First, compile a list of every material attached to the transform:
        foreach (MeshRenderer renderer in renderers)
        {
            if (renderer.transform == transform)
            {
                continue;
            }
            Material[] localMats = renderer.sharedMaterials;
            foreach(Material localMat in localMats)
            {
                if (!materials.Contains(localMat))
                {
                    materials.Add(localMat);
                }
            }
        }

        //Create a new list which will hold our submeshes (1 per material):
        List<Mesh> submeshes = new List<Mesh>();
        // For each material:
        foreach (Material mat in materials)
        {
            List<CombineInstance> combiners = new List<CombineInstance>();
            // And for each MeshFilter:
            foreach (MeshFilter filter in filters)
            {
                MeshRenderer renderer = filter.GetComponent<MeshRenderer>();
                // Ensure the object has a MeshRenderer:
                if (renderer == null)
                {
                    Debug.LogError(filter.name + " has no MeshRenderer.");
                    continue;
                }

                // For every material, we create a new submesh specifically for that material:
                Material[] localMaterials = renderer.sharedMaterials;
                for (int materialIndex = 0; materialIndex < localMaterials.Length; materialIndex++)
                {
                    if (localMaterials[materialIndex] != mat)
                    {
                        continue;
                    }
                    CombineInstance ci = new CombineInstance();
                    ci.mesh = filter.sharedMesh;
                    ci.subMeshIndex = materialIndex;
                    ci.transform = filter.transform.localToWorldMatrix;
                    combiners.Add(ci);
                }
            }

            // Compile a List of every submesh we just created:
            Mesh mesh = new Mesh();
            mesh.CombineMeshes(combiners.ToArray(), true);
            submeshes.Add(mesh);
        }

        // Now we combine each separate submesh into a final, single mesh:
        List<CombineInstance> finalCombiners = new List<CombineInstance>();
        foreach (Mesh mesh in submeshes)
        {
            CombineInstance ci = new CombineInstance();
            ci.mesh = mesh;
            ci.subMeshIndex = 0;
            ci.transform = Matrix4x4.identity;
            finalCombiners.Add(ci);
        }
        Mesh finalMesh = new Mesh();
        finalMesh.CombineMeshes(finalCombiners.ToArray(), false);

        GetComponent<MeshFilter>().sharedMesh = finalMesh;
        GetComponent<MeshCollider>().sharedMesh = finalMesh;

        // Reset the final mesh's rotation and position:
        transform.rotation = oldRot;
        transform.position = oldPos;

        // Deactivate all the old separate meshes:
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(false);
        }

        // Now we are left with a single combined mesh with all its submesh's materials in the correct places.
    }
}