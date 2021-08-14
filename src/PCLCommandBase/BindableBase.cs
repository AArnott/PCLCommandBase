// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace PCLCommandBase
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq.Expressions;
	using System.Reflection;
	using System.Runtime.CompilerServices;
	using System.Runtime.Serialization;
	using Validation;

	/// <summary>
	/// Implementation of <see cref="INotifyPropertyChanged"/> to simplify models.
	/// </summary>
	[DataContract]
	public abstract class BindableBase : INotifyPropertyChanged
	{
		/// <summary>
		/// Links between properties that are related.
		/// </summary>
		private Dictionary<string, HashSet<string>>? dependentPropertiesMap;

		/// <summary>
		/// Multicast event for property change notifications.
		/// </summary>
		public event PropertyChangedEventHandler? PropertyChanged;

		/// <summary>
		/// Gets a value indicating whether this instance is being disposed.
		/// </summary>
		/// <remarks>
		/// Unless overridden by a derived class, this always returns <see langword="false"/>.
		/// </remarks>
		protected virtual bool IsDisposing
		{
			get { return false; }
		}

		/// <summary>
		/// Gets the name of the property referenced in an expression.
		/// </summary>
		/// <typeparam name="T">The type of value returned by the property.</typeparam>
		/// <param name="propertyExpression">The property expression.</param>
		/// <returns>The name of the property.</returns>
		/// <exception cref="System.ArgumentException">Thrown if the expression is in an unrecognized format.</exception>
		protected static string GetPropertyName<T>(Expression<T> propertyExpression)
		{
			var memberExpression = propertyExpression.Body as MemberExpression;
			if (memberExpression is null)
			{
				var unaryExpression = propertyExpression.Body as UnaryExpression;
				memberExpression = unaryExpression?.Operand as MemberExpression;
			}

			if (memberExpression is null)
			{
				throw new ArgumentException();
			}

			return memberExpression.Member.Name;
		}

		/// <summary>
		/// Checks if a property already matches a desired value.  Sets the property and
		/// notifies listeners only when necessary.
		/// </summary>
		/// <typeparam name="T">Type of the property.</typeparam>
		/// <param name="storage">Reference to a property with both getter and setter.</param>
		/// <param name="value">Desired value for the property.</param>
		/// <param name="propertyName">Name of the property used to notify listeners.  This
		/// value is optional and can be provided automatically when invoked from compilers that
		/// support CallerMemberName.</param>
		/// <returns>True if the value was changed, false if the existing value matched the
		/// desired value.</returns>
		protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
		{
			if (typeof(T).GetTypeInfo().IsClass)
			{
				if (object.ReferenceEquals(storage, value))
				{
					return false;
				}
			}
			else
			{ // struct
				if (EqualityComparer<T>.Default.Equals(storage, value))
				{
					return false;
				}
			}

			storage = value;
			this.OnPropertyChanged(propertyName);
			return true;
		}

		/// <summary>
		/// Notifies listeners that a property value has changed.
		/// </summary>
		/// <param name="propertyName">Name of the property used to notify listeners.  This
		/// value is optional and can be provided automatically when invoked from compilers
		/// that support <see cref="CallerMemberNameAttribute"/>.</param>
		protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		{
			Requires.NotNull(propertyName!, nameof(propertyName));
			if (this.IsDisposing)
			{
				return;
			}

			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

			// The field could actually be null because the DataContractSerializer
			// skips the constructor when deserializing.
			HashSet<string> dependentProperties;
			if (this.dependentPropertiesMap is object && this.dependentPropertiesMap.TryGetValue(propertyName, out dependentProperties))
			{
				foreach (var dependentProperty in dependentProperties)
				{
					this.OnPropertyChanged(dependentProperty);
				}
			}
		}

		/// <summary>
		/// Notifies listeners that a property value has changed.
		/// </summary>
		/// <typeparam name="T">The type of value stored by the changed property.</typeparam>
		/// <param name="expression">An expression that is the member that has been changed.</param>
		protected void OnPropertyChanged<T>(Expression<Func<T>> expression)
		{
			this.OnPropertyChanged(GetPropertyName(expression));
		}

		/// <summary>
		/// Registers one property as a dependent property such that changed events for one
		/// causes the changed events of the other.
		/// </summary>
		/// <param name="baseProperty">The property with backing field that may change..</param>
		/// <param name="dependentProperty">The property that that derives its value from <paramref name="baseProperty"/>.</param>
		protected void RegisterDependentProperty(string baseProperty, string dependentProperty)
		{
			Requires.NotNullOrEmpty(baseProperty, "baseProperty");
			Requires.NotNullOrEmpty(dependentProperty, "dependentProperty");

			if (this.dependentPropertiesMap is null)
			{
				this.dependentPropertiesMap = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
			}

			HashSet<string> dependentProperties;
			if (!this.dependentPropertiesMap.TryGetValue(baseProperty, out dependentProperties))
			{
				this.dependentPropertiesMap[baseProperty] = dependentProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			}

			dependentProperties.Add(dependentProperty);
		}

		/// <summary>
		/// Registers one property as a dependent property such that changed events for one
		/// causes the changed events of the other.
		/// </summary>
		/// <typeparam name="T1">The type of value stored by the base property.</typeparam>
		/// <typeparam name="T2">The type of value stored by the dependent property.</typeparam>
		/// <param name="baseProperty">An expression of simply the property with backing field that may change..</param>
		/// <param name="dependentProperty">An expression of simply the property that derives its value from <paramref name="baseProperty"/>.</param>
		protected void RegisterDependentProperty<T1, T2>(Expression<Func<T1>> baseProperty, Expression<Func<T2>> dependentProperty)
		{
			Requires.NotNull(baseProperty, "baseProperty");
			Requires.NotNull(dependentProperty, "dependentProperty");

			this.RegisterDependentProperty(GetPropertyName(baseProperty), GetPropertyName(dependentProperty));
		}
	}
}
