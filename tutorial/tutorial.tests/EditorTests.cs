using System.Reflection;
using Godot;

namespace tutorial.Tests;

/// <summary>Tool scripts also run in the Godot editor, where only other tool scripts load.</summary>
public sealed class EditorTests
{
    [Fact]
    public void ToolScriptsExportOnlyTypesTheEditorCanLoad()
    {
        var assembly = typeof(Page).Assembly;
        const BindingFlags members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var problems =
            from type in assembly.GetTypes()
            where type.IsDefined(typeof(ToolAttribute))
            from member in type.GetProperties(members).Cast<MemberInfo>().Concat(type.GetFields(members))
            where member.IsDefined(typeof(ExportAttribute))
            let exported = member is PropertyInfo property ? property.PropertyType : ((FieldInfo)member).FieldType
            // In the editor, a node whose script isn't a tool script is a plain engine object: it can't be cast.
            where exported.Assembly == assembly && exported.IsSubclassOf(typeof(GodotObject))
                && !exported.IsDefined(typeof(ToolAttribute))
            select $"{type.Name}.{member.Name} is a {exported.Name}";
        Assert.Empty(problems);
    }
}
