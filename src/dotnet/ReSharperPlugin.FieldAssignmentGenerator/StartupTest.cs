using JetBrains.Application.Parts;
using JetBrains.Application.Threading;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.Util;


namespace ReSharperPlugin.FieldAssignmentGenerator;

[SolutionComponent(Instantiation.ContainerAsyncPrimaryThread)]
public class MyTestFrameworkBackendExt {
  public MyTestFrameworkBackendExt(Lifetime lifetime, ILogger logger, ISolution solution, IShellLocks locks) {
    /* TODO MyPluginModel model. cannot resolve Mix function in the model. rdGen 2026.2 is not out yet */
  }
}