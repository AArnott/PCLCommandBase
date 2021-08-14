// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace PCLCommandBase
{
	using System.Threading;
	using System.Threading.Tasks;

	/// <summary>
	/// A specialization of <see cref="CommandBase"/> that automatically
	/// requires and casts to a specific parameter type.
	/// </summary>
	/// <typeparam name="T">The type of command parameter expected.</typeparam>
	public abstract class ParameterizedCommandBase<T> : CommandBase
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ParameterizedCommandBase{T}"/> class.
		/// </summary>
		protected ParameterizedCommandBase()
		{
		}

		/// <inheritdoc/>
		public sealed override bool CanExecute(object? parameter)
		{
			return parameter is T && base.CanExecute(parameter) && this.CanExecute((T)parameter);
		}

		/// <inheritdoc cref="CanExecute(object?)"/>
		protected virtual bool CanExecute(T parameter)
		{
			// No need to call the base method here. This method's caller already has.
			return true;
		}

		/// <inheritdoc/>
		protected sealed override Task ExecuteCoreAsync(object? parameter, CancellationToken cancellationToken)
		{
			return this.ExecuteAsync((T?)parameter, cancellationToken);
		}

		/// <inheritdoc cref="ExecuteCoreAsync(object?, CancellationToken)"/>
		protected abstract Task ExecuteCoreAsync(T? parameter, CancellationToken cancellationToken);
	}
}
