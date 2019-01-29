﻿// Copyright © 2012-2017 Alex Kukhtin. All rights reserved.

using System;

namespace A2v10.Workflow
{
	[Serializable]
	public sealed class WorkflowException : Exception
	{
		public WorkflowException(String message)
			: base(message)
		{
		}
	}
}
