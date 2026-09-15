using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Magenheim.Core.Underworld;

/// <summary>
/// Canonical persistence codec for per-player Underworld layer/transition state.
/// The format is intentionally independent of Unity, Valheim and runtime serializer versions.
/// Text values are base64-encoded UTF-8; numeric values use invariant round-trip formatting.
/// Decode always revalidates the reconstructed state against the paired world identity.
/// </summary>
public static class UnderworldTransitionStateCodec
{
    public const int CurrentFormatVersion = 1;
    private const string Header = "magenheim-underworld-transition-state";

    public static string Encode(UnderworldPlayerLayerState state, UnderworldWorldIdentity identity)
    {
        UnderworldTransitionRules.ValidatePersistedState(state, identity);

        var lines = new List<string>
        {
            Header,
            "format=" + CurrentFormatVersion.ToString(CultureInfo.InvariantCulture),
            "schema=" + state.SchemaVersion.ToString(CultureInfo.InvariantCulture),
            "player=" + Text(state.PlayerId),
            "layer=" + ((int)state.CurrentLayer).ToString(CultureInfo.InvariantCulture),
            "world=" + Text(state.CurrentWorldId),
            "surface=" + Anchor(state.SurfaceReturnAnchor),
            "active=" + (state.ActiveTransition is null ? "0" : "1"),
        };

        if (state.ActiveTransition is not null)
        {
            var active = state.ActiveTransition;
            lines.Add("operation=" + Text(active.OperationId));
            lines.Add("direction=" + ((int)active.Direction).ToString(CultureInfo.InvariantCulture));
            lines.Add("sourceLayer=" + ((int)active.SourceLayer).ToString(CultureInfo.InvariantCulture));
            lines.Add("targetLayer=" + ((int)active.TargetLayer).ToString(CultureInfo.InvariantCulture));
            lines.Add("source=" + Anchor(active.SourceAnchor));
            lines.Add("target=" + Anchor(active.TargetAnchor));
            lines.Add("authority=" + active.AuthorityFingerprint);
            lines.Add("phase=" + ((int)active.Phase).ToString(CultureInfo.InvariantCulture));
            lines.Add("diagnostic=" + Text(active.Diagnostic ?? string.Empty));
        }

        return string.Join("\n", lines) + "\n";
    }

    public static UnderworldPlayerLayerState Decode(string payload, UnderworldWorldIdentity identity)
    {
        if (identity is null) throw new ArgumentNullException(nameof(identity));
        if (string.IsNullOrWhiteSpace(payload))
            throw new InvalidOperationException("Underworld transition-state payload is empty.");

        var rawLines = payload.Replace("\r\n", "\n").Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (rawLines.Length < 8 || !string.Equals(rawLines[0], Header, StringComparison.Ordinal))
            throw new InvalidOperationException("Underworld transition-state header is invalid.");

        var fields = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var index = 1; index < rawLines.Length; index++)
        {
            var separator = rawLines[index].IndexOf('=');
            if (separator <= 0)
                throw new InvalidOperationException("Underworld transition-state contains a malformed field.");
            var key = rawLines[index].Substring(0, separator);
            var value = rawLines[index].Substring(separator + 1);
            if (fields.ContainsKey(key))
                throw new InvalidOperationException($"Underworld transition-state contains duplicate field '{key}'.");
            fields.Add(key, value);
        }

        var format = Integer(fields, "format");
        if (format != CurrentFormatVersion)
            throw new InvalidOperationException($"Unsupported Underworld transition-state format {format}.");

        var hasActive = Integer(fields, "active");
        if (hasActive != 0 && hasActive != 1)
            throw new InvalidOperationException("Underworld transition-state active marker must be 0 or 1.");

        var expectedCount = hasActive == 1 ? 16 : 7;
        if (fields.Count != expectedCount)
            throw new InvalidOperationException("Underworld transition-state contains missing or unknown fields.");

        UnderworldTransitionIntent? active = null;
        if (hasActive == 1)
        {
            active = new UnderworldTransitionIntent(
                DecodeText(Field(fields, "operation")),
                EnumValue<UnderworldTransitionDirection>(fields, "direction"),
                EnumValue<UnderworldLayer>(fields, "sourceLayer"),
                EnumValue<UnderworldLayer>(fields, "targetLayer"),
                DecodeAnchor(Field(fields, "source"), "source"),
                DecodeAnchor(Field(fields, "target"), "target"),
                Field(fields, "authority"),
                EnumValue<UnderworldTransitionPhase>(fields, "phase"),
                DecodeText(Field(fields, "diagnostic")));
        }

        var state = new UnderworldPlayerLayerState(
            Integer(fields, "schema"),
            DecodeText(Field(fields, "player")),
            EnumValue<UnderworldLayer>(fields, "layer"),
            DecodeText(Field(fields, "world")),
            DecodeOptionalAnchor(Field(fields, "surface"), "surface"),
            active);

        UnderworldTransitionRules.ValidatePersistedState(state, identity);
        return state;
    }

    private static string Anchor(UnderworldAnchor? anchor)
    {
        if (anchor is null) return "-";
        return string.Join(",", new[]
        {
            Text(anchor.WorldId),
            anchor.X.ToString("R", CultureInfo.InvariantCulture),
            anchor.Y.ToString("R", CultureInfo.InvariantCulture),
            anchor.Z.ToString("R", CultureInfo.InvariantCulture),
            anchor.HeadingDegrees.ToString("R", CultureInfo.InvariantCulture),
        });
    }

    private static UnderworldAnchor? DecodeOptionalAnchor(string value, string field) =>
        value == "-" ? null : DecodeAnchor(value, field);

    private static UnderworldAnchor DecodeAnchor(string value, string field)
    {
        var parts = value.Split(',');
        if (parts.Length != 5)
            throw new InvalidOperationException($"Underworld transition-state anchor '{field}' is malformed.");
        return new UnderworldAnchor(
            DecodeText(parts[0]),
            Double(parts[1], field + ".x"),
            Double(parts[2], field + ".y"),
            Double(parts[3], field + ".z"),
            Float(parts[4], field + ".heading"));
    }

    private static string Text(string value) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(value ?? string.Empty));

    private static string DecodeText(string value)
    {
        try
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(value));
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException("Underworld transition-state contains invalid base64 text.", exception);
        }
    }

    private static string Field(IReadOnlyDictionary<string, string> fields, string key)
    {
        string value;
        if (!fields.TryGetValue(key, out value))
            throw new InvalidOperationException($"Underworld transition-state is missing field '{key}'.");
        return value;
    }

    private static int Integer(IReadOnlyDictionary<string, string> fields, string key)
    {
        var value = Field(fields, key);
        int result;
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
            throw new InvalidOperationException($"Underworld transition-state field '{key}' is not an integer.");
        return result;
    }

    private static T EnumValue<T>(IReadOnlyDictionary<string, string> fields, string key) where T : struct
    {
        var numeric = Integer(fields, key);
        var value = (T)Enum.ToObject(typeof(T), numeric);
        if (!Enum.IsDefined(typeof(T), value))
            throw new InvalidOperationException($"Underworld transition-state field '{key}' has an unknown enum value.");
        return value;
    }

    private static double Double(string value, string field)
    {
        double result;
        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result) ||
            double.IsNaN(result) || double.IsInfinity(result))
            throw new InvalidOperationException($"Underworld transition-state field '{field}' is not a finite number.");
        return result;
    }

    private static float Float(string value, string field)
    {
        float result;
        if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result) ||
            float.IsNaN(result) || float.IsInfinity(result))
            throw new InvalidOperationException($"Underworld transition-state field '{field}' is not a finite number.");
        return result;
    }
}
