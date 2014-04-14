namespace PCLCommandBase {
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using System.Linq.Expressions;
	using System.Runtime.CompilerServices;
	using System.Text;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Windows.Input;
	using Validation;

	/// <summary>
	/// A base class for <see cref="ICommand"/> implementations
	/// that automatically disables the command while it is currently running
	/// and supports async commands.
	/// </summary>
	public abstract class CommandBase : BindableBase, ICommand {
		/// <summary>
		/// A reusable completed task that may be returned by derived types from
		/// their <see cref="ExecuteAsync(object, CancellationToken)"/> overrides
		/// if the method does not use the <c>async</c> keyword.
		/// </summary>
		protected static readonly Task NonAsync = Task.FromResult<object>(null);

		/// <summary>
		/// The execution cancellation source for the currently executing command.
		/// </summary>
		/// <remarks>
		/// This is <c>null</c> when the command isn't executing.
		/// </remarks>
		private CancellationTokenSource executionCancellationSource;

		/// <summary>
		/// Occurs when the value returned by <see cref="CanExecute(object)"/> changes.
		/// </summary>
		public event EventHandler CanExecuteChanged;

		/// <summary>
		/// Initializes a new instance of the <see cref="CommandBase"/> class.
		/// </summary>
		protected CommandBase() {
		}

		/// <summary>
		/// Gets a value indicating whether this command is executing.
		/// </summary>
		/// <value>
		/// <c>true</c> if this command is executing; otherwise, <c>false</c>.
		/// </value>
		public bool IsExecuting {
			get { return this.executionCancellationSource != null; }
		}

		/// <summary>
		/// Gets a value indicating whether this command is executing and has been cancelled but has not yet terminated.
		/// </summary>
		public bool IsCancellationRequested {
			get {
				var cancellationSource = this.executionCancellationSource;
				return cancellationSource != null && cancellationSource.IsCancellationRequested;
			}
		}

		/// <summary>
		/// Determines whether this command can execute given the specified parameter.
		/// </summary>
		/// <param name="parameter">The parameter value.</param>
		/// <returns><c>true</c> if the command can execute; <c>false</c> otherwise.</returns>
		public virtual bool CanExecute(object parameter) {
			return this.executionCancellationSource == null;
		}

		/// <summary>
		/// Executes the command with the specified parameter.
		/// </summary>
		/// <param name="parameter">The parameter.</param>
		async void ICommand.Execute(object parameter) {
			this.executionCancellationSource = new CancellationTokenSource();
			this.OnCanExecuteChanged();

			try {
				await this.ExecuteAsync(parameter, this.executionCancellationSource.Token);
			} catch (OperationCanceledException) {
			} finally {
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
		public void Cancel() {
			var cancellationSource = this.executionCancellationSource;
			if (cancellationSource != null) {
				cancellationSource.Cancel();
				this.OnPropertyChanged(() => IsCancellationRequested);
			}
		}

		/// <summary>
		/// This executes the actual command body.
		/// </summary>
		/// <param name="parameter">The parameter, if any was provided.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>
		/// A task that represents the completion of the async command.
		/// May be <see cref="NonAsync" /> if the overriding method does not use the 
		/// <c>async</c> keyword.
		/// </returns>
		public abstract Task ExecuteAsync(object parameter, CancellationToken cancellationToken);

		/// <summary>
		/// Called when <see cref="CanExecute(object)"/> is expected to return a different result.
		/// </summary>
		protected virtual void OnCanExecuteChanged() {
			this.OnPropertyChanged(() => IsExecuting);
			this.OnPropertyChanged(() => IsCancellationRequested);
			var canExecuteChanged = this.CanExecuteChanged;
			if (canExecuteChanged != null) {
				canExecuteChanged(this, EventArgs.Empty);
			}
		}
	}
}
