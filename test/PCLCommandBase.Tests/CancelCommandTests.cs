// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using PCLCommandBase;
using Xunit;
using Xunit.Abstractions;

public class CancelCommandTests : TestBase
{
	public CancelCommandTests(ITestOutputHelper logger)
		: base(logger)
	{
	}

	[Fact]
	public void Ctor()
	{
		Assert.Throws<ArgumentNullException>(() => new CancelCommand(null!));
	}

	[Fact]
	public async Task CanExecute()
	{
		var waiter = new AsyncManualResetEvent();
		var primaryCommand = new MyCommand { ExecutionResult = waiter.WaitAsync() };
		var cancelCommand = new CancelCommand(primaryCommand);

		AsyncAutoResetEvent cancelCanExecuteChanged = new();
		cancelCommand.CanExecuteChanged += (sender, e) =>
		{
			Assert.Same(cancelCommand, sender);
			cancelCanExecuteChanged.Set();
		};

		Assert.False(cancelCommand.CanExecute());
		Task commandExecTask = primaryCommand.ExecuteAsync();

		await cancelCanExecuteChanged.WaitAsync(this.TimeoutToken);
		Assert.True(cancelCommand.CanExecute());

		primaryCommand.Cancel();
		await cancelCanExecuteChanged.WaitAsync(this.TimeoutToken);
		Assert.False(cancelCommand.CanExecute());
	}

	[Fact]
	public async Task ExecuteAsync()
	{
		var waiter = new AsyncManualResetEvent();
		var primaryCommand = new MyCommand { ExecutionResult = waiter.WaitAsync() };
		var cancelCommand = new CancelCommand(primaryCommand);

		AsyncAutoResetEvent cancelCanExecuteChanged = new();
		cancelCommand.CanExecuteChanged += (sender, e) =>
		{
			Assert.Same(cancelCommand, sender);
			cancelCanExecuteChanged.Set();
		};

		Assert.False(cancelCommand.CanExecute());
		Task commandExecTask = primaryCommand.ExecuteAsync();

		await cancelCanExecuteChanged.WaitAsync(this.TimeoutToken);
		Assert.True(cancelCommand.CanExecute());

		await cancelCommand.ExecuteAsync();
		await cancelCanExecuteChanged.WaitAsync(this.TimeoutToken);
		Assert.False(cancelCommand.CanExecute());
	}

	private class MyCommand : CommandBase
	{
		internal Task ExecutionResult { get; set; } = Task.CompletedTask;

		protected override async Task ExecuteCoreAsync(object? parameter, CancellationToken cancellationToken)
		{
			await this.ExecutionResult;
			cancellationToken.ThrowIfCancellationRequested();
		}
	}
}
