// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace PCLCommandBase
{
	using System;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Windows.Input;
	using Validation;

	/// <summary>
	/// A base class for <see cref="ICommand"/> implementations
	/// that automatically disables the command while it is currently running
	/// and supports async commands.
	/// </summary>
	public abstract class CommandBase : BindableBase, ICommand
	{
		/// <summary>
		/// The execution cancellation source for the currently executing command.
		/// </summary>
		/// <remarks>
		/// This is <c>null</c> when the command isn't executing.
		/// </remarks>
		private CancellationTokenSource? executionCancellationSource;

		/// <summary>
		/// The backing field for the <see cref="LastCommandFault"/> property.
		/// </summary>
		private Exception? lastCommandFault;

		/// <summary>
		/// Initializes a new instance of the <see cref="CommandBase"/> class.
		/// </summary>
		protected CommandBase()
		{
			this.RegisterDependentProperty(nameof(this.LastCommandFault), nameof(this.IsFaulted));
		}

		/// <summary>
		/// Occurs when the value returned by <see cref="CanExecute(object)"/> changes.
		/// </summary>
		public event EventHandler? CanExecuteChanged;

		/// <summary>
		/// Gets a value indicating whether this command is executing.
		/// </summary>
		/// <value>
		/// <see langword="true"/> if this command is executing; otherwise, <see langword="false"/>.
		/// </value>
		public bool IsExecuting => this.executionCancellationSource is object;

		/// <summary>
		/// Gets a value indicating whether this command is executing and has been cancelled but has not yet terminated.
		/// </summary>
		public bool IsCancellationRequested => this.executionCancellationSource?.IsCancellationRequested is true;

		/// <summary>
		/// Gets a value indicating whether the last invocation of this command faulted.
		/// </summary>
		/// <remarks>
		/// The fault may be observed by reading the <see cref="LastCommandFault"/> property.
		/// It may be reset by invoking the <see cref="ClearFault"/> method.
		/// </remarks>
		public bool IsFaulted => this.LastCommandFault is object;

		/// <summary>
		/// Gets or sets the exception thrown from the last invocation of <see cref="ICommand.Execute(object)"/>, if any.
		/// </summary>
		/// <remarks>
		/// This property does <em>not</em> capture faults from invocations of <see cref="ExecuteAsync(object?, CancellationToken)"/>
		/// since that result is visible to the caller.
		/// </remarks>
		public Exception? LastCommandFault
		{
			get => this.lastCommandFault;
			set => this.SetProperty(ref this.lastCommandFault, value);
		}

		/// <summary>
		/// Resets <see cref="LastCommandFault"/> to <c>null</c>.
		/// </summary>
		public void ClearFault() => this.LastCommandFault = null;

		/// <summary>
		/// Determines whether this command can execute given the specified parameter.
		/// </summary>
		/// <param name="parameter">The parameter value.</param>
		/// <returns><see langword="true"/> if the command can execute; <see langword="false"/> otherwise.</returns>
		/// <remarks>
		/// Overrides of this method should call the base implementation, which disables the command when it is currently running.
		/// </remarks>
		public virtual bool CanExecute(object? parameter = null) => this.executionCancellationSource is null;

		/// <summary>
		/// Executes the command.
		/// </summary>
		/// <param name="parameter">The command parameter.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The result of command execution.</returns>
		public async Task ExecuteAsync(object? parameter = null, CancellationToken cancellationToken = default)
		{
			Verify.Operation(this.CanExecute(parameter), "The command cannot execute right now. It may already be executing.");
			this.executionCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			this.OnCanExecuteChanged();
			this.ClearFault();

			try
			{
				await this.ExecuteCoreAsync(parameter, this.executionCancellationSource.Token);
			}
			finally
			{
				this.executionCancellationSource = null;
				this.OnCanExecuteChanged();
			}
		}

		/// <summary>
		/// Cancels this command if it is currently executing.
		/// </summary>
		/// <remarks>
		/// This method returns immediately after requesting cancellation.
		/// It does not wait for the command to actually stop executing.
		/// </remarks>
		public void Cancel()
		{
			CancellationTokenSource? cancellationSource = this.executionCancellationSource;
			if (cancellationSource is object)
			{
				cancellationSource.Cancel();
				this.OnPropertyChanged(nameof(this.IsCancellationRequested));
			}
		}

		/// <summary>
		/// Executes the command with the specified parameter.
		/// </summary>
		/// <param name="parameter">The parameter.</param>
		async void ICommand.Execute(object? parameter)
		{
			try
			{
				await this.ExecuteAsync(parameter, CancellationToken.None);
			}
			catch (Exception ex)
			{
				this.LastCommandFault = ex;
			}
		}

		/// <summary>
		/// This executes the actual command body.
		/// </summary>
		/// <param name="parameter">The parameter, if any was provided.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>A task that represents the completion of the async command.</returns>
		protected abstract Task ExecuteCoreAsync(object? parameter = null, CancellationToken cancellationToken = default);

		/// <summary>
		/// Called when <see cref="CanExecute(object)"/> is expected to return a different result.
		/// </summary>
		protected virtual void OnCanExecuteChanged()
		{
			this.OnPropertyChanged(nameof(this.IsExecuting));
			this.OnPropertyChanged(nameof(this.IsCancellationRequested));
			this.CanExecuteChanged?.Invoke(this, EventArgs.Empty);
		}
	}
}
