namespace PCLCommandBase {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading;
	using System.Threading.Tasks;
	using Validation;

	/// <summary>
	/// A specialization of <see cref="CommandBase"/> that automatically
	/// requires and casts to a specific parameter type.
	/// </summary>
	/// <typeparam name="T">The type of command parameter expected.</typeparam>
	public abstract class ParameterizedCommandBase<T> : CommandBase {
		/// <summary>
		/// Initializes a new instance of the <see cref="ParameterizedCommandBase{T}"/> class.
		/// </summary>
		protected ParameterizedCommandBase() {
		}

		/// <summary>
		/// Determines whether this command can execute given the specified parameter.
		/// </summary>
		/// <param name="parameter">The parameter value.</param>
		/// <returns><c>true</c> if the command can execute; <c>false</c> otherwise.</returns>
		public sealed override bool CanExecute(object parameter) {
			return parameter is T && base.CanExecute(parameter) && this.CanExecute((T)parameter);
		}

		/// <summary>
		/// Determines whether this command can execute given the specified parameter.
		/// </summary>
		/// <param name="parameter">The parameter value.</param>
		/// <returns><c>true</c> if the command can execute; <c>false</c> otherwise.</returns>
		protected virtual bool CanExecute(T parameter) {
			// No need to call the base method here. This method's caller already has.
			return true;
		}

		/// <summary>
		/// This executes the actual command body.
		/// </summary>
		/// <param name="parameter">The parameter, if any was provided.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>
		/// A task that represents the completion of the async command.
		/// May be <see cref="CommandBase.NonAsync" /> if the overriding method does not use the 
		/// <c>async</c> keyword.
		/// </returns>
		protected sealed override Task ExecuteCoreAsync(object parameter, CancellationToken cancellationToken) {
			Requires.NotNull(parameter, "parameter");
			return this.ExecuteAsync((T)parameter, cancellationToken);
		}

		/// <summary>
		/// This executes the actual command body.
		/// </summary>
		/// <param name="parameter">The parameter, if any was provided.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>
		/// A task that represents the completion of the async command.
		/// May be <see cref="CommandBase.NonAsync" /> if the overriding method does not use the 
		/// <c>async</c> keyword.
		/// </returns>
		protected abstract Task ExecuteCoreAsync(T parameter, CancellationToken cancellationToken);
	}
}
