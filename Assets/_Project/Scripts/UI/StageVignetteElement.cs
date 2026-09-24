using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Full-bleed radial vignette (UiTheme.Stage centre → UiTheme.StageEdge rim) drawn as a vertex-coloured
/// triangle fan — USS has no gradients. Usable from UXML as &lt;StageVignetteElement/&gt;.
/// </summary>
[UxmlElement]
public partial class StageVignetteElement : VisualElement
{
    const int Segments = 64;

    public StageVignetteElement()
    {
        pickingMode = PickingMode.Ignore;
        style.position = Position.Absolute;
        style.left = 0; style.top = 0; style.right = 0; style.bottom = 0;
        style.overflow = Overflow.Hidden;
        generateVisualContent += Draw;
    }

    void Draw(MeshGenerationContext ctx)
    {
        float w = resolvedStyle.width, h = resolvedStyle.height;
        if (w <= 0 || h <= 0) return;

        var center = new Vector2(w * 0.5f, h * 0.5f);
        float radius = Mathf.Sqrt(w * w + h * h) * 0.5f * 1.05f; // covers the corners

        var mesh = ctx.Allocate(Segments + 2, Segments * 3);
        var verts = new Vertex[Segments + 2];
        verts[0] = new Vertex { position = new Vector3(center.x, center.y, Vertex.nearZ), tint = UiTheme.Stage };
        for (int i = 0; i <= Segments; i++)
        {
            float a = i / (float)Segments * Mathf.PI * 2f;
            verts[i + 1] = new Vertex
            {
                position = new Vector3(center.x + Mathf.Cos(a) * radius, center.y + Mathf.Sin(a) * radius, Vertex.nearZ),
                tint     = UiTheme.StageEdge
            };
        }
        mesh.SetAllVertices(verts);

        var idx = new ushort[Segments * 3];
        for (int i = 0; i < Segments; i++)
        {
            idx[i * 3]     = 0;
            idx[i * 3 + 1] = (ushort)(i + 1);
            idx[i * 3 + 2] = (ushort)(i + 2);
        }
        mesh.SetAllIndices(idx);
    }
}
