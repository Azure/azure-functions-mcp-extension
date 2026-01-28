// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;
using Moq;

namespace Worker.Extensions.Mcp.Tests.Configuration;

public class FunctionMethodResolverTests
{
    private const string FunctionsApplicationDirectoryKey = "FUNCTIONS_APPLICATION_DIRECTORY";
    private const string FunctionsWorkerDirectoryKey = "FUNCTIONS_WORKER_DIRECTORY";

    [Fact]
    public void TryGetScriptRoot_ApplicationDirectorySet_ReturnsTrue()
    {
        var testPath = "/test/path";
        Environment.SetEnvironmentVariable(FunctionsApplicationDirectoryKey, testPath);
        Environment.SetEnvironmentVariable(FunctionsWorkerDirectoryKey, null);

        try
        {
            var result = FunctionMethodResolver.TryGetScriptRoot(out var scriptRoot);

            Assert.True(result);
            Assert.Equal(testPath, scriptRoot);
        }
        finally
        {
            Environment.SetEnvironmentVariable(FunctionsApplicationDirectoryKey, null);
        }
    }

    [Fact]
    public void TryGetScriptRoot_WorkerDirectorySet_ReturnsTrue()
    {
        var testPath = "/worker/path";
        Environment.SetEnvironmentVariable(FunctionsApplicationDirectoryKey, null);
        Environment.SetEnvironmentVariable(FunctionsWorkerDirectoryKey, testPath);

        try
        {
            var result = FunctionMethodResolver.TryGetScriptRoot(out var scriptRoot);

            Assert.True(result);
            Assert.Equal(testPath, scriptRoot);
        }
        finally
        {
            Environment.SetEnvironmentVariable(FunctionsWorkerDirectoryKey, null);
        }
    }

    [Fact]
    public void TryGetScriptRoot_ApplicationDirectoryTakesPrecedence()
    {
        var appPath = "/app/path";
        var workerPath = "/worker/path";
        Environment.SetEnvironmentVariable(FunctionsApplicationDirectoryKey, appPath);
        Environment.SetEnvironmentVariable(FunctionsWorkerDirectoryKey, workerPath);

        try
        {
            var result = FunctionMethodResolver.TryGetScriptRoot(out var scriptRoot);

            Assert.True(result);
            Assert.Equal(appPath, scriptRoot);
        }
        finally
        {
            Environment.SetEnvironmentVariable(FunctionsApplicationDirectoryKey, null);
            Environment.SetEnvironmentVariable(FunctionsWorkerDirectoryKey, null);
        }
    }

    [Fact]
    public void TryGetScriptRoot_NothingSet_ReturnsFalse()
    {
        Environment.SetEnvironmentVariable(FunctionsApplicationDirectoryKey, null);
        Environment.SetEnvironmentVariable(FunctionsWorkerDirectoryKey, null);

        var result = FunctionMethodResolver.TryGetScriptRoot(out var scriptRoot);

        Assert.False(result);
        Assert.Null(scriptRoot);
    }

    [Fact]
    public void TryGetScriptRoot_EmptyString_ReturnsFalse()
    {
        Environment.SetEnvironmentVariable(FunctionsApplicationDirectoryKey, "");
        Environment.SetEnvironmentVariable(FunctionsWorkerDirectoryKey, "");

        try
        {
            var result = FunctionMethodResolver.TryGetScriptRoot(out var scriptRoot);

            Assert.False(result);
        }
        finally
        {
            Environment.SetEnvironmentVariable(FunctionsApplicationDirectoryKey, null);
            Environment.SetEnvironmentVariable(FunctionsWorkerDirectoryKey, null);
        }
    }

    [Fact]
    public void TryGetScriptRoot_WhitespaceOnly_ReturnsFalse()
    {
        Environment.SetEnvironmentVariable(FunctionsApplicationDirectoryKey, "   ");
        Environment.SetEnvironmentVariable(FunctionsWorkerDirectoryKey, null);

        try
        {
            var result = FunctionMethodResolver.TryGetScriptRoot(out var scriptRoot);

            Assert.False(result);
        }
        finally
        {
            Environment.SetEnvironmentVariable(FunctionsApplicationDirectoryKey, null);
        }
    }

    [Fact]
    public void EnsureScriptRoot_NotSet_ThrowsInvalidOperationException()
    {
        Environment.SetEnvironmentVariable(FunctionsApplicationDirectoryKey, null);
        Environment.SetEnvironmentVariable(FunctionsWorkerDirectoryKey, null);

        var ex = Assert.Throws<InvalidOperationException>(() => FunctionMethodResolver.EnsureScriptRoot());
        Assert.Contains(FunctionsApplicationDirectoryKey, ex.Message);
    }

    [Fact]
    public void EnsureScriptRoot_Set_DoesNotThrow()
    {
        Environment.SetEnvironmentVariable(FunctionsApplicationDirectoryKey, "/some/path");

        try
        {
            var exception = Record.Exception(() => FunctionMethodResolver.EnsureScriptRoot());
            Assert.Null(exception);
        }
        finally
        {
            Environment.SetEnvironmentVariable(FunctionsApplicationDirectoryKey, null);
        }
    }

    [Fact]
    public void TryResolveMethod_NullEntryPoint_ReturnsFalse()
    {
        var fn = new Mock<IFunctionMetadata>();
        fn.SetupGet(f => f.EntryPoint).Returns((string?)null);

        var result = FunctionMethodResolver.TryResolveMethod(fn.Object, out var method);

        Assert.False(result);
        Assert.Null(method);
    }

    [Fact]
    public void TryResolveMethod_EmptyEntryPoint_ReturnsFalse()
    {
        var fn = new Mock<IFunctionMetadata>();
        fn.SetupGet(f => f.EntryPoint).Returns(string.Empty);

        var result = FunctionMethodResolver.TryResolveMethod(fn.Object, out var method);

        Assert.False(result);
        Assert.Null(method);
    }

    [Fact]
    public void TryResolveMethod_InvalidEntryPointFormat_ReturnsFalse()
    {
        var fn = new Mock<IFunctionMetadata>();
        fn.SetupGet(f => f.EntryPoint).Returns("InvalidEntryPoint");

        var result = FunctionMethodResolver.TryResolveMethod(fn.Object, out var method);

        Assert.False(result);
        Assert.Null(method);
    }

    [Fact]
    public void TryResolveMethod_NoScriptRoot_ReturnsFalse()
    {
        Environment.SetEnvironmentVariable(FunctionsApplicationDirectoryKey, null);
        Environment.SetEnvironmentVariable(FunctionsWorkerDirectoryKey, null);

        var fn = new Mock<IFunctionMetadata>();
        fn.SetupGet(f => f.EntryPoint).Returns("Namespace.Type.Method");

        var result = FunctionMethodResolver.TryResolveMethod(fn.Object, out var method);

        Assert.False(result);
        Assert.Null(method);
    }

    [Fact]
    public void TryResolveMethod_ValidEntryPoint_ReturnsMethod()
    {
        var type = typeof(TestFunctions);
        var entryPoint = $"{type.FullName}.{nameof(TestFunctions.SampleMethod)}";
        var scriptFile = Path.GetFileName(type.Assembly.Location);
        var outputDir = Path.GetDirectoryName(type.Assembly.Location)!;

        Environment.SetEnvironmentVariable(FunctionsApplicationDirectoryKey, outputDir);

        try
        {
            var fn = new Mock<IFunctionMetadata>();
            fn.SetupGet(f => f.EntryPoint).Returns(entryPoint);
            fn.SetupGet(f => f.ScriptFile).Returns(scriptFile);

            var result = FunctionMethodResolver.TryResolveMethod(fn.Object, out var method);

            Assert.True(result);
            Assert.NotNull(method);
            Assert.Equal(nameof(TestFunctions.SampleMethod), method.Name);
        }
        finally
        {
            Environment.SetEnvironmentVariable(FunctionsApplicationDirectoryKey, null);
        }
    }

    [Fact]
    public void TryResolveMethod_MethodNotFound_ReturnsFalse()
    {
        var type = typeof(TestFunctions);
        var entryPoint = $"{type.FullName}.NonExistentMethod";
        var scriptFile = Path.GetFileName(type.Assembly.Location);
        var outputDir = Path.GetDirectoryName(type.Assembly.Location)!;

        Environment.SetEnvironmentVariable(FunctionsApplicationDirectoryKey, outputDir);

        try
        {
            var fn = new Mock<IFunctionMetadata>();
            fn.SetupGet(f => f.EntryPoint).Returns(entryPoint);
            fn.SetupGet(f => f.ScriptFile).Returns(scriptFile);

            var result = FunctionMethodResolver.TryResolveMethod(fn.Object, out var method);

            Assert.False(result);
            Assert.Null(method);
        }
        finally
        {
            Environment.SetEnvironmentVariable(FunctionsApplicationDirectoryKey, null);
        }
    }

    [Fact]
    public void TryResolveMethod_TypeNotFound_ReturnsFalse()
    {
        var type = typeof(TestFunctions);
        var entryPoint = "NonExistent.Namespace.Type.Method";
        var scriptFile = Path.GetFileName(type.Assembly.Location);
        var outputDir = Path.GetDirectoryName(type.Assembly.Location)!;

        Environment.SetEnvironmentVariable(FunctionsApplicationDirectoryKey, outputDir);

        try
        {
            var fn = new Mock<IFunctionMetadata>();
            fn.SetupGet(f => f.EntryPoint).Returns(entryPoint);
            fn.SetupGet(f => f.ScriptFile).Returns(scriptFile);

            var result = FunctionMethodResolver.TryResolveMethod(fn.Object, out var method);

            Assert.False(result);
            Assert.Null(method);
        }
        finally
        {
            Environment.SetEnvironmentVariable(FunctionsApplicationDirectoryKey, null);
        }
    }

    internal class TestFunctions
    {
        public void SampleMethod() { }
    }
}
