// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq.Expressions;
using PCLCommandBase;
using Xunit;

public class BindableBaseTests
{
	private string? SomeProperty { get; set; }

	[Fact]
	public void GetPropertyName()
	{
		Assert.Equal(nameof(this.SomeProperty), BindableBaseDerived.GetPropertyName(() => this.SomeProperty));
	}

	private class BindableBaseDerived : BindableBase
	{
		internal static string GetPropertyName<T>(Expression<Func<T>> propertyExpression) => BindableBase.GetPropertyName(propertyExpression);
	}
}
