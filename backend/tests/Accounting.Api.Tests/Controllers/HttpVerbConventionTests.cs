using System.Reflection;
using Accounting.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Accounting.Api.Tests.Controllers;

/// <summary>
/// Locks in the project-owner mandate (phase 8 HTTP contract change, 2026-08-19): <c>PUT</c> and
/// <c>DELETE</c> are not usable in this environment, so this API only ever exposes <c>GET</c> and
/// <c>POST</c>. This is not an internal architecture preference — see the class-level XML doc on
/// <see cref="AccountCodesController"/>/<see cref="VoucherHeadsController"/> for the full
/// rationale. This test exists purely to prevent an accidental regression back to
/// <c>[HttpPut]</c>/<c>[HttpDelete]</c> on any future action, on any controller, anywhere in the
/// <c>Accounting.Api</c> assembly.
/// </summary>
public sealed class HttpVerbConventionTests
{
    private static IEnumerable<MethodInfo> AllControllerActions()
    {
        var controllerBaseType = typeof(ControllerBase);
        var controllerTypes = typeof(AccountCodesController).Assembly
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && controllerBaseType.IsAssignableFrom(t));

        foreach (var controllerType in controllerTypes)
        {
            foreach (var method in controllerType.GetMethods(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                yield return method;
            }
        }
    }

    /// <summary>
    /// QA regression lock (phase-10 code review finding): <see cref="AllControllerActions"/> is
    /// the whole basis of <see cref="NoActionInTheApiAssembly_UsesHttpPutOrHttpDelete"/> below —
    /// if it ever returned an empty set (e.g. because the assembly reference silently stopped
    /// resolving <see cref="VoucherDetailsController"/>, or a future refactor moved controllers to
    /// a different assembly), that test would still pass vacuously and the guard would be
    /// worthless without anyone noticing. This test asserts the scan is non-empty AND specifically
    /// contains <see cref="VoucherDetailsController"/>'s actions, proving the newest controller is
    /// actually covered.
    /// </summary>
    [Fact]
    public void AllControllerActions_IsNonEmpty_AndIncludesVoucherDetailsController()
    {
        var actions = AllControllerActions().ToList();

        Assert.NotEmpty(actions);
        Assert.Contains(actions, m => m.DeclaringType == typeof(VoucherDetailsController));
        Assert.Contains(actions, m => m.DeclaringType == typeof(VoucherDetailsController) && m.Name == nameof(VoucherDetailsController.Create));
        Assert.Contains(actions, m => m.DeclaringType == typeof(VoucherDetailsController) && m.Name == nameof(VoucherDetailsController.GetList));
        Assert.Contains(actions, m => m.DeclaringType == typeof(VoucherDetailsController) && m.Name == nameof(VoucherDetailsController.GetById));
        Assert.Contains(actions, m => m.DeclaringType == typeof(VoucherDetailsController) && m.Name == nameof(VoucherDetailsController.Update));
        Assert.Contains(actions, m => m.DeclaringType == typeof(VoucherDetailsController) && m.Name == nameof(VoucherDetailsController.Delete));
    }

    /// <summary>
    /// No action anywhere in the API may be attributed with <c>[HttpPut]</c> or
    /// <c>[HttpDelete]</c> — the project owner's network infrastructure does not allow either
    /// verb. This is a whole-assembly reflection scan, not a per-controller check, so it also
    /// covers every controller added after this test was written.
    /// </summary>
    [Fact]
    public void NoActionInTheApiAssembly_UsesHttpPutOrHttpDelete()
    {
        var offendingActions = AllControllerActions()
            .Where(m => m.GetCustomAttributes<HttpMethodAttribute>()
                .Any(a => a.HttpMethods.Contains("PUT") || a.HttpMethods.Contains("DELETE")))
            .Select(m => $"{m.DeclaringType!.Name}.{m.Name}")
            .ToList();

        Assert.True(
            offendingActions.Count == 0,
            $"Found action(s) still using HttpPut/HttpDelete: {string.Join(", ", offendingActions)}. "
                + "PUT/DELETE are not permitted by explicit project-owner mandate — use "
                + "POST {id}/update and POST {id}/delete instead.");
    }

    [Theory]
    [InlineData(nameof(AccountCodesController.Update))]
    [InlineData(nameof(AccountCodesController.Delete))]
    public void AccountCodesController_UpdateAndDelete_AreHttpPost(string methodName)
    {
        AssertActionIsHttpPost(typeof(AccountCodesController), methodName);
    }

    [Theory]
    [InlineData(nameof(VoucherHeadsController.Update))]
    [InlineData(nameof(VoucherHeadsController.Delete))]
    public void VoucherHeadsController_UpdateAndDelete_AreHttpPost(string methodName)
    {
        AssertActionIsHttpPost(typeof(VoucherHeadsController), methodName);
    }

    [Theory]
    [InlineData(nameof(VoucherDetailsController.Update))]
    [InlineData(nameof(VoucherDetailsController.Delete))]
    public void VoucherDetailsController_UpdateAndDelete_AreHttpPost(string methodName)
    {
        AssertActionIsHttpPost(typeof(VoucherDetailsController), methodName);
    }

    private static void AssertActionIsHttpPost(Type controllerType, string methodName)
    {
        var method = controllerType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(method);

        var httpMethodAttribute = method!.GetCustomAttribute<HttpMethodAttribute>();
        Assert.NotNull(httpMethodAttribute);
        Assert.Contains("POST", httpMethodAttribute!.HttpMethods);
    }
}
