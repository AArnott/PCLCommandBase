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
		/// The backing field for the <see cref="LastCommandFault"/> property.
		/// </summary>
		private Exception lastCommandFault;

		/// <summary>
		/// Occurs when the value returned by <see cref="CanExecute(object)"/> changes.
		/// </summary>
		public event EventHandler CanExecuteChanged;

		/// <summary>
		/// Initializes a new instance of the <see cref="CommandBase"/> class.
		/// </summary>
		protected CommandBase() {
			this.RegisterDependentProperty(() => LastCommandFault, () => IsFaulted);
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
		/// Gets a value indicating whether the last invocation of this command faulted.
		/// </summary>
		/// <remarks>
		/// The fault may be observed by reading the <see cref="LastCommandFault"/> property.
		/// It may be reset by invoking the <see cref="ClearFault"/> method.
		/// </remarks>
		public bool IsFaulted {
			get { return this.LastCommandFault != null; }
		}

		/// <summary>
		/// Gets the exception thrown from the last execution of this command, if any.
		/// </summary>
		public Exception LastCommandFault {
			get { return this.lastCommandFault; }
			set { this.SetProperty(ref this.lastCommandFault, value); }
		}

		/// <summary>
		/// Resets <see cref="LastCommandFault"/> to <c>null</c>.
		/// </summary>
		public void ClearFault() {
			this.LastCommandFault = null;
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
		/// Executes the command.
		/// </summary>
		/// <param name="parameter">The command parameter.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The result of command execution.</returns>
		public async Task ExecuteAsync(object parameter = null, CancellationToken cancellationToken = default(CancellationToken)) {
			Verify.Operation(this.CanExecute(parameter), "The command cannot execute right now. It may already be executing.");
			this.executionCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			this.OnCanExecuteChanged();
			this.ClearFault();

			try {
				await this.ExecuteCoreAsync(parameter, this.executionCancellationSource.Token);
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
		/// Executes the command with the specified parameter.
		/// </summary>
		/// <param name="parameter">The parameter.</param>
		async void ICommand.Execute(object parameter) {
			try {
				await this.ExecuteAsync(parameter, CancellationToken.None);
			} catch (Exception ex) {
				this.LastCommandFault = ex;
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
		protected abstract Task ExecuteCoreAsync(object parameter, CancellationToken cancellationToken);

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
