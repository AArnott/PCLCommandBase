namespace PCLCommandBase {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading;
	using System.Threading.Tasks;
	using Validation;

	public abstract class ParameterizedCommandBase<T> : CommandBase {
		protected ParameterizedCommandBase() {
		}

		public sealed override bool CanExecute(object parameter) {
			return parameter is T && base.CanExecute(parameter) && this.CanExecute((T)parameter);
		}

		public virtual bool CanExecute(T parameter) {
			return true;
		}

		public sealed override Task ExecuteAsync(object parameter, CancellationToken cancellationToken) {
			Requires.NotNull(parameter, "parameter");
			return this.ExecuteAsync((T)parameter, cancellationToken);
		}

		public abstract Task ExecuteAsync(T parameter, CancellationToken cancellationToken);
	}
}
