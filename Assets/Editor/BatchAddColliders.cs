using UnityEngine;
using UnityEditor;

public class BatchAddColliders : EditorWindow
{
    [MenuItem("Tools/Placement System/Batch Add MeshColliders to Selection")]
    private static void AddCollidersToSelection()
    {
        // Get the currently selected GameObject in the Hierarchy
        GameObject selectedRoot = Selection.activeGameObject;
        
        if (selectedRoot == null)
        {
            EditorUtility.DisplayDialog(
                "Warning", 
                "Please select the imported USD root object in the Hierarchy first.", 
                "OK"
            );
            return;
        }

        // Find all MeshFilter components in this object and its children (including inactive ones)
        MeshFilter[] meshFilters = selectedRoot.GetComponentsInChildren<MeshFilter>(true);
        int addedCount = 0;

        // Register Undo action so you can press Ctrl+Z to revert if needed
        Undo.RegisterFullObjectHierarchyUndo(selectedRoot, "Batch Add MeshColliders");

        foreach (MeshFilter mf in meshFilters)
        {
            // Verify that the mesh exists and no Collider is currently attached
            if (mf.sharedMesh != null && mf.GetComponent<Collider>() == null)
            {
                MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
                addedCount++;
            }
        }

        // Show summary result
        EditorUtility.DisplayDialog(
            "Success", 
            $"Successfully added {addedCount} MeshCollider components.", 
            "OK"
        );
    }
}