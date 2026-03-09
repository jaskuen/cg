using System.Xml.Linq;

namespace SilkOpenGL.Text;

public class FontMetadata
{
    public Dictionary<char, FontChar> Chars = new();
    public int TextureWidth;
    public int TextureHeight;
    public int BaseSize;

    public FontMetadata(string xmlPath)
    {
        XDocument doc = XDocument.Load(xmlPath);
        XElement common = doc.Element("font")?.Element("common")!;
        TextureWidth = int.Parse(common.Attribute("scaleW")!.Value);
        TextureHeight = int.Parse(common.Attribute("scaleH")!.Value);
        BaseSize = int.Parse(doc.Element("font")!.Element("info")!.Attribute("size")!.Value);

        foreach (var node in doc.Element("font")!.Element("chars")!.Elements("char"))
        {
            char c = (char)int.Parse(node.Attribute("id")!.Value);
            Chars[c] = new FontChar
            {
                X = int.Parse(node.Attribute("x")!.Value),
                Y = int.Parse(node.Attribute("y")!.Value),
                Width = int.Parse(node.Attribute("width")!.Value),
                Height = int.Parse(node.Attribute("height")!.Value),
                XOffset = int.Parse(node.Attribute("xoffset")!.Value),
                YOffset = int.Parse(node.Attribute("yoffset")!.Value),
                XAdvance = int.Parse(node.Attribute("xadvance")!.Value)
            };
        }
    }
}