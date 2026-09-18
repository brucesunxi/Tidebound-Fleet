using System.Linq;
using NUnit.Framework;
using Tidebound.Config;
using Tidebound.Core;
using Tidebound.Ship;
using UnityEditor.Compilation;

namespace Tidebound.Tests
{
    public sealed class AssemblyBoundaryTests
    {
        [Test]
        public void LogicAssembliesCannotReferenceUnityOrLegacyGameplay()
        {
            foreach (var assembly in new[] { typeof(ShipRuntimeData).Assembly, typeof(GameSession).Assembly })
            {
                var references = assembly.GetReferencedAssemblies().Select(x => x.Name).ToArray();
                Assert.That(references.Any(x => x.StartsWith("UnityEngine") || x.StartsWith("UnityEditor") || x.StartsWith("Assembly-CSharp")), Is.False);
            }
        }

        [Test]
        public void TideboundRuntimeDependenciesExcludeLegacyAndEditorAssemblies()
        {
            var runtimeNames = new[] { "Tidebound.Data", "Tidebound.Core", "Tidebound.Unity" };
            var assemblies = CompilationPipeline.GetAssemblies(AssembliesType.Player);
            foreach (var name in runtimeNames)
            {
                var assembly = assemblies.Single(x => x.name == name);
                // Unity 2022 injects its engine UI module into engine-enabled assemblies.
                Assert.That(assembly.assemblyReferences.All(x => runtimeNames.Contains(x.name) ||
                    (name == "Tidebound.Unity" && x.name == "UnityEngine.UI")), Is.True,
                    name + " references " + string.Join(",", assembly.assemblyReferences.Select(x => x.name)));
            }
            Assert.That(assemblies.Any(x => x.name == "Tidebound.Editor" || x.name == "Tidebound.Tests.EditMode"), Is.False);
            Assert.That(typeof(LevelConfigSO).Assembly.GetReferencedAssemblies().Any(x => x.Name.StartsWith("UnityEditor")), Is.False);
        }
    }
}
