﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace PCLCommandBase
{
	using System.Threading;
	using System.Threading.Tasks;
	using Validation;

	/// <summary>
	/// A command that can be used to cancel another.
	/// </summary>
	public class CancelCommand : CommandBase
	{
		/// <summary>
		/// The command to cancel.
		/// </summary>
		private readonly CommandBase commandToCancel;

		/// <summary>
		/// Initializes a new instance of the <see cref="CancelCommand"/> class.
		/// </summary>
		/// <param name="commandToCancel">The command to cancel.</param>
		public CancelCommand(CommandBase commandToCancel)
		{
			Requires.NotNull(commandToCancel, "commandToCancel");
			this.commandToCancel = commandToCancel;
			commandToCancel.PropertyChanged += (s, e) =>
			{
				if (e.PropertyName == nameof(CommandBase.IsCancellationRequested) || e.PropertyName == nameof(CommandBase.IsExecuting))
				{
					this.OnCanExecuteChanged();
				}
			};
		}

		/// <inheritdoc/>
		public override bool CanExecute(object? parameter = null) => this.commandToCancel.IsExecuting && !this.commandToCancel.IsCancellationRequested;

		/// <inheritdoc/>
		protected override Task ExecuteCoreAsync(object? parameter = null, CancellationToken cancellationToken = default)
		{
			this.commandToCancel.Cancel();
			return Task.CompletedTask;
		}
	}
}
