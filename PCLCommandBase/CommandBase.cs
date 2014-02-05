namespace Dart.Commands {
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

	public abstract class CommandBase : BindableBase, ICommand {
		protected static readonly Task NonAsync = Task.FromResult<object>(null);

		private CancellationTokenSource executionCancellationSource;

		public event EventHandler CanExecuteChanged;

		protected CommandBase() {
		}

		public bool IsExecuting {
			get { return this.executionCancellationSource != null; }
		}

		public bool IsCancellationRequested {
			get {
				var cancellationSource = this.executionCancellationSource;
				return cancellationSource != null && cancellationSource.IsCancellationRequested;
			}
		}

		public virtual bool CanExecute(object parameter) {
			return this.executionCancellationSource == null;
		}

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

		public void Cancel() {
			var cancellationSource = this.executionCancellationSource;
			if (cancellationSource != null) {
				cancellationSource.Cancel();
				this.OnPropertyChanged(() => IsCancellationRequested);
			}
		}

		public abstract Task ExecuteAsync(object parameter, CancellationToken cancellationToken);

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
