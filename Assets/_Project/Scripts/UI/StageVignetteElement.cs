using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Felt-green stage lit from above ("lamp over the board"): a bright centre at the top middle, felt green
/// around it, darker rims — drawn as vertex-coloured rings because USS has no gradients.
/// Usable from UXML as &lt;StageVignetteElement/&gt;.
/// </summary>
[UxmlElement]
public partial class StageVignetteElement : VisualElement
{
    const int Segments = 64;

    /// <summary>How far the lit centre lifts from the felt toward the chalk colour (tunable 0–0.35).</summary>
    public const float TopLight = 0.18f;
    const float LightY   = 0.12f; // lamp height as a fraction of the stage
    const float FeltRing = 0.55f; // radius fraction at which the light has faded to plain felt

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

        var center = new Vector2(w * 0.5f, h * LightY);
        float radius = new Vector2(w * 0.5f, h * (1f - LightY)).magnitude * 1.05f; // reaches the bottom corners
        var lit = Color.Lerp(UiTheme.Stage, UiTheme.Text, TopLight);

        int ring = Segments + 1;
        var mesh  = ctx.Allocate(1 + ring * 2, Segments * 9);
        var verts = new Vertex[1 + ring * 2];
        verts[0] = V(center, lit);
        for (int i = 0; i <= Segments; i++)
        {
            float a = i / (float)Segments * Mathf.PI * 2f;
            var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            verts[1 + i]        = V(center + dir * radius * FeltRing, UiTheme.Stage);
            verts[1 + ring + i] = V(center + dir * radius,            UiTheme.StageEdge);
        }
        mesh.SetAllVertices(verts);

        var idx = new ushort[Segments * 9];
        for (int i = 0, k = 0; i < Segments; i++)
        {
            ushort f0 = (ushort)(1 + i),        f1 = (ushort)(2 + i);        // felt ring
            ushort e0 = (ushort)(1 + ring + i), e1 = (ushort)(2 + ring + i); // rim
            idx[k++] = 0;  idx[k++] = f0; idx[k++] = f1;                     // light → felt
            idx[k++] = f0; idx[k++] = e0; idx[k++] = e1;                     // felt → rim (quad, 2 tris)
            idx[k++] = f0; idx[k++] = e1; idx[k++] = f1;
        }
        mesh.SetAllIndices(idx);
    }

    static Vertex V(Vector2 p, Color c) => new Vertex { position = new Vector3(p.x, p.y, Vertex.nearZ), tint = c };
}
