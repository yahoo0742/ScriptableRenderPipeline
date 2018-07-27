using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

[ExecuteInEditMode]
public class CustomPlanarReflection : MonoBehaviour
{
    private bool oldCulling;

    public void OnPreRender()
    {
        oldCulling = GL.invertCulling;
        GL.invertCulling = true;
    }

    public void OnPostRender()
    {
        GL.invertCulling = oldCulling;
    }
}