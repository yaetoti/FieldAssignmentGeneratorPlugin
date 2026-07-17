using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Psi;

namespace ReSharperPlugin.FieldAssignmentGenerator;

[ZoneDefinition]
public interface IFieldAssignmentGeneratorZone : ILanguageCppZone;
