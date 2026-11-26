using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// The dedicated server build (Server subtarget) strips every shader, so Shader.Find returns null
/// there and new Material(null) throws. A throw inside OnNetworkSpawn aborts the rest of the
/// server-side spawn of a joining hero. Runtime code must check the shader before new Material.
/// </summary>
public class ServerShaderGuardTests
{
    static readonly Regex Unguarded = new Regex(@"new\s+Material\s*\(\s*Shader\.Find\s*\(");

    [Test]
    public void RuntimeScripts_NeverPassShaderFindStraightIntoNewMaterial()
    {
        var root = Path.Combine(Application.dataPath, "Scripts");
        var offenders = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Replace('\\', '/').Contains("/Editor/"))
            .SelectMany(f => File.ReadAllLines(f)
                .Select((line, i) => (f, i, line))
                .Where(t => Unguarded.IsMatch(t.line)))
            .Select(t => $"{t.f.Substring(root.Length + 1)}:{t.i + 1}")
            .ToList();
        Assert.IsEmpty(offenders,
            "Shader.Find is null in the dedicated server build; null-check it before new Material: "
            + string.Join(", ", offenders));
    }
}
