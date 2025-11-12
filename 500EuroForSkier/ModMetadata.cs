using SPTarkov.Server.Core.Models.Spt.Mod;
using Range = SemanticVersioning.Range;
using Version = SemanticVersioning.Version;

namespace EuroForSkier;

public record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "ca.bushtail.euroforskier";
    public override string Name { get; init; } = "500EuroForSkier";
    public override string Author { get; init; } = "bushtail";
    public override List<string>? Contributors { get; init; }
    public override Version Version { get; init; } = new(typeof(ModMetadata).Assembly.GetName().Version?.ToString(3));
    public override Range SptVersion { get; init; } = new("~4.0.0");
    public override List<string>? Incompatibilities { get; init; }
    public override Dictionary<string, Range>? ModDependencies { get; init; }
    public override string? Url { get; init; }
    public override bool? IsBundleMod { get; init; }
    public override string License { get; init; } = "MIT";
}