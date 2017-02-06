namespace PCLCommandBase {
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using System.Text;
	using System.Threading;
	using System.Threading.Tasks;
	using Validation;

	/// <summary>
	/// A command that can be used to cancel another.
	/// </summary>
	public class CancelCommand : CommandBase {
		/// <summary>
		/// The command to cancel.
		/// </summary>
		private readonly CommandBase commandToCancel;

		/// <summary>
		/// Initializes a new instance of the <see cref="CancelCommand"/> class.
		/// </summary>
		/// <param name="commandToCancel">The command to cancel.</param>
		public CancelCommand(CommandBase commandToCancel) {
			Requires.NotNull(commandToCancel, "commandToCancel");
			this.commandToCancel = commandToCancel;
			commandToCancel.CanExecuteChanged += (s, e) => this.OnCanExecuteChanged();
		}

		/// <summary>
		/// Determines whether this command can execute given the specified parameter.
		/// </summary>
		/// <param name="parameter">The parameter value.</param>
		/// <returns>
		///   <c>true</c> if the command can execute; <c>false</c> otherwise.
		/// </returns>
		public override bool CanExecute(object parameter) {
			return this.commandToCancel.IsExecuting && !this.commandToCancel.IsCancellationRequested;
		}

		/// <summary>
		/// Cancels the command.
		/// </summary>
		/// <param name="parameter">The parameter value is ignored.</param>
		/// <param name="cancellationToken">The cancellation token is ignored.</param>
		/// <returns>A successfully completed task.</returns>
		protected override Task ExecuteCoreAsync(object parameter, CancellationToken cancellationToken) {
			this.commandToCancel.Cancel();
			return NonAsync;
		}
	}
}
