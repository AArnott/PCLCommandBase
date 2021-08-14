// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PCLCommandBase;
using Xunit;
using Xunit.Abstractions;

public class ParameterizedCommandBaseTests : TestBase
{
	public ParameterizedCommandBaseTests(ITestOutputHelper logger)
		: base(logger)
	{
	}

	[Fact]
	public void CanExecute()
	{
		var command = new MockParameterizedCommand();
		Assert.True(command.CanExecute("hi"));
		command.DerivedCanExecute = false;
		Assert.False(command.CanExecute("hi"));
		Assert.False(command.CanExecute(null));
		command.DerivedCanExecute = null;
		Assert.True(command.CanExecute("hi"));
	}

	[Fact]
	public async Task ExecuteAsync()
	{
		var command = new MockParameterizedCommand();
		await command.ExecuteAsync("test");
	}

	private class MockParameterizedCommand : ParameterizedCommandBase<string>
	{
		internal bool? DerivedCanExecute { get; set; } = true;

		internal int ExecutionCount { get; set; }

		internal Task CompleteCommandTask { get; set; } = Task.CompletedTask;

		protected override bool CanExecute(string? parameter) => this.DerivedCanExecute ?? base.CanExecute(parameter);

		protected override async Task ExecuteCoreAsync(string? parameter, CancellationToken cancellationToken)
		{
			Assert.Equal("test", parameter);
			this.ExecutionCount++;
			await this.CompleteCommandTask;
			cancellationToken.ThrowIfCancellationRequested();
		}
	}
}
