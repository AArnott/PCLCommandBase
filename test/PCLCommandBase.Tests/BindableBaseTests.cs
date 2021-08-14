// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using PCLCommandBase;
using Xunit;

public class BindableBaseTests
{
	[Fact]
	public void PropertyChangedEventRaised()
	{
		var instance = new BindableBaseDerived();
		var events = new List<PropertyChangedEventArgs>();
		instance.PropertyChanged += (s, e) =>
		{
			Assert.Same(instance, s);
			events.Add(e);
		};

		instance.SomeStringProperty = "Some other value";
		Assert.Equal(nameof(instance.SomeStringProperty), Assert.Single(events).PropertyName);
		Assert.Equal("Some other value", instance.SomeStringProperty);
		events.Clear();

		instance.SomeStringProperty = "Some other value";
		Assert.Empty(events);

		instance.SomeIntProperty = 3;
		Assert.Equal(nameof(instance.SomeIntProperty), Assert.Single(events).PropertyName);
		Assert.Equal(3, instance.SomeIntProperty);
		events.Clear();

		instance.SomeIntProperty = 3;
		Assert.Empty(events);
		events.Clear();

		instance.InputProperty = 3;
		Assert.Equal(2, events.Count);
		Assert.Equal(nameof(instance.InputProperty), events[0].PropertyName);
		Assert.Equal(nameof(instance.IntPlus5), events[1].PropertyName);
	}

	private class BindableBaseDerived : BindableBase
	{
		private string? someStringProperty;
		private int someIntProperty;
		private int inputProperty;

		internal BindableBaseDerived()
		{
			this.RegisterDependentProperty(nameof(this.InputProperty), nameof(this.IntPlus5));
		}

		internal string? SomeStringProperty
		{
			get => this.someStringProperty;
			set => this.SetProperty(ref this.someStringProperty, value);
		}

		internal int SomeIntProperty
		{
			get => this.someIntProperty;
			set => this.SetProperty(ref this.someIntProperty, value);
		}

		internal int InputProperty
		{
			get => this.inputProperty;
			set => this.SetProperty(ref this.inputProperty, value);
		}

		internal int IntPlus5 => this.SomeIntProperty + 5;
	}
}
