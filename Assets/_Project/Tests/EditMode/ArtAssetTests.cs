using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

// Prüft die kopierten Game-Look-Assets: Stone-Sprites/Icons und Lilita One liegen unter _Project, das Logo hat Alpha
// (Original bleibt), und die USS verweist nie in einen Asset-Pack-Ordner.
[TestFixture]
public class ArtAssetTests
{
    const string Ui = "Assets/_Project/Art/UI/";

    static readonly string[] Sprites =
    {
        "Icon_ItemIcon_Target.Png", "Icon_ItemIcon_Timer.Png", "Icon_ItemIcon_Scroll.Png", "Icon_ItemIcon_Trophy.Png",
        "Icon_ItemIcon_Setting.Png", "Icon_ItemIcon_Arrow.Png", "Icon_ItemIcon_Medal_Gold.Png", "Icon_ItemIcon_Clover.Png",
        "Icon_PictoIcon_Exit.Png", "Button_Square03_White.png", "Button01_Red.png", "BasicFrame_Square01_White.Png",
    };

    [TestCaseSource(nameof(Sprites))]
    public void Sprite_IsCopiedIntoProject(string file)
    {
        Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Texture2D>(Ui + file), Ui + file);
    }

    [Test]
    public void LilitaOne_IsCopiedWithLicense()
    {
        Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Font>("Assets/_Project/Fonts/LilitaOne-Regular.ttf"));
        Assert.IsTrue(File.Exists("Assets/_Project/Fonts/LilitaOne-OFL.txt"));
    }

    [Test]
    public void Logo_HasAlphaAndOriginalStays()
    {
        var imp = AssetImporter.GetAtPath("Assets/_Project/Art/BullseyeQ-Logo-Alpha.png") as TextureImporter;
        Assert.IsNotNull(imp, "BullseyeQ-Logo-Alpha.png fehlt");
        Assert.IsTrue(imp.DoesSourceTextureHaveAlpha(), "Logo ohne Alphakanal");
        Assert.IsTrue(File.Exists("Assets/_Project/Art/BullseyeQ-Logo.png"), "Original muss bleiben");
    }

    [Test]
    public void Uss_NeverReferencesAssetPacks()
    {
        var uss = File.ReadAllText("Assets/_Project/UI/TrainingsSessionStyle.uss");
        StringAssert.DoesNotContain("Layer Lab", uss);
        StringAssert.DoesNotContain("GUIPackCartoon", uss);
    }
}
