namespace PCLCommandBase {
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using System.Text;
	using System.Threading;
	using System.Threading.Tasks;
	using Validation;

	public class CancelCommand : CommandBase {
		private readonly CommandBase commandToCancel;

		public CancelCommand(CommandBase commandToCancel) {
			Requires.NotNull(commandToCancel, "commandToCancel");
			this.commandToCancel = commandToCancel;
			commandToCancel.CanExecuteChanged += (s, e) => this.OnCanExecuteChanged();
		}

		public override bool CanExecute(object parameter) {
			return this.commandToCancel.IsExecuting && !this.commandToCancel.IsCancellationRequested;
		}

		public override Task ExecuteAsync(object parameter, CancellationToken cancellationToken) {
			this.commandToCancel.Cancel();
			return Task.FromResult<object>(null);
		}
	}
}
