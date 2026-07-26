using System.Reflection;
using ReBuildTool.ToolChain;

namespace ReBuildTool.Test;

/// <summary>
/// A module rule declares from <c>Setup(ICppBuildContext)</c>, never from its
/// constructor: the rule's lists are rebuilt on every setup pass (Cleanup empties
/// them so a re-setup under a different build context cannot double-add), so
/// constructor-time entries would silently vanish on the second pass. The framework
/// rejects such a rule up front - these tests cover that guard.
/// </summary>
[TestFixture]
public class TestModuleRuleLifecycle
{
    private class EmptyModule : CppModuleRule
    {
    }

    private class DeclaringInConstructorModule : CppModuleRule
    {
        public DeclaringInConstructorModule()
        {
            Dependencies.Add("SomeOtherModule");
        }
    }

    [Test]
    public void DeclaringInConstructorIsRejected()
    {
        var rule = new DeclaringInConstructorModule();

        var exception = Assert.Throws<Exception>(() => rule.ThrowIfDeclaredInConstructor());

        // The message has to name what to move and where to move it.
        Assert.That(exception!.Message, Does.Contain(nameof(CppModuleRule.Dependencies)));
        Assert.That(exception.Message, Does.Contain("Setup"));
    }

    [Test]
    public void DeclaringNothingInConstructorPasses()
    {
        Assert.DoesNotThrow(() => new EmptyModule().ThrowIfDeclaredInConstructor());
    }

    /// <summary>
    /// Every declarative list has to be covered by the guard. Without this, adding a
    /// new <c>List&lt;string&gt;</c> to <see cref="CppModuleRule"/> and forgetting to
    /// register it would quietly bring back the old failure mode: entries added in a
    /// constructor, dropped on the next setup pass, no error anywhere.
    /// </summary>
    [Test]
    public void EveryDeclarativeListIsCoveredByTheGuard()
    {
        var listProperties = typeof(CppModuleRule)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.PropertyType == typeof(List<string>))
            .ToList();
        Assert.That(listProperties, Is.Not.Empty);

        foreach (var property in listProperties)
        {
            var rule = new EmptyModule();
            ((List<string>)property.GetValue(rule)!).Add("declared-in-constructor");

            var exception = Assert.Throws<Exception>(
                () => rule.ThrowIfDeclaredInConstructor(),
                $"{property.Name} is not covered by the constructor-declaration guard");
            Assert.That(exception!.Message, Does.Contain(property.Name));
        }
    }
}
