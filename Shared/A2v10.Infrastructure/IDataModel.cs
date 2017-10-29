﻿using System;
using System.Collections.Generic;
using System.Dynamic;

namespace A2v10.Infrastructure
{
    [Flags]
    public enum StdPermissions
    {
        None = 0,
        CanView   = 0x01,
        CanEdit   = 0x02,
        CanDelete = 0x04,
        CanApply  = 0x08
    }

	public interface IDataModel
	{
		Object Metadata { get; }
		ExpandoObject Root { get; }
        ExpandoObject System { get; }

		T Eval<T>(String expression, T fallback = default(T));
		void Traverse(Func<Object, Boolean> callback);
        IDictionary<String, dynamic> GetDynamic();

        Boolean IsReadOnly { get; }

        String CreateScript();
	}
}
