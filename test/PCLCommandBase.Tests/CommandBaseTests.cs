// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.VisualStudio.Threading;
using PCLCommandBase;
using Xunit;
using Xunit.Abstractions;

public class CommandBaseTests : TestBase
{
	private readonly DerivedCommandBase command = new();

	public CommandBaseTests(ITestOutputHelper logger)
		: base(logger)
	{
	}

	[Fact]
	public async Task ExecuteAsync()
	{
		await this.command.ExecuteAsync();
		Assert.Equal(1, this.command.ExecutionCount);
	}

	[Fact]
	public async Task CanExecute()
	{
		int canExecuteChangedCounter = 0;
		this.command.CanExecuteChanged += (s, e) =>
		{
			canExecuteChangedCounter++;
			Assert.Same(this.command, s);
		};

		Assert.True(this.command.CanExecute());
		Assert.Equal(0, canExecuteChangedCounter);

		var commandDelay = new AsyncManualResetEvent();
		this.command.CompleteCommandTask = commandDelay.WaitAsync();
		Task commandExecTask = this.command.ExecuteAsync();
		Assert.Equal(1, canExecuteChangedCounter);
		Assert.False(this.command.CanExecute());

		commandDelay.Set();
		await commandExecTask;
		Assert.Equal(2, canExecuteChangedCounter);
		Assert.True(this.command.CanExecute());
	}

	[Fact]
	public void IsCancellationRequested()
	{
		Assert.False(this.command.IsCancellationRequested);
		this.command.Cancel();
		Assert.False(this.command.IsCancellationRequested); // nothing to cancel
	}

	[Fact]
	public async Task ExecuteCoreAsync_CancellationToken()
	{
		CancellationTokenSource cts = new();
		var commandDelay = new AsyncManualResetEvent();
		this.command.CompleteCommandTask = commandDelay.WaitAsync();

		Task commandExecTask = this.command.ExecuteAsync(cancellationToken: cts.Token);
		cts.Cancel();
		Assert.True(this.command.IsCancellationRequested);
		commandDelay.Set();

		await Assert.ThrowsAsync<OperationCanceledException>(() => commandExecTask);

		Assert.Null(this.command.LastCommandFault);
		Assert.False(this.command.IsCancellationRequested);
	}

	[Fact]
	public async Task ExecuteCoreAsync_Cancel()
	{
		var commandDelay = new AsyncManualResetEvent();
		this.command.CompleteCommandTask = commandDelay.WaitAsync();

		Task commandExecTask = this.command.ExecuteAsync();
		this.command.Cancel();
		Assert.True(this.command.IsCancellationRequested);
		commandDelay.Set();

		await Assert.ThrowsAsync<OperationCanceledException>(() => commandExecTask);

		Assert.Null(this.command.LastCommandFault);
		Assert.False(this.command.IsCancellationRequested);
	}

	[Fact]
	public async Task Execute_Cancel()
	{
		var commandDelay = new AsyncManualResetEvent();
		this.command.CompleteCommandTask = commandDelay.WaitAsync();

		var eventsQueue = new AsyncQueue<PropertyChangedEventArgs>();
		this.command.PropertyChanged += (s, e) =>
		{
			Assert.Same(this.command, s);
			eventsQueue.Enqueue(e);
		};

		((ICommand)this.command).Execute(null);
		this.command.Cancel();
		Assert.True(this.command.IsCancellationRequested);
		commandDelay.Set();

		// Wait for command fault to be reported.
		while ((await eventsQueue.DequeueAsync(this.TimeoutToken)).PropertyName != nameof(this.command.LastCommandFault))
		{
		}

		Assert.IsType<OperationCanceledException>(this.command.LastCommandFault);
		Assert.False(this.command.IsCancellationRequested);
	}

	[Fact]
	public async Task IsExecuting()
	{
		Assert.False(this.command.IsExecuting);
		var eventsQueue = new AsyncQueue<PropertyChangedEventArgs>();
		this.command.PropertyChanged += (s, e) =>
		{
			Assert.Same(this.command, s);
			eventsQueue.Enqueue(e);
		};

		var commandDelay = new AsyncManualResetEvent();
		this.command.CompleteCommandTask = commandDelay.WaitAsync();

		Task commandTask = this.command.ExecuteAsync();

		while ((await eventsQueue.DequeueAsync(this.TimeoutToken)).PropertyName != nameof(this.command.IsExecuting))
		{
		}

		Assert.True(this.command.IsExecuting);

		commandDelay.Set();
		await commandTask;

#pragma warning disable CA1508 // Avoid dead conditional code -- buggy compiler
		while ((await eventsQueue.DequeueAsync(this.TimeoutToken)).PropertyName != nameof(this.command.IsExecuting))
#pragma warning restore CA1508 // Avoid dead conditional code
		{
		}

		Assert.False(this.command.IsExecuting);
	}

	private class DerivedCommandBase : CommandBase
	{
		internal int ExecutionCount { get; set; }

		internal Task CompleteCommandTask { get; set; } = Task.CompletedTask;

		protected override async Task ExecuteCoreAsync(object? parameter, CancellationToken cancellationToken)
		{
			this.ExecutionCount++;
			await this.CompleteCommandTask;
			cancellationToken.ThrowIfCancellationRequested();
		}
	}
}
