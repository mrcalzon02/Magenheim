using System;
using System.IO;
using System.Reflection;
using Magenheim.Core.TrueBlacksmithing;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Magenheim.Runtime.TrueBlacksmithing;

internal static class ManufacturingDataLoader
{
    internal static T Read<T>(string path) where T : class
    {
        var root = JObject.Parse(File.ReadAllText(path), new JsonLoadSettings
        {
            DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error
        });
        var settings = new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Error,
            ContractResolver = new RequiredManufacturingFieldsResolver()
        };
        settings.Converters.Add(new StringEnumConverter { AllowIntegerValues = false });
        var value = root.ToObject<T>(JsonSerializer.Create(settings))
            ?? throw new InvalidDataException("Manufacturing document is empty: " + path);
        if (value is ManufacturingCatalog catalog) catalog.Validate();
        else if (value is ManufacturingGrammar grammar) grammar.Validate();
        else throw new ArgumentException("Unsupported manufacturing document type.");
        return value;
    }

    private sealed class RequiredManufacturingFieldsResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization serialization)
        {
            var property = base.CreateProperty(member, serialization);
            if (member.DeclaringType?.Namespace == "Magenheim.Core.TrueBlacksmithing") property.Required = Required.Always;
            return property;
        }
    }
}
